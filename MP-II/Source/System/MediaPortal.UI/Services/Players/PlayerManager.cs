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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Players;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.Players
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
        lock (_playerManager.SyncObj)
        {
          PlayerBuilderRegistration builderRegistration = _playerManager.GetPlayerBuilderRegistration(itemRegistration.Metadata.Id);
          if (builderRegistration == null)
            return true;
          if (builderRegistration.IsInUse)
            return false;
          builderRegistration.Suspended = true;
          return true;
        }
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _playerManager.RevokePlayerBuilder(itemRegistration.Metadata.Id);
      }

      public void Continue(PluginItemRegistration itemRegistration)
      {
        lock (_playerManager.SyncObj)
        {
          PlayerBuilderRegistration builderRegistration = _playerManager.GetPlayerBuilderRegistration(itemRegistration.Metadata.Id);
          if (builderRegistration == null)
            return;
          builderRegistration.Suspended = false;
        }
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
            _playerManager.RequestPlayerBuilder(item.Id);
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
    internal IDictionary<string, PlayerBuilderRegistration> _playerBuilders = new Dictionary<string, PlayerBuilderRegistration>();
    internal PlayerSlotController[] _slots;
    protected int _volume = 100;
    protected bool _isMuted = false;

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
      ServiceScope.Get<IMessageBroker>().Register_Async(PluginManagerMessaging.QUEUE, OnPluginManagerMessageReceived);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      CloseAllSlots();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Adds player builders to our pool when all plugins are initialized.
    /// </summary>
    /// <param name="message">Message containing the notification data.</param>
    private void OnPluginManagerMessageReceived(QueueMessage message)
    {
      if (((PluginManagerMessaging.NotificationType) message.MessageData[PluginManagerMessaging.NOTIFICATION]) ==
          PluginManagerMessaging.NotificationType.PluginsInitialized)
      {
        LoadPlayerBuilders();

        ServiceScope.Get<IMessageBroker>().Unregister_Async(PluginManagerMessaging.QUEUE, OnPluginManagerMessageReceived);
      }
    }

    #endregion

    #region Protected & internal methods

    internal void ForEachInternal(PlayerSlotWorkerInternalDelegate execute)
    {
      lock (_syncObj)
        foreach (PlayerSlotController psc in _slots)
          if (psc.IsActive)
            execute(psc);
    }

    internal PlayerBuilderRegistration GetPlayerBuilderRegistration(string playerBuilderId)
    {
      PlayerBuilderRegistration result;
      lock (_syncObj)
        return _playerBuilders.TryGetValue(playerBuilderId, out result) ? result : null;
    }

    protected void LoadPlayerBuilders()
    {
      lock (_syncObj)
      {
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(PLAYERBUILDERS_REGISTRATION_PATH))
          RequestPlayerBuilder(itemMetadata.Id);
        pluginManager.AddItemRegistrationChangeListener(PLAYERBUILDERS_REGISTRATION_PATH, _playerBuilderRegistrationChangeListener);
      }
    }

    protected void RequestPlayerBuilder(string playerBuilderId)
    {
      lock (_syncObj)
      {
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        PlayerBuilderRegistration registration = new PlayerBuilderRegistration(
            pluginManager.RequestPluginItem<IPlayerBuilder>(PLAYERBUILDERS_REGISTRATION_PATH,
                playerBuilderId, _playerBuilderPluginItemStateTracker));
        _playerBuilders.Add(playerBuilderId, registration);
      }
    }

    internal void RevokePlayerBuilder(string playerBuilderId)
    {
      PlayerBuilderRegistration registration;
      lock (_syncObj)
      {
        registration = GetPlayerBuilderRegistration(playerBuilderId);
        if (registration == null)
          return;
        // Unregister player builder from internal player builder collection
        _playerBuilders.Remove(playerBuilderId);
        // Release slots with players built by the to-be-removed player builder
        ForEachInternal(psc =>
        {
          if (registration.UsingSlotControllers.Contains(psc))
            psc.ReleasePlayer_NeedLock();
        });
      }
    }

    protected void RemovePlayerBuilder(string playerBuilderId)
    {
      RevokePlayerBuilder(playerBuilderId);
      // Revoke player builder plugin item usage
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.RevokePluginItem(PLAYERBUILDERS_REGISTRATION_PATH, playerBuilderId, _playerBuilderPluginItemStateTracker);
    }

    protected void CleanupSlotOrder()
    {
      lock (_syncObj)
        if (!_slots[PlayerManagerConsts.PRIMARY_SLOT].IsActive && _slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
          SwitchSlots();
    }

    protected int GetIndexOfPlayer(IPlayer player)
    {
      lock (_syncObj)
        for (int i = 0; i < 2; i++)
          if (ReferenceEquals(_slots[i].CurrentPlayer, player))
            return i;
      return -1;
    }

    /// <summary>
    /// Will build the player for the specified <paramref name="locator"/> and <paramref name="mimeType"/>.
    /// </summary>
    /// <remarks>
    /// This method will be called from the <see cref="PlayerSlotController"/> methods, so this class' lock needs
    /// to be aquired from the <see cref="PlayerSlotController"/> class before calling this method (We always need
    /// to aquire the lock on this class first).
    /// </remarks>
    /// <param name="locator">Media item locator to access the to-be-played media item.</param>
    /// <param name="mimeType">Mime type of the media item to be played. May be <c>null</c>.</param>
    /// <param name="psc">Player slot controller which calls this method and which wants its
    /// <see cref="PlayerSlotController.CurrentPlayer"/> property built.</param>
    /// <returns><c>true</c>, if the player could successfully be played, else <c>false</c>.</returns>
    internal bool BuildPlayer_NeedLock(IMediaItemLocator locator, string mimeType, PlayerSlotController psc)
    {
      if (psc.CurrentPlayer != null || psc.BuilderRegistration != null)
        throw new IllegalCallException("Player slot controller has already a player assigned");
      foreach (PlayerBuilderRegistration builderRegistration in _playerBuilders.Values)
      {
        if (builderRegistration.Suspended)
          continue;
        // Build player
        IPlayer player = builderRegistration.PlayerBuilder.GetPlayer(locator, mimeType);
        if (player != null)
        {
          psc.AssignPlayerAndBuilderRegistration(player, builderRegistration);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Will revoke the current player of the specified <paramref name="psc"/>.
    /// </summary>
    /// <remarks>
    /// This method will be called from the <see cref="PlayerSlotController"/> methods, so this class' lock needs
    /// to be aquired from the <see cref="PlayerSlotController"/> class before calling this method (We always need
    /// to aquire the lock on this class first).
    /// </remarks>
    /// <param name="psc">Player slot controller which calls this method and which wants its
    /// <see cref="PlayerSlotController.CurrentPlayer"/> released.</param>
    internal void RevokePlayer_NeedLock(PlayerSlotController psc)
    {
      if (psc.BuilderRegistration != null)
        psc.BuilderRegistration.UsingSlotControllers.Remove(psc);
      psc.ResetPlayerAndBuilderRegistration();
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
          if (currentAudioSlot != null)
            currentAudioSlot.IsAudioSlot = false;
          PlayerSlotController newAudioSlot = GetPlayerSlotControllerInternal(value);
          if (newAudioSlot == null)
            return;
          if (!newAudioSlot.IsActive)
            // Don't move the audio slot to an inactive player slot
            return;
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
          ForEachInternal(psc => { psc.IsMuted = _isMuted; });
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
        lock (_syncObj)
        {
          if (_volume == value)
            return;
          if (value < 0)
            _volume = 0;
          else if (value > 100)
            _volume = 100;
          else
            _volume = value;
          ForEach(psc => { psc.Volume = _volume; });
        }
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
      lock (_syncObj)
      {
        PlayerSlotController psc = GetPlayerSlotControllerInternal(slotIndex);
        if (psc == null)
          return;
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
