using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cinema.OnlineLibraries.Tmdb.Data
{
  public partial class Find
  {
    [JsonProperty("movie_results", NullValueHandling = NullValueHandling.Ignore)]
    public List<MovieResult> MovieResults { get; set; }

    [JsonProperty("person_results", NullValueHandling = NullValueHandling.Ignore)]
    public List<object> PersonResults { get; set; }

    [JsonProperty("tv_results", NullValueHandling = NullValueHandling.Ignore)]
    public List<object> TvResults { get; set; }

    [JsonProperty("tv_episode_results", NullValueHandling = NullValueHandling.Ignore)]
    public List<object> TvEpisodeResults { get; set; }

    [JsonProperty("tv_season_results", NullValueHandling = NullValueHandling.Ignore)]
    public List<object> TvSeasonResults { get; set; }
  }

  public partial class MovieResult
  {
    [JsonProperty("adult", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Adult { get; set; }

    [JsonProperty("backdrop_path", NullValueHandling = NullValueHandling.Ignore)]
    public string BackdropPath { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string Title { get; set; }

    [JsonProperty("original_language", NullValueHandling = NullValueHandling.Ignore)]
    public string OriginalLanguage { get; set; }

    [JsonProperty("original_title", NullValueHandling = NullValueHandling.Ignore)]
    public string OriginalTitle { get; set; }

    [JsonProperty("overview", NullValueHandling = NullValueHandling.Ignore)]
    public string Overview { get; set; }

    [JsonProperty("poster_path", NullValueHandling = NullValueHandling.Ignore)]
    public string PosterPath { get; set; }

    [JsonProperty("media_type", NullValueHandling = NullValueHandling.Ignore)]
    public string MediaType { get; set; }

    [JsonProperty("genre_ids", NullValueHandling = NullValueHandling.Ignore)]
    public List<long> GenreIds { get; set; }

    [JsonProperty("popularity", NullValueHandling = NullValueHandling.Ignore)]
    public double? Popularity { get; set; }

    [JsonProperty("release_date", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? ReleaseDate { get; set; }

    [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Video { get; set; }

    [JsonProperty("vote_average", NullValueHandling = NullValueHandling.Ignore)]
    public double? VoteAverage { get; set; }

    [JsonProperty("vote_count", NullValueHandling = NullValueHandling.Ignore)]
    public long? VoteCount { get; set; }
  }
}
