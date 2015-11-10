namespace MediaPortal.Plugins.MP2Extended.WSS.General
{
    public class WebTranscodingInfo
    {
        /// <summary>
        /// The amount of video that has already been transcoded (0 is the start of playback position).
        /// </summary>
        public long TranscodedTime { get; set; }

        /// <summary>
        /// The number of frames already transcoded.
        /// </summary>
        public long TranscodedFrames { get; set; }

        /// <summary>
        /// The position in the file at which the transcoder currently is (0 is the start of the file).
        /// </summary>
        public long TranscodingPosition { get; set; }

        /// <summary>
        /// The framerate at which the transcoder is currently transcoding in frames per second. Doesn't have to be the framerate of the output stream.
        /// </summary>
        public long TranscodingFPS { get; set; } 

        /// <summary>
        /// The current bitrate of the output stream in kbit/s.
        /// </summary>
        public long OutputBitrate { get; set; }

        // whether or not getting the transcoding info is supported
        public bool Supported { get; set; }
        // is the transcoding finished?
        public bool Finished { get; set; }
        // did the transcoding fail?
        public bool Failed { get; set; }
    }
}