using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
	class MessagePlaylists : IMessage
    {
        public string Type
        {
            get { return "playlists"; }
        }

        /// <summary>
        /// List of available playlists
        /// </summary>
        public List<String> PlayLists { get; set; }
	}
}
