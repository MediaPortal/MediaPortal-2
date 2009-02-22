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
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Players;

namespace MediaPortal.Services.Players
{
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

    /// <summary>
    /// Registration structure holding data about an active player.
    /// </summary>
    protected class ActivePlayerData
    {
      protected PlayerBuilderRegistration _builderRegistration;
      protected IPlayer _player;

      public ActivePlayerData(PlayerBuilderRegistration builderRegistration, IPlayer player)
      {
        _builderRegistration = builderRegistration;
        _player = player;
      }

      public PlayerBuilderRegistration BuilderRegistration
      {
        get { return _builderRegistration; }
      }

      public IPlayer PlayerInstance
      {
        get { return _player; }
      }
    }

    /// <summary>
    /// Registration structure holding data about registered a player builder.
    /// </summary>
    protected class PlayerBuilderRegistration
    {
      protected IPlayerBuilder _builder = null;
      protected ICollection<int> _activePlayerSlots = new List<int>();
      protected bool _suspended = false;

      public PlayerBuilderRegistration(IPlayerBuilder builder)
      {
        _builder = builder;
      }

      public IPlayerBuilder PlayerBuilder
      {
        get { return _builder; }
      }

      public ICollection<int> ActivePlayerSlots
      {
        get { return _activePlayerSlots; }
      }

      public bool IsInUse
      {
        get { return _activePlayerSlots.Count > 0; }
      }

      public bool Suspended
      {
        get { return _suspended; }
        set { _suspended = value; }
      }
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
    protected IDictionary<string, PlayerBuilderRegistration> _playerBuilders = new Dictionary<string, PlayerBuilderRegistration>();
    protected IList<ActivePlayerData> _players = new List<ActivePlayerData>();
    protected int _primaryPlayer = -1;

    #endregion

    #region Ctor

    public PlayerManager()
    {
      _playerBuilderPluginItemStateTracker = new PlayerBuilderPluginItemStateTracker(this);
      _playerBuilderRegistrationChangeListener = new PlayerBuilderRegistrationChangeListener(this);
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.QUEUE);
      queue.OnMessageReceive += OnPluginManagerMessageReceived;
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      ReleaseAllPlayers();
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
        LoadPlayerBuilders();
    }

    #endregion

    #region Protected methods

    protected PlayerBuilderRegistration GetPlayerBuilderRegistration(string playerBuilderId)
    {
      PlayerBuilderRegistration result;
      if (!_playerBuilders.TryGetValue(playerBuilderId, out result))
        return null;
      return result;
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
      // Release players built by the to-be-removed player builder
      for (int i = 0; i < _players.Count; i++)
        if (registration.ActivePlayerSlots.Contains(i))
          ReleasePlayer(i);
    }

    protected void RemovePlayerBuilder(string playerBuilderId)
    {
      RevokePlayerBuilder(playerBuilderId);
      // Revoke player builder plugin item usage
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.RevokePluginItem(PLAYERBUILDERS_REGISTRATION_PATH, playerBuilderId, _playerBuilderPluginItemStateTracker);
    }

    #endregion

    #region IPlayerManager implementation

    public IPlayer PreparePlayer(IMediaProvider provider, string path, string mimeType, out int playerSlot)
    {
      foreach (PlayerBuilderRegistration builderRegistration in _playerBuilders.Values)
      {
        IPlayer player = builderRegistration.PlayerBuilder.GetPlayer(provider, path, mimeType);
        if (player == null)
          continue;
        ActivePlayerData result = new ActivePlayerData(builderRegistration, player);
        playerSlot = 0;
        while (_players.Count > playerSlot)
          if (_players[playerSlot] == null)
            break;
        if (_players.Count > playerSlot)
          _players.Add(result);
        else
          _players[playerSlot] = result;
        return player;
      }
      playerSlot = -1;
      return null;
    }

    public void ReleasePlayer(int playerSlot)
    {
      ActivePlayerData activePlayer = _players[playerSlot];
      if (activePlayer == null)
        return;
      IPlayer player = activePlayer.PlayerInstance;
      player.Stop();
      activePlayer.BuilderRegistration.ActivePlayerSlots.Remove(playerSlot);
      if (player is IDisposable)
        ((IDisposable) player).Dispose();
      _players[playerSlot] = null;
    }

    public void ReleaseAllPlayers()
    {
      for (int i=0; i<_players.Count; i++)
        ReleasePlayer(i);
    }

    public void ReleaseGUIResources()
    {
      foreach (IPlayer player in _players)
        if (player != null)
          player.ReleaseGUIResources();
    }

    public void ReallocGUIResources()
    {
      foreach (IPlayer player in _players)
        if (player != null)
          player.ReallocGUIResources();
    }

    public int PrimaryPlayer
    {
      get { return _primaryPlayer; }
    }

    public int NumActivePlayers
    {
      get
      {
        int result = 0;
        foreach (IPlayer player in _players)
          if (player != null)
            result++;
        return result;
      }
    }

    public IPlayer this[int slot]
    {
      get { return slot < _players.Count ? _players[slot].PlayerInstance : null; }
    }

    #endregion
  }
}
