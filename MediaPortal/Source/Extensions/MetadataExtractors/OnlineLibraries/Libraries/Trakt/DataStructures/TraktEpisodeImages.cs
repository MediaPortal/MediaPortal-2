using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktEpisodeImages
  {
    [DataMember(Name = "screenshot")]
    public TraktImage ScreenShot { get; set; }
  }
}