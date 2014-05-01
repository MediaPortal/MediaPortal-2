using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktWriter : TraktPerson
  {
    [DataMember(Name = "job")]
    public string Job { get; set; }
  }
}
