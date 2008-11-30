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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Core.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// Shares management class for client-local shares. All shares are managed redundantly at
  /// the client's media manager via this class and at the MediaPortal server's MediaLibrary.
  /// </summary>
  public class SharesManagement : ISharesManagement
  {
    #region Protected fields

    /// <summary>
    /// Contains the id of the LocalFsMediaProvider.
    /// </summary>
    protected const string LOCAL_FS_MEDIAPROVIDER_ID = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";

    /// <summary>
    /// Contains the id of the MusicMetadataExtractor.
    /// </summary>
    protected const string MUSIC_METADATAEXTRACTOR_ID = "{817FEE2E-8690-4355-9F24-3BDC65AEDFFE}";

    // TODO: When the movie and picture MEs are implemented, comment this in:
    ///// <summary>
    ///// Contains the id of the MovieMetadataExtractor.
    ///// </summary>
    //protected const string MOVIE_METADATAEXTRACTOR_ID = ...;

    ///// <summary>
    ///// Contains the id of the PictureMetadataExtractor.
    ///// </summary>
    //protected const string PICTURE_METADATAEXTRACTOR_ID = ...;

    protected IDictionary<Guid, ShareDescriptor> _shares = new Dictionary<Guid, ShareDescriptor>();

    #endregion

    #region Ctor

    public SharesManagement() { }

    #endregion

    /// <summary>
    /// Returns an enumeration of default metadata extractors which can cope with the specified
    /// <paramref name="mediaCategory"/>.
    /// </summary>
    /// <param name="mediaCategory">The category to find all default metadata extractors for. If
    /// this parameter is <c>null</c>, the ids of all default metadata extractors are returned,
    /// independent of their category.</param>
    /// <returns>Enumeration of metadata extractors which can be asigned to shares of the
    /// specified <paramref name="mediaCategory"/> by default.</returns>
    protected static IEnumerable<Guid> GetDefaultMetadataExtractorsForCategory(string mediaCategory)
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (IMetadataExtractor metadataExtractor in mediaManager.LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = metadataExtractor.Metadata;
        if (mediaCategory == null || metadata.ShareCategories.Contains(mediaCategory))
          yield return metadataExtractor.Metadata.MetadataExtractorId;
      }
    }

    protected void InitializeDefaultShares()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      Guid localFsMediaProviderId = new Guid(LOCAL_FS_MEDIAPROVIDER_ID);
      if (mediaManager.LocalMediaProviders.ContainsKey(localFsMediaProviderId))
      {
        string folderPath;
        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyMusic, out folderPath))
        {
          Guid shareId = Guid.NewGuid();
          string[] mediaCategories = new[] {DefaultMediaCategory.Audio.ToString()};
          ICollection<Guid> metadataExtractorIds = new List<Guid>();
          foreach (string mediaCategory in mediaCategories)
            CollectionUtils.AddAll(metadataExtractorIds, GetDefaultMetadataExtractorsForCategory(mediaCategory));
          // TODO: Localization resource for [Media.MyMusic]
          ShareDescriptor sd = new ShareDescriptor(
              shareId, SystemName.Loopback(), localFsMediaProviderId,
              folderPath, "[Media.MyMusic]",
              mediaCategories, metadataExtractorIds);
          _shares.Add(shareId, sd);
        }

        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyVideos, out folderPath))
        {
          Guid shareId = Guid.NewGuid();
          string[] mediaCategories = new[] { DefaultMediaCategory.Video.ToString() };
          ICollection<Guid> metadataExtractorIds = new List<Guid>();
          foreach (string mediaCategory in mediaCategories)
            CollectionUtils.AddAll(metadataExtractorIds, GetDefaultMetadataExtractorsForCategory(mediaCategory));
          // TODO: Localization resource for [Media.MyVideos]
          ShareDescriptor sd = new ShareDescriptor(
              shareId, SystemName.Loopback(), localFsMediaProviderId,
              folderPath, "[Media.MyVideos]",
              mediaCategories, metadataExtractorIds);
          _shares.Add(shareId, sd);
        }

        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyPictures, out folderPath))
        {
          Guid shareId = Guid.NewGuid();
          string[] mediaCategories = new[] { DefaultMediaCategory.Image.ToString() };
          ICollection<Guid> metadataExtractorIds = new List<Guid>();
          foreach (string mediaCategory in mediaCategories)
            CollectionUtils.AddAll(metadataExtractorIds, GetDefaultMetadataExtractorsForCategory(mediaCategory));
          // TODO: Localization resource for [Media.MyPictures]
          ShareDescriptor sd = new ShareDescriptor(
              shareId, SystemName.Loopback(), localFsMediaProviderId,
              folderPath, "[Media.MyPictures]",
              mediaCategories, metadataExtractorIds);
          _shares.Add(shareId, sd);
        }
      }
      if (_shares.Count > 0)
        return;
      // Fallback: If no share was added for the defaults above, use the provider's root folders
      foreach (IMediaProvider mediaProvider in mediaManager.LocalMediaProviders.Values)
      {
        MediaProviderMetadata metadata = mediaProvider.Metadata;
        Guid shareId = Guid.NewGuid();
        ShareDescriptor sd = new ShareDescriptor(
            shareId, SystemName.Loopback(), metadata.MediaProviderId,
            "/", metadata.Name, null, GetDefaultMetadataExtractorsForCategory(null));
        _shares.Add(shareId, sd);
      }
    }

    #region Public methods

    public void LoadSharesFromSettings()
    {
      SharesSettings sharesSettings = ServiceScope.Get<ISettingsManager>().Load<SharesSettings>();
      if (sharesSettings.LocalShares.Count == 0)
      { // The shares are still uninitialized - use defaults
        InitializeDefaultShares();
        SaveSharesToSettings();
        return;
      }
      foreach (ShareDescriptor share in sharesSettings.LocalShares)
        _shares.Add(share.ShareId, share);
    }

    public void SaveSharesToSettings()
    {
      SharesSettings settings = new SharesSettings();
      CollectionUtils.AddAll(settings.LocalShares, _shares.Values);
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    #endregion

    #region ISharesManagement implementation

    public void Initialize()
    {
      LoadSharesFromSettings();
    }

    public ShareDescriptor RegisterShare(SystemName systemName, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor sd = ShareDescriptor.CreateNewShare(systemName, providerId, path,
          shareName, mediaCategories, metadataExtractorIds);
      _shares.Add(sd.ShareId, sd);
      return sd;
    }

    public void RemoveShare(Guid shareId)
    {
      _shares.Remove(shareId);
    }

    public IDictionary<Guid, ShareDescriptor> GetShares()
    {
      return _shares;
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      return _shares.ContainsKey(shareId) ? _shares[shareId] : null;
    }

    public IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName)
    {
      if (SystemName.GetLocalNames().Contains(systemName))
        return _shares;
      return new Dictionary<Guid, ShareDescriptor>();
    }

    public ICollection<SystemName> GetManagedClients()
    {
      return SystemName.GetLocalNames();
    }

    public IDictionary<Guid, MetadataExtractorMetadata> GetMetadataExtractorsBySystem(SystemName systemName)
    {
      IDictionary<Guid, MetadataExtractorMetadata> result = new Dictionary<Guid, MetadataExtractorMetadata>();
      if (SystemName.GetLocalNames().Contains(systemName))
        foreach (MetadataExtractorMetadata metadata in
            ServiceScope.Get<MediaManager>().LocalMetadataExtractors.Values)
          result.Add(metadata.MetadataExtractorId, metadata);
      return result;
    }

    public void AddMetadataExtractorsToShare(Guid shareId, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor sd = GetShare(shareId);
      if (sd == null)
        return;
      foreach (Guid metadataExtractorId in metadataExtractorIds)
        sd.MetadataExtractorIds.Add(metadataExtractorId);
      ServiceScope.Get<MediaManager>().InvalidateMediaItemsFromShare(shareId);
    }

    public void RemoveMetadataExtractorsFromShare(Guid shareId, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor sd = GetShare(shareId);
      if (sd == null)
        return;
      foreach (Guid metadataExtractorId in metadataExtractorIds)
        sd.MetadataExtractorIds.Remove(metadataExtractorId);
      ServiceScope.Get<MediaManager>().InvalidateMediaItemsFromShare(shareId);
    }

    #endregion
  }
}
