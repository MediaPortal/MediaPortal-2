using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// Data structure for a follow response from Trakt
  /// </summary>
  [DataContract]
  public class TraktNetworkFollowResponse : TraktResponse
  {
    [DataMember(Name = "pending")]
    public bool Pending { get; set; }
  }
}
