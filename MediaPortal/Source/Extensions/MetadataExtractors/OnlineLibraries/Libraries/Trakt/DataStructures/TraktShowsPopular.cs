using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktShowsPopular : TraktPagination
  {
    public IEnumerable<TraktShowSummary> Shows { get; set; }
  }
}