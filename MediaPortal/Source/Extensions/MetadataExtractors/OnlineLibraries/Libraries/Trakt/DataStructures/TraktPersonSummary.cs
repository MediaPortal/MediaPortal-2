using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktPersonSummary : TraktPerson
  {
    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "biography")]
    public string Biography { get; set; }

    [DataMember(Name = "birthday")]
    public string Birthday { get; set; }

    [DataMember(Name = "birthplace")]
    public string Birthplace { get; set; }

    [DataMember(Name = "tmdb_id")]
    public int TmdbId { get; set; }
  }
}
