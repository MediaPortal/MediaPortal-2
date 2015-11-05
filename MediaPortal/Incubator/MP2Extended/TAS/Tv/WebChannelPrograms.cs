using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebChannelPrograms<TProgram> where TProgram : WebProgramBasic
    {
        public int ChannelId { get; set; }
        public IList<TProgram> Programs { get; set; }
    }
}
