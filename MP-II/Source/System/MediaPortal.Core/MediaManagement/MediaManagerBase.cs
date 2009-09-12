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
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Core.PluginManager;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Core.MediaManagement
{
  public delegate void NotifyMediaProviderChangeDelegate(IMediaProvider mediaProvider);
  public delegate void NotifyMetadataExtractorChangeDelegate(IMetadataExtractor metadataExtractor);

  /// <summary>
  /// This is the base class for client and server media managers.
  /// It contains the functionality to load media providers and metadata extractors.
  /// </summary>
  public class MediaManagerBase : IMediaManager
  {
    #region Constants

    /// <summary>
    /// Contains the id of the LocalFsMediaProvider.
    /// </summary>
    protected const string LOCAL_FS_MEDIAPROVIDER_ID = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";

    // Localization resources will be provided by the client's SkinBase plugin
    public const string MY_MUSIC_SHARE_NAME_RESOURE = "[Media.MyMusic]";
    public const string MY_VIDEOS_SHARE_NAME_RESOURCE = "[Media.MyVideos]";
    public const string MY_PICTURES_SHARE_NAME_RESOURCE = "[Media.MyPictures]";

    // Constants will be moved to some constants class
    protected const string MEDIA_PROVIDERS_PLUGIN_LOCATION = "/Media/MediaProviders";
    protected const string METADATA_EXTRACTORS_PLUGIN_LOCATION = "/Media/MetadataExtractors";

    protected const string METADATA_EXTRACTORS_USE_COMPONENT_NAME = "MediaManagerBase: MetadataExtractors";
    protected const string MEDIA_PROVIDERS_USE_COMPONENT_NAME = "MediaManagerBase: MediaProviders";

    #endregion

    protected class MetadataExtractorPluginItemChangeListener : IItemRegistrationChangeListener
    {
      protected MediaManagerBase _parent;

      internal MetadataExtractorPluginItemChangeListener(MediaManagerBase parent)
      {
        _parent = parent;
      }

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        foreach (PluginItemMetadata item in items)
        {
          IMetadataExtractor metadataExtractor = pluginManager.RequestPluginItem<IMetadataExtractor>(
              item.RegistrationLocation, item.Id, new FixedItemStateTracker(METADATA_EXTRACTORS_USE_COMPONENT_NAME));
          _parent.RegisterMetadataExtractor(metadataExtractor);
        }
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // TODO: Make MetadataExtractors removable?
      }
    }

    protected class MediaProviderPluginItemChangeListener : IItemRegistrationChangeListener
    {
      protected MediaManagerBase _parent;

      internal MediaProviderPluginItemChangeListener(MediaManagerBase parent)
      {
        _parent = parent;
      }

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        foreach (PluginItemMetadata item in items)
        {
          IMediaProvider mediaProvider = pluginManager.RequestPluginItem<IMediaProvider>(
              item.RegistrationLocation, item.Id, new FixedItemStateTracker(MEDIA_PROVIDERS_USE_COMPONENT_NAME));
          _parent.RegisterProvider(mediaProvider);
        }
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // TODO: Make MediaProviders removable?
      }
    }

    #region Protected fields

    protected MediaProviderPluginItemChangeListener _mediaProvidersPluginItemChangeListener;
    protected MetadataExtractorPluginItemChangeListener _metadataExtractorsPluginItemChangeListener;
    protected IDictionary<Guid, IMediaProvider> _providers = null;
    protected IDictionary<Guid, IMetadataExtractor> _metadataExtractors = null;

    #endregion

    #region Ctor

    public MediaManagerBase()
    {
      _mediaProvidersPluginItemChangeListener = new MediaProviderPluginItemChangeListener(this);
      _metadataExtractorsPluginItemChangeListener = new MetadataExtractorPluginItemChangeListener(this);
    }

    #endregion

    #region Protected methods

    protected void RegisterProvider(IMediaProvider provider)
    {
      _providers.Add(provider.Metadata.MediaProviderId, provider);
      MediaManagerMessaging.SendMediaProviderMessage(MediaManagerMessaging.MessageType.MediaProviderAdded, provider.Metadata.MediaProviderId);
    }

    protected void RegisterMetadataExtractor(IMetadataExtractor metadataExtractor)
    {
      _metadataExtractors.Add(metadataExtractor.Metadata.MetadataExtractorId, metadataExtractor);
      MediaManagerMessaging.SendMediaProviderMessage(MediaManagerMessaging.MessageType.MetadataExtractorAdded, metadataExtractor.Metadata.MetadataExtractorId);
    }

    /// <summary>
    /// Checks that the provider plugins are loaded.
    /// </summary>
    protected void CheckProviderPluginsLoaded()
    {
      if (_providers != null)
        return;
      _providers = new Dictionary<Guid, IMediaProvider>();
      foreach (IMediaProvider provider in ServiceScope.Get<IPluginManager>().RequestAllPluginItems<IMediaProvider>(
          MEDIA_PROVIDERS_PLUGIN_LOCATION, new FixedItemStateTracker(MEDIA_PROVIDERS_USE_COMPONENT_NAME))) // TODO: Make providers removable
        RegisterProvider(provider);
    }

    /// <summary>
    /// Checks that the provider plugins are loaded.
    /// </summary>
    protected void CheckMetadataExtractorPluginsLoaded()
    {
      if (_metadataExtractors != null)
        return;
      _metadataExtractors = new Dictionary<Guid, IMetadataExtractor>();

      foreach (IMetadataExtractor metadataExtractor in ServiceScope.Get<IPluginManager>().RequestAllPluginItems<IMetadataExtractor>(
          METADATA_EXTRACTORS_PLUGIN_LOCATION, new FixedItemStateTracker(METADATA_EXTRACTORS_USE_COMPONENT_NAME))) // TODO: Make metadata extractors removable
        RegisterMetadataExtractor(metadataExtractor);
    }

    protected void RegisterPluginItemListeners()
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.AddItemRegistrationChangeListener(MEDIA_PROVIDERS_PLUGIN_LOCATION,
          _mediaProvidersPluginItemChangeListener);
      pluginManager.AddItemRegistrationChangeListener(METADATA_EXTRACTORS_PLUGIN_LOCATION,
          _metadataExtractorsPluginItemChangeListener);
    }

    protected void UnregisterPluginItemListeners()
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.RemoveItemRegistrationChangeListener(MEDIA_PROVIDERS_PLUGIN_LOCATION,
          _mediaProvidersPluginItemChangeListener);
      pluginManager.RemoveItemRegistrationChangeListener(METADATA_EXTRACTORS_PLUGIN_LOCATION,
          _metadataExtractorsPluginItemChangeListener);
    }

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
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      foreach (IMetadataExtractor metadataExtractor in mediaManager.LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = metadataExtractor.Metadata;
        if (mediaCategory == null || metadata.ShareCategories.Contains(mediaCategory))
          yield return metadataExtractor.Metadata.MetadataExtractorId;
      }
    }

    protected ICollection<Share> CreateDefaultShares()
    {
      ICollection<Share> result = new List<Share>();
      Guid localFsMediaProviderId = new Guid(LOCAL_FS_MEDIAPROVIDER_ID);
      if (LocalMediaProviders.ContainsKey(localFsMediaProviderId))
      {
        string folderPath;
        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyMusic, out folderPath))
        {
          folderPath = LocalFsMediaProviderBase.ToProviderPath(folderPath);
          Guid shareId = Guid.NewGuid();
          string[] mediaCategories = new[] {DefaultMediaCategory.Audio.ToString()};
          ICollection<Guid> metadataExtractorIds = new List<Guid>();
          foreach (string mediaCategory in mediaCategories)
            CollectionUtils.AddAll(metadataExtractorIds, GetDefaultMetadataExtractorsForCategory(mediaCategory));
          Share sd = new Share(
              shareId, SystemName.GetLocalSystemName(), localFsMediaProviderId,
              folderPath, MY_MUSIC_SHARE_NAME_RESOURE,
              mediaCategories, metadataExtractorIds);
          result.Add(sd);
        }

        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyVideos, out folderPath))
        {
          folderPath = LocalFsMediaProviderBase.ToProviderPath(folderPath);
          Guid shareId = Guid.NewGuid();
          string[] mediaCategories = new[] { DefaultMediaCategory.Video.ToString() };
          ICollection<Guid> metadataExtractorIds = new List<Guid>();
          foreach (string mediaCategory in mediaCategories)
            CollectionUtils.AddAll(metadataExtractorIds, GetDefaultMetadataExtractorsForCategory(mediaCategory));
          Share sd = new Share(
              shareId, SystemName.GetLocalSystemName(), localFsMediaProviderId,
              folderPath, MY_VIDEOS_SHARE_NAME_RESOURCE,
              mediaCategories, metadataExtractorIds);
          result.Add(sd);
        }

        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyPictures, out folderPath))
        {
          folderPath = LocalFsMediaProviderBase.ToProviderPath(folderPath);
          Guid shareId = Guid.NewGuid();
          string[] mediaCategories = new[] { DefaultMediaCategory.Image.ToString() };
          ICollection<Guid> metadataExtractorIds = new List<Guid>();
          foreach (string mediaCategory in mediaCategories)
            CollectionUtils.AddAll(metadataExtractorIds, GetDefaultMetadataExtractorsForCategory(mediaCategory));
          Share sd = new Share(
              shareId, SystemName.GetLocalSystemName(), localFsMediaProviderId,
              folderPath, MY_PICTURES_SHARE_NAME_RESOURCE,
              mediaCategories, metadataExtractorIds);
          result.Add(sd);
        }
      }
      if (result.Count > 0)
        return result;
      // Fallback: If no share was added for the defaults above, use the provider's root folders
      foreach (IMediaProvider mediaProvider in LocalMediaProviders.Values)
      {
        MediaProviderMetadata metadata = mediaProvider.Metadata;
        Guid shareId = Guid.NewGuid();
        Share sd = new Share(
            shareId, SystemName.GetLocalSystemName(), metadata.MediaProviderId,
            "/", metadata.Name, null, GetDefaultMetadataExtractorsForCategory(null));
        result.Add(sd);
      }
      return result;
    }

    #endregion

    #region IMediaManager implementation

    public IDictionary<Guid, IMediaProvider> LocalMediaProviders
    {
      get
      {
        CheckProviderPluginsLoaded();
        return _providers;
      }
    }

    public IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors
    {
      get
      {
        CheckMetadataExtractorPluginsLoaded();
        return _metadataExtractors;
      }
    }

    public virtual void Initialize()
    {
      RegisterPluginItemListeners();
    }

    public virtual void Dispose()
    {
      UnregisterPluginItemListeners();
    }

    public IDictionary<Guid, MediaItemAspect> ExtractMetadata(Guid providerId, string path,
      IEnumerable<Guid> metadataExtractorIds)
    {
      if (!LocalMediaProviders.ContainsKey(providerId))
        return null;
      IMediaProvider provider = LocalMediaProviders[providerId];
      IDictionary<Guid, MediaItemAspect> result = new Dictionary<Guid, MediaItemAspect>();
      bool success = false;
      foreach (Guid extractorId in metadataExtractorIds)
      {
        if (!LocalMetadataExtractors.ContainsKey(extractorId))
          continue;
        IMetadataExtractor extractor = LocalMetadataExtractors[extractorId];
        foreach (MediaItemAspectMetadata miaMetadata in extractor.Metadata.ExtractedAspectTypes.Values)
          if (!result.ContainsKey(miaMetadata.AspectId))
            result.Add(miaMetadata.AspectId, new MediaItemAspect(miaMetadata));
        if (extractor.TryExtractMetadata(provider, path, result))
          success = true;
      }
      return success ? result : null;
    }

    public IMediaItemLocator GetMediaItemLocator(MediaItem item)
    {
      if (item == null)
        return null;
      MediaItemAspect providerAspect = item[ProviderResourceAspect.ASPECT_ID];
      string hostName = (string) providerAspect[ProviderResourceAspect.ATTR_SOURCE_COMPUTER];
      Guid providerId = new Guid((string) providerAspect[ProviderResourceAspect.ATTR_PROVIDER_ID]);
      string path = (string) providerAspect[ProviderResourceAspect.ATTR_PATH];
      return new MediaItemLocator(new SystemName(hostName), providerId, path);
    }

    #endregion

    #region IStatus Implementation

    public IList<string> GetStatus()
    {
      List<string> status = new List<string>();
      status.Add("=== MediaManager - MediaProviders");
      foreach (IMediaProvider provider in _providers.Values)
        status.Add(string.Format("     Provider '{0}'", provider.Metadata.Name));
      status.Add("=== MediaManager - MetadataExtractors");
      foreach (IMetadataExtractor metadataExtractor in _metadataExtractors.Values)
        status.Add(string.Format("     MetadataExtractor '{0}'", metadataExtractor.Metadata.Name));
      return status;
    }

    #endregion
  }
}
