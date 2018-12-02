#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.Media.MediaLists;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaListModel : BaseTimerControlledModel
  {
    #region MediaListProviderDictionary

    public class ProviderEventArgs : EventArgs
    {
      protected IMediaListProvider _provider;

      public ProviderEventArgs(IMediaListProvider provider)
      {
        _provider = provider;
      }

      public IMediaListProvider Provider
      {
        get { return _provider; }
      }
    }

    public class MediaListProviderDictionary
    {
      protected class ProviderWrapper
      {
        protected IMediaListProvider _provider;
        protected DateTime _nextUpdateTime;

        public ProviderWrapper(IMediaListProvider provider)
        {
          _provider = provider;
          _nextUpdateTime = DateTime.MinValue;
        }

        public IMediaListProvider Provider
        {
          get { return _provider; }
        }

        public DateTime NextUpdateTime
        {
          get { return _nextUpdateTime; }
          set { _nextUpdateTime = value; }
        }
      }

      public const int UPDATE_THRESHOLD_SEC = 10;

      protected object _syncObj = new object();
      protected IDictionary<string, ProviderWrapper> _providers = new Dictionary<string, ProviderWrapper>();
      protected IDictionary<string, IMediaListProvider> _enabledProviders = new Dictionary<string, IMediaListProvider>();

      public event EventHandler<ProviderEventArgs> ProviderRequested;

      protected virtual void OnProviderRequested(IMediaListProvider provider)
      {
        ProviderRequested?.Invoke(this, new ProviderEventArgs(provider));
      }

      public bool IsEnabled(string key)
      {
        lock (_syncObj)
          return _enabledProviders.ContainsKey(key);
      }

      public void Add(string key, IMediaListProvider value)
      {
        lock (_syncObj)
          _providers.Add(key, new ProviderWrapper(value));
      }

      public bool ContainsKey(string key)
      {
        lock (_syncObj)
          return _providers.ContainsKey(key);
      }

      public bool Remove(string key)
      {
        lock (_syncObj)
        {
          _enabledProviders.Remove(key);
          return _providers.Remove(key);
        }
      }

      public IMediaListProvider this[string key]
      {
        get { return GetProvider(key); }
        set
        {
          lock (_syncObj)
            _providers[key] = new ProviderWrapper(value);
        }
      }

      public IEnumerable<IMediaListProvider> EnabledProviders
      {
        get
        {
          lock (_syncObj)
            return _enabledProviders.Values.ToList();
        }
      }

      protected IMediaListProvider GetProvider(string key)
      {
        ProviderWrapper providerWrapper;
        bool update;
        lock (_syncObj)
        {
          if (!_providers.TryGetValue(key, out providerWrapper))
            return null;
          if (!_enabledProviders.ContainsKey(key))
          {
            ServiceRegistration.Get<ILogger>().Info("Enabling IMediaListProvider '{0}'", key);
            _enabledProviders[key] = providerWrapper.Provider;
          }
          update = providerWrapper.NextUpdateTime < DateTime.Now;
          if (update)
            providerWrapper.NextUpdateTime = DateTime.Now.AddSeconds(UPDATE_THRESHOLD_SEC);
        }

        if (update)
          OnProviderRequested(providerWrapper.Provider);
        return providerWrapper.Provider;
      }
    }

    #endregion

    #region Consts

    // Global ID definitions and references
    public const string MEDIA_LIST_MODEL_ID_STR = "6121E6CC-EB66-4ABC-8AA0-D952B64C0414";

    // ID variables
    public static readonly Guid MEDIA_LIST_MODEL_ID = new Guid(MEDIA_LIST_MODEL_ID_STR);

    protected readonly AbstractProperty _queryLimitProperty;
    protected bool _updatePending = true;
    protected bool _importUpdatePending = false;
    protected bool _playbackUpdatePending = false;
    protected IPluginItemStateTracker _providerPluginItemStateTracker;
    protected MediaListProviderDictionary _listProviders;
    protected DateTime _nextGet = DateTime.MinValue;
    protected DateTime _nextMinute = DateTime.MinValue;

    #endregion

    public const int DEFAULT_QUERY_LIMIT = 5;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public AbstractProperty LimitProperty { get { return _queryLimitProperty; } }

    public int Limit
    {
      get { return (int)_queryLimitProperty.GetValue(); }
      set { _queryLimitProperty.SetValue(value); }
    }

    public MediaListProviderDictionary Lists
    {
      get { return _listProviders; }
    }

    public MediaListModel()
      : base(true, 1000)
    {
      _queryLimitProperty = new WProperty(typeof(int), DEFAULT_QUERY_LIMIT);
      _queryLimitProperty.Attach(OnQueryLimitChanged);

      InitProviders();
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(ServerConnectionMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(ContentDirectoryMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(SharesMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(PlayerManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            _updatePending = true; //Update all
            break;
        }
      }
      else if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.ShareImportCompleted:
            _importUpdatePending = true; //Update latest added
            break;
        }
      }
      else if (message.ChannelName == SharesMessaging.CHANNEL)
      {
        SharesMessaging.MessageType messageType = (SharesMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SharesMessaging.MessageType.ShareChanged:
          case SharesMessaging.MessageType.ShareRemoved:
            _updatePending = true; //Update all
            break;
        }
      }
      else if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType =
            (PlayerManagerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerStopped:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
            _playbackUpdatePending = true; //Update most played and last played
            break;
        }
      }
    }

    private void OnQueryLimitChanged(AbstractProperty property, object oldValue)
    {
      _updatePending = true;
    }

    protected override void Update()
    {
      Task.Run(UpdateAllProvidersAsync);
    }

    private void OnProviderRequested(object sender, ProviderEventArgs e)
    {
      Task.Run(() => UpdateProviderAsync(e.Provider, Limit, UpdateReason.Forced));
    }

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (_listProviders != null)
          return;
        _listProviders = new MediaListProviderDictionary();
        _listProviders.ProviderRequested += OnProviderRequested;

        _providerPluginItemStateTracker = new FixedItemStateTracker("Media Lists - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(MediaListProviderBuilder.MEDIA_LIST_PROVIDER_PATH))
        {
          try
          {
            MediaListProviderRegistration providerRegistration = pluginManager.RequestPluginItem<MediaListProviderRegistration>(MediaListProviderBuilder.MEDIA_LIST_PROVIDER_PATH, itemMetadata.Id, _providerPluginItemStateTracker);
            if (providerRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Media List provider with id '{0}'", itemMetadata.Id);
            else
            {
              IMediaListProvider provider = Activator.CreateInstance(providerRegistration.ProviderClass) as IMediaListProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IMediaListProvider instance of class {0}", providerRegistration.ProviderClass);
              if (_listProviders.ContainsKey(providerRegistration.Key))
              {
                //The default providers cannot replace existing providers
                if (provider.GetType().Assembly != System.Reflection.Assembly.GetExecutingAssembly())
                {
                  //Replace the provider
                  _listProviders[providerRegistration.Key] = provider;
                  ServiceRegistration.Get<ILogger>().Info("Successfully replaced Media List '{1}' with provider '{0}' (Id '{2}')", itemMetadata.Attributes["ClassName"], itemMetadata.Attributes["Key"], itemMetadata.Id);
                }
              }
              else
              {
                _listProviders.Add(providerRegistration.Key, provider);
                ServiceRegistration.Get<ILogger>().Info("Successfully activated Media List '{1}' with provider '{0}' (Id '{2}')", itemMetadata.Attributes["ClassName"], itemMetadata.Attributes["Key"], itemMetadata.Id);
              }
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IMediaListProvider extension with id '{0}'", e, itemMetadata.Id);
          }
        }
      }
    }

    public async Task<bool> UpdateAllProvidersAsync()
    {
      try
      {
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (contentDirectory == null)
        {
          _updatePending = true;
          return false;
        }

        UpdateReason updateReason = UpdateReason.None;
        if (_updatePending) updateReason |= UpdateReason.Forced;
        if (_importUpdatePending) updateReason |= UpdateReason.ImportComplete;
        if (_playbackUpdatePending) updateReason |= UpdateReason.PlaybackComplete;
        if (_nextMinute < DateTime.Now) updateReason |= UpdateReason.PeriodicMinute;
        if (updateReason == UpdateReason.None)
          return false;

        _updatePending = false;
        _importUpdatePending = false;
        _playbackUpdatePending = false;
        _nextMinute = DateTime.Now.AddMinutes(1);

        int maxItems = Limit;
        foreach (var provider in _listProviders.EnabledProviders)
          await UpdateProviderAsync(provider, maxItems, updateReason);

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Media List", ex);
        return false;
      }
    }

    public async Task<bool> UpdateProviderAsync(IMediaListProvider provider, int maxItems, UpdateReason updateReason)
    {
      try
      {
        return await provider.UpdateItemsAsync(maxItems, updateReason);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Media List {0}", provider.GetType().Name, ex);
        return false;
      }
    }
  }
}
