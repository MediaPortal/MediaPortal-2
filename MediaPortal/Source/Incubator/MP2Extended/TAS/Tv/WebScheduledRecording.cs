using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebScheduledRecording
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; }
        public string ChannelName { get; set; }
        public int ChannelId { get; set; }
        public int ScheduleId { get; set; }
        public int ProgramId { get; set; }
    }
}
