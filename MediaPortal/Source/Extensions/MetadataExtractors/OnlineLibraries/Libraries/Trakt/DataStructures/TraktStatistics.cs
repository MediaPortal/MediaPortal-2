using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktStatistics
  {
    [DataMember(Name = "checkins")]
    public uint Checkins { get; set; }

    [DataMember(Name = "checkins_unique")]
    public uint CheckinsUnique { get; set; }

    [DataMember(Name = "collection")]
    public uint Collection { get; set; }

    [DataMember(Name = "collection_unique")]
    public uint CollectionUnique { get; set; }

    [DataMember(Name = "plays")]
    public uint Plays { get; set; }

    [DataMember(Name = "scrobbles")]
    public uint Scrobbles { get; set; }

    [DataMember(Name = "scrobbles_unique")]
    public uint ScrobblesUnique { get; set; }

    [DataMember(Name = "watchers")]
    public uint Watchers { get; set; }
  }
}
