/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// A place the weather can be read for: what the viewer called it, what the geocoder said around it (the
  /// state and country that tell two "Berlin"s apart), and the coordinates it is stored as.
  /// </summary>
  /// <remarks>
  /// Coordinates and not the old plugin's city ids: OpenWeatherMap's geocoding answers - the only search it still
  /// gives the free tier - hand out names and coordinates and no id at all, and every weather endpoint takes
  /// <c>lat</c>/<c>lon</c>. An id saved in 2021 is a number only the old endpoint understood.
  /// </remarks>
  public record WeatherCityBookmark(string Name, string Detail, double Lat, double Lon);

  /// <summary>
  /// What the sky is doing right now. The units are the client's business (<see cref="WeatherApiClient.IsMetric"/>)
  /// and the screen signs the numbers with them.
  /// </summary>
  public record WeatherNow(string Description, double Temp, double FeelsLike, int Humidity, int Pressure,
      double WindSpeed, string WindDirection);

  /// <summary>
  /// One day of the forecast. The description is the day's midday entry, falling back to its first: the free
  /// endpoint answers every three hours, and "what the day is like" is what a noon reading says far better than
  /// what the small hours do.
  /// </summary>
  public record WeatherDay(DateTimeOffset Date, string Description, double High, double Low,
      double PrecipitationChance);

  /// <summary>
  /// The answer for one city: the current weather and the days ahead, or the reason neither came. Like the
  /// feeds' answers, a refusal is data - a named reason - and never a silence.
  /// </summary>
  public record WeatherAnswer(WeatherCityBookmark City, WeatherNow? Now, IReadOnlyList<WeatherDay>? Days,
      string? Error);

  /// <summary>
  /// Reads the current weather and the coming days from OpenWeatherMap, the provider MP2's weather plugin used.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>The same provider, the same built-in keys, the same culture rules as the old plugin</b> - and the two
  /// endpoints it needed replaced by the two the API still serves. What the old grabber called
  /// <c>Search.GetByName</c> is the geocoding API, which hands out coordinates instead of city ids (hence
  /// <see cref="WeatherCityBookmark"/> storing coordinates); what it called
  /// <c>Forecast.GetByCityId</c> - a sixteen-day daily forecast - is gone from the free tier altogether, and the
  /// days are aggregated here from the five-day three-hourly answer instead. A day is then its own local day:
  /// the response carries the city's UTC offset, so a forecast day flips at the city's midnight and not at the
  /// television's, which matters for exactly one viewer per hemisphere and is still the truth.
  /// </para>
  /// <para>
  /// <b>Metric or imperial, and in which language</b>, is read off the viewer's culture the way the old plugin
  /// read it: <see cref="RegionInfo.IsMetric"/> for the units, the two-letter language for the descriptions.
  /// A viewer in Vienna gets Celsius and German without configuring anything, and a viewer in Texas gets
  /// Fahrenheit without the front end having a settings screen for it.
  /// </para>
  /// <para>
  /// The keys are the plugin's own - three public demo keys committed in this repository since 2021, tried in
  /// turn when one answers that it has had enough requests. They are rate-limited things that can expire, and
  /// when all three are spent the refusal is said out loud on the screen; a viewer who registers their own free
  /// key can put it in the settings file, which is the same escape the plugin offered.
  /// </para>
  /// </remarks>
  public sealed class WeatherApiClient : IDisposable
  {
    /// <summary>
    /// The built-in demo keys from the old weather plugin (<c>OpenWeatherMapCatcher.KEYS</c>), committed in this
    /// repository and used the same way there: rotate to the next one when the current one is refused.
    /// </summary>
    public static readonly string[] BuiltInKeys =
    {
      "91bab7dff33dbb8f3b2ac644272188fa",
      "dd1ec26c30b60782b76b5cd4beb2cd0b",
      "c700353fc67ddb8e6dee46b7f6ab64eb"
    };

    /// <summary>
    /// How long one endpoint gets. A weather screen is worth waiting for for a moment and not for longer; a key
    /// that has expired answers "401" quickly and only a hung endpoint takes seconds.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    // The addresses end with an empty "appid=" on purpose: the key is appended at the last moment, inside
    // GetAsync, so that a URL that is shown, logged or put in an exception message never carries a key.
    private const string GEO_URL = "https://api.openweathermap.org/geo/1.0/direct?q={0}&limit=8&appid=";
    private const string CURRENT_URL =
        "https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&units={2}&lang={3}&appid=";
    private const string FORECAST_URL =
        "https://api.openweathermap.org/data/2.5/forecast?lat={0}&lon={1}&units={2}&lang={3}&appid=";

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly string[] _keys;
    private readonly CultureInfo _culture;
    private readonly string _units;
    private readonly string _language;
    private int _keyIndex;

    public WeatherApiClient(HttpClient? httpClient = null, string? userApiKey = null, string? cultureName = null)
    {
      _ownsHttpClient = httpClient == null;
      _http = httpClient ?? new HttpClient();
      if (_ownsHttpClient)
        _http.Timeout = DefaultTimeout;
      if (string.IsNullOrEmpty(_http.DefaultRequestHeaders.UserAgent.ToString()))
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaPortal 2");

      // The viewer's own key first when they registered one; the product's three after it, started at a random
      // index exactly as the old plugin did so that a thousand MP2 televisions do not all spend the same key's
      // daily allowance before the second one is ever tried.
      List<string> keys = new List<string>();
      if (!string.IsNullOrWhiteSpace(userApiKey))
        keys.Add(userApiKey.Trim());
      keys.AddRange(BuiltInKeys);
      _keys = keys.ToArray();
      _keyIndex = new Random().Next(_keys.Length);

      _culture = CultureInfo.CurrentUICulture;
      if (!string.IsNullOrWhiteSpace(cultureName))
      {
        try
        {
          _culture = CultureInfo.GetCultureInfo(cultureName.Trim());
        }
        catch (CultureNotFoundException)
        {
          // A culture the machine does not know is answered with the one it does, not with an exception on a
          // screen whose whole job is to be pleasant to look at.
        }
      }
      _units = IsMetricCulture(_culture) ? "metric" : "imperial";
      _language = _culture.TwoLetterISOLanguageName;
    }

    /// <summary>
    /// Whether a culture's country measures in Celsius. <see cref="RegionInfo"/> refuses a neutral culture name
    /// - "en" is a language, not a place - so the culture's own <c>TextInfo</c> is asked for the specific
    /// culture it stands in, which is the documented way round; and if even that leaves the region unknown,
    /// Fahrenheit is the safe answer rather than an exception, because it is only ever wrong for the handful of
    /// English-speaking places that chose Celsius.
    /// </summary>
    private static bool IsMetricCulture(CultureInfo culture)
    {
      try
      {
        return new RegionInfo(culture.TextInfo.CultureName).IsMetric;
      }
      catch (ArgumentException)
      {
        try
        {
          return new RegionInfo(culture.Name).IsMetric;
        }
        catch (ArgumentException)
        {
          return false;
        }
      }
    }

    /// <summary>Whether temperatures and wind are in Celsius and metres per second, for the screen's units sign.</summary>
    public bool IsMetric
    {
      get { return _units == "metric"; }
    }

    public void Dispose()
    {
      if (_ownsHttpClient)
        _http.Dispose();
    }

    /// <summary>
    /// Looks places up by name. Never throws on what the geocoder answers - an empty list is "nothing found",
    /// and the screen says that; only a transport failure leaves this method with nothing to say.
    /// </summary>
    public async Task<IReadOnlyList<WeatherCityBookmark>> FindCitiesAsync(string query,
        CancellationToken cancellation = default)
    {
      string body = await GetAsync(string.Format(GEO_URL, Uri.EscapeDataString(query.Trim())), cancellation)
          .ConfigureAwait(false);
      List<WeatherCityBookmark> found = new List<WeatherCityBookmark>();
      using (JsonDocument document = JsonDocument.Parse(body))
      {
        foreach (JsonElement place in document.RootElement.EnumerateArray())
        {
          double lat = ReadDouble(place, "lat");
          double lon = ReadDouble(place, "lon");
          if (lat == 0 && lon == 0)
            continue;
          found.Add(new WeatherCityBookmark(ReadString(place, "name"), SurroundingsOf(place), lat, lon));
        }
      }
      return found;
    }

    /// <summary>
    /// Reads the current weather and the forecast days for one place. The two endpoints answer on their own and
    /// a failure of either comes back as the answer's <c>Error</c> naming it, because a screen showing current
    /// weather under a forecast that refused has to say so rather than showing the forecast it did not get.
    /// </summary>
    public async Task<WeatherAnswer> ReadAsync(WeatherCityBookmark city, CancellationToken cancellation = default)
    {
      string lat = city.Lat.ToString("R", CultureInfo.InvariantCulture);
      string lon = city.Lon.ToString("R", CultureInfo.InvariantCulture);
      try
      {
        string nowBody = await GetAsync(
            string.Format(CURRENT_URL, lat, lon, _units, _language), cancellation).ConfigureAwait(false);
        string forecastBody = await GetAsync(
            string.Format(FORECAST_URL, lat, lon, _units, _language), cancellation).ConfigureAwait(false);

        WeatherNow now = ParseNow(nowBody);
        IReadOnlyList<WeatherDay> days = ParseDays(forecastBody);
        return new WeatherAnswer(city, now, days, null);

      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception ex)
      {
        return new WeatherAnswer(city, null, null, ex.Message);
      }
    }

    /// <summary>
    /// The compass point the wind comes from, out of the degrees the API answers with. The words and not the
    /// number, because "wind NW 4 m/s" is what a person reads off a weather screen.
    /// </summary>
    public static string WindDirectionOf(double degrees)
    {
      string[] points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
      double wrapped = (degrees % 360 + 360) % 360;
      return points[(int) (Math.Floor(((wrapped + 22.5) % 360) / 45.0)) % 8];
    }

    /// <summary>
    /// The state and country the geocoder named around a place, for the list of matches: "Berlin, DE" against
    /// "Berlin, New Hampshire, US" is the whole reason a search answer needs a second line.
    /// </summary>
    private static string SurroundingsOf(JsonElement place)
    {
      List<string> parts = new List<string>();
      string state = ReadString(place, "state");
      string country = ReadString(place, "country");
      if (!string.IsNullOrWhiteSpace(state))
        parts.Add(state.Trim());
      if (!string.IsNullOrWhiteSpace(country))
        parts.Add(country.Trim());
      return string.Join(", ", parts);
    }

    private static WeatherNow ParseNow(string body)
    {
      using (JsonDocument document = JsonDocument.Parse(body))
      {
        JsonElement root = document.RootElement;
        JsonElement main = root.GetProperty("main");
        JsonElement wind = root.GetProperty("wind");
        return new WeatherNow(ConditionOf(root), main.GetProperty("temp").GetDouble(),
            main.TryGetProperty("feels_like", out JsonElement feels) ? feels.GetDouble() : main.GetProperty("temp").GetDouble(),
            main.GetProperty("humidity").GetInt32(),
            main.TryGetProperty("pressure", out JsonElement pressure) ? pressure.GetInt32() : 0,
            wind.TryGetProperty("speed", out JsonElement speed) ? speed.GetDouble() : 0,
            WindDirectionOf(wind.TryGetProperty("deg", out JsonElement deg) ? deg.GetDouble() : 0));
      }
    }

    /// <summary>
    /// The three-hourly entries become days: grouped by the city's own local date, high of the day's highs, low
    /// of the day's lows, the midday entry's words for the day's own, biggest rain chance of the day. Entries
    /// arrive in time order and the days keep that order, today first.
    /// </summary>
    private static IReadOnlyList<WeatherDay> ParseDays(string body)
    {
      using (JsonDocument document = JsonDocument.Parse(body))
      {
        JsonElement root = document.RootElement;
        int offsetSeconds = 0;
        if (root.TryGetProperty("city", out JsonElement city) &&
            city.TryGetProperty("timezone", out JsonElement timezone))
          offsetSeconds = timezone.GetInt32();

        Dictionary<DateTime, List<JsonElement>> byDay = new Dictionary<DateTime, List<JsonElement>>();
        List<JsonElement> entries = root.GetProperty("list").EnumerateArray().ToList();
        foreach (JsonElement entry in entries)
        {
          DateTimeOffset at = DateTimeOffset.FromUnixTimeSeconds(entry.GetProperty("dt").GetInt64())
              .ToOffset(TimeSpan.FromSeconds(offsetSeconds));
          DateTime day = at.Date;
          if (!byDay.TryGetValue(day, out List<JsonElement> ofDay))
          {
            ofDay = new List<JsonElement>();
            byDay[day] = ofDay;
          }
          ofDay.Add(entry);
        }

        List<WeatherDay> days = new List<WeatherDay>();
        foreach (KeyValuePair<DateTime, List<JsonElement>> day in byDay.OrderBy(pair => pair.Key))
        {
          List<JsonElement> ofDay = day.Value;
          JsonElement midday = ofDay.FirstOrDefault(entry =>
                  DateTimeOffset.FromUnixTimeSeconds(entry.GetProperty("dt").GetInt64())
                      .ToOffset(TimeSpan.FromSeconds(offsetSeconds)).Hour == 12,
              ofDay[0]);
          days.Add(new WeatherDay(new DateTimeOffset(day.Key, TimeSpan.FromSeconds(offsetSeconds)),
              ConditionOf(midday),
              ofDay.Max(entry => entry.GetProperty("main").GetProperty("temp_max").GetDouble()),
              ofDay.Min(entry => entry.GetProperty("main").GetProperty("temp_min").GetDouble()),
              ofDay.Max(entry => entry.TryGetProperty("pop", out JsonElement pop) ? pop.GetDouble() : 0)));
        }
        return days;
      }
    }

    private static string ConditionOf(JsonElement entry)
    {
      if (entry.TryGetProperty("weather", out JsonElement weather) && weather.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement condition in weather.EnumerateArray())
        {
          string description = ReadString(condition, "description");
          if (!string.IsNullOrWhiteSpace(description))
            return description.Trim();
        }
      }
      return "no description";
    }

    private static string ReadString(JsonElement element, string name)
    {
      return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
          ? value.GetString() ?? ""
          : "";
    }

    private static double ReadDouble(JsonElement element, string name)
    {
      return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number
          ? value.GetDouble()
          : 0;
    }

    /// <summary>
    /// One GET with the key rotation: a key that answers "unauthorised" or "too many requests" is the key that
    /// has been spent, not an address that is wrong, so the next key is asked before anybody is told that the
    /// weather did not arrive. Every other refusal travels up as it is, for <see cref="ReadAsync"/> to name.
    /// </summary>
    private async Task<string> GetAsync(string urlWithoutKey, CancellationToken cancellation)
    {
      string lastError = "no key was tried";
      for (int tryNumber = 0; tryNumber < _keys.Length; tryNumber++)
      {
        string key = _keys[(_keyIndex + tryNumber) % _keys.Length];
        string url = urlWithoutKey + key;
        try
        {
          using (HttpResponseMessage response = await _http.GetAsync(url, cancellation).ConfigureAwait(false))
          {
            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.TooManyRequests)
            {
              lastError = (int) response.StatusCode + " " + response.ReasonPhrase;
              continue;
            }

            if (!response.IsSuccessStatusCode)
              throw new HttpRequestException((int) response.StatusCode + " " + response.ReasonPhrase +
                  " from " + response.RequestMessage?.RequestUri?.Host);
            return await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
          }
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (HttpRequestException ex)
        {
          lastError = ex.Message;
        }
      }
      throw new HttpRequestException("OpenWeatherMap refused: " + lastError);
    }
  }
}
