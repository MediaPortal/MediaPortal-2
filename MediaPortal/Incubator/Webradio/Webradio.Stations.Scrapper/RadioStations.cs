using Newtonsoft.Json;
using Webradio.Stations.Helper;

namespace Webradio.Stations;

public class RadioStations
{
  public RadioStations()
  {
    var version = DateTime.Today.Date.ToString("yyyyMMdd");
    Version = Convert.ToInt32(version);
  }

  [JsonProperty("Stations")] public List<RadioStation> Stations { get; set; } = new();

  [JsonProperty("Version", Order = -2)] public int Version { get; set; }

  public string FileName { get; set; } = "RadioStations.json";

  public void Read()
  {
    var s = File.ReadAllText(FileName);
    var js = Json.Deserialize<RadioStations>(s);
    Stations = js.Stations;
    Version = js.Version;
  }

  public void Write()
  {
    var s = Json.Serialize(this);
    File.WriteAllText(FileName, s);
  }
}

public class RadioStation
{
  [JsonProperty("City", NullValueHandling = NullValueHandling.Ignore)]
  public string City { get; set; } = string.Empty;

  [JsonProperty("Country", NullValueHandling = NullValueHandling.Ignore)]
  public string Country { get; set; } = string.Empty;

  [JsonProperty("Genres", NullValueHandling = NullValueHandling.Ignore)]
  public List<string> Genres { get; set; } = new();

  [JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
  public string Id { get; set; } = string.Empty;

  [JsonProperty("Language", NullValueHandling = NullValueHandling.Ignore)]
  public string Language { get; set; } = string.Empty;

  [JsonProperty("Logo", NullValueHandling = NullValueHandling.Ignore)]
  public string Logo { get; set; } = string.Empty;

  [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
  public string Name { get; set; } = string.Empty;

  [JsonProperty("Streams", NullValueHandling = NullValueHandling.Ignore)]
  public List<Stream> Streams { get; set; } = new();
}

public class Stream
{
  [JsonProperty("Status", NullValueHandling = NullValueHandling.Ignore)]
  public string Status { get; set; } = string.Empty;

  [JsonProperty("Url", NullValueHandling = NullValueHandling.Ignore)]
  public string Url { get; set; } = string.Empty;
}
