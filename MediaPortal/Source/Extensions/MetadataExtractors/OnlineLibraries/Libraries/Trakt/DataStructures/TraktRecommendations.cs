using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktRecommendations : TraktAuthentication
  {
    [DataMember(Name = "genre")]
    public string Genre { get; set; }

    [DataMember(Name = "start_year")]
    public int StartYear { get; set; }

    [DataMember(Name = "end_year")]
    public int EndYear { get; set; }

    [DataMember(Name = "hide_collected")]
    public bool HideCollected { get; set; }

    [DataMember(Name = "hide_watchlisted")]
    public bool HideWatchlisted { get; set; }
  }
}
