using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieHistory
  {
    [DataMember(Name = "action")]
    public string Action { get; set; }

    [DataMember(Name = "watched_at")]
    public string WatchedAt { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }
  }
}