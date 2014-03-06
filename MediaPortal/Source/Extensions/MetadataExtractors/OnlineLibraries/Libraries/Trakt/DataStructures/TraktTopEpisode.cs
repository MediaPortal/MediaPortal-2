using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktTopEpisode
  {
    [DataMember(Name = "first_aired")]
    public long FirstAired { get; set; }

    [DataMember(Name = "first_aired_utc")]
    public long FirstAiredUtc { get; set; }

    [DataMember(Name = "first_aired_iso")]
    public string FirstAiredIso { get; set; }

    [DataMember(Name = "number")]
    public uint Number { get; set; }

    [DataMember(Name = "plays")]
    public uint Plays { get; set; }

    [DataMember(Name = "season")]
    public uint Season { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "url")]
    public string url { get; set; }
  }
}
