using Newtonsoft.Json;
using System.Collections.Generic;

namespace Webradio.OnlineLibraries.Discogs
{
  public class Urls
  {

    [JsonProperty("last")]
    public string Last { get; set; }

    [JsonProperty("next")]
    public string Next { get; set; }
  }

  public class Pagination
  {

    [JsonProperty("page")]
    public int Page { get; set; }

    [JsonProperty("pages")]
    public int Pages { get; set; }

    [JsonProperty("per_page")]
    public int PerPage { get; set; }

    [JsonProperty("items")]
    public int Items { get; set; }

    [JsonProperty("urls")]
    public Urls Urls { get; set; }
  }

  public class Community
  {

    [JsonProperty("want")]
    public int Want { get; set; }

    [JsonProperty("have")]
    public int Have { get; set; }
  }

  public class Result
  {

    [JsonProperty("country")]
    public string Country { get; set; }

    [JsonProperty("year")]
    public string Year { get; set; }

    [JsonProperty("format")]
    public IList<string> Format { get; set; }

    [JsonProperty("label")]
    public IList<string> Label { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("genre")]
    public IList<string> Genre { get; set; }

    [JsonProperty("style")]
    public IList<string> Style { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("barcode")]
    public IList<string> Barcode { get; set; }

    [JsonProperty("master_id")]
    public int MasterId { get; set; }

    [JsonProperty("master_url")]
    public string MasterUrl { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }

    [JsonProperty("catno")]
    public string Catno { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("thumb")]
    public string Thumb { get; set; }

    [JsonProperty("cover_image")]
    public string CoverImage { get; set; }

    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }

    [JsonProperty("community")]
    public Community Community { get; set; }
  }

  public class Query
  {

    [JsonProperty("pagination")]
    public Pagination Pagination { get; set; }

    [JsonProperty("results")]
    public IList<Result> Results { get; set; }
  }
}
