using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktFriend : TraktAuthentication
  {
    [DataMember(Name = "friend")]
    public string Friend { get; set; }
  }
}
