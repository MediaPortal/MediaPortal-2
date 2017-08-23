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
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Services.Players
{
  public class PlayerContext : IPlayerContext, IDisposable
  {
    #region Consts

    public const double MAX_SEEK_RATE = 100;

    public const string KEY_PLAYER_CONTEXT = "PlayerContext: Assigned PlayerContext";
    public const string KEY_RESUME_STATE = "PlayerContext: ResumeState";

    #endregion

    #region Protected fields

    protected volatile bool _closeWhenFinished = false;
    protected volatile MediaItem _currentMediaItem = null;
    protected volatile bool _isCurrentPlayerContext = false;
    protected volatile bool _isPrimaryPlayerContext = false;

    protected IPlayerSlotController _slotController;
    protected readonly IPlaylist _playlist;
    protected readonly Guid _mediaModuleId;
    protected readonly string _name;
    protected readonly AVType _type;
    protected readonly Guid _currentlyPlayingWorkflowStateId;
    protected readonly Guid _fullscreenContentWorkflowStateId;

    #endregion

    #region Ctor

    internal PlayerContext(IPlayerSlotController slotController, Guid mediaModuleId, string name, AVType type,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId)
    {
      _slotController = slotController;
      _slotController.Closed += OnClosed;
      SetContextVariable(KEY_PLAYER_CONTEXT, this);
      _playlist = new Playlist(this);
      _mediaModuleId = mediaModuleId;
      _name = name;
      _type = type;
      _currentlyPlayingWorkflowStateId = currentlyPlayingWorkflowStateId;
      _fullscreenContentWorkflowStateId = fullscreenContentWorkflowStateId;
    }

    #endregion

    public void Dispose()
    {
      Revoke();
    }

    private void OnClosed(IPlayerSlotController slotController)
    {
      Revoke();
    }

    protected static object SyncObj
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        return playerManager.SyncObj;
      }
    }

    protected IPlayer GetCurrentPlayer()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return null;
      lock (SyncObj)
        return psc.IsClosed ? null : psc.CurrentPlayer;
    }

    protected bool DoPlay_NoLock(MediaItem mediaItem, StartTime startTime)
    {
      if (mediaItem == null)
        return false;

      _currentMediaItem = mediaItem;
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      return !psc.IsClosed && psc.Play(mediaItem, startTime);
    }

    internal bool RequestNextItem_NoLock()
    {
      MediaItem item = _playlist.MoveAndGetNext();
      return item != null && DoPlay_NoLock(item, StartTime.Enqueue);
    }

    protected void Seek(double startValue)
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player == null)
        return;
      double newRate;
      if (player.IsPaused)
        newRate = startValue;
      else
      {
        double currentRate = player.PlaybackRate;
        if (currentRate > MAX_SEEK_RATE)
          return;
        if (Math.Sign(currentRate) != Math.Sign(startValue))
          newRate = -currentRate;
        else
          newRate = currentRate * 2;
      }
      if (!player.SetPlaybackRate(newRate) && !player.SetPlaybackRate(2 * newRate))
        player.SetPlaybackRate(4 * newRate);
    }

    public static PlayerContext GetPlayerContext(IPlayerSlotController psc)
    {
      if (psc == null)
        return null;
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (psc.IsClosed)
          return null;
        object result;
        if (psc.ContextVariables.TryGetValue(KEY_PLAYER_CONTEXT, out result))
          return result as PlayerContext;
      }
      return null;
    }

    #region IPlayerContext implementation

    public bool IsActive
    {
      get { return _slotController != null; }
    }

    public Guid MediaModuleId
    {
      get { return _mediaModuleId; }
    }

    public AVType AVType
    {
      get { return _type; }
    }

    public bool IsCurrentPlayerContext
    {
      get { return _isCurrentPlayerContext; }
      internal set { _isCurrentPlayerContext = value; }
    }

    public bool IsPrimaryPlayerContext
    {
      get { return _isPrimaryPlayerContext; }
      internal set { _isPrimaryPlayerContext = value; }
    }

    public IPlaylist Playlist
    {
      get { return _playlist; }
    }

    public MediaItem CurrentMediaItem
    {
      get { return _currentMediaItem; }
    }

    public bool CloseWhenFinished
    {
      get { return _closeWhenFinished; }
      set { _closeWhenFinished = value; }
    }

    public IPlayer CurrentPlayer
    {
      get { return GetCurrentPlayer(); }
    }

    public PlaybackState PlaybackState
    {
      get
      {
        IPlayer player = CurrentPlayer;
        if (player == null)
          return PlaybackState.Idle;
        switch (player.State)
        {
          case PlayerState.Active:
            IMediaPlaybackControl mpc = player as IMediaPlaybackControl;
            if (mpc == null)
              return PlaybackState.Playing;
            if (mpc.IsPaused)
              return PlaybackState.Paused;
            if (mpc.IsSeeking)
              return PlaybackState.Seeking;
            return PlaybackState.Playing;
          case PlayerState.Ended:
            return PlaybackState.Ended;
          case PlayerState.Stopped:
            return PlaybackState.Idle;
          default:
            throw new UnexpectedStateException("Handling code for {0}.{1} is not implemented",
                typeof(PlayerState).Name, player.State);
        }
      }
    }

    public IPlayerSlotController PlayerSlotController
    {
      get { return _slotController; }
    }

    public string Name
    {
      get { return _name; }
    }

    public Guid CurrentlyPlayingWorkflowStateId
    {
      get { return _currentlyPlayingWorkflowStateId; }
    }

    public Guid FullscreenContentWorkflowStateId
    {
      get { return _fullscreenContentWorkflowStateId; }
    }

    public bool DoPlay(MediaItem item)
    {
      return DoPlay_NoLock(item, StartTime.AtOnce);
    }

    public ICollection<AudioStreamDescriptor> GetAudioStreamDescriptors(out AudioStreamDescriptor currentAudioStream)
    {
      currentAudioStream = null;
      ICollection<AudioStreamDescriptor> result = new List<AudioStreamDescriptor>();
      IVideoPlayer videoPlayer = CurrentPlayer as IVideoPlayer;
      if (videoPlayer != null)
      {
        ICollection<string> audioStreamNames = videoPlayer.AudioStreams;
        string currentAudioStreamName = videoPlayer.CurrentAudioStream;
        foreach (string streamName in audioStreamNames)
        {
          AudioStreamDescriptor descriptor = new AudioStreamDescriptor(this, videoPlayer.Name, streamName);
          if (streamName == currentAudioStreamName)
            currentAudioStream = descriptor;
          result.Add(descriptor);
        }
        return result;
      }
      IAudioPlayer audioPlayer = CurrentPlayer as IAudioPlayer;
      if (audioPlayer != null)
      {
        string title = audioPlayer.MediaItemTitle;
        if (string.IsNullOrEmpty(title))
        {
          MediaItem item = Playlist.Current;
          string mimeType;
          string mediaItemTitle;
          title = item.GetPlayData(out mimeType, out mediaItemTitle) ? mediaItemTitle : "Audio";
        }
        result.Add(currentAudioStream = new AudioStreamDescriptor(this, audioPlayer.Name, title));
      }
      return result;
    }

    public void OverrideGeometry(IGeometry geometry)
    {
      IPlayerSlotController slotController = _slotController;
      if (slotController == null)
        return;
      IVideoPlayer player = CurrentPlayer as IVideoPlayer;
      if (player == null)
        return;
      player.GeometryOverride = geometry;
    }

    public void OverrideEffect(string effect)
    {
      IPlayerSlotController slotController = _slotController;
      if (slotController == null)
        return;
      IVideoPlayer player = CurrentPlayer as IVideoPlayer;
      if (player == null)
        return;
      player.EffectOverride = effect;
    }

    public void SetContextVariable(string key, object value)
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return;
      lock (SyncObj)
        psc.ContextVariables[key] = value;
    }

    public void ResetContextVariable(string key)
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return;
      lock (SyncObj)
        psc.ContextVariables.Remove(key);
    }

    public object GetContextVariable(string key)
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return null;
      lock (SyncObj)
      {
        object result;
        if (IsActive && _slotController.ContextVariables.TryGetValue(key, out result))
          return result;
      }
      return null;
    }

    public void Close()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.CloseSlot(_slotController);
    }

    public void Stop()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return;
      IPlaylist playlist;
      lock (SyncObj)
        playlist = _playlist;
      playlist.ResetStatus();
      psc.Stop();
    }

    public void Pause()
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player != null)
        player.Pause();
    }

    public void Play()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
      {
        NextItem();
        return;
      }
      IMediaPlaybackControl mpc = player as IMediaPlaybackControl;
      if (mpc != null)
        mpc.Resume();
    }

    public void TogglePlayPause()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
      {
        NextItem();
        return;
      }
      IMediaPlaybackControl mpc = player as IMediaPlaybackControl;
      if (mpc == null)
        return;
      if (player.State == PlayerState.Active)
        if (mpc.IsPaused)
          mpc.Resume();
        else
          mpc.Pause();
      else
        mpc.Restart();
    }

    public void Restart()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
      {
        NextItem();
        return;
      }
      IMediaPlaybackControl mpc = player as IMediaPlaybackControl;
      if (mpc != null)
        mpc.Restart();
    }

    public void SeekForward()
    {
      Seek(0.5);
    }

    public void SeekBackward()
    {
      Seek(-0.5);
    }

    public bool CanSkipRelative(TimeSpan skipDuration)
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player == null)
        return false;

      TimeSpan currentPosition = player.CurrentTime;
      TimeSpan duration = player.Duration;
      TimeSpan targetPosition = currentPosition.Add(skipDuration);
      return targetPosition.TotalSeconds > 0 && targetPosition <= duration;
    }

    public void SkipRelative(TimeSpan skipDuration)
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player == null)
        return;

      TimeSpan targetPosition = player.CurrentTime.Add(skipDuration);
      player.CurrentTime = targetPosition;
    }

    public void SkipToStart()
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player == null)
        return;

      player.CurrentTime = TimeSpan.FromSeconds(0);
    }

    public void SkipToEnd()
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player == null)
        return;

      player.CurrentTime = player.Duration;
    }

    public bool PreviousItem()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      int countLeft = _playlist.ItemList.Count; // Limit number of tries to current playlist size. If the PL doesn't contain any playable item, this avoids an endless loop.
      MediaItem previousItem;
      do // Loop: Try until we find an item which is able to play
      {
        if (--countLeft < 0 || !_playlist.HasPrevious) // Break loop if we don't have any more items left
          return false;
        previousItem = _playlist.MoveAndGetPrevious();
        if (previousItem == null)
          return false;
      } while (!DoPlay_NoLock(previousItem, StartTime.AtOnce));
      return true;
    }

    public bool NextItem()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      int countLeft = _playlist.ItemList.Count; // Limit number of tries to current playlist size. If the PL doesn't contain any playable item, this avoids an endless loop.
      bool playOk;
      do // Loop: Try until we find an item we can play
      {
        if (--countLeft < 0 || !_playlist.HasNext) // Break loop if we don't have any more items left
        {
          _playlist.ResetStatus();
          return false;
        }
        playOk = DoPlay_NoLock(_playlist.MoveAndGetNext(), StartTime.AtOnce);
      } while (!playOk);
      return true;
    }

    public void InstantSkip(int skipPercent)
    {
      IMediaPlaybackControl player = GetCurrentPlayer() as IMediaPlaybackControl;
      if (player == null)
        return;

      TimeSpan currentPosition = player.CurrentTime;
      TimeSpan duration = player.Duration;
      double skipSeconds = skipPercent * player.Duration.TotalSeconds / 100;
      if (skipSeconds > 0)
      {
        if (currentPosition.TotalSeconds + skipSeconds < duration.TotalSeconds)
          player.CurrentTime = currentPosition.Add(TimeSpan.FromSeconds(skipSeconds));
      }
      else
      {
        // skipSeconds is negative
        player.CurrentTime = currentPosition.TotalSeconds + skipSeconds > 0 ?
            currentPosition.Add(TimeSpan.FromSeconds(skipSeconds)) : TimeSpan.FromSeconds(0);
      }
    }

    public IPlayerSlotController Revoke()
    {
      lock (SyncObj)
      {
        IPlayerSlotController slotController = _slotController;
        _slotController = null;
        if (slotController == null)
          return null;
        slotController.Closed -= OnClosed;
        ResetContextVariable(KEY_PLAYER_CONTEXT);
        return slotController;
      }
    }

    #endregion
  }
}
