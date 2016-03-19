using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetworkUser
  {
    [DataMember(Name = "followed_at")]
    public string FollowedAt { get; set; }

    [DataMember(Name = "user")]
    public TraktUserSummary User { get; set; }
  }
}
