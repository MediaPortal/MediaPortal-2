using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPerson
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "images")]
    public PersonImages Images { get; set; }

    [DataContract]
    public class PersonImages
    {
      [DataMember(Name = "headshot")]
      public string Headshot { get; set; }
    }
  }
}
