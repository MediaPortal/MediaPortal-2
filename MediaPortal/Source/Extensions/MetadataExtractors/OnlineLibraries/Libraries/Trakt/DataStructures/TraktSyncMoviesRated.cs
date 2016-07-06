using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncMoviesRated
  {
    [DataMember(Name = "movies")]
    public List<TraktSyncMovieRated> Movies { get; set; }
  }
}