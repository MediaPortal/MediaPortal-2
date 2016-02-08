using System;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.WSS.General
{
    public class WebStreamingSession
    {
        public string Profile { get; set; }
        public string Identifier { get; set; }

        public WebMediaType SourceType { get; set; }
        public string SourceId { get; set; }
        public string DisplayName { get; set; }

        public string ClientDescription { get; set; }
        // Do not actually parse this as an IP address, because it isn't: it might have some comments in it too
        public string ClientIPAddress { get; set; }
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The place where the user started the playback. 
        /// </summary>
        public long StartPosition { get; set; }

        /// <summary>
        /// The current place of the player.
        /// </summary>
        public long PlayerPosition { get; set; }

        /// <summary>
        /// Percentage of the file the user is at
        /// </summary>
        public int PercentageProgress { get; set; }

        public WebTranscodingInfo TranscodingInfo { get; set; }
    }
}
