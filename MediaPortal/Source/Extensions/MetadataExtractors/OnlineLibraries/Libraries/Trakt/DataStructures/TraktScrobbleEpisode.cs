using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktScrobbleEpisode : TraktScrobble
  {
    [DataMember(Name = "episode")]
    public TraktEpisode Episode { get; set; }

    [DataMember(Name = "show")]
    public TraktShow Show { get; set; }
  }
}