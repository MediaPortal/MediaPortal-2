using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncMovies
  {
    [DataMember(Name = "movies")]
    public List<TraktMovie> Movies { get; set; }
  }
}