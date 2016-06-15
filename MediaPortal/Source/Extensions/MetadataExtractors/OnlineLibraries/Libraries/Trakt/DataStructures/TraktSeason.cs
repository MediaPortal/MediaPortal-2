using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSeason
  {
    [DataMember(Name = "number")]
    public int Number { get; set; }

    [DataMember(Name = "ids")]
    public TraktSeasonId Ids { get; set; }
  }
}