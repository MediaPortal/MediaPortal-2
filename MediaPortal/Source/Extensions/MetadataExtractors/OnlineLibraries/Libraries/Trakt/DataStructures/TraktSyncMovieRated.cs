using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncMovieRated : TraktMovie
  {
    [DataMember(Name = "rated_at")]
    public string RatedAt { get; set; }

    [DataMember(Name = "rating")]
    public int Rating { get; set; }
  }
}