using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebChannelDetailed : WebChannelBasic
    {
        public WebProgramDetailed CurrentProgram { get; set; }
        public WebProgramDetailed NextProgram { get; set; }
        public bool EpgHasGaps { get; set; }
        public string ExternalId { get; set; }
        public int FreeToAir { get; set; }
        public bool GrabEpg { get; set; }
        public IList<string> GroupNames { get; set; }
        public bool IsChanged { get; set; }
        public DateTime LastGrabTime { get; set; }
        public int TimesWatched { get; set; }
        public DateTime TotalTimeWatched { get; set; }
        public bool VisibleInGuide { get; set; }
    }
}
