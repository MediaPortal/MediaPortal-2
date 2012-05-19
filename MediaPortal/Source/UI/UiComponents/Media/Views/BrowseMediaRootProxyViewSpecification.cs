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
  /// Depending on the information if the home server is located on the local machine and/or if both the home server and this client
  /// have shares configured, this view specification only shows the client's shares or the server's shares or both system's shares in different sub views.
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

    protected static bool IsSingleSeat(IServerConnectionManager serverConnectionManager)
    {
      SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
      bool isLocalHomeServer = homeServerSystem == null ? false : homeServerSystem.IsLocalSystem();
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
        string viewName = localShare.SystemId == serverConnectionManager.HomeServerSystemId ?
            serverConnectionManager.LastHomeServerName : ServerCommunicationHelper.GetClientName(localShare.SystemId);
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

    protected override ViewSpecification NavigateCreateViewSpecification(IFileSystemResourceAccessor viewRA)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();

      IContentDirectory cd = serverConnectionManager.ContentDirectory;
      if (cd == null)
        return null;

      string localSystemId = systemResolver.LocalSystemId;
      ResourcePath directoryPath = viewRA.CanonicalLocalResourcePath;
      MediaItem directoryItem = cd.LoadItem(localSystemId, directoryPath,
          SystemSharesViewSpecification.DIRECTORY_MIA_ID_ENUMERATION, SystemSharesViewSpecification.EMPTY_ID_ENUMERATION);
      return new MediaLibraryBrowseViewSpecification(viewRA.ResourceName, directoryItem.MediaItemId, localSystemId,
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
