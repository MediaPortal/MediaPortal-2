using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktScrobble
  {
    [DataMember(Name = "progress")]
    public double Progress { get; set; }

    [DataMember(Name = "app_version")]
    public string AppVersion { get; set; }

    [DataMember(Name = "app_date")]
    public string AppDate { get; set; }
  }
}