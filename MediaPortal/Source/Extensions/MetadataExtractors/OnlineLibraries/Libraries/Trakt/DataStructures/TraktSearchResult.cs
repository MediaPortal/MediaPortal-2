using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSearchResult
  {
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "score")]
    public double? Score { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisodeSummary Episode { get; set; }

    [DataMember(Name = "person")]
    public TraktPersonSummary Person { get; set; }

    [DataMember(Name = "user")]
    public TraktUserSummary User { get; set; }

    [DataMember(Name = "list")]
    public TraktList List { get; set; }
  }
}
