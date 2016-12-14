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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
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

    #region Protected fields

    protected PlayerManager _playerManager;
    protected bool _isAudioSlot = false;
    protected IPlayer _player = null;
    protected readonly IDictionary<string, object> _contextVariables = new Dictionary<string, object>();
    protected float _volumeCoefficient = 100;
    protected bool _isMuted = false;
    protected bool _isClosed = false;

    #endregion

    internal PlayerSlotController(PlayerManager parent)
    {
      _playerManager = parent;
      PlayerManagerMessaging.SendPlayerManagerPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotStarted, this);
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

      // Handling of resume data
      NotifyResumeState(player);
      ResetPlayerEvents_NoLock(player);
      IPlayer stopPlayer = null;
      IDisposable disposePlayer;
      lock (_playerManager.SyncObj)
      {
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
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error stopping player '{0}'", e, _player);
        }
      if (disposePlayer != null)
        try
        {
          disposePlayer.Dispose();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error disposing player '{0}'", e, disposePlayer);
        }
    }

    protected void NotifyResumeState(IPlayer player)
    {
      IResumablePlayer resumablePlayer = player as IResumablePlayer;
      if (resumablePlayer == null)
        return;

      // Get the current MediaItem ID at this time, later the PSC is already closed (in case of PlayerEnded state) and MediaItem information is lost.
      object oContext;
      if (!ContextVariables.TryGetValue(PlayerContext.KEY_PLAYER_CONTEXT, out oContext) || !(oContext is IPlayerContext))
        return;

      IPlayerContext playerContext = (IPlayerContext) oContext;
      if (playerContext.CurrentMediaItem == null)
        return;

      try
      {
        IResumeState resumeState;
        if (resumablePlayer.GetResumeState(out resumeState))
          PlayerManagerMessaging.SendPlayerResumeStateMessage(this, playerContext.CurrentMediaItem, resumeState);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error getting resume state from player '{0}'", e, resumablePlayer);
      }
    }

    protected void CheckActive()
    {
      lock (_playerManager.SyncObj)
        if (_isClosed)
          throw new IllegalCallException("PlayerSlotController is not active");
    }

    #region Player events handling

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
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error initializing player events in player '{0}'", e, pe);
        }
      if (rp != null)
        try
        {
          rp.NextItemRequest += OnNextItemRequest;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error initializing player NextItemRequest event in player '{0}'", e, rp);
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
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error resetting player events in player '{0}'", e, pe);
        }
      if (rp != null)
        try
        {
          rp.NextItemRequest -= OnNextItemRequest;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error resetting player NextItemRequest event in player '{0}'", e, rp);
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

    #endregion

    internal void Close_NoLock()
    {
      lock (SyncObj)
      {
        if (_isClosed)
          return;
      }
      Reset();
      InvokeSlotClosed_NoLock();
      PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerSlotClosed, this);
    }

    protected void InvokeSlotClosed_NoLock()
    {
      ClosedDlgt dlgt = Closed;
      if (dlgt != null)
        dlgt(this);
    }

    /// <summary>
    /// This method handles two cases:
    /// <list type="bullet">
    /// <item>No player available to play the resource</item>
    /// <item>The resouce to play is broken</item>
    /// </list>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="exceptions"></param>
    protected void HandleUnableToPlay(MediaItem item, ICollection<Exception> exceptions)
    {
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      // Start a heuristics to find a proper error message for the user
      IResourceLocator locator = item.GetResourceLocator();
      if (locator == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Could not play media item '{0}', resource locator could not be built", item);
        return;
      }
      if (exceptions.Count != 0) // This is the indicator that at least one player builder tried to open the resource but threw an exception
      {
        // 1) Check if resource is present
        IResourceAccessor ra = null;
        try
        {
          bool exists;
          try
          {
            ra = locator.CreateAccessor();
            IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
            exists = fsra != null && fsra.Exists;
          }
          catch (Exception)
          {
            exists = false;
          }
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

    public void CheckAudio_NoLock()
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
        volume = (int) ((_volumeCoefficient * _playerManager.Volume) / 100.0);
      }
      try
      {
        if (vc != null)
        {
          if (mute && !vc.Mute)
            // If we are switching the audio off, first disable the audio before setting the volume -
            // perhaps both properties were changed and we want to avoid a short volume change before the audio gets disabled
            vc.Mute = true;
          vc.Volume = volume;
          vc.Mute = mute;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error checking the audio state in player '{0}'", e, _player);
      }
    }

    #region IPlayerSlotController implementation

    public event ClosedDlgt Closed;

    public bool IsClosed
    {
      get
      {
        lock (SyncObj)
          return _isClosed;
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

    public float VolumeCoefficient
    {
      get
      {
        lock (SyncObj)
          return _volumeCoefficient;
      }
      set
      {
        lock (SyncObj)
          if (value < 0)
            _volumeCoefficient = 0;
          else if (value > 100)
            _volumeCoefficient = 100;
          else
            _volumeCoefficient = value;
        CheckAudio_NoLock();
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

    public bool Play(MediaItem mediaItem, StartTime startTime)
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
          if (rp.NextItem(mediaItem, startTime))
          {
            OnPlayerStarted(rp);
            return true;
          }
        }
        ReleasePlayer_NoLock();
        ICollection<Exception> exceptions;
        player = _playerManager.BuildPlayer_NoLock(mediaItem, out exceptions);
        if (player == null)
        {
          HandleUnableToPlay(mediaItem, exceptions);
          OnPlaybackError(null);
        }
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

          // Handling of resume info.
          object resumeObject;
          if (ContextVariables.TryGetValue(PlayerContext.KEY_RESUME_STATE, out resumeObject))
          {
            IResumeState resumeState = (IResumeState) resumeObject;
            IResumablePlayer resumablePlayer = player as IResumablePlayer;
            if (resumablePlayer != null)
              resumablePlayer.SetResumeState(resumeState);
          }

          if (mpc != null)
            mpc.Resume();
          return true;
        }
        return false;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerSlotController: Error playing '{0}'", e, mediaItem);
        IDisposable disposePlayer = player as IDisposable;
        if (disposePlayer != null)
          disposePlayer.Dispose();
        return false;
      }
    }

    public void Stop()
    {
      bool sendStopEvent;
      lock (SyncObj)
      {
        CheckActive();
        IPlayer player = CurrentPlayer;
        sendStopEvent = player != null && player.State == PlayerState.Active;
      }
      // Simply discard the player - we'll send the PlayerStopped event later in this method
      ReleasePlayer_NoLock();
      if (sendStopEvent)
        // We need to simulate the PlayerStopped event, as the ReleasePlayer_NoLock() method discards all further player events
        PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStopped, this);
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
