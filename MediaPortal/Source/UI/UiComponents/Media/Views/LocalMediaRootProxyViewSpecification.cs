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
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which can be used for the root view of the local media view hierarchy.
  /// Depending on the information if the home server is located on the local machine and/or if both the home server and this client
  /// have shares configured, this view specification only shows the client's shares or the server's shares or both system's shares in different sub views.
  /// </summary>
  public class LocalMediaRootProxyViewSpecification : AbstractMediaRootProxyViewSpecification
  {
    #region Ctor

    public LocalMediaRootProxyViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds) { }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return true; }
    }

    protected override void NavigateToLocalRootView(Share localShare, NavigateToViewDlgt navigateToViewDlgt)
    {
      // We need to simulate the logic from method ReLoadItemsAndSubViewSpecifications
      ICollection<Share> localServerShares;
      ICollection<Share> localClientShares;
      GetShares(out localServerShares, out localClientShares);
      if (localServerShares.Count > 0 && localClientShares.Count > 0)
      {
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        if (localShare.SystemId == serverConnectionManager.HomeServerSystemId)
          navigateToViewDlgt(new LocalSharesViewSpecification(localServerShares, Consts.RES_SERVER_SHARES, _necessaryMIATypeIds, _optionalMIATypeIds));
        else if (localShare.SystemId == systemResolver.LocalSystemId)
          navigateToViewDlgt(new LocalSharesViewSpecification(localClientShares, Consts.RES_CLIENT_SHARES, _necessaryMIATypeIds, _optionalMIATypeIds));
      }
      navigateToViewDlgt(new LocalDirectoryViewSpecification(localShare.Name, localShare.BaseResourcePath, _necessaryMIATypeIds, _optionalMIATypeIds));
    }

    protected override ViewSpecification NavigateCreateViewSpecification(IFileSystemResourceAccessor viewRa)
    {
      return new LocalDirectoryViewSpecification(null, viewRa.CanonicalLocalResourcePath, _necessaryMIATypeIds, _optionalMIATypeIds);
    }

    protected void GetShares(out ICollection<Share> localServerShares, out ICollection<Share> localClientShares)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
      bool isLocalHomeServer = homeServerSystem == null ? false : homeServerSystem.IsLocalSystem();
      IContentDirectory cd = serverConnectionManager.ContentDirectory;
      ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      localServerShares = (isLocalHomeServer && cd != null) ? cd.GetShares(serverConnectionManager.HomeServerSystemId, SharesFilter.All) : new List<Share>();
      localClientShares = localSharesManagement.Shares.Values;
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      ICollection<Share> localServerShares;
      ICollection<Share> localClientShares;
      GetShares(out localServerShares, out localClientShares);

      if (localServerShares.Count > 0 && localClientShares.Count > 0)
      {
        // Both local client shares and local server shares are present - this should only happen in special situations because the shares configuration doesn't allow
        // to add local client shares if the home server's system is the local system. But it can yet happen, for example if the client added local shares before
        // it was attached to the local server.
        // In this case, we show two sub views - one for the local client shares and one for the server shares.
        mediaItems = new List<MediaItem>();
        subViewSpecifications = new List<ViewSpecification>
          {
              new LocalSharesViewSpecification(localClientShares, Consts.RES_CLIENT_SHARES, _necessaryMIATypeIds, _optionalMIATypeIds),
              new LocalSharesViewSpecification(localServerShares, Consts.RES_SERVER_SHARES, _necessaryMIATypeIds, _optionalMIATypeIds)
          };
      }
      else
      {
        ICollection<Share> source = localServerShares.Count > 0 ? localServerShares : localClientShares;
        new LocalSharesViewSpecification(source, null, _necessaryMIATypeIds, _optionalMIATypeIds).ReLoadItemsAndSubViewSpecifications(out mediaItems, out subViewSpecifications);
      }
    }

    #endregion
  }
}
