using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktTopWatcher : TraktUser
  {
    [DataMember(Name = "plays")]
    public int Plays { get; set; }
  }
}
