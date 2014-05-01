using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetworkUser : TraktUser
  {
    [DataMember(Name = "since")]
    public long ApprovedDate { get; set; }
  }
}
