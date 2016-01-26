using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShowWatchedEx : TraktShow
  {
    [DataMember(Name = "seasons")]
    public List<Season> Seasons { get; set; }

    [DataContract]
    public class Season
    {
      [DataMember(Name = "number")]
      public int Number { get; set; }

      [DataMember(Name = "episodes")]
      public List<Episode> Episodes { get; set; }

      [DataContract]
      public class Episode
      {
        [DataMember(Name = "number")]
        public int Number { get; set; }

        [DataMember(Name = "watched_at")]
        public string WatchedAt { get; set; }
      }
    }
  }
}