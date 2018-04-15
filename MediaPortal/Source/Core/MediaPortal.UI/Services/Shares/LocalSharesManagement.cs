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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Services.Shares.Settings;
using MediaPortal.UI.Shares;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.Shares
{
  /// <summary>
  /// Shares management class for client-local shares. All shares are managed redundantly at
  /// the client's media manager via this class and at the MediaPortal server's MediaLibrary.
  /// </summary>
  public class LocalSharesManagement : ILocalSharesManagement
  {
    #region Consts

    // Localization resources will be provided by the SkinBase plugin
    public const string MY_MUSIC_SHARE_NAME_RESOURE = "[Media.MyMusic]";
    public const string MY_VIDEOS_SHARE_NAME_RESOURCE = "[Media.MyVideos]";
    public const string MY_PICTURES_SHARE_NAME_RESOURCE = "[Media.MyPictures]";

    #endregion

    #region Protected fields

    protected IDictionary<Guid, Share> _shares = new Dictionary<Guid, Share>();

    #endregion

    #region Public methods

    public void LoadSharesFromSettings()
    {
      SharesSettings sharesSettings = ServiceRegistration.Get<ISettingsManager>().Load<SharesSettings>();
      foreach (Share share in sharesSettings.LocalShares)
        _shares.Add(share.ShareId, share);
    }

    public void SaveSharesToSettings()
    {
      SharesSettings settings = new SharesSettings();
      CollectionUtils.AddAll(settings.LocalShares, _shares.Values);
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    public IDictionary<Guid, Share> Shares
    {
      get { return _shares; }
    }

    public void Initialize()
    {
      ServiceRegistration.Get<ILogger>().Info("LocalSharesManagement: Initialize");
      LoadSharesFromSettings();
    }

    public void Shutdown()
    {
    }

    public void SetupDefaultShares()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (Share share in mediaAccessor.CreateDefaultShares())
        RegisterShare(share.BaseResourcePath, share.Name, share.UseShareWatcher, share.MediaCategories);
    }

    public Share GetShare(Guid shareId)
    {
      return _shares.ContainsKey(shareId) ? _shares[shareId] : null;
    }

    public Share RegisterShare(ResourcePath baseResourcePath, string shareName, bool useShareWatcher, IEnumerable<string> mediaCategories)
    {
      Share sd = Share.CreateNewLocalShare(baseResourcePath, shareName, useShareWatcher, mediaCategories);
      _shares.Add(sd.ShareId, sd);
      SaveSharesToSettings();
      SharesMessaging.SendShareMessage(SharesMessaging.MessageType.ShareAdded, sd);
      return sd;
    }

    public void RemoveShare(Guid shareId)
    {
      Share share;
      if (!_shares.TryGetValue(shareId, out share))
        return;
      _shares.Remove(shareId);
      SaveSharesToSettings();
      SharesMessaging.SendShareMessage(SharesMessaging.MessageType.ShareRemoved, share);
    }

    public Share UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      Share result = GetShare(shareId);
      if (result == null)
        return null;
      result.BaseResourcePath = baseResourcePath;
      result.Name = shareName;
      result.UseShareWatcher = useShareWatcher;
      result.MediaCategories.Clear();
      CollectionUtils.AddAll(result.MediaCategories, mediaCategories);
      SaveSharesToSettings();
      SharesMessaging.SendShareChangedMessage(result, relocationMode);
      return result;
    }

    public void ReImportShare(Guid shareId)
    {
      Share share = GetShare(shareId);
      if (share == null)
        return;
      SharesMessaging.SendShareReimportMessage(share);
    }

    #endregion
  }
}
