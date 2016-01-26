using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonShowCast
  {
    [DataMember(Name = "character")]
    public string Character { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }
  }
}