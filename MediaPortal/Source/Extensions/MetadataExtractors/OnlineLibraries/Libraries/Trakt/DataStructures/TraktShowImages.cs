using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShowImages
  {
    [DataMember(Name = "fanart")]
    public TraktImage Fanart { get; set; }

    [DataMember(Name = "poster")]
    public TraktImage Poster { get; set; }

    [DataMember(Name = "logo")]
    public TraktImage Logo { get; set; }

    [DataMember(Name = "clearart")]
    public TraktImage ClearArt { get; set; }

    [DataMember(Name = "banner")]
    public TraktImage Banner { get; set; }

    [DataMember(Name = "thumb")]
    public TraktImage Thumb { get; set; }
  }
}