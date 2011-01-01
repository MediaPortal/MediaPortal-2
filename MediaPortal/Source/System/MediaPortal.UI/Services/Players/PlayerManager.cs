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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Services.Players
{
  internal delegate void PlayerSlotWorkerInternalDelegate(PlayerSlotController slotController);

  /// <summary>
  /// Management class for player builder registrations and active players.
  /// </summary>
  public class PlayerManager : IPlayerManager
  {
    #region Classes

    /// <summary>
    /// Plugin item state tracker which allows player plugins to be revoked. This will check if there are
    /// active players. If stopped, this will automatically remove the player builder registration at the player manager.
    /// </summary>
    protected class PlayerBuilderPluginItemStateTracker: IPluginItemStateTracker
    {
      protected PlayerManager _playerManager;

      public PlayerBuilderPluginItemStateTracker(PlayerManager playerManager)
      {
        _playerManager = playerManager;
      }

      public string UsageDescription
      {
        get { return "PlayerManager: PlayerBuilder usage"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        // Albert, 2010-12-06: It's too difficult to revoke a player builder. We cannot guarantee that no player of that
        // player builder is currently in use by other threads, so we simply don't allow to revoke them.
        return false;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        // Nothing to do
      }

      public void Continue(PluginItemRegistration itemRegistration)
      {
        // Nothing to do
      }
    }

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

    protected PlayerBuilderPluginItemStateTracker _playerBuilderPluginItemStateTracker;
    protected PlayerBuilderRegistrationChangeListener _playerBuilderRegistrationChangeListener;
    internal PlayerSlotController[] _slots;

    /// <summary>
    /// Maps player builder plugin item ids to player builders.
    /// </summary>
    internal IDictionary<string, IPlayerBuilder> _playerBuilders = new Dictionary<string, IPlayerBuilder>();
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public PlayerManager()
    {
      _slots = new PlayerSlotController[] {
          new PlayerSlotController(this, PlayerManagerConsts.PRIMARY_SLOT),
          new PlayerSlotController(this, PlayerManagerConsts.SECONDARY_SLOT)
      };
      _playerBuilderPluginItemStateTracker = new PlayerBuilderPluginItemStateTracker(this);
      _playerBuilderRegistrationChangeListener = new PlayerBuilderRegistrationChangeListener(this);
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

    /// <summary>
    /// Iterates synchronously over all player slot controllers (active and inactive). No lock is being requested so if
    /// a lock is needed, this should be done outside or inside the <paramref name="execute"/> worker.
    /// </summary>
    /// <param name="execute"></param>
    internal void ForEachInternal(PlayerSlotWorkerInternalDelegate execute)
    {
      foreach (PlayerSlotController psc in _slots)
        try
        {
          execute(psc);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Problem executing batch action for player slot controller {0}", e, psc.SlotIndex);
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
      IPlayerBuilder playerBuilder = pluginManager.RequestPluginItem<IPlayerBuilder>(PLAYERBUILDERS_REGISTRATION_PATH,
              playerBuilderId, _playerBuilderPluginItemStateTracker);
      if (playerBuilder == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("Could not instantiate player builder with id '{0}'", playerBuilderId);
        return;
      }
      lock (_syncObj)
        _playerBuilders.Add(playerBuilderId, playerBuilder);
    }

    #endregion

    protected void CleanupSlotOrder()
    {
      lock (_syncObj)
        if (!_slots[PlayerManagerConsts.PRIMARY_SLOT].IsActive && _slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
          SwitchSlots();
    }

    /// <summary>
    /// Tries to build a player for the media resource specified by the given resource <paramref name="locator"/>
    /// and <paramref name="mimeType"/>.
    /// </summary>
    /// <param name="locator">Resource locator to access the to-be-played media item.</param>
    /// <param name="mimeType">Mime type of the media item to be played. May be <c>null</c>.</param>
    /// <returns>Player which was built or <c>null</c>, if no player could be built for the given parameters.</returns>
    internal IPlayer BuildPlayer_NoLock(IResourceLocator locator, string mimeType)
    {
      ICollection<IPlayerBuilder> builders;
      lock (_syncObj)
        builders = new List<IPlayerBuilder>(_playerBuilders.Values);
      foreach (IPlayerBuilder playerBuilder in builders)
      {
        try
        {
          IPlayer player = playerBuilder.GetPlayer(locator, mimeType);
          if (player != null)
            return player;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Unable to create media player for media resource '{0}'", e, locator);
        }
      }
      return null;
    }

    internal PlayerSlotController GetPlayerSlotControllerInternal(int slotIndex)
    {
      if (slotIndex < 0 || slotIndex > 1)
        return null;
      lock (_syncObj)
        return _slots[slotIndex];
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
        int result = 0;
        lock (_syncObj)
          foreach (PlayerSlotController psc in _slots)
            if (psc.IsActive)
              result++;
        return result;
      }
    }

    public IPlayer this[int slotIndex]
    {
      get
      {
        lock (_syncObj)
        {
          PlayerSlotController psc = GetPlayerSlotControllerInternal(slotIndex);
          if (psc == null)
            return null;
          return psc.IsActive ? psc.CurrentPlayer : null;
        }
      }
    }

    public int AudioSlotIndex
    {
      get
      {
        lock (_syncObj)
          for (int i = 0; i < 2; i++)
          {
            PlayerSlotController psc = _slots[i];
            if (psc.IsActive && psc.IsAudioSlot)
              return i;
          }
        return -1;
      }
      set
      {
        lock (_syncObj)
        {
          int oldAudioSlotIndex = AudioSlotIndex;
          if (oldAudioSlotIndex == value)
            return;
          PlayerSlotController currentAudioSlot = oldAudioSlotIndex == -1 ? null : _slots[oldAudioSlotIndex];
          PlayerSlotController newAudioSlot = GetPlayerSlotControllerInternal(value);
          if (newAudioSlot == null)
            return;
          if (!newAudioSlot.IsActive)
            // Don't move the audio slot to an inactive player slot
            return;
          if (currentAudioSlot != null)
            currentAudioSlot.IsAudioSlot = false;
          // Message will be sent by the next command
          newAudioSlot.IsAudioSlot = true;
        }
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
              if (psc.IsActive)
                psc.IsMuted = _isMuted;
            });
          if (_isMuted)
            PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayersMuted);
          else
            PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayersResetMute);
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
        }
        ForEachInternal(psc =>
          {
            // Lock is acquired outside
            if (psc.IsActive)
              psc.Volume = vol;
          });
        PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.VolumeChanged);
      }
    }

    public IPlayerSlotController GetPlayerSlotController(int slotIndex)
    {
      return GetPlayerSlotControllerInternal(slotIndex);
    }

    public bool OpenSlot(out int slotIndex, out IPlayerSlotController slotController)
    {
      lock (_syncObj)
      {
        slotIndex = -1;
        slotController = null;
        // Find a free slot
        if (!_slots[PlayerManagerConsts.PRIMARY_SLOT].IsActive)
          slotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        else if (!_slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
          slotIndex = PlayerManagerConsts.SECONDARY_SLOT;
        else
          return false;
        PlayerSlotController psc = _slots[slotIndex];
        psc.IsActive = true;
        psc.IsMuted = _isMuted;
        psc.Volume = _volume;
        psc.IsAudioSlot = false;
        if (AudioSlotIndex == -1)
          AudioSlotIndex = slotIndex;
        slotController = psc;
        return true;
      }
    }

    public void CloseSlot(int slotIndex)
    {
      CloseSlot(GetPlayerSlotControllerInternal(slotIndex));
    }

    public void CloseSlot(IPlayerSlotController playerSlotController)
    {
      PlayerSlotController psc = playerSlotController as PlayerSlotController;
      if (psc == null)
        return;
      lock (_syncObj)
      {
        bool isAudio = psc.IsActive && psc.IsAudioSlot;
        psc.IsActive = false;
        CleanupSlotOrder();
        if (isAudio)
          AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
      }
    }

    public void CloseAllSlots()
    {
      lock (_syncObj)
      {
        bool muted = Muted;
        // Avoid switching the sound to the other slot for a short time, in case we close the audio slot first
        Muted = true;
        foreach (PlayerSlotController psc in _slots)
          psc.IsActive = false;
        // The audio slot property is stored in the slot instances itself and thus doesn't need to be updated here
        Muted = muted;
      }
    }

    public void SwitchSlots()
    {
      lock (_syncObj)
      {
        if (!_slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
          // Don't move an inactive player slot to the primary slot index
          return;
        PlayerSlotController tmp = _slots[PlayerManagerConsts.PRIMARY_SLOT];
        _slots[PlayerManagerConsts.PRIMARY_SLOT] = _slots[PlayerManagerConsts.SECONDARY_SLOT];
        _slots[PlayerManagerConsts.SECONDARY_SLOT] = tmp;
        for (int i = 0; i < 2; i++)
          _slots[i].SlotIndex = i;
        // Audio slot index changes automatically as it is stored in the slot instance itself
        PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotsChanged);
      }
    }

    public void ForEach(PlayerSlotWorkerDelegate execute)
    {
      ForEachInternal(psc => execute(psc));
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
