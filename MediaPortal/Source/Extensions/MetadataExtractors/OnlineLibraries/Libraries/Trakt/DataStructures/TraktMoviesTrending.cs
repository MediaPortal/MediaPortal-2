using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktMoviesTrending : TraktPagination
  {
    public int TotalWatchers { get; set; }
    public IEnumerable<TraktMovieTrending> Movies { get; set; }
  }
}