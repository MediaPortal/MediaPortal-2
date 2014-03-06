using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShout : TraktResponse
  {
    [DataMember(Name = "id")]
    public long Id { get; set; }

    [DataMember(Name = "inserted")]
    public long InsertedDate { get; set; }

    [DataMember(Name = "text")]
    public string Shout { get; set; }

    [DataMember(Name = "text_html")]
    public string ShoutHTML { get; set; }

    [DataMember(Name = "spoiler")]
    public bool Spoiler { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "user")]
    public TraktUser User { get; set; }

    [DataMember(Name = "likes")]
    public int Likes { get; set; }

    [DataMember(Name = "replies")]
    public int Replies { get; set; }

    [DataMember(Name = "user_ratings")]
    public UserRating UserRatings { get; set; }

    [DataContract]
    public class UserRating
    {
      [DataMember(Name = "rating")]
      public string Rating { get; set; }

      [DataMember(Name = "rating_advanced")]
      public int AdvancedRating { get; set; }
    }
  }
}
