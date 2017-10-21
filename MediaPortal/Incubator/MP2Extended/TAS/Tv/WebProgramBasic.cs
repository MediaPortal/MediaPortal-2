using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebProgramBasic
    {
        public string Description { get; set; }
        public DateTime EndTime { get; set; }
        public int ChannelId { get; set; }
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public string Title { get; set; }

        // additional properties
        public int DurationInMinutes { get; set; }
        public bool IsScheduled { get; set; }
    }
}
