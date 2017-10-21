namespace MediaPortal.Extensions.MediaServer.Objects.Basic
{
    public class Resource : IDirectoryResource
    {
        public string Uri { get; set; }

        public ulong Size { get; set; }

        public string Duration { get; set; }

        public uint BitRate { get; set; }

        public uint SampleFrequency { get; set; }

        public uint BitsPerSample { get; set; }

        public uint NumberOfAudioChannels { get; set; }

        public string Resolution { get; set; }

        public uint ColorDepth { get; set; }

        public string ProtocolInfo { get; set; }

        public string Protection { get; set; }

        public string ImportUri { get; set; }

        public string DlnaIfoFileUrl { get; set; }
    }
}
