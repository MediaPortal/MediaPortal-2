using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetworkReqUser : TraktUser
  {
    [DataMember(Name = "requested")]
    public long RequestDate { get; set; }
  }
}
