using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieRated
  {
    [DataMember(Name = "rating")]
    public int Rating { get; set; }

    [DataMember(Name = "rated_at")]
    public string RatedAt { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }
  }
}