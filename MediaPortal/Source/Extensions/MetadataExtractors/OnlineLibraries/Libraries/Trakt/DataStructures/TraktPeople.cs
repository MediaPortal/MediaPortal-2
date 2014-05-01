using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPeople
  {
    [DataMember(Name = "directors")]
    public List<TraktPerson> Directors { get; set; }

    [DataMember(Name = "writers")]
    public List<TraktWriter> Writers { get; set; }

    [DataMember(Name = "producers")]
    public List<TraktProducer> Producers { get; set; }

    [DataMember(Name = "actors")]
    public List<TraktActor> Actors { get; set; }
  }
}
