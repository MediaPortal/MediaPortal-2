using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  /// <summary>
  /// Data structure for a response from Trakt
  /// </summary>
  [DataContract]
  public class TraktResponse
  {
    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "message")]
    public string Message { get; set; }

    [DataMember(Name = "error")]
    public string Error { get; set; }
  }
}
