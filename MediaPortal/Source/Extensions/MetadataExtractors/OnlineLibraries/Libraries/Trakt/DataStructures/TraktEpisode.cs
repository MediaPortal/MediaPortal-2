using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktEpisode
  {
    [DataMember(Name = "season")]
    public int Season { get; set; }

    [DataMember(Name = "number")]
    public int Number { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "tvdb_id")]
    public int TVDbID { get; set; }

    [DataMember(Name = "imdb_id")]
    public string IMDbID { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "first_aired")]
    public long FirstAired { get; set; }

    [DataMember(Name = "first_aired_utc")]
    public long FirstAiredUtc { get; set; }

    [DataMember(Name = "first_aired_iso")]
    public string FirstAiredIso { get; set; }

    [DataMember(Name = "first_aired_localized")]
    public long FirstAiredLocalized { get; set; }

    [DataMember(Name = "runtime")]
    public int Runtime { get; set; }

    [DataMember(Name = "in_watchlist")]
    public bool InWatchList { get; set; }

    [DataMember(Name = "in_collection")]
    public bool InCollection { get; set; }

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

    [DataMember(Name = "images")]
    public ShowImages Images { get; set; }

    [DataContract]
    public class ShowImages
    {
      [DataMember(Name = "screen")]
      public string Screen { get; set; }
    }
  }
}
