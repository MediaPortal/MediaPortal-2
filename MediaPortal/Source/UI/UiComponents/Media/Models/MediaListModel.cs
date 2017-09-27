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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.Utilities.Collections;
using MediaPortal.Common.Threading;
using MediaPortal.UiComponents.Media.MediaLists;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Shares;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Players;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaListModel : BaseTimerControlledModel
  {
    public class MediaListProviderDictionary : SafeDictionary<string, IMediaListProvider>
    {
      private IDictionary<string, bool> _enabledElements = new Dictionary<string, bool>();

      public Action OnProviderRequested;

      public bool IsEnabled(string key)
      {
        return _enabledElements.ContainsKey(key) && _enabledElements[key];
      }

      public new void Add(KeyValuePair<string, IMediaListProvider> item)
      {
        _enabledElements.Add(item.Key, false);
        _elements.Add(item);
      }

      public new void Add(string key, IMediaListProvider value)
      {
        _enabledElements.Add(key, false);
        _elements.Add(key, value);
      }

      public new bool Remove(KeyValuePair<string, IMediaListProvider> item)
      {
        _enabledElements.Remove(item.Key);
        return _elements.Remove(item);
      }

      public new bool Remove(string key)
      {
        _enabledElements.Remove(key);
        return _elements.Remove(key);
      }

      public new IMediaListProvider this[string key]
      {
        get
        {
          if (_elements.ContainsKey(key))
          {
            if (_enabledElements.ContainsKey(key))
              _enabledElements[key] = true;
            OnProviderRequested?.Invoke();
            return _elements[key];
          }
          return null;
        }
        set { _elements[key] = value; }
      }
    }

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
    public const int GETTER_THRESHOLD_SEC = 10;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public AbstractProperty LimitProperty { get { return _queryLimitProperty; } }

    public int Limit
    {
      get { return (int)_queryLimitProperty.GetValue(); }
      set { _queryLimitProperty.SetValue(value); }
    }

    public MediaListProviderDictionary Lists { get; }

    public MediaListModel()
      : base(false, 1000)
    {
      _queryLimitProperty = new WProperty(typeof(int), DEFAULT_QUERY_LIMIT);
      _queryLimitProperty.Attach(OnQueryLimitChanged);

      InitProviders();
      SubscribeToMessages();
      ISystemStateService systemStateService = ServiceRegistration.Get<ISystemStateService>();
      if (systemStateService.CurrentState == SystemState.Running)
        StartTimer();
    }

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
          SystemMessaging.CHANNEL,
          ServerConnectionMessaging.CHANNEL,
          ContentDirectoryMessaging.CHANNEL,
          SharesMessaging.CHANNEL,
          PlayerManagerMessaging.CHANNEL,
        });
      messageQueue.MessageReceived += OnMessageReceived;
      messageQueue.Start();
      lock (_syncObj)
        _messageQueue = messageQueue;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState state = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
          switch (state)
          {
            case SystemState.Running:
              StartTimer();
              break;
            case SystemState.ShuttingDown:
              StopTimer();
              break;
          }
        }
      }
      else if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
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
      if (property.GetValue() != oldValue)
        _updatePending = true;
    }

    protected override void Update()
    {
      UpdateItems();
    }

    private void OnProviderRequested()
    {
      if (_nextGet < DateTime.Now)
      {
        _updatePending = true;
        _nextGet = DateTime.Now.AddSeconds(GETTER_THRESHOLD_SEC);
      }
    }

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (_listProviders != null)
          return;
        _listProviders = new MediaListProviderDictionary();
        _listProviders.OnProviderRequested = OnProviderRequested;

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

    public bool UpdateItems()
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

        SetLayout();

        foreach (var provider in _listProviders.Where(p => _listProviders.IsEnabled(p.Key)).Select(p => p.Value))
        {
          UpdateAsync(provider, updateReason);
        }

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Media List", ex);
        return false;
      }
    }

    protected void UpdateAsync(IMediaListProvider provider, UpdateReason updateReason)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => provider.UpdateItems(Limit, updateReason));
    }

    protected void SetLayout()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      ViewModeModel vwm = workflowManager.GetModel(ViewModeModel.VM_MODEL_ID) as ViewModeModel;
      if (vwm != null)
      {
        vwm.LayoutType = LayoutType.GridLayout;
        vwm.LayoutSize = LayoutSize.Medium;
      }
    }
  }
}
