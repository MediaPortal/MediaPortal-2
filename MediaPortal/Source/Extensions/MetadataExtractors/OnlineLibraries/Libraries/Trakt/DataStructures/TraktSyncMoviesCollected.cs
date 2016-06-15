using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncMoviesCollected
  {
    [DataMember(Name = "movies")]
    public List<TraktSyncMovieCollected> Movies { get; set; }
  }
}