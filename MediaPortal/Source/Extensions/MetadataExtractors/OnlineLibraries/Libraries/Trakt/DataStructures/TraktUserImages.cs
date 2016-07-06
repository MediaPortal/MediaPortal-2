using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktUserImages
  {
    [DataMember(Name = "avatar")]
    public TraktImage Avatar { get; set; }
  }
}