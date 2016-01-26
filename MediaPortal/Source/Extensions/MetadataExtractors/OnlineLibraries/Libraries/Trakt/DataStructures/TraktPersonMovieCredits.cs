using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonMovieCredits
  {
    [DataMember(Name = "cast")]
    public List<TraktPersonMovieCast> Cast { get; set; }

    [DataMember(Name = "crew")]
    public TraktPersonMovieCrew Crew { get; set; }
  }
}