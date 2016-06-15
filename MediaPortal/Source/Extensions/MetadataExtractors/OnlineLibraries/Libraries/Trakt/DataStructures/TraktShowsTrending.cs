using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktShowsTrending : TraktPagination
  {
    public int TotalWatchers { get; set; }
    public IEnumerable<TraktShowTrending> Shows { get; set; }
  }
}