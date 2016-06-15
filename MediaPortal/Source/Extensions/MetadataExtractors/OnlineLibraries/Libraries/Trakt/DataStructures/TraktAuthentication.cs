using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktAuthentication
  {
    [DataMember(Name = "login")]
    public string Username { get; set; }

    [DataMember(Name = "password")]
    public string Password { get; set; }
  }
}
