using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieWatchList
  {
    [DataMember(Name = "listed_at")]
    public string ListedAt { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }
  }
}