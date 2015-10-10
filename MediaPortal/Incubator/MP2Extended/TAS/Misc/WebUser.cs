using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc
{
    public class WebUser
    {
        public int CardId { get; set; }
        public DateTime HeartBeat { get; set; }
        public int ChannelId { get; set; }
        public bool IsAdmin { get; set; }
        public string Name { get; set; }
        public int SubChannel { get; set; }
        public int TvStoppedReason { get; set; }
    }
}
