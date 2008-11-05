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
using MediaPortal.Media.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  public interface IPlaylist
  {
    /// <summary>
    /// Returns a list of all queued media to play.
    /// </summary>
    /// <value>The queue.</value>
    List<IMediaItem> Queue { get;}

    /// <summary>
    /// Clears the playlist.
    /// </summary>
    void Clear();

    /// <summary>
    /// Adds the specified media item to the playlist
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    void Add(IMediaItem mediaItem);

    /// <summary>
    /// Removes the specified media item from the playlist
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    void Remove(IMediaItem mediaItem);

    /// <summary>
    /// Removes the media at the specified index item from the playlist
    /// </summary>
    /// <param name="index">The index.</param>
    void RemoveAt(int index);

    /// <summary>
    /// moves the media at the specified index 1item up in the playlist
    /// </summary>
    /// <param name="index">The index.</param>
    void MoveUp(int index);

    /// <summary>
    /// moves the media at the specified index 1 item down in the playlist
    /// </summary>
    /// <param name="index">The index.</param>
    void MoveDown(int index);

    /// <summary>
    /// Inserts a new  media item after the media item specified.
    /// </summary>
    /// <param name="item">The new media item.</param>
    /// <param name="afterThisItem">The media item after which the new media item should be inserted.</param>
    /// <returns></returns>
    bool Insert(IMediaItem item, IMediaItem afterThisItem);
    
    /// <summary>
    /// Gets a value indicating whether all items have been played
    /// </summary>
    /// <value><c>true</c> if all items have been played; otherwise, <c>false</c>.</value>
    bool AllPlayed { get;}

    /// <summary>
    /// Resets the status for all items to not-played
    /// </summary>
    void ResetStatus();

    /// <summary>
    /// Shuffles the playlist.
    /// </summary>
    void Shuffle();
  }
}
