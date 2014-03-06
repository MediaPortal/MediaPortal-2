using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class SyncMovieCheck
  {
    [DataMember(Name = "last_skipped_sync")]
    public long LastSkippedSync { get; set; }

    [DataMember(Name = "movies")]
    public List<TraktMovieSync.Movie> Movies { get; set; }
  }
}
