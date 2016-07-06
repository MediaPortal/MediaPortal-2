using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSeasonImages
  {
    [DataMember(Name = "poster")]
    public TraktImage Poster { get; set; }

    [DataMember(Name = "thumb")]
    public TraktImage Thumb { get; set; }
  }
}