using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieWatched
  {
    [DataMember(Name = "plays")]
    public int Plays { get; set; }

    [DataMember(Name = "last_watched_at")]
    public string LastWatchedAt { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }
  }
}