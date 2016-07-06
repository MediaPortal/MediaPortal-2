using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktScrobbleMovie : TraktScrobble
  {
    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }
  }
}