using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncSeasonsRatedEx
  {
    [DataMember(Name = "shows")]
    public List<TraktSyncSeasonRatedEx> Shows { get; set; }
  }
}