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
using MediaPortal.UI.ContentLists;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.UI.Presentation.Models
{
  public abstract class BaseContentListModel : BaseTimerControlledModel, IContentListModel
  {
    public const int DEFAULT_QUERY_LIMIT = 5;

    #region Consts

    protected readonly AbstractProperty _limitProperty;
    protected bool _updatePending = true;
    protected bool _importUpdatePending = false;
    protected bool _playbackUpdatePending = false;
    protected bool _mediaItemUpdatePending = false;
    protected IPluginItemStateTracker _providerPluginItemStateTracker;
    protected ContentListProviderDictionary _listProviders;
    protected DateTime _nextGet = DateTime.MinValue;
    protected DateTime _nextMinute = DateTime.MinValue;
    protected Guid _currentUserId;

    private bool _providersInititialized = false;

    #endregion

    public AbstractProperty LimitProperty { get { return _limitProperty; } }

    public int Limit
    {
      get { return (int)_limitProperty.GetValue(); }
      set { _limitProperty.SetValue(value); }
    }

    public ContentListProviderDictionary Lists
    {
      get { return _listProviders; }
    }

    protected virtual bool ServerConnectionRequired
    {
      get { return true; }
    }

    protected virtual bool UpdateRequired
    {
      get { return false; }
    }

    public BaseContentListModel(string providerPath)
      : base(true, 1000)
    {
      _limitProperty = new WProperty(typeof(int), DEFAULT_QUERY_LIMIT);
      _limitProperty.Attach(OnLimitChanged);

      InitProviders(providerPath);
      SubscribeToMessages();
    }

    protected override void Update()
    {
      Task.Run(UpdateAllProvidersAsync);
    }

    protected virtual async Task<bool> UpdateAllProvidersAsync()
    {
      try
      {
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (ServerConnectionRequired && contentDirectory == null)
        {
          //Server not connected. Request update when it does
          _updatePending = true;
          return false;
        }

        IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>(false);

        UpdateReason updateReason = UpdateReason.None;
        if (_updatePending || UpdateRequired) updateReason |= UpdateReason.Forced;
        if (_importUpdatePending) updateReason |= UpdateReason.ImportComplete;
        if (_playbackUpdatePending) updateReason |= UpdateReason.PlaybackComplete;
        if (_nextMinute < DateTime.UtcNow) updateReason |= UpdateReason.PeriodicMinute;
        if (_mediaItemUpdatePending) updateReason |= UpdateReason.MediaItemChanged;
        if (userProfileDataManagement?.CurrentUser != null && _currentUserId != userProfileDataManagement.CurrentUser.ProfileId) updateReason |= UpdateReason.UserChanged;
        if (updateReason == UpdateReason.None)
          return false;

        _updatePending = false;
        _importUpdatePending = false;
        _playbackUpdatePending = false;
        _mediaItemUpdatePending = false;
        if (userProfileDataManagement?.CurrentUser != null)
          _currentUserId = userProfileDataManagement.CurrentUser.ProfileId;
        _nextMinute = DateTime.UtcNow.AddMinutes(1);

        int maxItems = Limit;
        foreach (var provider in _listProviders.EnabledProviders)
          await UpdateProviderAsync(provider, maxItems, updateReason);

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Content List", ex);
        return false;
      }
    }

    protected virtual async Task<bool> UpdateProviderAsync(IContentListProvider provider, int maxItems, UpdateReason updateReason)
    {
      try
      {
        return await provider.UpdateItemsAsync(maxItems, updateReason);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Content List {0}", provider.GetType().Name, ex);
        return false;
      }
    }

    private void InitProviders(string providerPath)
    {
      lock (_syncObj)
      {
        if (_providersInititialized)
          return;

        _providersInititialized = true;
        _listProviders = new ContentListProviderDictionary();
        _listProviders.ProviderRequested += OnProviderRequested;

        _providerPluginItemStateTracker = new FixedItemStateTracker($"Content Lists - Provider registration for path {providerPath}");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(providerPath))
        {
          try
          {
            ContentListProviderRegistration providerRegistration = pluginManager.RequestPluginItem<ContentListProviderRegistration>(providerPath, itemMetadata.Id, _providerPluginItemStateTracker);
            if (providerRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Content List provider with id '{0}'", itemMetadata.Id);
            else
            {
              IContentListProvider provider = Activator.CreateInstance(providerRegistration.ProviderClass) as IContentListProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IContentListProvider instance of class {0}", providerRegistration.ProviderClass);
              if (_listProviders.ContainsKey(providerRegistration.Key))
              {
                //The default providers cannot replace existing providers
                if (provider.GetType().Assembly != System.Reflection.Assembly.GetExecutingAssembly())
                {
                  //Replace the provider
                  _listProviders[providerRegistration.Key] = provider;
                  ServiceRegistration.Get<ILogger>().Info("Successfully replaced Content List '{1}' with provider '{0}' (Id '{2}')", itemMetadata.Attributes["ClassName"], itemMetadata.Attributes["Key"], itemMetadata.Id);
                }
              }
              else
              {
                _listProviders.Add(providerRegistration.Key, provider);
                ServiceRegistration.Get<ILogger>().Info("Successfully activated Content List '{1}' with provider '{0}' (Id '{2}')", itemMetadata.Attributes["ClassName"], itemMetadata.Attributes["Key"], itemMetadata.Id);
              }
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IContentListProvider extension with id '{0}'", e, itemMetadata.Id);
          }
        }
      }
    }

    private void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(ServerConnectionMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(ContentDirectoryMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(SharesMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(PlayerManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
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
          case ContentDirectoryMessaging.MessageType.MediaItemChanged:
            if ((ContentDirectoryMessaging.MediaItemChangeType)message.MessageData[ContentDirectoryMessaging.MEDIA_ITEM_CHANGE_TYPE] != ContentDirectoryMessaging.MediaItemChangeType.None)
              _mediaItemUpdatePending = true;
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

    private void OnProviderRequested(object sender, ProviderEventArgs e)
    {
      Task.Run(() => UpdateProviderAsync(e.Provider, Limit, UpdateReason.Forced));
    }

    private void OnLimitChanged(AbstractProperty property, object oldValue)
    {
      _updatePending = true;
    }
  }

  #region ContentListProviderDictionary

  public class ProviderEventArgs : EventArgs
  {
    protected IContentListProvider _provider;

    public ProviderEventArgs(IContentListProvider provider)
    {
      _provider = provider;
    }

    public IContentListProvider Provider
    {
      get { return _provider; }
    }
  }

  public class ContentListProviderDictionary
  {
    protected class ProviderWrapper
    {
      protected IContentListProvider _provider;
      protected DateTime _nextUpdateTime;

      public ProviderWrapper(IContentListProvider provider)
      {
        _provider = provider;
        _nextUpdateTime = DateTime.MinValue;
      }

      public IContentListProvider Provider
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
    protected IDictionary<string, IContentListProvider> _enabledProviders = new Dictionary<string, IContentListProvider>();

    public event EventHandler<ProviderEventArgs> ProviderRequested;

    protected virtual void OnProviderRequested(IContentListProvider provider)
    {
      ProviderRequested?.Invoke(this, new ProviderEventArgs(provider));
    }

    public bool IsEnabled(string key)
    {
      lock (_syncObj)
        return _enabledProviders.ContainsKey(key);
    }

    public void Add(string key, IContentListProvider value)
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

    public IContentListProvider this[string key]
    {
      get { return GetProvider(key); }
      set
      {
        lock (_syncObj)
          _providers[key] = new ProviderWrapper(value);
      }
    }

    public IEnumerable<IContentListProvider> EnabledProviders
    {
      get
      {
        lock (_syncObj)
          return _enabledProviders.Values.ToList();
      }
    }

    protected IContentListProvider GetProvider(string key)
    {
      ProviderWrapper providerWrapper;
      bool update;
      lock (_syncObj)
      {
        if (!_providers.TryGetValue(key, out providerWrapper))
          return null;
        if (!_enabledProviders.ContainsKey(key))
        {
          ServiceRegistration.Get<ILogger>().Info("Enabling IContentListProvider '{0}'", key);
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
}
