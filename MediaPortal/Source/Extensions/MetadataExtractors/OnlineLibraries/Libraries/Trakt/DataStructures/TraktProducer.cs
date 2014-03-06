using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktProducer : TraktPerson
  {
    [DataMember(Name = "executive")]
    public bool Executive { get; set; }
  }
}
