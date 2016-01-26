using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonMovieCrew
  {
    [DataMember(Name = "directing")]
    public List<TraktPersonMovieJob> Directing { get; set; }

    [DataMember(Name = "writing")]
    public List<TraktPersonMovieJob> Writing { get; set; }

    [DataMember(Name = "production")]
    public List<TraktPersonMovieJob> Production { get; set; }

    [DataMember(Name = "art")]
    public List<TraktPersonMovieJob> Art { get; set; }

    [DataMember(Name = "costume & make-up")]
    public List<TraktPersonMovieJob> CostumeAndMakeUp { get; set; }

    [DataMember(Name = "sound")]
    public List<TraktPersonMovieJob> Sound { get; set; }

    [DataMember(Name = "camera")]
    public List<TraktPersonMovieJob> Camera { get; set; }

    [DataMember(Name = "crew")]
    public List<TraktPersonMovieJob> Crew { get; set; }
  }
}