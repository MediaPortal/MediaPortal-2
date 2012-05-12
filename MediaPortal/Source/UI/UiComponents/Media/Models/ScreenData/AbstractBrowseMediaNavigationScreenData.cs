#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.ClientCommunication;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractBrowseMediaNavigationScreenData : AbstractItemsScreenData
  {
    protected AbstractBrowseMediaNavigationScreenData(string screen, string menuItemLabel, string navbarSubViewNavigationDisplayLabel,
        PlayableItemCreatorDelegate playableItemCreator, bool presentsBaseView) :
        base(screen, menuItemLabel, navbarSubViewNavigationDisplayLabel, playableItemCreator, presentsBaseView) { }

    /// <summary>
    /// There are two modes for browsing media in its directory structure; ML browsing and local browsing.
    /// This method switches between those modes and tries to navigate to the most sensible sibling state, i.e.
    /// tries to navigate as close as possible to the current directory navigation in the other mode.
    /// </summary>
    /// <param name="workflowNavigationContext">Workflow navigation context to start navigation.</param>
    public static void NavigateToSiblingState(NavigationContext workflowNavigationContext)
    {
      string localSystemId = ServiceRegistration.Get<ISystemResolver>().LocalSystemId;
      NavigationData nd = MediaNavigationModel.GetNavigationData(workflowNavigationContext, false);
      if (nd == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to sibling browse media state - there is no active media screen");
        return;
      }
      ViewSpecification vs = nd.BaseViewSpecification;

      // Unwrap potentially available removable media VS to get access to the inner ML VS or Local Browsing VS
      AddedRemovableMediaViewSpecificationFacade armvs = vs as AddedRemovableMediaViewSpecificationFacade;
      if (armvs != null)
        vs = armvs.DelegateViewSpecification;

      // Check Browse view states

      MediaLibraryBrowseViewSpecification mlbvs = vs as MediaLibraryBrowseViewSpecification;
      if (mlbvs != null)
      { // We're in some MediaLibrary browsing state - match the local media browsing which corresponds to the state
        if (mlbvs.SystemId != localSystemId)
        { // If the currently browsed system is a different one, we can just navigate to the local browsing root view
          NavigateToLocalBrowse(null);
          return;
        }
        // In a browsing state for the local system, we should be able to navigate to the corresponding local media browsing view
        NavigateToLocalBrowse(mlbvs.BasePath);
        return;
      }
      SystemSharesViewSpecification ssvs = vs as SystemSharesViewSpecification;
      AllSystemsViewSpecification asvs = vs as AllSystemsViewSpecification;
      if (ssvs != null || asvs != null)
      { // If the current browsing state shows all systems or the shares of a single system, we can just navigate to the local browsing root view
        NavigateToLocalBrowse(null);
        return;
      }
      LocalSharesViewSpecification lsvs = vs as LocalSharesViewSpecification;
      if (lsvs != null)
      { // If the current browsing state shows the local shares, we can just switch to the shares view of the local system as ML browsing view
        NavigateToMLBrowse(null);
        return;
      }
      LocalDirectoryViewSpecification ldvs = vs as LocalDirectoryViewSpecification;
      if (ldvs != null)
      {
        NavigateToMLBrowse(ldvs.ViewPath);
        return;
      }
    }

    protected static Share FindLocalShareContainingPath(ResourcePath path)
    {
      if (path == null)
        return null;
      int bestMatchPathLength = int.MaxValue;
      Share bestMatchShare = null;
      ILocalSharesManagement lsm = ServiceRegistration.Get<ILocalSharesManagement>();
      foreach (Share share in lsm.Shares.Values)
      {
        ResourcePath sharePath = share.BaseResourcePath;
        if (!sharePath.IsSameOrParentOf(path))
          // The path is not located in the current share
          continue;
        if (bestMatchShare == null)
        {
          bestMatchShare = share;
          continue;
        }
        // We want to find a share which is as close as possible to the given path
        int sharePathLength = sharePath.Serialize().Length;
        if (bestMatchPathLength >= sharePathLength)
          continue;
        bestMatchShare = share;
        bestMatchPathLength = sharePathLength;
      }
      return bestMatchShare;
    }

    protected delegate ViewSpecification CreateVSDlgt(IFileSystemResourceAccessor viewRA);

    protected static void Build(IResourceAccessor startRA, ResourcePath targetPath, NavigationData nd, CreateVSDlgt createVsDlgt)
    {
      AbstractItemsScreenData sd = (AbstractItemsScreenData) nd.CurrentScreenData;
      IFileSystemResourceAccessor current = startRA as IFileSystemResourceAccessor;
      if (current == null)
      {
        // Wrong path resource, cannot navigate. Should not happen if the share is based on a filesystem resource,
        // but might happen if we have found a non-standard share.
        startRA.Dispose();
        return;
      }
      while (true)
      {
        ICollection<IFileSystemResourceAccessor> children = FileSystemResourceNavigator.GetChildDirectories(current);
        current.Dispose();
        current = null;
        foreach (IFileSystemResourceAccessor childDirectory in children)
        {
          if (childDirectory.CanonicalLocalResourcePath.IsSameOrParentOf(targetPath))
          {
            current = childDirectory;
            break;
          }
          childDirectory.Dispose();
        }
        if (current == null)
          break;
        ViewSpecification newVS = createVsDlgt(current);
        if (newVS == null)
          return;
        nd = sd.NavigateToView(newVS);
        sd = (AbstractItemsScreenData) nd.CurrentScreenData;
      }
    }

    protected static bool GetLocalSystemData(out string localSystemId, out string localSystemName)
    {
      localSystemName = null;
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      localSystemId = systemResolver.LocalSystemId;
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      if (scm == null)
        return false;
      IServerController sc = scm.ServerController;
      if (sc == null)
        return false;
      foreach (MPClientMetadata client in sc.GetAttachedClients())
      {
        if (client.SystemId == localSystemId)
        {
          localSystemName = client.LastClientName;
          return true;
        }
      }
      return false;
    }

    protected static void NavigateToLocalBrowse(ResourcePath path)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdate();
      try
      {
        workflowManager.NavigatePopStates(new Guid[] {Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT, Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT});

        workflowManager.NavigatePush(Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT);

        Share localShare = FindLocalShareContainingPath(path);
        if (localShare == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to local browse view - no local share countaining path '{0}' found", path);
          return;
        }
        NavigationData nd = MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
        if (nd == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to local browse view - no navigation data found");
          return;
        }
        AbstractItemsScreenData sd = (AbstractItemsScreenData) nd.CurrentScreenData;

        ViewSpecification vs = nd.BaseViewSpecification;
        ICollection<Guid> necessaryMIATypeIds = vs.NecessaryMIATypeIds;
        ICollection<Guid> optionalMIATypeIds = vs.OptionalMIATypeIds;
        nd = sd.NavigateToView(new LocalDirectoryViewSpecification(localShare.Name, localShare.BaseResourcePath,
            necessaryMIATypeIds, optionalMIATypeIds));

        IResourceAccessor startRa = localShare.BaseResourcePath.CreateLocalResourceAccessor();
        Build(startRa, path, nd, viewRA =>
            new LocalDirectoryViewSpecification(null, viewRA.CanonicalLocalResourcePath, necessaryMIATypeIds, optionalMIATypeIds));
      }
      finally
      {
        workflowManager.EndBatchUpdate();
      }
    }

    protected static void NavigateToMLBrowse(ResourcePath path)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdate();
      try
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        IContentDirectory cd = scm.ContentDirectory;
        if (cd == null)
        {
          ServiceRegistration.Get<ILogger>().Error("AbstractBrowseMediaNavigationScreenData: Cannot navigate to ML browse view - the MP2 server is not connected");
          return;
        }

        workflowManager.NavigatePopStates(new Guid[] {Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT, Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT});
        workflowManager.NavigatePush(Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT);

        NavigationData nd = MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
        if (nd == null)
        {
          ServiceRegistration.Get<ILogger>().Error("AbstractBrowseMediaNavigationScreenData: Cannot navigate to ML browse view - no navigation data found");
          return;
        }
        AbstractItemsScreenData sd = (AbstractItemsScreenData) nd.CurrentScreenData;
        ViewSpecification vs = nd.BaseViewSpecification;
        string localSystemId;
        string localSystemName;
        if (!GetLocalSystemData(out localSystemId, out localSystemName))
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to ML browse view - error retrieving local system data");
          return;
        }
        ICollection<Guid> necessaryMIATypeIds = vs.NecessaryMIATypeIds;
        ICollection<Guid> optionalMIATypeIds = vs.OptionalMIATypeIds;
        nd = sd.NavigateToView(new SystemSharesViewSpecification(localSystemId, localSystemName, vs.NecessaryMIATypeIds, vs.OptionalMIATypeIds));
        sd = (AbstractItemsScreenData) nd.CurrentScreenData;

        Share localShare = FindLocalShareContainingPath(path);
        if (localShare == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to ML browse view - no local share countaining path '{0}' found", path);
          return;
        }
        IResourceAccessor startRa = localShare.BaseResourcePath.CreateLocalResourceAccessor();
        ResourcePath shareDirectoryPath = startRa.CanonicalLocalResourcePath;
        MediaItem shareDirectoryItem = cd.LoadItem(localSystemId, shareDirectoryPath, new Guid[] {DirectoryAspect.ASPECT_ID}, new Guid[] {});
        nd = sd.NavigateToView(new MediaLibraryBrowseViewSpecification(startRa.ResourceName, shareDirectoryItem.MediaItemId, localSystemId,
            shareDirectoryPath, necessaryMIATypeIds, optionalMIATypeIds));

        Build(startRa, path, nd, viewRA =>
          {
            ResourcePath directoryPath = viewRA.CanonicalLocalResourcePath;
            MediaItem directoryItem = cd.LoadItem(localSystemId, directoryPath, new Guid[] {DirectoryAspect.ASPECT_ID}, new Guid[] {});
            return new MediaLibraryBrowseViewSpecification(viewRA.ResourceName, directoryItem.MediaItemId, localSystemId, directoryPath, necessaryMIATypeIds, optionalMIATypeIds);
          });
      }
      finally
      {
        workflowManager.EndBatchUpdate();
      }
    }
  }
}