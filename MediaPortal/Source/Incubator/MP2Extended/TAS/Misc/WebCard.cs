using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc
{
    public class WebCard
    {
        public bool CAM { get; set; }
        public int CamType { get; set; }
        public int DecryptLimit { get; set; }
        public string DevicePath { get; set; }
        public bool Enabled { get; set; }
        public bool GrabEPG { get; set; }
        public int Id { get; set; }
        public bool IsChanged { get; set; }
        public DateTime LastEpgGrab { get; set; }
        public string Name { get; set; }
        public int NetProvider { get; set; }
        public bool PreloadCard { get; set; }
        public int Priority { get; set; }
        public string RecordingFolder { get; set; }
        public int RecordingFormat { get; set; }
        public bool SupportSubChannels { get; set; }
        public string TimeShiftFolder { get; set; }
    }
}
