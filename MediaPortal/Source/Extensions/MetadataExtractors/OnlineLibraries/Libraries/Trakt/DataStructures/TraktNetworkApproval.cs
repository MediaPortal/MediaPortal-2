using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetworkApproval
  {
    [DataMember(Name = "approved_at")]
    public string ApprovedAt { get; set; }

    [DataMember(Name = "user")]
    public TraktUserSummary User { get; set; }
  }
}