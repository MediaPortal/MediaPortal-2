using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Movie
{
  public class WebMovieBasic : WebMediaItem, IYearSortable, IGenreSortable, IRatingSortable, IActors
  {
    public WebMovieBasic()
    {
      Genres = new List<string>();
      ExternalId = new List<WebExternalId>();
      Actors = new List<WebActor>();
    }

    public bool IsProtected { get; set; }
    public IList<string> Genres { get; set; }
    public IList<WebExternalId> ExternalId { get; set; }
    public IList<WebActor> Actors { get; set; }

    public int Year { get; set; }
    public float Rating { get; set; }
    public int Runtime { get; set; }

    public bool Watched { get; set; }

    public override WebMediaType Type
    {
      get { return WebMediaType.Movie; }
    }

    public override string ToString()
    {
      return Title;
    }
  }
}