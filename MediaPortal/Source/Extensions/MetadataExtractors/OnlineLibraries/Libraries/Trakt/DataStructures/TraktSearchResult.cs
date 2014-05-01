using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// this class is used to combine multiple search results
  /// </summary>
  public class TraktSearchResult
  {
    public IEnumerable<TraktMovie> Movies = null;
    public IEnumerable<TraktShow> Shows = null;
    public IEnumerable<TraktEpisodeSummary> Episodes = null;
    public IEnumerable<TraktPersonSummary> People = null;
    public IEnumerable<TraktUser> Users = null;

    public int Count
    {
      get
      {
        int retValue = 0;

        if (Movies != null) retValue += Movies.Count();
        if (Shows != null) retValue += Shows.Count();
        if (Episodes != null) retValue += Episodes.Count();
        if (People != null) retValue += People.Count();
        if (Users != null) retValue += Users.Count();

        return retValue;

      }
    }
  }
}
