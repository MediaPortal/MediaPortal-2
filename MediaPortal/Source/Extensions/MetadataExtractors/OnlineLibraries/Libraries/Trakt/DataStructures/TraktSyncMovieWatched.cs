using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncMovieWatched : TraktMovie
  {
    [DataMember(Name = "watched_at")]
    public string WatchedAt { get; set; }
  }
}