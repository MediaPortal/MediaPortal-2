using System;

namespace MediaPortal.Plugins.WifiRemote.Messages.Playlist
{
    /// <summary>
    /// One item of a MP playlist
    /// </summary>
    public class PlaylistEntry
    {
        /// <summary>
        /// Name of the file that will get displayed in the playlist
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// Second Name of the file that will get displayed in the playlist (e.g. album)
        /// </summary>
        public String Name2 { get; set; }

        /// <summary>
        /// Album Artist of the file that will get displayed in the playlist
        /// </summary>
        public String AlbumArtist { get; set; }

        /// <summary>
        /// Title of the file that will get displayed in the playlist
        /// </summary>
        public String Title { get; set; }

        /// <summary>
        /// Full path to the file
        /// </summary>
        public String FileName { get; set; }

        /// <summary>
        /// Duration of the file
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Indicates if the item has been played already
        /// </summary>
        public bool Played { get; set; }

        /// <summary>
        /// MpExtended Id (optional)
        /// </summary>
        public string MpExtId{ get; set; }

        /// <summary>
        /// MpExtended Media Type (optional)
        /// </summary>
        public int MpExtMediaType{ get; set; }

        /// <summary>
        /// MpExtended Provider (optional)
        /// </summary>
        public int MpExtProviderId{ get; set; }
    }
}
