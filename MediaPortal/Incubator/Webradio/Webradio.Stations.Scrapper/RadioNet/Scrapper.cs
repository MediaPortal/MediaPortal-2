using System.Diagnostics;
using Webradio.Stations.Helper;
using Webradio.Stations.RadioNet.Data.Links;
using Root = Webradio.Stations.RadioNet.Data.Genre.Root;

namespace Webradio.Stations.RadioNet;

internal class Scrapper
{
  private static readonly List<string> iDs = new();
  private static Translations translations;
  private static CountryCodes countryCodes;
  private static LanguageCodes languageCodes;
  private static int count;
  private static int ok;

  public async Task<bool> Start()
  {
    var ff = await File.ReadAllTextAsync("Translations.json");
    translations = Json.Deserialize<Translations>(ff);

    countryCodes = new CountryCodes();
    languageCodes = new LanguageCodes();

    foreach (var trans in translations)
    {
      if (trans.Name.StartsWith("Country"))
        countryCodes.Add(trans);
      if (trans.Name.StartsWith("Language"))
        languageCodes.Add(trans);
    }

    var sw = new Stopwatch();
    sw.Start();

    Console.Write("\r{0}", "Read Genres ...");
    var genres = await GetGenres();
    Console.Write("\r{0}", "Read Genres ... " + genres.Count + " found.");
    Console.WriteLine();

    var stations = new Data.Links.Stations();
    var gi = 0;
    foreach (var g in genres)
    {
      gi++;
      count = 0;
      var value = gi.ToString("D3") + " Read: " + g.PadRight(60);
      Console.Write("\r{0}   ", value);

      await AddStations(g);

      value = gi.ToString("D3") + " Read: " + g.PadRight(60) + " Done:   1 of " + count.ToString().PadLeft(3);
      Console.Write("\r{0}   ", value);

      var gok = ok;
      for (var i = 2; i <= count; i++)
      {
        await AddStations(g + "?p=" + i);

        gok = gok + ok;
        value = gi.ToString("D3") + " Read: " + g.PadRight(60) + " Done: " + i.ToString().PadLeft(3) + " of " +
                count.ToString().PadLeft(3);
        Console.Write("\r{0}   ", value);
      }

      Console.WriteLine("");
    }

    sw.Stop();
    var ts = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
    var time = ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");

    Console.WriteLine("Found " + Program.RadioStations.Stations.Count + " Stations in " + time);
    Console.WriteLine("");

    return true;
  }

  private async Task<List<string>> GetGenres()
  {
    var ret = await Http.GetSiteJson("https://www.radio.net/genre").ConfigureAwait(false);

    var sl = Json.Deserialize<Root>(ret);

    var genres = new List<string>();

    if (sl?.props?.pageProps?.data?.tags != null)
      foreach (var tag in sl.props.pageProps.data.tags)
        genres.Add("https://www.radio.net/genre/" + tag.slug);

    return genres;
  }

  private async Task<Data.Links.Stations?> GetStations(string url)
  {
    var ret = await Http.GetSiteJson(url);

    var sl = Json.Deserialize<Data.Links.Root>(ret);

    var playables = new List<Playable>();

    if (sl?.props?.pageProps?.data?.stations != null) return sl.props.pageProps.data.stations;

    return null;
  }

  private int GetStationsCount(Data.Links.Stations stations)
  {
    var c = stations.totalCount / stations.count;
    var rest = stations.totalCount % stations.count;
    if (rest != 0)
      c++;

    return c;
  }

  private async Task AddStations(string url)
  {
    var stations = await GetStations(url);

    if (stations != null)
    {
      count = GetStationsCount(stations);

      if (stations.playables != null)
        foreach (var pl in stations.playables)
          if (pl.streams != null)
            if (!iDs.Contains(pl.id))
            {
              iDs.Add(pl.id);
              var rs = GetRadioStation(pl);
              if (rs != null) Program.RadioStations.Stations.Add(rs);
            }
    }
  }

  private RadioStation? GetRadioStation(Playable playable)
  {
    if (playable.type != "STATION") return null;

    var radioStation = new RadioStation();

    radioStation.City = playable.city;
    radioStation.Country = countryCodes.GetCountryCode(playable.country);
    radioStation.Language = GetLanguage(playable.adParams);
    radioStation.Genres = playable.genres;
    radioStation.Id = playable.id;
    radioStation.Name = playable.name;

    if (playable.logo300x300 != "")
      radioStation.Logo = playable.logo300x300;
    else if (playable.logo100x100 != "")
      radioStation.Logo = playable.logo100x100;

    foreach (var st in playable.streams) radioStation.Streams.Add(new Stream { Url = st.url });

    return radioStation;
  }

  private string GetLanguage(string value)
  {
    var lng = "";

    var sln = value.Split('&');
    foreach (var sl in sln)
      if (sl.StartsWith("languages"))
      {
        lng = languageCodes.GetLanguageCode(sl.Replace("languages=", ""));
        if (lng != "") break;
      }

    return lng;
  }
}
