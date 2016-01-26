using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonMovieCast
  {
    [DataMember(Name = "character")]
    public string Character { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }
  }
}