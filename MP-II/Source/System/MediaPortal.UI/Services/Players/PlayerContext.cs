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
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;

namespace MediaPortal.Services.Players
{
  public class PlayerContext : IPlayerContext, IDisposable
  {
    #region Protected fields

    protected bool _closeWhenFinished = false;

    protected IPlayerSlotController _slotController;
    protected PlayerContextManager _contextManager;
    protected IPlaylist _playlist;
    protected string _name;

    protected PlayerContextType _type;

    #endregion

    #region Ctor

    internal PlayerContext(PlayerContextManager contextManager, IPlayerSlotController slotController,
        PlayerContextType type, string name)
    {
      _contextManager = contextManager;
      _slotController = slotController;
      _playlist = new Playlist();
      _type = type;
      _name = name;
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

    protected static bool GetItemData(MediaItem item, out IMediaItemLocator locator, out string mimeType,
        out string mediaItemTitle)
    {
      locator = null;
      mimeType = null;
      mediaItemTitle = null;
      if (item == null)
        return false;
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      locator = mediaManager.GetMediaItemLocator(item);
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

    public bool PushFullscreenContentWorkflowState()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
        return false;
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(player.FullscreenContentWorkflowStateId);
      return true;
    }

    #region IPlayerContext implementation

    public bool IsValid
    {
      get { return _slotController != null; }
    }

    public PlayerContextType MediaType
    {
      get { return _type; }
    }

    public IPlaylist Playlist
    {
      get { return _playlist; }
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
        return player == null ? PlaybackState.Stopped : player.State;
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

    public bool DoPlay(MediaItem item)
    {
      IMediaItemLocator locator;
      string mimeType;
      string mediaItemTitle;
      if (!GetItemData(item, out locator, out mimeType, out mediaItemTitle))
        return false;
      return DoPlay(locator, mimeType, mediaItemTitle);
    }

    public bool DoPlay(IMediaItemLocator locator, string mimeType, string mediaItemTitle)
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      lock (SyncObj)
        if (!psc.IsActive)
          return false;
        else
          return psc.Play(locator, mimeType, mediaItemTitle);
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

    public bool PushCurrentlyPlayingWorkflowState()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
        return false;
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(player.CurrentlyPlayingWorkflowStateId);
      return true;
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
      IPlayer player = GetCurrentPlayer();
      if (player == null)
        return;
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
      player.Resume();
    }

    public void TogglePlayPause()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
      {
        NextItem();
        return;
      }
      if (player.State == PlaybackState.Playing)
        player.Pause();
      else if (player.State == PlaybackState.Paused)
        player.Resume();
      else
        player.Restart();
    }

    public void Restart()
    {
      IPlayer player = GetCurrentPlayer();
      if (player == null)
      {
        NextItem();
        return;
      }
      player.Restart();
    }

    public bool PreviousItem()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      // Locking not necessary here. If a lock should be placed in future, be aware that the DoPlay method
      // will lock the PM as well
      MediaItem item = _playlist.Previous();
      return item != null && DoPlay(item);
    }

    public bool NextItem()
    {
      IPlayerSlotController psc = _slotController;
      if (psc == null)
        return false;
      // Locking not necessary here. If a lock should be placed in future, be aware that the DoPlay method
      // will lock the PM as well
      MediaItem item = _playlist.Next();
      return item != null && DoPlay(item);
    }

    #endregion
  }
}
