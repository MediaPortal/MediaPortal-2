using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktUserMovieRating : TraktMovieBase
  {
    [DataMember(Name = "inserted")]
    public long Inserted { get; set; }

    [DataMember(Name = "rating")]
    public string Rating { get; set; }

    [DataMember(Name = "rating_advanced")]
    public int RatingAdvanced { get; set; }
  }
}
