using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShowsEx
  {
    [DataMember(Name = "shows")]
    public List<TraktSyncShowEx> Shows { get; set; }
  }
}