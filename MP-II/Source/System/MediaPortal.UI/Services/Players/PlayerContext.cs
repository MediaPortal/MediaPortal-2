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
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Presentation.Players;

namespace MediaPortal.Services.Players
{
  public class PlayerContext : IPlayerContext, IDisposable
  {
    #region Protected fields

    protected bool _closeWhenFinished = false;

    protected IPlayerSlotController _slotController;
    protected PlayerContextManager _contextManager;
    protected IPlaylist _playlist;

    protected PlayerContextType _type;

    #endregion

    #region Ctor

    internal PlayerContext(PlayerContextManager contextManager, IPlayerSlotController slotController, PlayerContextType type)
    {
      _contextManager = contextManager;
      _slotController = slotController;
      _playlist = new Playlist();
      _type = type;
    }

    public void Dispose()
    {
      _slotController = null;
    }

    #endregion

    protected static bool GetItemData(MediaItem item, out IMediaItemLocator locator, out string mimeType)
    {
      locator = null;
      mimeType = null;
      if (item == null)
        return false;
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      locator = mediaManager.GetMediaItemLocator(item);
      MediaItemAspect mediaAspect = item[MediaAspect.ASPECT_ID];
      mimeType = (string) mediaAspect[MediaAspect.ATTR_MIME_TYPE];
      return locator != null;
    }

    protected IPlayer GetCurrentPlayer()
    {
      return IsValid && _slotController.IsActive ? _slotController.CurrentPlayer : null;
    }

    #region IPlayerContext implementation

    public PlayerContextType MediaType
    {
      get { return _type; }
    }

    public bool IsValid
    {
      get { return _slotController != null; }
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
      get { return IsValid && _slotController.IsActive ? _slotController.CurrentPlayer : null; }
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

    public bool DoPlay(MediaItem item)
    {
      IMediaItemLocator locator;
      string mimeType;
      if (!GetItemData(item, out locator, out mimeType))
        return false;
      return DoPlay(locator, mimeType);
    }

    public bool DoPlay(IMediaItemLocator locator, string mimeType)
    {
      if (!IsValid)
        return false;
      return _slotController.Play(locator, mimeType);
    }

    public void Close()
    {
      Dispose();
    }

    public void SetContextVariable(string key, object value)
    {
      if (IsValid)
        _slotController.ContextVariables[key] = value;
    }

    public void ResetContextVariable(string key)
    {
      if (IsValid)
        _slotController.ContextVariables.Remove(key);
    }

    public object GetContextVariable(string key)
    {
      object result;
      if (IsValid && _slotController.ContextVariables.TryGetValue(key, out result))
        return result;
      return null;
    }

    public void Stop()
    {
      if (!IsValid)
        return;
      _slotController.Stop();
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
      MediaItem item = _playlist.Previous();
      return item != null && DoPlay(item);
    }

    public bool NextItem()
    {
      MediaItem item = _playlist.Next();
      return item != null && DoPlay(item);
    }

    #endregion
  }
}
