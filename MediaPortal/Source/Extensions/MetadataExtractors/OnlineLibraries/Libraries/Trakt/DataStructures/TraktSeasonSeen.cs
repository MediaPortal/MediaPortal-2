using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSeasonSeen : TraktShowSeen
  {
    [DataMember(Name = "season")]
    public int Season { get; set; }
  }

  [DataContract]
  public class TraktSeasonLibrary : TraktSeasonSeen { }
}
