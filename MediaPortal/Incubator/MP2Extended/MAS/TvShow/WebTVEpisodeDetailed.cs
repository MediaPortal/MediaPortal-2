using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.TvShow
{
  public class WebTVEpisodeDetailed : WebTVEpisodeBasic, IGuestStars
  {
    public WebTVEpisodeDetailed() : base()
    {
      GuestStars = new List<WebActor>();
      Directors = new List<string>();
      Writers = new List<string>();
    }

    public IList<WebActor> GuestStars { get; set; }
    public IList<string> Directors { get; set; }
    public IList<string> Writers { get; set; }

    public string Show { get; set; }
    public string Summary { get; set; }
  }
}