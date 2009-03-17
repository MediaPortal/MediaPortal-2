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
  /// <summary>
  /// Management class for player builder registrations and active players.
  /// </summary>
  /*
   * - Each slot must be switched active and inactive EXPLICITLY (no implicit CloseSlot(N)!)
   */
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
        PlayerBuilderRegistration builderRegistration = _playerManager.GetPlayerBuilderRegistration(itemRegistration.Metadata.Id);
        if (builderRegistration == null)
          return true;
        if (builderRegistration.IsInUse)
          return false;
        builderRegistration.Suspended = true;
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _playerManager.RevokePlayerBuilder(itemRegistration.Metadata.Id);
      }

      public void Continue(PluginItemRegistration itemRegistration)
      {
        PlayerBuilderRegistration builderRegistration = _playerManager.GetPlayerBuilderRegistration(itemRegistration.Metadata.Id);
        if (builderRegistration == null)
          return;
        builderRegistration.Suspended = false;
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

    #endregion

    #region Protected fields

    protected PlayerBuilderPluginItemStateTracker _playerBuilderPluginItemStateTracker;
    protected PlayerBuilderRegistrationChangeListener _playerBuilderRegistrationChangeListener;
    internal IDictionary<string, PlayerBuilderRegistration> _playerBuilders = new Dictionary<string, PlayerBuilderRegistration>();
    internal PlayerSlotController[] _slots;

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
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.QUEUE);
      queue.MessageReceived += OnPluginManagerMessageReceived;
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

        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.QUEUE);
        queue.MessageReceived -= OnPluginManagerMessageReceived;
      }
    }

    #endregion

    #region Protected methods

    internal PlayerBuilderRegistration GetPlayerBuilderRegistration(string playerBuilderId)
    {
      PlayerBuilderRegistration result;
      return _playerBuilders.TryGetValue(playerBuilderId, out result) ? result : null;
    }

    protected void LoadPlayerBuilders()
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(PLAYERBUILDERS_REGISTRATION_PATH))
        RequestPlayerBuilder(itemMetadata.Id);
      pluginManager.AddItemRegistrationChangeListener(PLAYERBUILDERS_REGISTRATION_PATH, _playerBuilderRegistrationChangeListener);
    }

    protected void RequestPlayerBuilder(string playerBuilderId)
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      PlayerBuilderRegistration registration = new PlayerBuilderRegistration(
          pluginManager.RequestPluginItem<IPlayerBuilder>(PLAYERBUILDERS_REGISTRATION_PATH,
              playerBuilderId, _playerBuilderPluginItemStateTracker));
      _playerBuilders.Add(playerBuilderId, registration);
    }

    internal void RevokePlayerBuilder(string playerBuilderId)
    {
      PlayerBuilderRegistration registration = GetPlayerBuilderRegistration(playerBuilderId);
      if (registration == null)
        return;
      // Unregister player builder from internal player builder collection
      _playerBuilders.Remove(playerBuilderId);
      // Release slots with players built by the to-be-removed player builder
      for (int i = 0; i < 2; i++)
      {
        PlayerSlotController psc = _slots[i];
        if (registration.UsingSlots.Contains(psc))
          psc.NextItem();
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
      if (!_slots[PlayerManagerConsts.PRIMARY_SLOT].IsActive && _slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
        SwitchPlayers();
      if (_slots[PlayerManagerConsts.PRIMARY_SLOT].IsActive && !_slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
        AudioSlotIndex = 0;
    }

    protected int GetIndexOfPlayer(IPlayer player)
    {
      for (int i = 0; i < 2; i++)
        if (ReferenceEquals(_slots[i].CurrentPlayer, player))
          return i;
      return -1;
    }

    internal bool BuildPlayer(IMediaItemLocator locator, string mimeType, PlayerSlotController psc)
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

    internal void RevokePlayer(PlayerSlotController psc)
    {
      if (psc.BuilderRegistration != null)
        psc.BuilderRegistration.UsingSlots.Remove(psc);
      psc.ResetPlayerAndBuilderRegistration();
    }

    #endregion

    #region IPlayerManager implementation

    public int NumOpenSlots
    {
      get
      {
        int result = 0;
        foreach (PlayerSlotController psc in _slots)
          if (psc.IsActive)
            result++;
        return result;
      }
    }

    public bool CanOpenSlot
    {
      get { return NumOpenSlots < 2; }
    }

    public int AudioSlotIndex
    {
      get
      {
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
        int oldAudioSlotIndex = AudioSlotIndex;
        if (oldAudioSlotIndex == value)
          return;
        PlayerSlotController currentAudioSlot = oldAudioSlotIndex == -1 ? null : _slots[oldAudioSlotIndex];
        if (currentAudioSlot != null)
          currentAudioSlot.IsAudioSlot = false;
        PlayerSlotController newAudioSlot = _slots[value];
        if (!newAudioSlot.IsActive)
          // Don't move the audio slot to an inactive player slot
          return;
        newAudioSlot.IsAudioSlot = true;
        PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.AudioSlotChanged, value);
      }
    }

    public IPlayer this[int slotIndex]
    {
      get
      {
        IPlayerSlotController psc = GetSlot(slotIndex);
        return psc != null && psc.IsActive ? psc.CurrentPlayer : null;
      }
    }

    public IPlayerSlotController GetSlot(int slotIndex)
    {
      if (slotIndex < 0 || slotIndex > 1)
        return null;
      return _slots[slotIndex];
    }

    public bool OpenSlot(out int slotIndex, out IPlayerSlotController controller)
    {
      slotIndex = -1;
      controller = null;
      int index = -1;
      // Find a free slot
      if (!_slots[PlayerManagerConsts.PRIMARY_SLOT].IsActive)
        index = PlayerManagerConsts.PRIMARY_SLOT;
      else if (!_slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
        index = PlayerManagerConsts.SECONDARY_SLOT;
      else
        return false;
      PlayerSlotController psc = _slots[index];
      psc.IsActive = true;
      if (AudioSlotIndex == -1)
        AudioSlotIndex = slotIndex;
      controller = psc;
      slotIndex = index;
      return true;
    }

    public void CloseSlot(int slotIndex)
    {
      PlayerSlotController psc = GetSlot(slotIndex) as PlayerSlotController;
      if (psc == null)
        return;
      psc.IsActive = false;
      CleanupSlotOrder();
    }

    public void CloseAllSlots()
    {
      foreach (PlayerSlotController psc in _slots)
        psc.IsActive = false;
      CleanupSlotOrder();
    }

    public void SwitchPlayers()
    {
      if (!_slots[PlayerManagerConsts.SECONDARY_SLOT].IsActive)
        // Don't move an inactive player slot to the primary slot index
        return;
      PlayerSlotController tmp = _slots[PlayerManagerConsts.PRIMARY_SLOT];
      _slots[PlayerManagerConsts.PRIMARY_SLOT] = _slots[PlayerManagerConsts.SECONDARY_SLOT];
      _slots[PlayerManagerConsts.SECONDARY_SLOT] = tmp;
      // Audio slot index changes automatically as it is stored in the slot instance itself
      PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotsChanged);
    }

    public void ForEach(PlayerSlotWorkerDelegate execute)
    {
      foreach (PlayerSlotController psc in _slots)
        if (psc.IsActive)
          execute(psc);
    }

    #endregion
  }
}
