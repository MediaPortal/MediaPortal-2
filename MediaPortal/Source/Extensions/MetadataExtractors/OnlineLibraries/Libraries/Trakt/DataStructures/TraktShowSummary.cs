using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShowSummary : TraktShow
  {
    [DataMember(Name = "last_updated")]
    public long LastUpdated { get; set; }

    [DataMember(Name = "people")]
    public TraktPeople People { get; set; }

    [DataMember(Name = "stats")]
    public TraktStatistics Stats { get; set; }

    [DataMember(Name = "top_episodes")]
    public List<TraktEpisode> TopEpisodes { get; set; }

    [DataMember(Name = "top_watchers")]
    public List<TraktTopWatcher> TopWatchers { get; set; }

    [DataMember(Name = "seasons")]
    public List<TraktShowSeasonEx> Seasons { get; set; }
  }
}
