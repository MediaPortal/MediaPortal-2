using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktRatings
  {
    [DataMember(Name = "percentage")]
    public int Percentage { get; set; }

    [DataMember(Name = "votes")]
    public int Votes { get; set; }

    [DataMember(Name = "loved")]
    public int LovedCount { get; set; }

    [DataMember(Name = "hated")]
    public int HatedCount { get; set; }
  }
}
