using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncEpisodeWatched : TraktEpisode
  {
    [DataMember(Name = "watched_at")]
    public string WatchedAt { get; set; }
  }
}