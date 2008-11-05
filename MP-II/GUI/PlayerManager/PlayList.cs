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
using System.Text;
using MediaPortal.Core;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Messaging;

using MediaPortal.Media.MediaManagement;

namespace Components.Services.PlayerManager
{
  public class PlayList : IPlaylist
  {
    List<IMediaItem> _queue = new List<IMediaItem>();

    #region IPlaylist Members

    /// <summary>
    /// Returns a list of all queued media to play.
    /// </summary>
    /// <value>The queue.</value>
    public List<IMediaItem> Queue
    {
      get
      {
        return _queue;
      }
    }

    /// <summary>
    /// Clears the playlist.
    /// </summary>
    public void Clear()
    {
      _queue.Clear();
      SendQueueChangeMessage(true);
    }

    /// <summary>
    /// Adds the specified media item to the playlist
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    public void Add(IMediaItem mediaItem)
    {
      mediaItem.MetaData["playlistplayed"] = false;
      _queue.Add(mediaItem);
      SendQueueChangeMessage(true);
    }

    /// <summary>
    /// Removes the specified media item from the playlist
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    public void Remove(IMediaItem mediaItem)
    {
      _queue.Remove(mediaItem);
      SendQueueChangeMessage(true);
    }

    /// <summary>
    /// Removes the media at the specified index item from the playlist
    /// </summary>
    /// <param name="index">The index.</param>
    public void RemoveAt(int index)
    {
      _queue.RemoveAt(index);
      SendQueueChangeMessage(true);
    }
    public void MoveUp(int index)
    {
      if (index <= 0) return;
      if (index >= _queue.Count) return;
      IMediaItem mediaItem = _queue[index];
      _queue.RemoveAt(index);
      _queue.Insert(index - 1, mediaItem);
      SendQueueChangeMessage(false);
    }
    public void MoveDown(int index)
    {
      if (index < 0) return;
      if (index + 1 >= _queue.Count) return;
      IMediaItem mediaItem = _queue[index];
      _queue.Insert(index + 2, mediaItem);
      _queue.RemoveAt(index);
      SendQueueChangeMessage(false);
    }
    /// <summary>
    /// Inserts a new  media item after the media item specified.
    /// </summary>
    /// <param name="item">The new media item.</param>
    /// <param name="afterThisItem">The media item after which the new media item should be inserted.</param>
    /// <returns></returns>
    public bool Insert(IMediaItem item, IMediaItem afterThisItem)
    {
      for (int i = 0; i < _queue.Count; ++i)
      {
        if (_queue[i] == afterThisItem)
        {
          item.MetaData["playlistplayed"] = false;
          _queue.Insert(i + 1, item);
          SendQueueChangeMessage(true);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Gets a value indicating whether all items have been played
    /// </summary>
    /// <value>
    /// 	<c>true</c> if all items have been played; otherwise, <c>false</c>.
    /// </value>
    public bool AllPlayed
    {
      get
      {
        foreach (IMediaItem item in _queue)
        {
          if (item.MetaData.ContainsKey("playlistplayed"))
          {
            bool played = (bool)item.MetaData["playlistplayed"];
            if (!played) return false;
          }
        }
        return true;
      }
    }

    /// <summary>
    /// Resets the status for all items to not-played
    /// </summary>
    public void ResetStatus()
    {
      foreach (IMediaItem item in _queue)
      {
        item.MetaData["playlistplayed"] = false;
      }
      SendQueueChangeMessage(false);
    }

    /// <summary>
    /// Shuffles the playlist.
    /// </summary>
    public void Shuffle()
    {
      Random r = new System.Random(DateTime.Now.Millisecond);

      // iterate through each catalogue item performing arbitrary swaps
      for (int item = 0; item < _queue.Count; item++)
      {
        int nArbitrary = r.Next(_queue.Count);

        IMediaItem anItem = _queue[nArbitrary];
        _queue[nArbitrary] = _queue[item];
        _queue[item] = anItem;
      }
    }

    void SendQueueChangeMessage(bool refreshAll)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("playlist");
      QueueMessage message = new QueueMessage();
      message.MessageData["action"] = "changed";
      message.MessageData["refreshAll"] = refreshAll;
      queue.Send(message);
    }

    #endregion
  }
}
