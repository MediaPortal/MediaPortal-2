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
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Core.MediaManagement
{
  public delegate void NotifyMediaProviderChangeDelegate(IMediaProvider mediaProvider);
  public delegate void NotifyMetadataExtractorChangeDelegate(IMetadataExtractor metadataExtractor);

  /// <summary>
  /// This is the base class for client and server media managers.
  /// It contains the functionality to load media providers and metadata extractors.
  /// </summary>
  public class MediaManagerBase
  {
    #region Constants

    // Constants will be moved to some constants class
    protected const string MEDIA_PROVIDERS_PLUGIN_LOCATION = "/Media/MediaProviders";
    protected const string METADATA_EXTRACTORS_PLUGIN_LOCATION = "/Media/MetadataExtractors";

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
              item.RegistrationLocation, item.Id, new FixedItemStateTracker());
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
              item.RegistrationLocation, item.Id, new FixedItemStateTracker());
          _parent.RegisterProvider(mediaProvider);
        }
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // TODO: Make MediaProviders removable?
      }
    }

    #region Protected fields

    protected bool _providerPluginsLoaded = false;
    protected bool _metadataExtractorPluginsLoaded = false;
    protected MediaProviderPluginItemChangeListener _mediaProvidersPluginItemChangeListener;
    protected MetadataExtractorPluginItemChangeListener _metadataExtractorsPluginItemChangeListener;
    protected readonly IDictionary<Guid, IMediaProvider> _providers;
    protected readonly IDictionary<Guid, IMetadataExtractor> _metadataExtractors;

    #endregion

    #region Ctor

    public MediaManagerBase()
    {
      _mediaProvidersPluginItemChangeListener = new MediaProviderPluginItemChangeListener(this);
      _metadataExtractorsPluginItemChangeListener = new MetadataExtractorPluginItemChangeListener(this);
      _providers = new Dictionary<Guid, IMediaProvider>();
      _metadataExtractors = new Dictionary<Guid, IMetadataExtractor>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Initializes media providers, metadata extractors and internal structures.
    /// </summary>
    public virtual void Initialize()
    {
      RegisterPluginItemListeners();
    }

    /// <summary>
    /// Cleans up the runtime data of the media manager.
    /// </summary>
    public virtual void Dispose()
    {
      UnregisterPluginItemListeners();
    }

    /// <summary>
    /// Collection of all registered local media providers, organized as a dictionary of
    /// (GUID; provider) mappings.
    /// This media provider collection is the proposed entry point to get access to physical media
    /// files.
    /// </summary>
    public IDictionary<Guid, IMediaProvider> LocalMediaProviders
    {
      get
      {
        CheckProviderPluginsLoaded();
        return _providers;
      }
    }

    /// <summary>
    /// Collection of all registered local metadata extractors, organized as a dictionary of
    /// (GUID; metadata extractor) mappings.
    /// </summary>
    public IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors
    {
      get
      {
        CheckMetadataExtractorPluginsLoaded();
        return _metadataExtractors;
      }
    }

    #endregion

    #region Public events

    /// <summary>
    /// Will be raised when a media provider was added to the system.
    /// </summary>
    public event NotifyMediaProviderChangeDelegate MediaProviderAdded;

    /// <summary>
    /// Will be raised when a media provider was removed from the system.
    /// </summary>
    public event NotifyMediaProviderChangeDelegate MediaProviderRemoved;

    /// <summary>
    /// Will be raised when a metadata extractor was added to the system.
    /// </summary>
    public event NotifyMetadataExtractorChangeDelegate MetadataExtractorAdded;

    /// <summary>
    /// Will be raised when a metadata extractor was removed from the system.
    /// </summary>
    public event NotifyMetadataExtractorChangeDelegate MetadataExtractorRemoved;

    #endregion

    #region Protected methods

    protected void InvokeMediaProviderAdded(IMediaProvider mediaProvider)
    {
      NotifyMediaProviderChangeDelegate Delegate = MediaProviderAdded;
      if (Delegate != null) Delegate(mediaProvider);
    }

    protected void InvokeMediaProviderRemoved(IMediaProvider mediaProvider)
    {
      NotifyMediaProviderChangeDelegate Delegate = MediaProviderRemoved;
      if (Delegate != null) Delegate(mediaProvider);
    }

    protected void InvokeMetadataExtractorAdded(IMetadataExtractor metadataExtractor)
    {
      NotifyMetadataExtractorChangeDelegate Delegate = MetadataExtractorAdded;
      if (Delegate != null) Delegate(metadataExtractor);
    }

    protected void InvokeMetadataExtractorRemoved(IMetadataExtractor metadataExtractor)
    {
      NotifyMetadataExtractorChangeDelegate Delegate = MetadataExtractorRemoved;
      if (Delegate != null) Delegate(metadataExtractor);
    }

    protected void RegisterProvider(IMediaProvider provider)
    {
      _providers.Add(provider.Metadata.MediaProviderId, provider);
      InvokeMediaProviderAdded(provider);
    }

    protected void RegisterMetadataExtractor(IMetadataExtractor metadataExtractor)
    {
      _metadataExtractors.Add(metadataExtractor.Metadata.MetadataExtractorId, metadataExtractor);
      InvokeMetadataExtractorAdded(metadataExtractor);
    }

    /// <summary>
    /// Checks that the provider plugins are loaded.
    /// </summary>
    protected void CheckProviderPluginsLoaded()
    {
      if (_providerPluginsLoaded)
        return;
      foreach (IMediaProvider provider in ServiceScope.Get<IPluginManager>().RequestAllPluginItems<IMediaProvider>(
          MEDIA_PROVIDERS_PLUGIN_LOCATION, new FixedItemStateTracker())) // TODO: Make providers removable
        RegisterProvider(provider);
      _providerPluginsLoaded = true;
    }

    /// <summary>
    /// Checks that the provider plugins are loaded.
    /// </summary>
    protected void CheckMetadataExtractorPluginsLoaded()
    {
      if (_metadataExtractorPluginsLoaded)
        return;
      foreach (IMetadataExtractor metadataExtractor in ServiceScope.Get<IPluginManager>().RequestAllPluginItems<IMetadataExtractor>(
          METADATA_EXTRACTORS_PLUGIN_LOCATION, new FixedItemStateTracker())) // TODO: Make metadata extractors removable
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
