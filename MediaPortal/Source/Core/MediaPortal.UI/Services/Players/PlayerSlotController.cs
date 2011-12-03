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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Services.Players
{
  /// <summary>
  /// Controller for one player slot. This class manages a player slot state, a current player, the audio setting
  /// (audio slot, volume, muted state) and context variables.
  /// </summary>
  internal class PlayerSlotController : IPlayerSlotController
  {
    #region Consts

    public const string RES_NO_PLAYER_AVAILABLE_NOTIFICATION_TITLE = "[Players.NoPlayerAvailableNotificationTitle]";
    public const string RES_NO_PLAYER_AVAILABLE_NOTIFICATION_TEXT = "[Players.NoPlayerAvailableNotificationText]";

    public const string RES_ERROR_PLAYING_MEDIA_ITEM_TITLE = "[Players.ErrorPlayingMediaItemTitle]";
    public const string RES_UNABLE_TO_PLAY_MEDIA_ITEM_TEXT = "[Players.UnableToPlayMediaItemText]";
    public const string RES_RESOURCE_NOT_FOUND_TEXT = "[Players.ResourceNotFoundText]";

    #endregion

    protected PlayerManager _playerManager;
    protected int _slotIndex;
    protected bool _isAudioSlot = false;
    protected IPlayer _player = null;
    protected readonly IDictionary<string, object> _contextVariables = new Dictionary<string, object>();
    protected PlayerSlotState _slotState = PlayerSlotState.Inactive;
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected uint _activationSequence = 0;

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
    /// Releases the current player.
    /// </summary>
    internal void ReleasePlayer_NoLock()
    {
      IPlayer player;
      lock (SyncObj)
      {
        player = _player;
        _player = null;
      }
      if (player == null)
        return;
      ResetPlayerEvents_NoLock(player);
      IPlayer stopPlayer = null;
      IDisposable disposePlayer;
      lock (_playerManager.SyncObj)
      {
        SetSlotState(PlayerSlotState.Stopped);
        if (player.State != PlayerState.Stopped)
          stopPlayer = player;
        disposePlayer = player as IDisposable;
      }
      if (stopPlayer != null)
        try
        {
          stopPlayer.Stop();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error stopping player '{1}'", e, _slotIndex, _player);
        }
      if (disposePlayer != null)
        try
        {
          disposePlayer.Dispose();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error disposing player '{1}'", e, _slotIndex, disposePlayer);
        }
    }

    protected void CheckAudio_NoLock()
    {
      bool mute;
      int volume;
      IVolumeControl vc;
      lock (SyncObj)
      {
        if (_player == null)
          return;
        mute = !_isAudioSlot || _isMuted;
        vc = _player as IVolumeControl;
        volume = _volume;
      }
      try
      {
        if (vc != null && mute && !vc.Mute)
          // If we are switching the audio off, first disable the audio before setting the volume -
          // perhaps both properties were changed and we want to avoid a short volume change before the audio gets disabled
          vc.Mute = true;
        if (vc != null)
          vc.Volume = volume;
        if (vc != null)
          vc.Mute = mute;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error checking the audio state in player '{1}'", e, _slotIndex, _player);
      }
    }

    protected void CheckActive()
    {
      lock (_playerManager.SyncObj)
        if (_slotState == PlayerSlotState.Inactive)
          throw new IllegalCallException("PlayerSlotController: PSC is not active");
    }

    protected void RegisterPlayerEvents_NoLock(IPlayer player)
    {
      IPlayerEvents pe = player as IPlayerEvents;
      IReusablePlayer rp = player as IReusablePlayer;
      if (pe != null)
        try
        {
          pe.InitializePlayerEvents(OnPlayerStarted, OnPlayerStateReady, OnPlayerStopped, OnPlayerEnded,
              OnPlaybackStateChanged, OnPlaybackError);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error initializing player events in player '{1}'", e, _slotIndex, pe);
        }
      if (rp != null)
        try
        {
          rp.NextItemRequest += OnNextItemRequest;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error initializing player NextItemRequest event in player '{1}'", e, _slotIndex, rp);
        }
    }

    protected void ResetPlayerEvents_NoLock(IPlayer player)
    {
      IPlayerEvents pe = player as IPlayerEvents;
      IReusablePlayer rp = player as IReusablePlayer;
      if (pe != null)
        try
        {
          pe.ResetPlayerEvents();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error resetting player events in player '{1}'", e, _slotIndex, pe);
        }
      if (rp != null)
        try
        {
          rp.NextItemRequest -= OnNextItemRequest;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error resetting player NextItemRequest event in player '{1}'", e, _slotIndex, rp);
        }
    }

    internal void OnPlayerStarted(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStarted, this);
    }

    internal void OnPlayerStateReady(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStateReady, this);
    }

    internal void OnPlayerStopped(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStopped, this);
    }

    internal void OnPlayerEnded(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerEnded, this);
    }

    internal void OnPlaybackStateChanged(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlaybackStateChanged, this);
    }

    internal void OnPlaybackError(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerError, this);
    }

    internal void OnNextItemRequest(IPlayer player)
    {
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.RequestNextItem, this);
    }

    protected void SetSlotState(PlayerSlotState slotState)
    {
      PlayerSlotState oldSlotState;
      lock (SyncObj)
      {
        if (slotState == _slotState)
          return;
        if (slotState == PlayerSlotState.Inactive)
          _activationSequence++;
        oldSlotState = _slotState;
        _slotState = slotState;
      }
      InvokeSlotStateChanged(slotState); // Outside the lock
      lock (SyncObj)
      {
        if (oldSlotState == PlayerSlotState.Inactive && slotState != PlayerSlotState.Inactive)
          PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotActivated);
        if (oldSlotState != PlayerSlotState.Inactive || slotState != PlayerSlotState.Stopped)
          // Suppress "PlayerStopped" message if slot was activated
          switch (slotState)
          {
            case PlayerSlotState.Inactive:
              PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotDeactivated, this);
              break;
            case PlayerSlotState.Playing:
              PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotStarted, this);
              break;
            // Presentation.Players.PlayerSlotState.Stopped:
            // this is no extra message, as we sent the PlayerSlotActivated message above
          }
      }
    }

    protected void InvokeSlotStateChanged(PlayerSlotState slotState)
    {
      SlotStateChangedDlgt dlgt = SlotStateChanged;
      if (dlgt != null)
        dlgt(this, slotState);
    }

    protected void HandleUnableToPlay(IResourceLocator locator, string mimeType, ICollection<Exception> exceptions)
    { // We come here in two cases: 1) No player available to play the resource and 2) resouce broken
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      // Start a heuristics to find a proper error message for the user
      if (exceptions.Count != 0) // This is the indicator that at least one player builder tried to open the resource but threw an exception
      {
        // 1) Check if resource is present
        IResourceAccessor ra = null;
        try
        {
          bool exists = true;
          try
          {
            ra = locator.CreateAccessor();
          }
          catch (Exception)
          {
            exists = false;
          }
          exists &= ra != null && ra.Exists;
          notificationService.EnqueueNotification(
              NotificationType.UserInteractionRequired, RES_ERROR_PLAYING_MEDIA_ITEM_TITLE, exists ?
                  LocalizationHelper.Translate(RES_UNABLE_TO_PLAY_MEDIA_ITEM_TEXT, ra.ResourceName) :
                  LocalizationHelper.Translate(RES_RESOURCE_NOT_FOUND_TEXT, locator.NativeResourcePath.FileName), true);
        }
        finally
        {
          if (ra != null)
            ra.Dispose();
        }
      }
      else
      {
        using (IResourceAccessor ra = locator.CreateAccessor())
          notificationService.EnqueueNotification(NotificationType.UserInteractionRequired, RES_NO_PLAYER_AVAILABLE_NOTIFICATION_TITLE,
              LocalizationHelper.Translate(RES_NO_PLAYER_AVAILABLE_NOTIFICATION_TEXT, ra.ResourceName), true);
      }
    }

    #region IPlayerSlotController implementation

    public event SlotStateChangedDlgt SlotStateChanged;

    public int SlotIndex
    {
      get
      {
        lock (SyncObj)
          return _slotIndex;
      }
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
          return _isAudioSlot;
      }
      internal set
      {
        bool wasChanged;
        lock (SyncObj)
        {
          wasChanged = value != _isAudioSlot;
          _isAudioSlot = value;
        }
        CheckAudio_NoLock();
        if (wasChanged)
          PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.AudioSlotChanged, this);
      }
    }

    public bool IsMuted
    {
      get
      {
        lock (SyncObj)
          return _isMuted;
      }
      internal set
      {
        lock (SyncObj)
          _isMuted = value;
        CheckAudio_NoLock();
      }
    }

    public int Volume
    {
      get
      {
        lock (SyncObj)
          return _volume;
      }
      set
      {
        lock (SyncObj)
          _volume = value;
        CheckAudio_NoLock();
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
        bool doSetInactive = false;
        lock (SyncObj)
        {
          if (value == IsActive)
            return;
          if (value)
            SetSlotState(PlayerSlotState.Stopped);
          else
            doSetInactive = true;
        }
        if (doSetInactive)
        {
          Reset(); // Outside the lock
          lock (SyncObj)
          {
            _isAudioSlot = false;
            SetSlotState(PlayerSlotState.Inactive);
          }
        }
      }
    }

    public uint ActivationSequence
    {
      get
      {
        lock (SyncObj)
          return _activationSequence;
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
          return _player;
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

    public bool Play(IResourceLocator locator, string mimeType, string mediaItemTitle, StartTime startTime)
    {
      IPlayer player = null;
      try
      {
        IReusablePlayer rp;
        lock (SyncObj)
        {
          CheckActive();
          player = _player;
          rp = _player as IReusablePlayer;
        }
        if (rp != null)
        {
          if (rp.NextItem(locator, mimeType, startTime))
          {
            OnPlayerStarted(rp);
            return true;
          }
        }
        ReleasePlayer_NoLock();
        ICollection<Exception> exceptions;
        player = _playerManager.BuildPlayer_NoLock(locator, mimeType, out exceptions);
        if (player == null)
          HandleUnableToPlay(locator, mimeType, exceptions);
        else
        {
          IMediaPlaybackControl mpc;
          IDisposable disposePlayer = null;
          lock (SyncObj)
          {
            if (_player != null)
              disposePlayer = _player as IDisposable; // If we got a race condition between the locks
            _player = player;
            mpc = player as IMediaPlaybackControl;
          }
          RegisterPlayerEvents_NoLock(player);
          CheckAudio_NoLock();
          if (disposePlayer != null)
            disposePlayer.Dispose();
          OnPlayerStarted(player);
          if (mpc != null)
            mpc.Resume();
          return true;
        }
        return false;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error playing '{1}'", e, _slotIndex, locator);
        IDisposable disposePlayer = player as IDisposable;
        if (disposePlayer != null)
          disposePlayer.Dispose();
        player = null;
        return false;
      }
      finally
      {
        if (player != null)
        {
          SetSlotState(PlayerSlotState.Playing);
          player.SetMediaItemTitleHint(mediaItemTitle);
        }
      }
    }

    public void Play(IPlayer player)
    {
      try
      {
        lock (SyncObj)
          CheckActive();
        ReleasePlayer_NoLock();
        IMediaPlaybackControl mpc;
        lock (SyncObj)
        {
          _player = player;
          mpc = player as IMediaPlaybackControl;
        }
        RegisterPlayerEvents_NoLock(player);
        CheckAudio_NoLock();
        OnPlayerStarted(player);
        if (mpc != null)
          mpc.Resume();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Slot {0} - error preparing '{1}'", e, _slotIndex, player);
        player = null;
      }
      finally
      {
        if (player != null)
          SetSlotState(PlayerSlotState.Playing);
      }
    }

    public void Stop()
    {
      lock (SyncObj)
      {
        CheckActive();
        if (_slotState != PlayerSlotState.Stopped)
        {
          SetSlotState(PlayerSlotState.Stopped);
          // We need to simulate the PlayerStopped event, as the ReleasePlayer_NoLock() method discards all further player events
          PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStopped, this);
        }
      }
      ReleasePlayer_NoLock();
    }

    public void Reset()
    {
      Stop();
      lock (SyncObj)
        _contextVariables.Clear();
    }

   #endregion
  }
}
