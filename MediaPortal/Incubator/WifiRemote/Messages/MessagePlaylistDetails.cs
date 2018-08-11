#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Plugins.WifiRemote.Messages.Playlist;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
    /// <summary>
    /// Represents a MP playlist
    /// </summary>
    public class MessagePlaylistDetails : IMessage
    {
        public string Type
        {
            get { return "playlistdetails"; }
        }

        /// <summary>
        /// Repeat mode of the playlist
        /// </summary>
        public bool PlaylistRepeat { get; set; }

        /// <summary>
        /// Name of the playlist
        /// </summary>
        public String PlaylistName { get; set; }

        /// <summary>
        /// Type of the playlist (currently supported: music, video)
        /// </summary>
        public String PlaylistType { get; set; }

        /// <summary>
        /// List of all items in this playlist
        /// </summary>
        public List<PlaylistEntry> PlaylistItems { get; set; }
    }
}
