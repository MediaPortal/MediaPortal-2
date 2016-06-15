using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieTrending
  {
    [DataMember(Name = "watchers")]
    public int Watchers { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }
  }
}