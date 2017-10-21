using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.MAS.Movie
{
  public class WebMovieDetailed : WebMovieBasic
  {
    public WebMovieDetailed() : base()
    {
      Directors = new List<string>();
      Writers = new List<string>();
    }

    public IList<string> Directors { get; set; }
    public IList<string> Writers { get; set; }
    public string Summary { get; set; }
    public string Tagline { get; set; }

    // use ISO short name (en, nl, de, etc)
    public string Language { get; set; }
  }
}