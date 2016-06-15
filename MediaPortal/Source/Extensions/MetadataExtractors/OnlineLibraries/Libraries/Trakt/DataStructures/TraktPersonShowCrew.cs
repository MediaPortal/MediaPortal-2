using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonShowCrew
  {
    [DataMember(Name = "directing")]
    public List<TraktPersonShowJob> Directing { get; set; }

    [DataMember(Name = "writing")]
    public List<TraktPersonShowJob> Writing { get; set; }

    [DataMember(Name = "production")]
    public List<TraktPersonShowJob> Production { get; set; }

    [DataMember(Name = "art")]
    public List<TraktPersonShowJob> Art { get; set; }

    [DataMember(Name = "costume & make-up")]
    public List<TraktPersonShowJob> CostumeAndMakeUp { get; set; }

    [DataMember(Name = "sound")]
    public List<TraktPersonShowJob> Sound { get; set; }

    [DataMember(Name = "camera")]
    public List<TraktPersonShowJob> Camera { get; set; }

    [DataMember(Name = "crew")]
    public List<TraktPersonShowJob> Crew { get; set; }
  }
}