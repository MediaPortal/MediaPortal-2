using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktScrobbleResponse : TraktStatus
  {
    [DataMember(Name = "action")]
    public string Action { get; set; }

    [DataMember(Name = "progress")]
    public float Progress { get; set; }

    [DataMember(Name = "sharing")]
    public SocialMedia Sharing { get; set; }

    [DataContract]
    public class SocialMedia
    {
      [DataMember(Name = "facebook")]
      public bool Facebook { get; set; }

      [DataMember(Name = "twitter")]
      public bool Twitter { get; set; }

      [DataMember(Name = "tumblr")]
      public bool Tumblr { get; set; }
    }

    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisode Episode { get; set; }

    [DataMember(Name = "show")]
    public TraktShow Show { get; set; }
  }
}