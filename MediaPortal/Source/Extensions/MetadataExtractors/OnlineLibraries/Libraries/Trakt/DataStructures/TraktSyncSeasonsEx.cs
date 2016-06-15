using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncSeasonsEx
  {
    [DataMember(Name = "shows")]
    public List<TraktSyncSeasonEx> Shows { get; set; }
  }
}