using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShowsWatchedEx
  {
    [DataMember(Name = "shows")]
    public List<TraktSyncShowWatchedEx> Shows { get; set; }
  }
}