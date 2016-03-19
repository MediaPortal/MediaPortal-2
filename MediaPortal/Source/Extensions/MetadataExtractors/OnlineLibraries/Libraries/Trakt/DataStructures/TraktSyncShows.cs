using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShows
  {
    [DataMember(Name = "shows")]
    public List<TraktShow> Shows { get; set; }
  }
}