using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktStatus
  {
    [DataMember(Name = "reason", EmitDefaultValue = false)]
    public string Description { get; set; }

    [DataMember(Name = "code", EmitDefaultValue = false)]
    public int Code { get; set; }
  }
}