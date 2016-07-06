using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShowRated
  {
    [DataMember(Name = "rating")]
    public int Rating { get; set; }

    [DataMember(Name = "rated_at")]
    public string RatedAt { get; set; }

    [DataMember(Name = "show")]
    public TraktShow Show { get; set; }
  }
}