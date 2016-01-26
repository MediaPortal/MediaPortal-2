using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktListItem
  {
    [DataMember(Name = "listed_at")]
    public string ListedAt { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }

    [DataMember(Name = "season")]
    public TraktSeasonSummary Season { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisodeSummary Episode { get; set; }

    [DataMember(Name = "person")]
    public TraktPersonSummary Person { get; set; }
  }
}