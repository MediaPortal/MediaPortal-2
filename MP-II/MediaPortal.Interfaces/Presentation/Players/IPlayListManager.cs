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
  public interface IPlaylistManager
  {
    /// <summary>
    /// Gets or sets the play list.
    /// </summary>
    /// <value>The play list.</value>
    IPlaylist PlayList { get;set;}

    /// <summary>
    /// Gets or sets the current item playing.
    /// </summary>
    /// <value>The current item playing.</value>
    int CurrentItemPlaying { get;}

    /// <summary>
    /// Gets the current media item playing.
    /// </summary>
    /// <value>The current media item playing.</value>
    IMediaItem CurrentMediaItemPlaying { get;}

    /// <summary>
    /// Plays the next item from the playlist
    /// </summary>
    void PlayNext();

    /// <summary>
    /// Plays the previous item from the playlist
    /// </summary>
    void PlayPrevious();

    /// <summary>
    /// Plays the media item at the specified index from the playlist
    /// </summary>
    /// <param name="index">The index.</param>
    void PlayAt(int index);

    /// <summary>
    /// Shuffles the playlist.
    /// </summary>
    void Shuffle();

    /// <summary>
    /// Loads a playlist.
    /// </summary>
    void Load(string fileName);
  }
}
