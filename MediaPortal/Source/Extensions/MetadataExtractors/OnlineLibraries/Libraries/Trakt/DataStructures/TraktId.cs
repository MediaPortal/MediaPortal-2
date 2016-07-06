using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktId
  {
    [DataMember(Name = "trakt")]
    public int? Trakt { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }
  }
}