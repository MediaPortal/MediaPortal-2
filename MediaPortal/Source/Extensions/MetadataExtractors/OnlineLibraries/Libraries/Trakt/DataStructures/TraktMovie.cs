using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovie : TraktMovieBase
  {
    [DataMember(Name = "certification")]
    public string Certification { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "released")]
    public long Released { get; set; }

    [DataMember(Name = "runtime")]
    public int Runtime { get; set; }

    [DataMember(Name = "tagline")]
    public string Tagline { get; set; }

    [DataMember(Name = "rt_id")]
    public string RtId { get; set; }

    [DataMember(Name = "trailer")]
    public string Trailer { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "plays")]
    public int Plays { get; set; }

    [DataMember(Name = "watched")]
    public bool Watched { get; set; }

    [DataMember(Name = "in_collection")]
    public bool InCollection { get; set; }

    [DataMember(Name = "in_watchlist")]
    public bool InWatchList { get; set; }

    [DataMember(Name = "rating")]
    public string Rating { get; set; }

    [DataMember(Name = "rating_advanced")]
    public int RatingAdvanced { get; set; }

    [DataMember(Name = "ratings")]
    public TraktRatings Ratings { get; set; }

    [DataMember(Name = "genres")]
    public List<string> Genres { get; set; }

    [DataMember(Name = "images")]
    public MovieImages Images { get; set; }

    [DataContract]
    public class MovieImages
    {
      [DataMember(Name = "fanart")]
      public string Fanart { get; set; }

      [DataMember(Name = "poster")]
      public string Poster { get; set; }
    }
  }
}