using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShowWatchList
  {
    [DataMember(Name = "listed_at")]
    public string ListedAt { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }
  }
}