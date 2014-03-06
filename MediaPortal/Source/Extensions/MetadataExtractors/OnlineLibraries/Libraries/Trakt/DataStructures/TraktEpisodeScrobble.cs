using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktEpisodeScrobble
  {
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "year")]
    public string Year { get; set; }

    [DataMember(Name = "season")]
    public string Season { get; set; }

    [DataMember(Name = "episode")]
    public string Episode { get; set; }

    [DataMember(Name = "tvdb_id")]
    public string SeriesID { get; set; }

    [DataMember(Name = "episode_tvdb_id")]
    public string EpisodeID { get; set; }

    [DataMember(Name = "imdb_id")]
    public string IMDBID { get; set; }

    [DataMember(Name = "progress")]
    public string Progress { get; set; }

    [DataMember(Name = "plugin_version")]
    public string PluginVersion { get; set; }

    [DataMember(Name = "media_center")]
    public string MediaCenter { get; set; }

    [DataMember(Name = "media_center_version")]
    public string MediaCenterVersion { get; set; }

    [DataMember(Name = "media_center_date")]
    public string MediaCenterBuildDate { get; set; }

    [DataMember(Name = "duration")]
    public string Duration { get; set; }

    [DataMember(Name = "username")]
    public string UserName { get; set; }

    [DataMember(Name = "password")]
    public string Password { get; set; }
  }
}
