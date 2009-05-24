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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.Players;
using MediaPortal.Services.Players.Settings;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.Players
{
  /// <summary>
  /// Controller for one player slot. This class manages a player slot state, a current player, the audio setting
  /// (audio slot, volume, muted state), context variables and a <see cref="PlayerBuilderRegistration"/> instance.
  /// </summary>
  internal class PlayerSlotController : IPlayerSlotController
  {
    protected PlayerManager _playerManager;
    protected int _slotIndex;
    protected bool _isAudioSlot = false;
    protected PlayerBuilderRegistration _builderRegistration = null;
    protected IPlayer _player = null;
    protected IDictionary<string, object> _contextVariables = new Dictionary<string, object>();
    protected PlayerSlotState _slotState = PlayerSlotState.Inactive;
    protected int _volume = 100;
    protected bool _isMuted = false;

    internal PlayerSlotController(PlayerManager parent, int slotIndex)
    {
      _playerManager = parent;
      _slotIndex = slotIndex;
    }

    protected object SyncObj
    {
      get { return _playerManager.SyncObj; }
    }

    /// <summary>
    /// Creates a new player for the specified <paramref name="locator"/> and <paramref name="mimeType"/>.
    /// </summary>
    /// <remarks>
    /// This method may only be called if the current thread occupies both the <see cref="_playerManager"/>'s lock
    /// and this instance's lock.
    /// </remarks>
    /// <param name="locator">Media item locator of the media item to be played.</param>
    /// <param name="mimeType">Mime type of the media item to be played. May be <c>null</c>.</param>
    /// <returns><c>true</c>, if the player could be created, else <c>false</c>.</returns>
    internal bool CreatePlayer_NeedLock(IMediaItemLocator locator, string mimeType)
    {
      ReleasePlayer_NeedLock();
      _playerManager.BuildPlayer_NeedLock(locator, mimeType, this);
      if (_player != null)
      {
        // Initialize new player
        CheckAudio();
        RegisterPlayerEvents();
        return true;
      }
      return false;
    }

    /// <summary>
    /// Releases the current player.
    /// </summary>
    /// <remarks>
    /// This method may only be called if the current thread occupies both the <see cref="_playerManager"/>'s lock
    /// and this instance's lock.
    /// </remarks>
    internal void ReleasePlayer_NeedLock()
    {
      if (_player != null)
      {
        ResetPlayerEvents();
        SetSlotState(PlayerSlotState.Stopped);
        if (_player.State != PlaybackState.Stopped)
          _player.Stop();
        if (_player is IDisposable)
          ((IDisposable) _player).Dispose();
        _player = null;
      }
      _playerManager.RevokePlayer_NeedLock(this);
    }

    /// <summary>
    /// Returns the builder registration of the current player.
    /// </summary>
    /// <remarks>
    /// Access to the returned object has to be synchronized via the <see cref="_playerManager"/>'s
    /// <see cref="PlayerManager.SyncObj"/>.
    /// </remarks>
    internal PlayerBuilderRegistration BuilderRegistration
    {
      get { return _builderRegistration; }
    }

    /// <summary>
    /// Assigns both the current player and the builder registration.
    /// </summary>
    /// <remarks>
    /// This method may only be called if the current thread occupies both the <see cref="_playerManager"/>'s
    /// and this instance's lock objects.
    /// </remarks>
    /// <param name="player">The player to be assigned to the <see cref="CurrentPlayer"/> property.</param>
    /// <param name="builderRegistration">The builder registration to be assigned to the <see cref="BuilderRegistration"/>
    /// property.</param>
    internal void AssignPlayerAndBuilderRegistration(IPlayer player, PlayerBuilderRegistration builderRegistration)
    {
      _player = player;
      _builderRegistration = builderRegistration;
      _builderRegistration.UsingSlotControllers.Add(this);
    }

    /// <summary>
    /// Releases both the current player and the builder registration.
    /// </summary>
    /// <remarks>
    /// This method may only be called if the current thread occupies both the <see cref="_playerManager"/>'s
    /// and this instance's lock objects.
    /// </remarks>
    internal void ResetPlayerAndBuilderRegistration()
    {
      _player = null;
      if (_builderRegistration != null)
      {
        _builderRegistration.UsingSlotControllers.Remove(this);
        _builderRegistration = null;
      }
    }

    protected void CheckAudio()
    {
      lock (SyncObj)
      {
        if (_player == null)
          return;
        bool enableAudio = _isAudioSlot && !_isMuted;
        if (_player.IsAudioEnabled && !enableAudio)
          // If we are switching the audio off, first disable the audio before setting the volume -
          // perhaps both properties were changed and we want to avoid a short volume change before the audio gets disabled
          _player.IsAudioEnabled = false;
        IVolumeControl vc = _player as IVolumeControl;
        if (vc != null)
          vc.Volume = _volume;
        _player.IsAudioEnabled = enableAudio;
      }
    }

    protected void CheckActive()
    {
      if (_slotState == PlayerSlotState.Inactive)
        throw new IllegalCallException("PlayerSlotController: PSC is not active");
    }

    protected void RegisterPlayerEvents()
    {
      lock (SyncObj)
      {
        IPlayerEvents pe = (IPlayerEvents) _player;
        pe.InitializePlayerEvents(OnPlayerStarted, OnPlayerStopped, OnPlayerEnded,
            OnPlayerPaused, OnPlayerResumed, OnPlaybackError);
      }
    }

    protected void ResetPlayerEvents()
    {
      lock (SyncObj)
      {
        IPlayerEvents pe = _player as IPlayerEvents;
        if (pe != null)
          pe.ResetPlayerEvents();
      }
    }

    internal void OnPlayerStarted(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStarted, _slotIndex);
    }

    internal void OnPlayerStopped(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStopped, _slotIndex);
    }

    internal void OnPlayerEnded(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerEnded, _slotIndex);
    }

    internal void OnPlayerPaused(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerPaused, _slotIndex);
    }

    internal void OnPlayerResumed(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStarted, _slotIndex);
    }

    internal void OnPlaybackError(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerError, _slotIndex);
    }

    protected void SetSlotState(PlayerSlotState slotState)
    {
      lock (SyncObj)
      {
        if (slotState == _slotState)
          return;
        PlayerSlotState oldSlotState = _slotState;
        _slotState = slotState;
        if (oldSlotState == PlayerSlotState.Inactive && slotState != PlayerSlotState.Inactive)
          PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotActivated);
        if (oldSlotState != PlayerSlotState.Inactive || slotState != PlayerSlotState.Stopped)
          // Suppress "PlayerStopped" message if slot was activated
          switch (slotState)
          {
            case Presentation.Players.PlayerSlotState.Inactive:
              PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotDeactivated, _slotIndex);
              break;
            case Presentation.Players.PlayerSlotState.Playing:
              PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStarted, _slotIndex);
              break;
            case Presentation.Players.PlayerSlotState.Stopped:
              PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStopped, _slotIndex);
              break;
          }
      }
    }

    #region IPlayerSlotController implementation

    public int SlotIndex
    {
      get { return _slotIndex; }
      internal set
      {
        lock (SyncObj)
          _slotIndex = value;
      }
    }

    public bool IsAudioSlot
    {
      get
      {
        lock (SyncObj)
        {
          CheckActive();
          return _isAudioSlot;
        }
      }
      internal set
      {
        lock (SyncObj)
        {
          bool wasChanged = value != _isAudioSlot;
          _isAudioSlot = value;
          CheckAudio();
          if (wasChanged)
            PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.AudioSlotChanged, _slotIndex);
        }
      }
    }

    public bool IsMuted
    {
      get { return _isMuted; }
      internal set
      {
        lock (SyncObj)
        {
          _isMuted = value;
          CheckAudio();
        }
      }
    }

    public int Volume
    {
      get { return _volume; }
      set
      {
        lock (SyncObj)
        {
          _volume = value;
          CheckAudio();
        }
      }
    }

    public bool IsActive
    {
      get
      {
        lock (SyncObj)
          return _slotState != PlayerSlotState.Inactive;
      }
      internal set
      {
        lock (SyncObj)
        {
          if (value == IsActive)
            return;
          if (value)
            SetSlotState(PlayerSlotState.Stopped);
          else
          {
            Reset();
            _isAudioSlot = false;
            SetSlotState(PlayerSlotState.Inactive);
          }
        }
      }
    }

    public PlayerSlotState PlayerSlotState
    {
      get
      {
        lock (SyncObj)
          return _slotState;
      }
    }

    public IPlayer CurrentPlayer
    {
      get
      {
        lock (SyncObj)
        {
          CheckActive();
          return _player;
        }
      }
    }

    public IDictionary<string, object> ContextVariables
    {
      get
      {
        lock (SyncObj)
        {
          CheckActive();
          return _contextVariables;
        }
      }
    }

    public bool Play(IMediaItemLocator locator, string mimeType, string mediaItemTitle)
    {
      bool result = false;
      lock (SyncObj)
        try
        {
          CheckActive();
          FadingSettings settings = ServiceScope.Get<ISettingsManager>().Load<FadingSettings>();
          if (settings.CrossFadingEnabled)
          {
            ICrossfadingEnabledPlayer cep = _player as ICrossfadingEnabledPlayer;
            if (cep != null)
              return result = cep.Crossfade(locator, mimeType, CrossFadeMode.FadeDuration,
                  new TimeSpan((long) (10000000*settings.CrossFadeDuration)));
          }
          IReusablePlayer rp = _player as IReusablePlayer;
          if (rp != null)
            return result = rp.NextItem(locator, mimeType);
          if (CreatePlayer_NeedLock(locator, mimeType))
          {
            _player.Resume();
            return result = true;
          }
          return result = false;
        }
        finally
        {
          if (result)
            SetSlotState(PlayerSlotState.Playing);
          if (_player != null)
            _player.SetMediaItemTitleHint(mediaItemTitle);
        }
    }

    public void Stop()
    {
      lock (SyncObj)
      {
        CheckActive();
        SetSlotState(PlayerSlotState.Stopped);
        ReleasePlayer_NeedLock();
      }
    }

    public void Reset()
    {
      lock (SyncObj)
      {
        Stop();
        _contextVariables.Clear();
      }
    }

   #endregion
  }
}
