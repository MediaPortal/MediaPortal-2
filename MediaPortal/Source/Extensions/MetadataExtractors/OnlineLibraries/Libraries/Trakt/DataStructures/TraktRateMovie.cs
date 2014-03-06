using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktRateMovie
  {
    [DataMember(Name = "username")]
    public string UserName { get; set; }

    [DataMember(Name = "password")]
    public string Password { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "year")]
    public string Year { get; set; }

    [DataMember(Name = "imdb_id")]
    public string IMDBID { get; set; }

    [DataMember(Name = "tmdb_id")]
    public string TMDBID { get; set; }

    [DataMember(Name = "rating")]
    public string Rating { get; set; }
  }
}
