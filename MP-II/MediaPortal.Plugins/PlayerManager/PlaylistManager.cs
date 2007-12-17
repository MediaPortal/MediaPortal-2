#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Players;
using MediaPortal.Core.MediaManager;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Plugins.PlayerManager
{
  public class PlaylistManager : IPlaylistManager
  {
    #region IPlaylistManager Members
    IPlaylist _playlist = new PlayList();
    int _currentIndex = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistManager"/> class.
    /// </summary>
    public PlaylistManager()
    {
      IQueue queue = ServiceScope.Get<IMessageBroker>().Get("players");
      queue.OnMessageReceive += new MessageReceivedHandler(queue_OnMessageReceive);
      PlayerCollection players = ServiceScope.Get<PlayerCollection>();
    }


    /// <summary>
    /// Gets or sets the play list.
    /// </summary>
    /// <value>The play list.</value>
    public IPlaylist PlayList
    {
      get
      {
        return _playlist;
      }
      set
      {
        _playlist = value;
      }
    }

    /// <summary>
    /// Gets or sets the current item playing.
    /// </summary>
    /// <value>The current item playing.</value>
    public int CurrentItemPlaying
    {
      get
      {
        if (_currentIndex >= 0 && _currentIndex < _playlist.Queue.Count)
          return -1;

        PlayerCollection players = ServiceScope.Get<PlayerCollection>();
        if (players.Count > 0)
        {
          if (players[0].MediaItem == _playlist.Queue[_currentIndex])
          {
            return _currentIndex;
          }
        }
        return -1;
      }
    }

    /// <summary>
    /// Gets the current media item playing.
    /// </summary>
    /// <value>The current media item playing.</value>
    public IMediaItem CurrentMediaItemPlaying
    {
      get
      {
        if (_currentIndex >= 0 && _currentIndex < _playlist.Queue.Count)
        {
          PlayerCollection players = ServiceScope.Get<PlayerCollection>();
          if (players.Count > 0)
          {
            if (players[0].MediaItem == _playlist.Queue[_currentIndex])
            {
              return _playlist.Queue[_currentIndex];
            }
          }
        }
        return null;
      }
    }

    /// <summary>
    /// Plays the next item from the playlist
    /// </summary>
    public void PlayNext()
    {
      if (_currentIndex + 1 < _playlist.Queue.Count)
      {
        _currentIndex++;
        Play();
        SendMsgCurrentItemChanged(true);
      }
    }

    /// <summary>
    /// Plays the previous item from the playlist
    /// </summary>
    public void PlayPrevious()
    {
      if (_currentIndex > 0)
      {
        _currentIndex--;
        Play();
        SendMsgCurrentItemChanged(true);
      }
    }

    /// <summary>
    /// Plays the media item at the specified index from the playlist
    /// </summary>
    /// <param name="index">The index.</param>
    public void PlayAt(int index)
    {
      if (index >= 0 && index < _playlist.Queue.Count)
      {
        _currentIndex = index;
        Play();
        SendMsgCurrentItemChanged(true);
      }
    }

    /// <summary>
    /// Shuffles the playlist.
    /// </summary>
    public void Shuffle()
    {
      IMediaItem current = CurrentMediaItemPlaying;
      _playlist.Shuffle();
      for (int i = 0; i < _playlist.Queue.Count; ++i)
      {
        if (_playlist.Queue[i] == current)
        {
          _currentIndex = i;
          break;
        }
      }
      SendMsgCurrentItemChanged(true);
    }

    /// <summary>
    /// Plays this instance.
    /// </summary>
    void Play()
    {
      if (_currentIndex >= 0 && _currentIndex < _playlist.Queue.Count)
      {
        IPlayerFactory factory = ServiceScope.Get<IPlayerFactory>();
        IPlayer player = factory.GetPlayer(_playlist.Queue[_currentIndex]);
        if (player != null)
        {
          PlayerCollection players = ServiceScope.Get<PlayerCollection>();
          if (player.Name == "Bass")
          {
            // Only add the Player once
            if (!players.CollectionContainsPlayer(player))
              players.Add(player);
          }
          else
          {
            players.Dispose();
            players.Add(player);
          }

          player.Play(_playlist.Queue[_currentIndex]);
        }
      }
    }


    void queue_OnMessageReceive(MPMessage message)
    {
      IPlayer player = message.MetaData["player"] as IPlayer;
      string action = message.MetaData["action"] as string;
      if (action == "nextfile")
      {
        //...start next song of the playlist
        if (player.MediaItem == CurrentMediaItemPlaying)
        {
          CurrentMediaItemPlaying.MetaData["playlistplayed"] = true;
          Remove(player.MediaItem);
          PlayNext();
        }
      }
    }

    void Remove(IMediaItem item)
    {
      for (int i = 0; i < _playlist.Queue.Count; ++i)
      {
        if (_playlist.Queue[i] == item)
        {
          _playlist.Queue.Remove(item);
          if (_currentIndex >= i)
            _currentIndex--;
        }
      }
      SendMsgCurrentItemChanged(true);
    }

    void SendMsgCurrentItemChanged(bool refreshAll)
    {
      IQueue queue = ServiceScope.Get<IMessageBroker>().Get("playlist");
      MPMessage message = new MPMessage();
      message.MetaData["action"] = "changed";
      message.MetaData["refreshAll"] = refreshAll;
      queue.Send(message);
    }
    #endregion
  }
}
