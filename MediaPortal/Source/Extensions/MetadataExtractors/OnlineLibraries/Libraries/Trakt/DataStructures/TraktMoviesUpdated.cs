using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktMoviesUpdated : TraktPagination
  {
    public IEnumerable<TraktMovieUpdate> Movies { get; set; }
  }
}