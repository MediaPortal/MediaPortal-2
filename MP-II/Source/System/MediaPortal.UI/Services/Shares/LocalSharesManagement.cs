#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Settings;
using MediaPortal.Services.Shares.Settings;
using MediaPortal.Shares;
using MediaPortal.Utilities;

namespace MediaPortal.Services.Shares
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

    /// <summary>
    /// Contains the id of the LocalFsMediaProvider.
    /// </summary>
    protected const string LOCAL_FS_MEDIAPROVIDER_ID = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";

    protected IDictionary<Guid, Share> _shares = new Dictionary<Guid, Share>();

    #endregion

    #region Ctor

    public LocalSharesManagement() { }

    #endregion

    #region Public methods

    public void LoadSharesFromSettings()
    {
      SharesSettings sharesSettings = ServiceScope.Get<ISettingsManager>().Load<SharesSettings>();
      foreach (Share share in sharesSettings.LocalShares)
        _shares.Add(share.ShareId, share);
    }

    public void SaveSharesToSettings()
    {
      SharesSettings settings = new SharesSettings();
      CollectionUtils.AddAll(settings.LocalShares, _shares.Values);
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    public IDictionary<Guid, Share> Shares
    {
      get { return _shares; }
    }

    public void Initialize()
    {
      ServiceScope.Get<ILogger>().Info("LocalSharesManagement: Initialize");
      LoadSharesFromSettings();
      if (_shares.Count == 0)
      { // The shares are still uninitialized - use defaults
        IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
        foreach (Share share in mediaAccessor.CreateDefaultShares())
          _shares.Add(share.ShareId, share);
        SaveSharesToSettings();
      }
    }

    public void Shutdown()
    {
    }

    public Share GetShare(Guid shareId)
    {
      return _shares.ContainsKey(shareId) ? _shares[shareId] : null;
    }

    public Share RegisterShare(Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories)
    {
      Share sd = Share.CreateNewShare(SystemName.GetLocalSystemName(), providerId, path,
          shareName, mediaCategories);
      _shares.Add(sd.ShareId, sd);
      SaveSharesToSettings();
      SharesMessaging.SendShareMessage(SharesMessaging.MessageType.ShareAdded, sd.ShareId);
      return sd;
    }

    public void RemoveShare(Guid shareId)
    {
      _shares.Remove(shareId);
      SaveSharesToSettings();
      SharesMessaging.SendShareMessage(SharesMessaging.MessageType.ShareRemoved, shareId);
    }

    public Share UpdateShare(Guid shareId, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      Share result = GetShare(shareId);
      if (result == null)
        return null;
      result.MediaProviderId = providerId;
      result.Path = path;
      result.Name = shareName;
      result.MediaCategories.Clear();
      CollectionUtils.AddAll(result.MediaCategories, mediaCategories);
      SaveSharesToSettings();
      SharesMessaging.SendShareChangedMessage(shareId, relocationMode);
      return result;
    }

    #endregion
  }
}