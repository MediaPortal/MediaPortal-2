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
