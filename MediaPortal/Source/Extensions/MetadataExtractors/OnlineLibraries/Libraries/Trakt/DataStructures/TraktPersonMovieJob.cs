using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonMovieJob
  {
    [DataMember(Name = "job")]
    public string Job { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }
  }
}