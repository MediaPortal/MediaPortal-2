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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which can be used for the root view of the browse media view hierarchy.
  /// Depending on the information if we are in a single-seat configuration or not, this view specification shows
  /// only shows the server's shares or different sub views for each system.
  /// </summary>
  public class BrowseMediaRootProxyViewSpecification : AbstractMediaRootProxyViewSpecification
  {
    #region Ctor

    public BrowseMediaRootProxyViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds) { }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return ServiceRegistration.Get<IServerConnectionManager>().IsHomeServerConnected; }
    }

    /// <summary>
    /// Tries to find the resource path corresponding to the given media library <paramref name="viewSpecification"/>.
    /// </summary>
    /// <param name="viewSpecification">View specification to be examined.</param>
    /// <param name="path">Path corresponding to the given <paramref name="viewSpecification"/>, if it is a media library view specification (i.e. one of the
    /// view specifications which are created in any of the sub views of this view specification). Else, this parameter will return <c>null</c>.</param>
    /// <returns><c>true</c>, if the given <paramref name="viewSpecification"/> is one of the direct or indirect view specifications which are created as sub view specifications
    /// of this view specification.</returns>
    public static bool TryGetLocalBrowseViewPath(ViewSpecification viewSpecification, out ResourcePath path)
    {
      MediaLibraryBrowseViewSpecification mlbvs = viewSpecification as MediaLibraryBrowseViewSpecification;
      if (mlbvs != null)
      { // We're in some MediaLibrary browsing state
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        string localSystemId = ServiceRegistration.Get<ISystemResolver>().LocalSystemId;
        if (mlbvs.SystemId != localSystemId && mlbvs.SystemId != serverConnectionManager.HomeServerSystemId)
        { // If the currently browsed system is a different one, the path must be set to null
          path = null;
          return true;
        }
        // In a browsing state for the local system, we can return the base path from the view specification
        path = mlbvs.BasePath;
        return true;
      }

      BrowseMediaRootProxyViewSpecification bmrvs = viewSpecification as BrowseMediaRootProxyViewSpecification;
      SystemSharesViewSpecification ssvs = viewSpecification as SystemSharesViewSpecification;
      AllSystemsViewSpecification asvs = viewSpecification as AllSystemsViewSpecification;
      if (ssvs != null || asvs != null || bmrvs != null)
      { // If the current browsing state shows one of the root browse states, we can just set the path to null
        path = null;
        return true;
      }
      path = null;
      return false;
    }

    protected static bool IsSingleSeat(IServerConnectionManager serverConnectionManager)
    {
      SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
      bool isLocalHomeServer = homeServerSystem != null && homeServerSystem.IsLocalSystem();
      IServerController serverController = serverConnectionManager.ServerController;
      ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      ICollection<Share> localClientShares = localSharesManagement.Shares.Values;
      return serverController != null && serverController.GetAttachedClients().Count == 1 && isLocalHomeServer && localClientShares.Count == 0;
    }

    protected override void NavigateToLocalRootView(Share localShare, NavigateToViewDlgt navigateToViewDlgt)
    {
      // We need to simulate the logic from method ReLoadItemsAndSubViewSpecifications
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();

      if (!IsSingleSeat(serverConnectionManager))
      {
        string viewName;
        if (localShare.SystemId == serverConnectionManager.HomeServerSystemId)
          viewName = serverConnectionManager.LastHomeServerName;
        else
        {
          MPClientMetadata clientMetadata = ServerCommunicationHelper.GetClientMetadata(localShare.SystemId);
          viewName = clientMetadata == null ? null : clientMetadata.LastClientName;
        }
        if (viewName != null)
          navigateToViewDlgt(new SystemSharesViewSpecification(localShare.SystemId, viewName, _necessaryMIATypeIds, _optionalMIATypeIds));
      }

      IContentDirectory cd = serverConnectionManager.ContentDirectory;
      if (cd == null)
        return;

      MediaItem parentDirectory = cd.LoadItem(localShare.SystemId, localShare.BaseResourcePath,
          SystemSharesViewSpecification.DIRECTORY_MIA_ID_ENUMERATION, SystemSharesViewSpecification.EMPTY_ID_ENUMERATION);
      if (parentDirectory == null)
        return;
      navigateToViewDlgt(new MediaLibraryBrowseViewSpecification(localShare.Name, parentDirectory.MediaItemId,
            localShare.SystemId, localShare.BaseResourcePath, _necessaryMIATypeIds, _optionalMIATypeIds));
    }

    protected override ViewSpecification NavigateCreateViewSpecification(string systemId, IFileSystemResourceAccessor viewRA)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();

      IContentDirectory cd = serverConnectionManager.ContentDirectory;
      if (cd == null)
        return null;

      ResourcePath directoryPath = viewRA.CanonicalLocalResourcePath;
      MediaItem directoryItem = cd.LoadItem(systemId, directoryPath,
          SystemSharesViewSpecification.DIRECTORY_MIA_ID_ENUMERATION, SystemSharesViewSpecification.EMPTY_ID_ENUMERATION);
      if (directoryItem == null)
        return null;
      return new MediaLibraryBrowseViewSpecification(viewRA.ResourceName, directoryItem.MediaItemId, systemId,
          directoryPath, _necessaryMIATypeIds, _optionalMIATypeIds);
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();

      if (IsSingleSeat(serverConnectionManager))
        // This code branch represents the typical single-seat scenario, so we only show the server's shares.
        new SystemSharesViewSpecification(serverConnectionManager.HomeServerSystemId, null, _necessaryMIATypeIds, _optionalMIATypeIds).ReLoadItemsAndSubViewSpecifications(out mediaItems, out subViewSpecifications);
      else
        // This code branch represents all other scenarios than the single-seat scenario.
        new AllSystemsViewSpecification(null, _necessaryMIATypeIds, _optionalMIATypeIds).ReLoadItemsAndSubViewSpecifications(out mediaItems, out subViewSpecifications);
    }

    #endregion
  }
}
