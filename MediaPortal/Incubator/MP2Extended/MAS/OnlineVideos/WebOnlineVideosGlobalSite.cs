using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos
{
  class WebOnlineVideosGlobalSite : WebObject, ITitleSortable
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Creator { get; set; }
    public string Language { get; set; }
    public bool IsAdult { get; set; }
    public WebOnlineVideosSiteState State { get; set; }
    public uint ReportCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool Added { get; set; }

    public override string ToString()
    {
      return Title;
    }
  }
}
