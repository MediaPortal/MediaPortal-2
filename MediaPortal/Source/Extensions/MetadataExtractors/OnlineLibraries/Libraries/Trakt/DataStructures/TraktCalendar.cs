using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktCalendar : TraktEpisodeSummaryEx
  {
    [DataMember(Name = "airs_at")]
    public string AirsAt { get; set; }
  }
}