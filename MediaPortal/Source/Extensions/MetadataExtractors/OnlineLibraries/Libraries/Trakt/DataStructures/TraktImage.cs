using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktImage
  {
    [DataMember(Name = "full")]
    public string FullSize { get; set; }

    [DataMember(Name = "medium")]
    public string MediumSize { get; set; }

    [DataMember(Name = "thumb")]
    public string ThumbSize { get; set; }
  }
}