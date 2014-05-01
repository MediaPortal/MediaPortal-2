using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktEpisodeScrobble : AbstractScrobble
  {
    [DataMember(Name = "season")]
    public string Season { get; set; }

    [DataMember(Name = "episode")]
    public string Episode { get; set; }

    [DataMember(Name = "tvdb_id")]
    public string SeriesID { get; set; }

    [DataMember(Name = "episode_tvdb_id")]
    public string EpisodeID { get; set; }
  }
}
