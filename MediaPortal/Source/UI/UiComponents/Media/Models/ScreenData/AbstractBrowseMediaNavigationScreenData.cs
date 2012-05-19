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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
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

    protected static ViewSpecification Unwrap(ViewSpecification vs)
    {
      // Unwrap potentially available removable media VS to get access to the inner ML VS or Local Browsing VS
      AddedRemovableMediaViewSpecificationFacade armvs = vs as AddedRemovableMediaViewSpecificationFacade;
      return armvs == null ? vs : armvs.DelegateViewSpecification;
    }

    protected static T FindParentViewSpecification<T>(NavigationData nd) where T : class
    {
      T result = null;
      while (nd != null && (result = Unwrap(nd.BaseViewSpecification) as T) == null)
        nd = nd.Parent;
      return result;
    }

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
      AbstractBrowseMediaNavigationScreenData screenData = nd.CurrentScreenData as AbstractBrowseMediaNavigationScreenData;
      if (screenData == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to sibling browse media state - there is no active media items screen");
        return;
      }
      ViewSpecification vs = Unwrap(nd.BaseViewSpecification);

      // Check Browse view states

      MediaLibraryBrowseViewSpecification mlbvs = vs as MediaLibraryBrowseViewSpecification;
      if (mlbvs != null)
      { // We're in some MediaLibrary browsing state - match the local media browsing which corresponds to the state
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        if (mlbvs.SystemId != localSystemId && mlbvs.SystemId != serverConnectionManager.HomeServerSystemId)
        { // If the currently browsed system is a different one, we can just navigate to the local browsing root view
          NavigateToLocalBrowse(null);
          return;
        }
        // In a browsing state for the local system, we should be able to navigate to the corresponding local media browsing view
        NavigateToLocalBrowse(mlbvs.BasePath);
        return;
      }

      BrowseMediaRootProxyViewSpecification bmrvs = vs as BrowseMediaRootProxyViewSpecification;
      SystemSharesViewSpecification ssvs = vs as SystemSharesViewSpecification;
      AllSystemsViewSpecification asvs = vs as AllSystemsViewSpecification;
      if (ssvs != null || asvs != null || bmrvs != null)
      { // If the current browsing state shows one of the root browse states, we can just navigate to the local browsing root view
        NavigateToLocalBrowse(null);
        return;
      }

      // Check local view states

      LocalMediaRootProxyViewSpecification lmrpvs = vs as LocalMediaRootProxyViewSpecification;
      LocalSharesViewSpecification lsvs = vs as LocalSharesViewSpecification;
      if (lmrpvs != null || lsvs != null)
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

    protected static void NavigateToLocalBrowse(ResourcePath path)
    {
      Navigate(path, Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT);
    }

    protected static void NavigateToMLBrowse(ResourcePath path)
    {
      Navigate(path, Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT);
    }

    protected static void Navigate(ResourcePath path, Guid targetState)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdate();
      try
      {
        workflowManager.NavigatePopStates(new Guid[] {Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT, Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT});

        workflowManager.NavigatePush(targetState);

        Share localShare = FindLocalShareContainingPath(path);
        if (localShare == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to local browse view - no local share countaining path '{0}' found", path);
          return;
        }

        NavigationData nd = MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
        if (nd == null)
          return;
        AbstractMediaRootProxyViewSpecification root = FindParentViewSpecification<AbstractMediaRootProxyViewSpecification>(nd);

        if (root == null)
          return;

        root.Navigate(localShare, path, navigateVS =>
          {
            NavigationData currentNd = MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
            if (currentNd == null)
            {
              ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to local browse view - no navigation data found");
              return;
            }
            AbstractItemsScreenData sd = (AbstractItemsScreenData) currentNd.CurrentScreenData;
            sd.NavigateToView(navigateVS);
          });
      }
      finally
      {
        workflowManager.EndBatchUpdate();
      }
    }
  }
}