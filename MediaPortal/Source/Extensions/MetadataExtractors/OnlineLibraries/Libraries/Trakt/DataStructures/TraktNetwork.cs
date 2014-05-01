using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktNetwork : TraktAuthentication
  {
    [DataMember(Name = "user")]
    public string User { get; set; }
  }
}
