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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Services.Players.Builders;
using MediaPortal.UI.Services.Players.Settings;

namespace MediaPortal.UI.Services.Players
{
  /// <summary>
  /// Management class for player builder registrations and active players. Implements the <see cref="IPlayerManager"/> service.
  /// </summary>
  public class PlayerManager : IPlayerManager
  {
    #region Classes

    /// <summary>
    /// Change listener for player builder registrations at the plugin manager. This will dynamically add
    /// new player builders to our pool of builders.
    /// </summary>
    protected class PlayerBuilderRegistrationChangeListener: IItemRegistrationChangeListener
    {
      protected PlayerManager _playerManager;

      public PlayerBuilderRegistrationChangeListener(PlayerManager playerManager)
      {
        _playerManager = playerManager;
      }

      #region IItemRegistrationChangeListener implementation

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        lock (_playerManager.SyncObj)
          foreach (PluginItemMetadata item in items)
          {
            if (item.RegistrationLocation != PLAYERBUILDERS_REGISTRATION_PATH)
              // This check actually is not necessary if everything works correctly, because this
              // item registration listener was only registered for the PLAYERBUILDERS_REGISTRATION_PATH,
              // but you never know...
              continue;
            _playerManager.LoadPlayerBuilder(item.Id);
          }
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // Item removals are handeled by the PlayerBuilderPluginItemStateTracker
      }

      #endregion
    }

    #endregion

    #region Consts

    /// <summary>
    /// Plugin item registration path where skin resources are registered.
    /// </summary>
    public const string PLAYERBUILDERS_REGISTRATION_PATH = "/Players/Builders";

    protected const int VOLUME_CHANGE = 10;

    #endregion

    #region Protected fields

    protected IPluginItemStateTracker _playerBuilderPluginItemStateTracker;
    protected PlayerBuilderRegistrationChangeListener _playerBuilderRegistrationChangeListener;
    internal IList<PlayerSlotController> _slots;
    internal PlayerSlotController _audioPlayerSlotController = null;

