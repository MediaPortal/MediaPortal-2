using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktActor : TraktPerson
  {
    [DataMember(Name = "character")]
    public string Character { get; set; }
  }
}
