using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktWatchListMovie : TraktMovie
  {
    [DataMember(Name = "inserted")]
    public long Inserted { get; set; }
  }
}