    /// <summary>
    /// Maps player builder plugin item ids to player builders.
    /// </summary>
    internal IDictionary<string, PlayerBuilderWrapper> _playerBuilders = new Dictionary<string, PlayerBuilderWrapper>();
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public PlayerManager()
    {
      _slots = new List<PlayerSlotController>();

      // Albert, 2010-12-06: It's too difficult to revoke a player builder. We cannot guarantee that no player of that
      // player builder is currently in use by other threads, so we simply don't allow to revoke them by using a FixedItemStateTracker.
      _playerBuilderPluginItemStateTracker = new FixedItemStateTracker("PlayerManager: PlayerBuilder usage");
      _playerBuilderRegistrationChangeListener = new PlayerBuilderRegistrationChangeListener(this);
      LoadSettings();
      SubscribeToMessages();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      CloseAllSlots();
      UnsubscribeFromMessages();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Adds player builders to our pool when all plugins are initialized.
    /// </summary>
    /// <param name="queue">Queue which sent the message.</param>
    /// <param name="message">Message containing the notification data.</param>
    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PluginManagerMessaging.CHANNEL)
      {
        if (((PluginManagerMessaging.MessageType) message.MessageType) == PluginManagerMessaging.MessageType.PluginsInitialized)
        {
          LoadPlayerBuilders();
          UnsubscribeFromMessages();
        }
      }
    }

    #endregion

    #region Protected & internal methods

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PluginManagerMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    internal void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PlayerManagerSettings settings = settingsManager.Load<PlayerManagerSettings>();
      _volume = settings.Volume;
    }

    /// <summary>
    /// Iterates synchronously over all active player slot controllers. No lock is requested during the given <paramref name="action"/>
    /// so if a lock is needed for the action, this should be done outside or inside the <paramref name="action"/> worker.
    /// </summary>
    /// <param name="action">Action to be executed.</param>
    internal void ForEachInternal(Action<PlayerSlotController> action)
    {
      ICollection<PlayerSlotController> slots;
      lock (_syncObj)
        slots = new List<PlayerSlotController>(_slots);
      foreach (PlayerSlotController psc in slots)
        try
        {
          if (psc.IsClosed)
            continue;
          action(psc);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Problem executing batch action for player slot controller", e);
        }
    }

    #region Player builders

    protected void LoadPlayerBuilders()
    {
      lock (_syncObj)
      {
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        pluginManager.AddItemRegistrationChangeListener(PLAYERBUILDERS_REGISTRATION_PATH, _playerBuilderRegistrationChangeListener);
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(PLAYERBUILDERS_REGISTRATION_PATH))
          LoadPlayerBuilder(itemMetadata.Id);
      }
    }

    protected void LoadPlayerBuilder(string playerBuilderId)
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      PlayerBuilderWrapper playerBuilder;
      try
      {
        playerBuilder = pluginManager.RequestPluginItem<PlayerBuilderWrapper>(PLAYERBUILDERS_REGISTRATION_PATH,
                playerBuilderId, _playerBuilderPluginItemStateTracker);
        if (playerBuilder == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("Could not instantiate player builder with id '{0}'", playerBuilderId);
          return;
        }
      }
      catch (PluginInvalidStateException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Cannot load player builder for player builder id '{0}'", e, playerBuilderId);
        return;
      }
      lock (_syncObj)
        _playerBuilders.Add(playerBuilderId, playerBuilder);
    }

    #endregion

    /// <summary>
    /// Tries to build a player for the given <paramref name="mediaItem"/>.
    /// </summary>
    /// <param name="mediaItem">Media item to be played.</param>
    /// <param name="exceptions">All exceptions which have been thrown by any player builder which was tried.</param>
    /// <returns>Player which was built or <c>null</c>, if no player could be built for the given resource.</returns>
    internal IPlayer BuildPlayer_NoLock(MediaItem mediaItem, out ICollection<Exception> exceptions)
    {
      ICollection<IPlayerBuilder> builders;
      lock (_syncObj)
        builders = new List<IPlayerBuilder>(_playerBuilders.Values.OrderByDescending(w => w.Priority).ThenBy(w => w.Id).Select(w => w.PlayerBuilder));
      exceptions = new List<Exception>();
      foreach (IPlayerBuilder playerBuilder in builders)
      {
        try
        {
          IPlayer player = playerBuilder.GetPlayer(mediaItem);
          if (player != null)
            return player;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Unable to create media player for media item '{0}'", e, mediaItem);
          exceptions.Add(e);
        }
      }
      return null;
    }

    #endregion

    #region IPlayerManager implementation

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public int NumActiveSlots
    {
      get
      {
        lock (_syncObj)
          return _slots.Count;
      }
    }

    public bool Muted
    {
      get { return _isMuted; }
      set
      {
        lock (_syncObj)
        {
          if (_isMuted == value)
            return;
          _isMuted = value;
          ForEachInternal(psc =>
            {
              // Locking is done outside
              psc.IsMuted = _isMuted;
            });
          PlayerManagerMessaging.SendPlayerManagerPlayerMessage(_isMuted ?
              PlayerManagerMessaging.MessageType.PlayersMuted :
              PlayerManagerMessaging.MessageType.PlayersResetMute);
        }
      }
    }

    public int Volume
    {
      get { return _volume; }
      set
      {
        int vol = value;
        lock (_syncObj)
        {
          if (_volume == vol)
            return;
          if (vol < 0)
            vol = 0;
          else if (vol > 100)
            vol = 100;
          _volume = vol;
          ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
          PlayerManagerSettings settings = settingsManager.Load<PlayerManagerSettings>();
          settings.Volume = _volume;
          settingsManager.Save(settings);
        }
        ForEachInternal(psc => psc.CheckAudio_NoLock());
        PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.VolumeChanged);
      }
    }

    public IPlayerSlotController AudioSlotController
    {
      get { return _slots.FirstOrDefault(psc => psc.IsAudioSlot); }
      set
      {
        PlayerSlotController audioSlotController = (PlayerSlotController) AudioSlotController;
        if (audioSlotController == value)
          return;
        if (audioSlotController != null)
          audioSlotController.IsAudioSlot = false;
        audioSlotController = value as PlayerSlotController;
        if (audioSlotController != null)
          audioSlotController.IsAudioSlot = true;
      }
    }

    public ICollection<IPlayerSlotController> PlayerSlotControllers
    {
      get { return _slots.Cast<IPlayerSlotController>().ToList(); }
    }

    public IPlayerSlotController OpenSlot()
    {
      // We don't set a lock because the IsActive property must be set outside the lock. It is no very good solution
      // to avoid the lock completely but I'll risk it here. Concurrent accesses to the player manager should be avoided
      // by organizational means.
      PlayerSlotController result = new PlayerSlotController(this)
        {
            IsMuted = _isMuted,
            IsAudioSlot = false
        };
      if (AudioSlotController == null)
        result.IsAudioSlot = true;
      lock (SyncObj)
        _slots.Add(result);
      return result;
    }

    public void CloseSlot(IPlayerSlotController playerSlotController)
    {
      PlayerSlotController psc = playerSlotController as PlayerSlotController;
      if (psc == null)
        return;
      bool isAudio = psc.IsAudioSlot && !psc.IsClosed;
      PlayerSlotController nextPsc;
      lock (_syncObj)
      {
        int nextIndex = _slots.IndexOf(psc);
        _slots.Remove(psc);
        int numSlots = _slots.Count;
        nextIndex = numSlots == 0 ? 0 : (nextIndex + 1) % numSlots;
        nextPsc = numSlots > nextIndex ? _slots[nextIndex] : null;
      }
      psc.Close_NoLock(); // Must be done outside the lock
      if (isAudio && nextPsc != null)
        nextPsc.IsAudioSlot = true;
    }

    public void CloseAllSlots()
    {
      bool muted = Muted;
      // Avoid switching the sound to the other slot for a short time, in case we close the audio slot when another slot is still available
      Muted = true;
      ForEachInternal(CloseSlot);
      // The IsAudioSlot property is stored in the slot instances itself and thus doesn't need to be updated here
      Muted = muted;
    }

    public void ForEach(Action<IPlayerSlotController> action)
    {
      ForEachInternal(action);
    }

    public void VolumeUp()
    {
      Volume += VOLUME_CHANGE;
    }

    public void VolumeDown()
    {
      Volume -= VOLUME_CHANGE;
    }

    #endregion
  }
}
