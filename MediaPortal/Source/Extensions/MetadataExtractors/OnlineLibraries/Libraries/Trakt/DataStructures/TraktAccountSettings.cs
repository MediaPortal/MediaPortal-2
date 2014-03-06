using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktAccountSettings : TraktResponse
  {
    [DataMember(Name = "profile")]
    public TraktUser Profile { get; set; }

    [DataMember(Name = "account")]
    public Account AccountSettings { get; set; }

    [DataMember(Name = "viewing")]
    public Viewing ViewingSettings { get; set; }

    [DataMember(Name = "connections")]
    public Connections ConnectionSettings { get; set; }

    [DataMember(Name = "sharing_text")]
    public SharingText SharingTextSettings { get; set; }

    [DataContract]
    public class Account
    {
      [DataMember(Name = "timezone")]
      public string Timezone { get; set; }

      [DataMember(Name = "use_24hr")]
      public bool Use24Hr { get; set; }

      [DataMember(Name = "protected")]
      public bool Protected { get; set; }
    }

    [DataContract]
    public class Viewing
    {
      [DataMember(Name = "ratings")]
      public Ratings RatingSettings { get; set; }

      [DataMember(Name = "shouts")]
      public Shouts ShoutSettings { get; set; }

      [DataContract]
      public class Ratings
      {
        [DataMember(Name = "mode")]
        public string Mode { get; set; }
      }

      [DataContract]
      public class Shouts
      {
        [DataMember(Name = "show_badges")]
        public bool ShowBadges { get; set; }

        [DataMember(Name = "show_spoilers")]
        public bool ShowSpoilers { get; set; }
      }
    }

    [DataContract]
    public class Connections
    {
      [DataMember(Name = "facebook")]
      public Facebook FacebookSettings { get; set; }

      [DataMember(Name = "twitter")]
      public Twitter TwitterSettings { get; set; }

      [DataMember(Name = "tumblr")]
      public Tumblr TumblrSettings { get; set; }

      [DataContract]
      public class BaseConnections
      {
        [DataMember(Name = "connected")]
        public bool Connected { get; set; }

        [DataMember(Name = "share_scrobbles_start")]
        public bool ShareScrobblesAtStart { get; set; }

        [DataMember(Name = "share_scrobbles_end")]
        public bool ShareScrobblesAtEnd { get; set; }

        [DataMember(Name = "share_tv")]
        public bool ShareTV { get; set; }

        [DataMember(Name = "share_movies")]
        public bool ShareMovies { get; set; }

        [DataMember(Name = "share_ratings")]
        public bool ShareRatings { get; set; }

        [DataMember(Name = "share_checkins")]
        public bool ShareCheckIns { get; set; }
      }

      [DataContract]
      public class Facebook : BaseConnections
      {
        [DataMember(Name = "timeline_enabled")]
        public bool TimelineEnabled { get; set; }
      }

      [DataContract]
      public class Twitter : BaseConnections { }

      [DataContract]
      public class Tumblr : BaseConnections { }
    }

    [DataContract]
    public class SharingText
    {
      [DataMember(Name = "watching")]
      public string Watching { get; set; }

      [DataMember(Name = "watched")]
      public string Watched { get; set; }
    }
  }
}
