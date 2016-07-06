using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShowsRatedEx
  {
    [DataMember(Name = "shows")]
    public List<TraktSyncShowRatedEx> Shows { get; set; }
  }
}