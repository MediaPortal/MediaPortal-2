using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktCalendar : TraktResponse
  {
    [DataMember(Name = "date")]
    public string Date { get; set; }

    [DataMember(Name = "episodes")]
    public List<TraktEpisodeSummary> Episodes { get; set; }
  }
}
