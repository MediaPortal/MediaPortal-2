using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktWatchListEpisode : TraktShow
  {
    [DataMember(Name = "episodes")]
    public List<Episode> Episodes { get; set; }

    [DataContract]
    public class Episode : TraktEpisode
    {
      [DataMember(Name = "inserted")]
      public long Inserted { get; set; }
    }
  }
}
