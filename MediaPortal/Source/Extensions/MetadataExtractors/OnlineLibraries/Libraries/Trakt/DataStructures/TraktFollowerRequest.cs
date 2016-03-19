using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktFollowerRequest
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "requested_at")]
    public string RequestedAt { get; set; }

    [DataMember(Name = "user")]
    public TraktUserSummary User { get; set; }
  }
}