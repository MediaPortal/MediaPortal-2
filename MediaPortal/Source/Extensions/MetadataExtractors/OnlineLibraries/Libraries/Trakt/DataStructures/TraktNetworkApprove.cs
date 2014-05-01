using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetworkApprove : TraktNetwork
  {
    [DataMember(Name = "follow_back")]
    public bool FollowBack { get; set; }
  }
}
