using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktMoviesPopular : TraktPagination
  {
    public IEnumerable<TraktMovieSummary> Movies { get; set; }
  }
}