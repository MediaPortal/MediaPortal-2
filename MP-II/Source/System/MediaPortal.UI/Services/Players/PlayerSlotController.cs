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
    internal class PlayerSlotController : IPlayerSlotController
    {
      protected PlayerManager _playerManager;
      protected int _slotIndex;
      protected bool _isAudioSlot = false;
      protected PlayerBuilderRegistration _builderRegistration = null;
      protected IPlayer _player = null;
      protected IDictionary<string, object> _contextVariables = new Dictionary<string, object>();
      protected PlayerSlotState _slotState = PlayerSlotState.Inactive;

      internal PlayerSlotController(PlayerManager parent, int slotIndex)
      {
        _playerManager = parent;
        _slotIndex = slotIndex;
      }

      internal bool CreatePlayer(IMediaItemLocator locator, string mimeType)
      {
        ReleasePlayer();
        _playerManager.BuildPlayer(locator, mimeType, this);
        if (_player != null)
        {
          _player.IsAudioEnabled = IsAudioSlot;
          RegisterPlayerEvents();
          return true;
        }
        return false;
      }

      internal void ReleasePlayer()
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
        _playerManager.RevokePlayer(this);
      }

      internal PlayerBuilderRegistration BuilderRegistration
      {
        get { return _builderRegistration; }
      }

      internal void AssignPlayerAndBuilderRegistration(IPlayer player, PlayerBuilderRegistration builderRegistration)
      {
        _player = player;
        _builderRegistration = builderRegistration;
        _builderRegistration.UsingSlotControllers.Add(this);
      }

      internal void ResetPlayerAndBuilderRegistration()
      {
        _player = null;
        if (_builderRegistration != null)
        {
          _builderRegistration.UsingSlotControllers.Remove(this);
          _builderRegistration = null;
        }
      }

      protected void CheckActive()
      {
        if (_slotState == PlayerSlotState.Inactive)
          throw new InvalidStateException("PlayerSlotController: PSC is not active");
      }

      protected void RegisterPlayerEvents()
      {
        IPlayerEvents pe = (IPlayerEvents) _player;
        pe.InitializePlayerEvents(OnPlayerStarted, OnPlayerStopped, OnPlayerEnded,
            OnPlayerPaused, OnPlayerResumed, OnPlaybackError);
      }

      protected void ResetPlayerEvents()
      {
        IPlayerEvents pe = _player as IPlayerEvents;
        if (pe != null)
          pe.ResetPlayerEvents();
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
        if (slotState == _slotState)
          return;
        PlayerSlotState oldSlotState = _slotState;
        _slotState = slotState;
        if (oldSlotState == PlayerSlotState.Inactive && slotState != PlayerSlotState.Inactive)
          PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotActivated);
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

      #region IPlayerSlot implementation

      public int SlotIndex
      {
        get { return _slotIndex; }
        internal set { _slotIndex = value; }
      }

      public bool IsAudioSlot
      {
        get
        {
          CheckActive();
          return _isAudioSlot;
        }
        internal set
        {
          _isAudioSlot = value;
          if (_player != null)
            _player.IsAudioEnabled = IsAudioSlot;
          PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.AudioSlotChanged, _slotIndex);
        }
      }

      public bool IsActive
      {
        get { return _slotState != PlayerSlotState.Inactive; }
        internal set
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

      public PlayerSlotState PlayerSlotState
      {
        get { return _slotState; }
      }

      public IPlayer CurrentPlayer
      {
        get
        {
          CheckActive();
          return _player;
        }
      }

      public IDictionary<string, object> ContextVariables
      {
        get
        {
          CheckActive();
          return _contextVariables;
        }
      }

      public bool Play(IMediaItemLocator locator, string mimeType)
      {
        bool result = false;
        try
        {
          CheckActive();
          PlayerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PlayerSettings>();
          if (settings.CrossFading)
          {
            ICrossfadingEnabledPlayer cep = _player as ICrossfadingEnabledPlayer;
            if (cep != null)
              return result = cep.Crossfade(locator, mimeType, CrossFadeMode.FadeDuration,
                  new TimeSpan((long) (10000000*settings.CrossFadeDuration)));
          }
          IReusablePlayer rp = _player as IReusablePlayer;
          if (rp != null)
            return result = rp.NextItem(locator, mimeType);
          if (CreatePlayer(locator, mimeType))
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
        }
      }

      public void Stop()
      {
        CheckActive();
        SetSlotState(PlayerSlotState.Stopped);
        ReleasePlayer();
      }

      public void Reset()
      {
        Stop();
        ContextVariables.Clear();
      }

     #endregion
    }
}