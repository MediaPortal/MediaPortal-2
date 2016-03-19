using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetworkFriend
  {
    [DataMember(Name = "friends_at")]
    public string FriendsAt { get; set; }

    [DataMember(Name = "user")]
    public TraktUserSummary User { get; set; }
  }
}