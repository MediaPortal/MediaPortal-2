using Newtonsoft.Json;
using System.Collections.Generic;

namespace Webradio.OnlineLibraries.Discogs
{
  public class Image
  {

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }

    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }

    [JsonProperty("uri150")]
    public string Uri150 { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }
  }

  public class Alias
  {

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }
  }

  public class Member
  {

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }
  }

  public class Artist
  {

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("resource_url")]
    public string ResourceUrl { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }

    [JsonProperty("releases_url")]
    public string ReleasesUrl { get; set; }

    [JsonProperty("images")]
    public IList<Image> Images { get; set; }

    [JsonProperty("profile")]
    public string Profile { get; set; }

    [JsonProperty("urls")]
    public IList<string> Urls { get; set; }

    [JsonProperty("namevariations")]
    public IList<string> Namevariations { get; set; }

    [JsonProperty("aliases")]
    public IList<Alias> Aliases { get; set; }

    [JsonProperty("members")]
    public IList<Member> Members { get; set; }

    [JsonProperty("data_quality")]
    public string DataQuality { get; set; }
  }
}
