using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktEpisodeWatchList : TraktEpisodeSummaryEx
  {
    [DataMember(Name = "listed_at")]
    public string ListedAt { get; set; }
  }
}