#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;

namespace UiComponents.Media.Models
{
  /// <summary>
  /// Base class for playlist navigation models.
  /// </summary>
  public abstract class BasePlaylistModel
  {
    #region Consts

    public const string AUDIO_PLAYLIST_RES = "[Media.AudioPlaylist]";
    public const string VIDEO_PLAYLIST_RES = "[Media.VideoPlaylist]";
    public const string PIP_PLAYLIST_RES = "[Media.PiPPlaylist]";

    public const string KEY_NUMBERSTR = "NumberStr";
    public const string KEY_INDEX = "Playlist-Index";

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();
    protected IPlaylist _playlist = null;
    protected AbstractProperty _playlistHeaderProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _numItemsStrProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _isPlaylistEmptyProperty = new WProperty(typeof(bool), false);

    #endregion

    protected virtual void PlayItem(int index)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      lock (_syncObj)
        if (pc == null || pc.Playlist != _playlist)
          return;
      playlist.ItemListIndex = index;
      pc.DoPlay(playlist.Current);
    }

    protected virtual bool RemoveItem(int index)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      lock (_syncObj)
        if (pc == null || pc.Playlist != _playlist)
          return false;
      playlist.RemoveAt(index);
      return true;
    }

    protected virtual bool MoveItemUp(int index, ListItem item)
    {
      IPlaylist playlist = _playlist;
      if (playlist == null)
        return false;
      lock (playlist.SyncObj)
      {
        if (index <= 0 || index >= playlist.ItemList.Count)
          return false;
        playlist.Swap(index, index - 1);
        return true;
      }
    }

    protected virtual bool MoveItemDown(int index, ListItem item)
    {
      IPlaylist playlist = _playlist;
      if (playlist == null)
        return false;
      lock (playlist.SyncObj)
      {
        if (index < 0 || index >= playlist.ItemList.Count - 1)
          return false;
        playlist.Swap(index, index + 1);
        return true;
      }
    }

    protected bool TryGetIndex(ListItem item, out int index)
    {
      index = -1;
      if (item == null)
        return false;
      object oIndex;
      if (item.AdditionalProperties.TryGetValue(KEY_INDEX, out oIndex))
      {
        int? i = oIndex as int?;
        if (i.HasValue)
        {
          index = i.Value;
          return true;
        }
      }
      return false;
    }

    protected void UpdatePlaylistHeader(PlayerContextType? mediaType, int slotIndex)
    {
      if (!mediaType.HasValue)
      {
        PlaylistHeader = null;
        return;
      }
      switch (mediaType.Value)
      {
        case PlayerContextType.Audio:
          PlaylistHeader = AUDIO_PLAYLIST_RES;
          break;
        case PlayerContextType.Video:
          PlaylistHeader = slotIndex == PlayerManagerConsts.PRIMARY_SLOT ?
              VIDEO_PLAYLIST_RES : PIP_PLAYLIST_RES;
          break;
        default:
          // Unknown player context type
          PlaylistHeader = null;
          break;
      }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty PlaylistHeaderProperty
    {
      get { return _playlistHeaderProperty; }
    }

    public string PlaylistHeader
    {
      get { return (string) _playlistHeaderProperty.GetValue(); }
      internal set { _playlistHeaderProperty.SetValue(value); }
    }

    public AbstractProperty NumItemsStrProperty
    {
      get { return _numItemsStrProperty; }
    }

    public string NumItemsStr
    {
      get { return (string) _numItemsStrProperty.GetValue(); }
      internal set { _numItemsStrProperty.SetValue(value); }
    }

    public AbstractProperty IsPlaylistEmptyProperty
    {
      get { return _isPlaylistEmptyProperty; }
    }

    public bool IsPlaylistEmpty
    {
      get { return (bool) _isPlaylistEmptyProperty.GetValue(); }
      internal set { _isPlaylistEmptyProperty.SetValue(value); }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item of the playlist.
    /// The item will be played.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void Play(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        PlayItem(index);
    }

    /// <summary>
    /// Provides a callable method for the skin to remove an item from the playlist.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void Remove(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        RemoveItem(index);
    }

    /// <summary>
    /// Provides a callable method for the skin to move the given playlist <paramref name="item"/> up in the playlist.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void MoveUp(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        MoveItemUp(index, item);
    }

    /// <summary>
    /// Provides a callable method for the skin to move the given playlist <paramref name="item"/> down in the playlist.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void MoveDown(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        MoveItemDown(index, item);
    }

    #endregion
  }
}
