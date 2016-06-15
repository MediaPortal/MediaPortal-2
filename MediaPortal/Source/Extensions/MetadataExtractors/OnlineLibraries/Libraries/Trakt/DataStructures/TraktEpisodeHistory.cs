using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktEpisodeHistory
  {
    [DataMember(Name = "action")]
    public string Action { get; set; }

    [DataMember(Name = "watched_at")]
    public string WatchedAt { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisodeSummary Episode { get; set; }
  }
}