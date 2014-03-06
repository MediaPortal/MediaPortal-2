using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktAddListResponse : TraktResponse
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "privacy")]
    public string Privacy { get; set; }

    [DataMember(Name = "slug")]
    public string Slug { get; set; }
  }
}
