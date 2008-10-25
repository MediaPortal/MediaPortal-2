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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Messaging;
using MediaPortal.Media.MetaData;
using MediaPortal.Media.MediaManager;


namespace Models.PlayList
{
  public class PlayList
  {
    private readonly ItemsCollection _playList;
    ListItem _selectedItem;

    public PlayList()
    {
      _playList = new ItemsCollection();
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("playlist");
      queue.OnMessageReceive += new MessageReceivedHandler(onPlayListMessage);
    }

    void onPlayListMessage(QueueMessage message)
    {
      Refresh();
      _playList.FireChange();
    }

    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("playlist-main"));

      }
    }
    void MapMetaData(IMetaDataMappingCollection mapping, int sortMethod, IDictionary<string, object> localMetaData, IAbstractMediaItem mediaItem, ListItem newItem)
    {
      IDictionary<string, object> metadata;
      if (mediaItem == null)
      {
        metadata = localMetaData;
      }
      else
      {
        metadata = mediaItem.MetaData;
      }
      if (sortMethod >= mapping.Mappings.Count)
        sortMethod = 0;
      if (mapping != null)
      {
        foreach (IMetadataMappingItem item in mapping.Mappings[sortMethod].Items)
        {
          if (localMetaData != null && localMetaData.ContainsKey(item.MetaDataField))
          {
            string text = item.Formatter.Format(localMetaData[item.MetaDataField], item.Formatting);
            if (newItem.Contains(item.SkinLabel))
            {
              newItem.Labels.Remove(item.SkinLabel);
            }
            newItem.Add(item.SkinLabel, text);
          }
          else if (metadata.ContainsKey(item.MetaDataField))
          {
            string text = item.Formatter.Format(metadata[item.MetaDataField], item.Formatting);
            if (newItem.Contains(item.SkinLabel))
            {
              newItem.Labels.Remove(item.SkinLabel);
            }
            newItem.Add(item.SkinLabel, text);
          }
        }
      }
    }
    /// <summary>
    /// provides a collection of moves to the skin
    /// </summary>
    /// <value>The movies.</value>
    public ItemsCollection PlayListItems
    {
      get
      {
        Refresh();
        return _playList;
      }
    }
    void Refresh()
    {
      _playList.Clear();
      IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
      IMetaDataMappingCollection maps = ServiceScope.Get<IMetadataMappingProvider>().Get("music-playlist");
      foreach (IMediaItem mediaItem in playlistMgr.PlayList.Queue)
      {
        PlayListItem item = new PlayListItem(mediaItem);
        MapMetaData(maps, 0, new Dictionary<string, object>(), mediaItem, item);

        if (playlistMgr.CurrentMediaItemPlaying == mediaItem)
        {
          item.Add("isplaying", "true");
        }
        else
        {
          item.Add("isplaying", "false");
        }
        _playList.Add(item);
      }
    }

    /// <summary>
    /// starts playing the playlist
    /// </summary>
    public void Play()
    {
      IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
      playlistMgr.PlayAt(0);
    }

    /// <summary>
    /// starts playing an item from the playlist
    /// </summary>
    public void PlayItem(ListItem item)
    {
      PlayListItem playListItem = item as PlayListItem;
      if (playListItem == null) return;
      IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
      for (int i = 0; i < playlistMgr.PlayList.Queue.Count; ++i)
      {
        if (playListItem.MediaItem == playlistMgr.PlayList.Queue[i])
        {
          playlistMgr.PlayAt(i);
          return;
        }
      }
    }

    public void OnSelectionChange(ListItem item)
    {
      if (item == null) return;
      SelectedItem = item;
    }
    /// <summary>
    /// allows skin to set/get the current selected list item
    /// </summary>
    /// <value>The selected item.</value>
    public ListItem SelectedItem
    {
      get
      {
        return _selectedItem;
      }
      set
      {
        if (_selectedItem != value)
        {
          _selectedItem = value;
        }
      }
    }
    /// <summary>
    /// Clears the playlist.
    /// </summary>
    public void Clear()
    {
      IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
      playlistMgr.PlayList.Clear();
    }
    public void Shuffle()
    {
      IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
      playlistMgr.Shuffle();
    }
    public void MoveUp()
    {
      for (int i = 0; i < _playList.Count; ++i)
      {
        if (SelectedItem == _playList[i])
        {
          IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
          playlistMgr.PlayList.MoveUp(i);
          break;
        }
      }
    }
    public void MoveDown()
    {
      for (int i = 0; i < _playList.Count; ++i)
      {
        if (SelectedItem == _playList[i])
        {
          IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
          playlistMgr.PlayList.MoveDown(i);
          break;
        }
      }
    }
    public void RemoveFromPlayList()
    {
      for (int i = 0; i < _playList.Count; ++i)
      {
        if (SelectedItem == _playList[i])
        {
          IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
          playlistMgr.PlayList.RemoveAt(i);
          return;
        }
      }
    }

    #region Load / Save Playlist

    /// <summary>
    /// Gets the available Playlists from the Playlist Directory and exposes them to the skin
    /// </summary>
    /// <value>The zoom modes.</value>
    public ItemsCollection StoredPlayLists
    {
      get
      {
        ItemsCollection items = new ItemsCollection();
        // FIXME Albert78: No constants in the code! Move it to PathManager.
        string playlistfolder = @"C:\My Playlists";
        foreach (string file in Directory.GetFiles(playlistfolder, "*.*"))
        {
          ListItem item = new ListItem("Name", Path.GetFileNameWithoutExtension(file));
          item.Add("FullPath", file);
          items.Add(item);
        }
        return items;
      }
    }

    /// <summary>
    /// Loads the Selected Playlist
    /// </summary>
    /// <param name="item">The item.</param>
    public void LoadPlayList(ListItem item)
    {
      string fullpath = item.Label("FullPath", "").Evaluate();
      IPlaylistManager playlistMgr = ServiceScope.Get<IPlaylistManager>();
      playlistMgr.Load(fullpath);
    }

    #endregion
  }
}
