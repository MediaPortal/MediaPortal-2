using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovieCollected
  {
    [DataMember(Name = "collected_at")]
    public string CollectedAt { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }
  }
}