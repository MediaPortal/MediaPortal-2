using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShowSeen : TraktAuthentication
  {
    [DataMember(Name = "imdb_id")]
    public string Imdb { get; set; }

    [DataMember(Name = "tvdb_id")]
    public string Tvdb { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "year")]
    public int Year { get; set; }
  }

  [DataContract]
  public class TraktShowLibrary : TraktShowSeen { }
}
