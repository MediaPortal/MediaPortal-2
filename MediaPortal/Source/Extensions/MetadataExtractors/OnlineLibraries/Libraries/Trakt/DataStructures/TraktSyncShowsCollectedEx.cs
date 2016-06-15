using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShowsCollectedEx
  {
    [DataMember(Name = "shows")]
    public List<TraktSyncShowCollectedEx> Shows { get; set; }
  }
}