using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cinema.OnlineLibraries.Tmdb.Data
{
  public partial class Movie
  {
    [JsonProperty("adult", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Adult { get; set; }

    [JsonProperty("backdrop_path", NullValueHandling = NullValueHandling.Ignore)]
    public string BackdropPath { get; set; }

    [JsonProperty("belongs_to_collection", NullValueHandling = NullValueHandling.Ignore)]
    public BelongsToCollection BelongsToCollection { get; set; }

    [JsonProperty("budget", NullValueHandling = NullValueHandling.Ignore)]
    public long? Budget { get; set; }

    [JsonProperty("genres", NullValueHandling = NullValueHandling.Ignore)]
    public List<Genre> Genres { get; set; }

    [JsonProperty("homepage", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Homepage { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; set; }

    [JsonProperty("imdb_id", NullValueHandling = NullValueHandling.Ignore)]
    public string ImdbId { get; set; }

    [JsonProperty("original_language", NullValueHandling = NullValueHandling.Ignore)]
    public string OriginalLanguage { get; set; }

    [JsonProperty("original_title", NullValueHandling = NullValueHandling.Ignore)]
    public string OriginalTitle { get; set; }

    [JsonProperty("overview", NullValueHandling = NullValueHandling.Ignore)]
    public string Overview { get; set; }

    [JsonProperty("popularity", NullValueHandling = NullValueHandling.Ignore)]
    public double? Popularity { get; set; }

    [JsonProperty("poster_path", NullValueHandling = NullValueHandling.Ignore)]
    public string PosterPath { get; set; }

    [JsonProperty("production_companies", NullValueHandling = NullValueHandling.Ignore)]
    public List<ProductionCompany> ProductionCompanies { get; set; }

    [JsonProperty("production_countries", NullValueHandling = NullValueHandling.Ignore)]
    public List<ProductionCountry> ProductionCountries { get; set; }

    [JsonProperty("release_date", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime ReleaseDate { get; set; }

    [JsonProperty("revenue", NullValueHandling = NullValueHandling.Ignore)]
    public string Revenue { get; set; }

    [JsonProperty("runtime", NullValueHandling = NullValueHandling.Ignore)]
    public string Runtime { get; set; }

    [JsonProperty("spoken_languages", NullValueHandling = NullValueHandling.Ignore)]
    public List<SpokenLanguage> SpokenLanguages { get; set; }

    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public string Status { get; set; }

    [JsonProperty("tagline", NullValueHandling = NullValueHandling.Ignore)]
    public string Tagline { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string Title { get; set; }

    [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Video { get; set; }

    [JsonProperty("vote_average", NullValueHandling = NullValueHandling.Ignore)]
    public double? VoteAverage { get; set; }

    [JsonProperty("vote_count", NullValueHandling = NullValueHandling.Ignore)]
    public long? VoteCount { get; set; }

    [JsonProperty("videos", NullValueHandling = NullValueHandling.Ignore)]
    public Videos Videos { get; set; }
  }

  public partial class BelongsToCollection
  {
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("poster_path", NullValueHandling = NullValueHandling.Ignore)]
    public string PosterPath { get; set; }

    [JsonProperty("backdrop_path", NullValueHandling = NullValueHandling.Ignore)]
    public string BackdropPath { get; set; }
  }

  public partial class Genre
  {
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }
  }

  public partial class ProductionCompany
  {
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; set; }

    [JsonProperty("logo_path", NullValueHandling = NullValueHandling.Ignore)]
    public string LogoPath { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("origin_country", NullValueHandling = NullValueHandling.Ignore)]
    public string OriginCountry { get; set; }
  }

  public partial class ProductionCountry
  {
    [JsonProperty("iso_3166_1", NullValueHandling = NullValueHandling.Ignore)]
    public string Iso3166_1 { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }
  }

  public partial class SpokenLanguage
  {
    [JsonProperty("english_name", NullValueHandling = NullValueHandling.Ignore)]
    public string EnglishName { get; set; }

    [JsonProperty("iso_639_1", NullValueHandling = NullValueHandling.Ignore)]
    public string Iso639_1 { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }
  }

  public partial class Videos
  {
    [JsonProperty("results", NullValueHandling = NullValueHandling.Ignore)]
    public List<Result> Results { get; set; }
  }

  public partial class Result
  {
    [JsonProperty("iso_639_1", NullValueHandling = NullValueHandling.Ignore)]
    public string Iso639_1 { get; set; }

    [JsonProperty("iso_3166_1", NullValueHandling = NullValueHandling.Ignore)]
    public string Iso3166_1 { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
    public string Key { get; set; }

    [JsonProperty("site", NullValueHandling = NullValueHandling.Ignore)]
    public string Site { get; set; }

    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    public long? Size { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("official", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Official { get; set; }

    [JsonProperty("published_at", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }
  }
}
