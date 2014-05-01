using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktListItem
  {
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    #region Movies
    [DataMember(Name = "imdb_id")]
    public string ImdbId { get; set; }

    [DataMember(Name = "year")]
    public int Year { get; set; }
    #endregion

    #region Shows/Episodes
    [DataMember(Name = "tvdb_id")]
    public string TvdbId { get; set; }

    [DataMember(Name = "season")]
    public int Season { get; set; }

    [DataMember(Name = "episode")]
    public int Episode { get; set; }
    #endregion
  }
}
