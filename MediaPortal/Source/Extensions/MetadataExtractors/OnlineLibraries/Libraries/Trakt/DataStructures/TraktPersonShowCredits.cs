using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonShowCredits
  {
    [DataMember(Name = "cast")]
    public List<TraktPersonShowCast> Cast { get; set; }

    [DataMember(Name = "crew")]
    public TraktPersonShowCrew Crew { get; set; }
  }
}