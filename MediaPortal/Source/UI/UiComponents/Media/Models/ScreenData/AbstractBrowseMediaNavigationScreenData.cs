#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
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
    /// This method switches between those modes. It takes the current media navigation state and tries to navigate to the most sensible sibling state, i.e.
    /// tries to navigate as close as possible to the current directory navigation in the other mode.
    /// </summary>
    public static void NavigateToSiblingState()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      NavigationContext workflowNavigationContext = workflowManager.CurrentNavigationContext;

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

      ResourcePath path;
      if (BrowseMediaRootProxyViewSpecification.TryGetLocalBrowseViewPath(vs, out path))
        NavigateToLocalBrowsing(path);
      else if (LocalMediaRootProxyViewSpecification.TryGetLocalBrowseViewPath(vs, out path))
        NavigateToMLBrowsing(path);
    }

    protected static Share FindLocalShareContainingPath(ResourcePath path)
    {
      if (path == null)
        return null;
      ILocalSharesManagement lsm = ServiceRegistration.Get<ILocalSharesManagement>();
      Share result = lsm.Shares.Values.BestContainingPath(path);
      if (result == null)
      {
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        IContentDirectory contentDirectory = serverConnectionManager.ContentDirectory;
        if (contentDirectory != null)
          result = contentDirectory.GetShares(serverConnectionManager.HomeServerSystemId, SharesFilter.All).BestContainingPath(path);
      }
      return result;
    }

    /// <summary>
    /// Navigates to the local browsing state to the given <paramref name="path"/> if that path is located in a local share.
    /// </summary>
    /// <param name="path">Local path in a local share to navigate to. The share can be a client share or a server share.</param>
    public static void NavigateToLocalBrowsing(ResourcePath path)
    {
      Navigate(Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT, path);
    }

    /// <summary>
    /// Navigates to the media library browsing state to the given <paramref name="path"/> if that path is located in a share.
    /// </summary>
    /// <param name="path">Local path in a local share to navigate to. The share can be a client share or a server share.</param>
    public static void NavigateToMLBrowsing(ResourcePath path)
    {
      Navigate(Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT, path);
    }

    protected static void Navigate(Guid rootWorkflowState, ResourcePath path)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdate();
      try
      {
        workflowManager.NavigatePopStates(new Guid[] {Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT, Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT});

        workflowManager.NavigatePush(rootWorkflowState);

        Share localShare = FindLocalShareContainingPath(path);
        if (localShare == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractBrowseMediaNavigationScreenData: Cannot navigate to local browse view - no local share countaining path '{0}' found", path);
          return;
        }

        NavigationData nd = MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
        if (nd == null)
          return;
        AbstractMediaRootProxyViewSpecification rootVS = FindParentViewSpecification<AbstractMediaRootProxyViewSpecification>(nd);

        if (rootVS == null)
          return;

        rootVS.Navigate(localShare, path, navigateVS =>
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