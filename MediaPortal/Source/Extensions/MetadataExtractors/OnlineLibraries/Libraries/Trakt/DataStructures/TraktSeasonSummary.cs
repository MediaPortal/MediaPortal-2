using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSeasonSummary : TraktSeason
  {
    [DataMember(Name = "rating")]
    public double? Rating { get; set; }

    [DataMember(Name = "votes")]
    public int Votes { get; set; }

    [DataMember(Name = "episode_count")]
    public int EpisodeCount { get; set; }

    [DataMember(Name = "aired_episodes")]
    public int EpisodeAiredCount { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "images")]
    public TraktSeasonImages Images { get; set; }

    [DataMember(Name = "episodes")]
    public IEnumerable<TraktEpisodeSummary> Episodes { get; set; }
  }
}