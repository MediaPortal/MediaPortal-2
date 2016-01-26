using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktShowsUpdated : TraktPagination
  {
    public IEnumerable<TraktShowUpdate> Shows { get; set; }
  }
}