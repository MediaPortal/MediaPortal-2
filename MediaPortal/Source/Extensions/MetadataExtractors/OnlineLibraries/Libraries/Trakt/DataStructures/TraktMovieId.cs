using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieId : TraktId
  {
    [DataMember(Name = "imdb")]
    public string Imdb { get; set; }

    [DataMember(Name = "tmdb")]
    public int? Tmdb { get; set; }
  }
}