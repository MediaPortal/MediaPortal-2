using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.MAS.Music
{
  public class WebMusicArtistDetailed : WebMusicArtistBasic
  {
    public string Biography { get; set; }
    public string Tones { get; set; }
    public string Styles { get; set; }

    public IList<string> Genres { get; set; }
  }
}