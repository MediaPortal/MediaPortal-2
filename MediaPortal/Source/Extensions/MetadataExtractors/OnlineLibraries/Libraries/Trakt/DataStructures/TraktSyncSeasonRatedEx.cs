using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncSeasonRatedEx : TraktShow
  {
    [DataMember(Name = "seasons")]
    public List<Season> Seasons { get; set; }

    [DataContract]
    public class Season
    {
      [DataMember(Name = "number")]
      public int Number { get; set; }

      [DataMember(Name = "rated_at")]
      public string RatedAt { get; set; }

      [DataMember(Name = "rating")]
      public int Rating { get; set; }
    }
  }
}