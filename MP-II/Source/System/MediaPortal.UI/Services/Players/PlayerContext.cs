#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Services.Players
{
  public class PlayerContext : IPlayerContext, IDisposable
  {
    public const double MAX_SEEK_RATE = 100;

    #region Protected fields

    protected volatile bool _closeWhenFinished = false;
    protected volatile MediaItem _currentMediaItem = null;

    protected IPlayerSlotController _slotController;
    protected readonly PlayerContextManager _contextManager;
    protected readonly IPlaylist _playlist;
    protected readonly Guid _mediaModuleId;
    protected readonly string _name;
    protected readonly PlayerContextType _type;
    protected readonly Guid _currentlyPlayingWorkflowStateId;
    protected readonly Guid _fullscreenContentWorkflowStateId;

    #endregion

    #region Ctor

    internal PlayerContext(PlayerContextManager contextManager, IPlayerSlotController slotController,
        Guid mediaModuleId, string name, PlayerContextType type,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId)
    {
      _contextManager = contextManager;
      _slotController = slotController;
      _playlist = new Playlist();
      _mediaModuleId = mediaModuleId;
      _name = name;
      _type = type;
      _currentlyPlayingWorkflowStateId = currentlyPlayingWorkflowStateId;
      _fullscreenContentWorkflowStateId = fullscreenContentWorkflowStateId;
    }

    public void Dispose()
    {
      _slotController = null;
    }

    #endregion

    protected static object SyncObj
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        return playerManager.SyncObj;
      }
    }

    protected static bool GetItemData(MediaItem item, out IResourceLocator locator, out string mimeType,
        out string mediaItemTitle)
    {
      locator = null;
      mimeType = null;
      mediaItemTitle = null;
      if (item == null)
        return false;
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      locator = mediaAccessor.GetResourceLocator(item);
      MediaItemAspect mediaAspect = item[MediaAspect.ASPECT_ID];
      mimeType = (string) mediaAspect[MediaAspect.ATTR_MIME_TYPE];
      mediaItemTitle = (string) mediaAspect[MediaAspect.ATTR_TITLE];
      return locator != null;
    }

    protected IPlayer GetCurrentPlayer()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return null;
      lock (SyncObj)
        return psc.IsActive ? psc.CurrentPlayer : null;
    }

    protected bool DoPlay(IResourceLocator locator, string mimeType, string mediaItemTitle, StartTime startTime)
    {
      _currentMediaItem = null;
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      lock (SyncObj)
        if (!psc.IsActive)
          return false;
        else
          return psc.Play(locator, mimeType, mediaItemTitle, startTime);
    }

    protected bool DoPlay(MediaItem item, StartTime startTime)
    {
      IResourceLocator locator;
      string mimeType;
      string mediaItemTitle;
      if (!GetItemData(item, out locator, out mimeType, out mediaItemTitle))
        return false;
      bool result = DoPlay(locator, mimeType, mediaItemTitle, startTime);
      _currentMediaItem = item;
      return result;
    }

    internal bool RequestNextItem()
    {
      MediaItem item = _playlist.GetNext();
      if (item == null)
        return false;
      return DoPlay(item, StartTime.Enqueue);
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
          newRate = currentRate*2;
      }
      if (!player.SetPlaybackRate(newRate) && !player.SetPlaybackRate(2*newRate))
        player.SetPlaybackRate(4*newRate);
    }

    #region IPlayerContext implementation

    public bool IsValid
    {
      get { return _slotController != null; }
    }

    public Guid MediaModuleId
    {
       get { return _mediaModuleId; }
    }

    public PlayerContextType MediaType
    {
      get { return _type; }
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

    public PlaybackState PlayerState
    {
      get
      {
        IPlayer player = CurrentPlayer;
        if (player == null)
          return PlaybackState.Stopped;
        switch (player.State)
        {
          case Presentation.Players.PlayerState.Active:
            IMediaPlaybackControl mpc = player as IMediaPlaybackControl;
            if (mpc == null)
              return PlaybackState.Playing;
            if (mpc.IsPaused)
              return PlaybackState.Paused;
            else if (mpc.IsSeeking)
              return PlaybackState.Seeking;
            else
              return PlaybackState.Playing;
          case Presentation.Players.PlayerState.Ended:
            return PlaybackState.Ended;
          case Presentation.Players.PlayerState.Stopped:
            return PlaybackState.Stopped;
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
      return DoPlay(item, StartTime.AtOnce);
    }

    public bool DoPlay(IResourceLocator locator, string mimeType, string mediaItemTitle)
    {
      return DoPlay(locator, mimeType, mediaItemTitle, StartTime.AtOnce);
    }

    public IEnumerable<AudioStreamDescriptor> GetAudioStreamDescriptors()
    {
      IVideoPlayer player = CurrentPlayer as IVideoPlayer;
      if (player == null)
        yield break;
      ICollection<string> audioStreamNames = player.AudioStreams;
      foreach (string streamName in audioStreamNames)
        yield return new AudioStreamDescriptor(this, player.Name, streamName);
    }

    public void OverrideGeometry(IGeometry geometry)
    {
      IPlayerSlotController slotController = _slotController;
      if (slotController == null)
        return;
      IVideoPlayer player = CurrentPlayer as IVideoPlayer;
      if (player == null)
        return;
      bool changed = player.GeometryOverride != geometry;
      player.GeometryOverride = geometry;
      if (changed)
        PlayerGeometryMessaging.SendGeometryChangedMessage(slotController.SlotIndex);
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
        if (IsValid && _slotController.ContextVariables.TryGetValue(key, out result))
          return result;
      }
      return null;
    }

    public void Close()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseSlot(_slotController);
    }

    public void Stop()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return;
      lock (SyncObj)
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
      if (player.State == Presentation.Players.PlayerState.Active)
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

    public bool PreviousItem()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      // Locking not necessary here. If a lock should be placed in future, be aware that the DoPlay method
      // will lock the PM as well
      int countLeft = _playlist.ItemList.Count; // Limit number of tries to current playlist size. If the PL doesn't contain any playable item, this avoids an endless loop.
      do // Loop: Try until we find an item which is able to play
      {
        if (--countLeft < 0 || !_playlist.HasPrevious) // Break loop if we don't have any more items left
          return false;
      } while (!DoPlay(_playlist.GetPrevious(), StartTime.AtOnce));
      return true;
    }

    public bool NextItem()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      // Locking not necessary here. If a lock should be placed in future, be aware that the DoPlay method
      // will lock the PM as well
      int countLeft = _playlist.ItemList.Count; // Limit number of tries to current playlist size. If the PL doesn't contain any playable item, this avoids an endless loop.
      bool playOk = true;
      do // Loop: Try until we find an item which is able to play
      {
        if (--countLeft < 0 || !_playlist.HasNext) // Break loop if we don't have any more items left
        {
          if (!playOk && CloseWhenFinished)
            // Close PSC if we failed to play
            Close();
          return false;
        }
        playOk = DoPlay(_playlist.GetNext(), StartTime.AtOnce);
      } while (!playOk);
      return true;
    }

    #endregion
  }
}
