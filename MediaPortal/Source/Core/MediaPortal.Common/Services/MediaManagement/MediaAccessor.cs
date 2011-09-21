#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.ResourceAccess;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Common.Services.MediaManagement
{
  public delegate void NotifyMediaProviderChangeDelegate(IMediaProvider mediaProvider);

  public delegate void NotifyMetadataExtractorChangeDelegate(IMetadataExtractor metadataExtractor);

  /// <summary>
  /// This is the base class for client and server media managers.
  /// It contains the functionality to load media providers and metadata extractors.
  /// </summary>
  public class MediaAccessor : IMediaAccessor
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

    protected const string METADATA_EXTRACTORS_USE_COMPONENT_NAME = "MediaAccessor: MetadataExtractors";
    protected const string MEDIA_PROVIDERS_USE_COMPONENT_NAME = "MediaAccessor: MediaProviders";

    #endregion

    #region Internal classes

    protected class MetadataExtractorPluginItemChangeListener : IItemRegistrationChangeListener
    {
      protected MediaAccessor _parent;

      internal MetadataExtractorPluginItemChangeListener(MediaAccessor parent)
      {
        _parent = parent;
      }

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
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
      protected MediaAccessor _parent;

      internal MediaProviderPluginItemChangeListener(MediaAccessor parent)
      {
        _parent = parent;
      }

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
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

    #endregion

    #region Protected fields

    protected MediaProviderPluginItemChangeListener _mediaProvidersPluginItemChangeListener;
    protected MetadataExtractorPluginItemChangeListener _metadataExtractorsPluginItemChangeListener;
    protected IDictionary<Guid, IMediaProvider> _providers = null;
    protected IDictionary<Guid, IMetadataExtractor> _metadataExtractors = null;

    #endregion

    #region Ctor

    public MediaAccessor()
    {
      _mediaProvidersPluginItemChangeListener = new MediaProviderPluginItemChangeListener(this);
      _metadataExtractorsPluginItemChangeListener = new MetadataExtractorPluginItemChangeListener(this);
    }

    #endregion

    #region Protected methods

    protected void RegisterProvider(IMediaProvider provider)
    {
      _providers.Add(provider.Metadata.MediaProviderId, provider);
      MediaAccessorMessaging.SendMediaProviderMessage(MediaAccessorMessaging.MessageType.MediaProviderAdded, provider.Metadata.MediaProviderId);
    }

    protected void RegisterMetadataExtractor(IMetadataExtractor metadataExtractor)
    {
      _metadataExtractors.Add(metadataExtractor.Metadata.MetadataExtractorId, metadataExtractor);
      MediaAccessorMessaging.SendMediaProviderMessage(MediaAccessorMessaging.MessageType.MetadataExtractorAdded, metadataExtractor.Metadata.MetadataExtractorId);
    }

    /// <summary>
    /// Checks that the provider plugins are loaded.
    /// </summary>
    protected void CheckProviderPluginsLoaded()
    {
      if (_providers != null)
        return;
      _providers = new Dictionary<Guid, IMediaProvider>();
      foreach (IMediaProvider provider in ServiceRegistration.Get<IPluginManager>().RequestAllPluginItems<IMediaProvider>(
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

      foreach (IMetadataExtractor metadataExtractor in ServiceRegistration.Get<IPluginManager>().RequestAllPluginItems<IMetadataExtractor>(
          METADATA_EXTRACTORS_PLUGIN_LOCATION, new FixedItemStateTracker(METADATA_EXTRACTORS_USE_COMPONENT_NAME))) // TODO: Make metadata extractors removable
        RegisterMetadataExtractor(metadataExtractor);
    }

    protected void RegisterPluginItemListeners()
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      pluginManager.AddItemRegistrationChangeListener(MEDIA_PROVIDERS_PLUGIN_LOCATION,
          _mediaProvidersPluginItemChangeListener);
      pluginManager.AddItemRegistrationChangeListener(METADATA_EXTRACTORS_PLUGIN_LOCATION,
          _metadataExtractorsPluginItemChangeListener);
    }

    protected void UnregisterPluginItemListeners()
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      pluginManager.RemoveItemRegistrationChangeListener(MEDIA_PROVIDERS_PLUGIN_LOCATION,
          _mediaProvidersPluginItemChangeListener);
      pluginManager.RemoveItemRegistrationChangeListener(METADATA_EXTRACTORS_PLUGIN_LOCATION,
          _metadataExtractorsPluginItemChangeListener);
    }

    #endregion

    #region IMediaAccessor implementation

    public IDictionary<Guid, IMediaProvider> LocalMediaProviders
    {
      get
      {
        CheckProviderPluginsLoaded();
        return _providers;
      }
    }

    public IEnumerable<IBaseMediaProvider> LocalBaseMediaProviders
    {
      get
      {
        foreach (IMediaProvider mediaProvider in LocalMediaProviders.Values)
        {
          IBaseMediaProvider provider = mediaProvider as IBaseMediaProvider;
          if (provider != null)
            yield return provider;
        }
        yield break;
      }
    }

    public IEnumerable<IChainedMediaProvider> LocalChainedMediaProviders
    {
      get
      {
        foreach (IMediaProvider mediaProvider in LocalMediaProviders.Values)
        {
          IChainedMediaProvider provider = mediaProvider as IChainedMediaProvider;
          if (provider != null)
            yield return provider;
        }
        yield break;
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

    public virtual void Shutdown()
    {
      UnregisterPluginItemListeners();
    }

    public ICollection<Share> CreateDefaultShares()
    {
      ICollection<Share> result = new List<Share>();
      Guid localFsMediaProviderId = new Guid(LOCAL_FS_MEDIAPROVIDER_ID);
      if (LocalMediaProviders.ContainsKey(localFsMediaProviderId))
      {
        string folderPath;
        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyMusic, out folderPath))
        {
          folderPath = LocalFsMediaProviderBase.ToProviderPath(folderPath);
          string[] mediaCategories = new[] {DefaultMediaCategory.Audio.ToString()};
          Share sd = Share.CreateNewLocalShare(ResourcePath.BuildBaseProviderPath(localFsMediaProviderId, folderPath),
              MY_MUSIC_SHARE_NAME_RESOURE, mediaCategories);
          result.Add(sd);
        }

        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyVideos, out folderPath))
        {
          folderPath = LocalFsMediaProviderBase.ToProviderPath(folderPath);
          string[] mediaCategories = new[] { DefaultMediaCategory.Video.ToString() };
          Share sd = Share.CreateNewLocalShare(ResourcePath.BuildBaseProviderPath(localFsMediaProviderId, folderPath),
              MY_VIDEOS_SHARE_NAME_RESOURCE, mediaCategories);
          result.Add(sd);
        }

        if (WindowsAPI.GetSpecialFolder(WindowsAPI.SpecialFolder.MyPictures, out folderPath))
        {
          folderPath = LocalFsMediaProviderBase.ToProviderPath(folderPath);
          string[] mediaCategories = new[] { DefaultMediaCategory.Image.ToString() };
          Share sd = Share.CreateNewLocalShare(ResourcePath.BuildBaseProviderPath(localFsMediaProviderId, folderPath),
              MY_PICTURES_SHARE_NAME_RESOURCE, mediaCategories);
          result.Add(sd);
        }
      }
      if (result.Count > 0)
        return result;
      // Fallback: If no share was added for the defaults above, use the provider's root folders
      foreach (IBaseMediaProvider mediaProvider in LocalBaseMediaProviders)
      {
        MediaProviderMetadata metadata = mediaProvider.Metadata;
        Share sd = Share.CreateNewLocalShare(ResourcePath.BuildBaseProviderPath(metadata.MediaProviderId, "/"), metadata.Name, null);
        result.Add(sd);
      }
      return result;
    }

    public IResourceLocator GetResourceLocator(MediaItem item)
    {
      if (item == null || !item.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        return null;
      MediaItemAspect providerAspect = item[ProviderResourceAspect.ASPECT_ID];
      string systemId = (string) providerAspect[ProviderResourceAspect.ATTR_SYSTEM_ID];
      string resourceAccessorPath = (string) providerAspect[ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
      return new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
    }

    public IEnumerable<Guid> GetMetadataExtractorsForCategory(string mediaCategory)
    {
      foreach (IMetadataExtractor metadataExtractor in LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = metadataExtractor.Metadata;
        if (mediaCategory == null || metadata.ShareCategories.Contains(mediaCategory))
          yield return metadataExtractor.Metadata.MetadataExtractorId;
      }
    }

    public IEnumerable<Guid> GetMetadataExtractorsForMIATypes(IEnumerable<Guid> miaTypeIDs)
    {
      // Return all metadata extractors which fill our desired media item aspects
      return LocalMetadataExtractors.Where(
          extractor => extractor.Value.Metadata.ExtractedAspectTypes.Keys.Intersect(miaTypeIDs).Count() > 0).Select(
          kvp => kvp.Key);
    }

    public IDictionary<Guid, MediaItemAspect> ExtractMetadata(IResourceAccessor mediaItemAccessor,
        IEnumerable<Guid> metadataExtractorIds, bool forceQuickMode)
    {
      ICollection<IMetadataExtractor> extractors = new List<IMetadataExtractor>();
      foreach (Guid extractorId in metadataExtractorIds)
      {
        IMetadataExtractor extractor;
        if (LocalMetadataExtractors.TryGetValue(extractorId, out extractor))
          extractors.Add(extractor);
      }
      return ExtractMetadata(mediaItemAccessor, extractors, forceQuickMode);
    }

    public IDictionary<Guid, MediaItemAspect> ExtractMetadata(IResourceAccessor mediaItemAccessor,
        IEnumerable<IMetadataExtractor> metadataExtractors, bool forceQuickMode)
    {
      IDictionary<Guid, MediaItemAspect> result = new Dictionary<Guid, MediaItemAspect>();
      bool success = false;
      foreach (IMetadataExtractor extractor in metadataExtractors)
      {
        if (extractor.TryExtractMetadata(mediaItemAccessor, result, forceQuickMode))
          success = true;
      }
      return success ? result : null;
    }

    public MediaItem CreateLocalMediaItem(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      const bool forceQuickMode = true;
      IDictionary<Guid, MediaItemAspect> aspects = ExtractMetadata(mediaItemAccessor, metadataExtractorIds, forceQuickMode);
      if (aspects == null)
        return null;
      MediaItemAspect providerResourceAspect;
      if (aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        providerResourceAspect = aspects[ProviderResourceAspect.ASPECT_ID];
      else
        providerResourceAspect = aspects[ProviderResourceAspect.ASPECT_ID] = new MediaItemAspect(
            ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemResolver.LocalSystemId);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, mediaItemAccessor.LocalResourcePath.Serialize());
      return new MediaItem(Guid.Empty, aspects);
    }

    #endregion

    #region IStatus Implementation

    public IList<string> GetStatus()
    {
      List<string> status = new List<string> {"=== MediaManager - MediaProviders"};
      status.AddRange(_providers.Values.Select(provider => string.Format("     Provider '{0}'", provider.Metadata.Name)));
      status.Add("=== MediaManager - MetadataExtractors");
      status.AddRange(_metadataExtractors.Values.Select(metadataExtractor => string.Format("     MetadataExtractor '{0}'", metadataExtractor.Metadata.Name)));
      return status;
    }

    #endregion
  }
}
