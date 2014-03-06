using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShow
  {
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "year")]
    public int Year { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "first_aired")]
    public long FirstAired { get; set; }

    [DataMember(Name = "first_aired_iso")]
    public string FirstAiredIso { get; set; }

    [DataMember(Name = "first_aired_utc")]
    public long FirstAiredUtc { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "runtime")]
    public int Runtime { get; set; }

    [DataMember(Name = "network")]
    public string Network { get; set; }

    [DataMember(Name = "air_day")]
    public string AirDay { get; set; }

    [DataMember(Name = "air_day_utc")]
    public string AirDayUtc { get; set; }

    [DataMember(Name = "air_time")]
    public string AirTime { get; set; }

    [DataMember(Name = "air_time_utc")]
    public string AirTimeUtc { get; set; }

    [DataMember(Name = "air_time_localized")]
    public string AirTimeLocalized { get; set; }

    [DataMember(Name = "certification")]
    public string Certification { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "imdb_id")]
    public string Imdb { get; set; }

    [DataMember(Name = "tvdb_id")]
    public string Tvdb { get; set; }

    [DataMember(Name = "tvrage_id")]
    public string TvRage { get; set; }

    [DataMember(Name = "in_watchlist")]
    public bool InWatchList { get; set; }

    [DataMember(Name = "watched")]
    public bool Watched { get; set; }

    [DataMember(Name = "plays")]
    public int Plays { get; set; }

    [DataMember(Name = "rating")]
    public string Rating { get; set; }

    [DataMember(Name = "rating_advanced")]
    public int RatingAdvanced { get; set; }

    [DataMember(Name = "ratings")]
    public TraktRatings Ratings { get; set; }

    [DataMember(Name = "genres")]
    public List<string> Genres { get; set; }

    [DataMember(Name = "images")]
    public ShowImages Images { get; set; }

    [DataContract]
    public class ShowImages
    {
      [DataMember(Name = "fanart")]
      public string Fanart { get; set; }

      [DataMember(Name = "poster")]
      public string Poster { get; set; }

      [DataMember(Name = "banner")]
      public string Banner { get; set; }

      [DataMember(Name = "season")]
      public string Season { get; set; }
    }
  }
}
