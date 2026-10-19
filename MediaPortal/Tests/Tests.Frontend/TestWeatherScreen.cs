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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The weather screen: the geocoder's matches, the sky right now, and what the free three-hourly answer has
  /// to become - days of the city's own calendar.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nothing here waits for the actual weather or the actual API: OpenWeatherMap is scripted inside this file,
  /// answering with one fixed current sky and four three-hourly entries whose local times - in a city six hours
  /// east of Greenwich - deliberately split at a local midnight rather than at the UTC one. What is under test is
  /// this front end's half: the culture choosing units and language, coordinates replacing the city ids the old
  /// plugin saved because the geocoder hands out coordinates now, a day taking its midday's words and its own
  /// extremes, the watched places surviving a restart through their file, a spent key stepping aside for the
  /// next one, and a refusal being said.
  /// </para>
  /// <para>
  /// No test awaits, for the reason spelled out in <c>TestMediaBrowser</c>; the view-model tests pump the queue
  /// until the screen says it is done. Each test passes its own settings file in the temporary folder, so no
  /// test ever reads or writes what the viewer who runs the build saved.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestWeatherScreen
  {
    #region Fixtures

    /// <summary>
    /// Two Berlin sixty degrees of latitude apart, because the whole point of the geocoder's answer is the second
    /// line that tells them apart.
    /// </summary>
    private const string GEO_JSON = "["
        + "{\"name\":\"Berlin\",\"lat\":52.517,\"lon\":13.389,\"country\":\"DE\"},"
        + "{\"name\":\"Berlin\",\"lat\":44.469,\"lon\":-71.184,\"country\":\"US\",\"state\":\"New Hampshire\"}"
        + "]";

    /// <summary>Eight degrees and few clouds, wind at sixty degrees, which is north-east.</summary>
    private const string CURRENT_JSON = "{\"coord\":{\"lon\":13.39,\"lat\":52.52},"
        + "\"weather\":[{\"id\":801,\"main\":\"Clouds\",\"description\":\"few clouds\",\"icon\":\"02d\"}],"
        + "\"main\":{\"temp\":8.04,\"feels_like\":6.1,\"temp_min\":6.5,\"temp_max\":8.98,\"pressure\":1032,"
        + "\"humidity\":78},\"wind\":{\"speed\":3.09,\"deg\":60},\"name\":\"Berlin\"}";

    /// <summary>
    /// Four three-hourly entries for a city six hours east of Greenwich - Ürümqi, a real OpenWeatherMap city at
    /// UTC+6, chosen because its local midnight sits far from the UTC one. The first two entries belong to the
    /// city's 3rd of January (its 10:00 and its noon) and the last two to its 5th; the third entry is 18:30 UTC
    /// on the 4th, which is already the city's 5th, so grouping by UTC date and grouping by the city's date
    /// answer differently and the answer under test is the city's. Both days carry an entry at the city's exact
    /// noon, because that entry's words are what the day is supposed to read.
    /// </summary>
    private const string FORECAST_JSON = "{\"cod\":\"200\",\"cnt\":4,\"city\":{\"id\":1529068,"
        + "\"name\":\"Ürümqi\",\"timezone\":21600},\"list\":["
        // 2026-01-03 04:00 UTC = 10:00 local: light rain, the day's cold morning.
        + "{\"dt\":1767412800,\"main\":{\"temp\":2.0,\"feels_like\":-1.0,\"temp_min\":1.2,\"temp_max\":2.4,"
        + "\"pressure\":1030,\"humidity\":88},\"weather\":[{\"id\":500,\"main\":\"Rain\","
        + "\"description\":\"light rain\",\"icon\":\"10d\"}],\"pop\":0.2,\"wind\":{\"speed\":2.0,\"deg\":200}},"
        // 2026-01-03 06:00 UTC = 12:00 local: the day's warm midday, and therefore its words.
        + "{\"dt\":1767420000,\"main\":{\"temp\":6.0,\"feels_like\":4.0,\"temp_min\":3.0,\"temp_max\":6.1,"
        + "\"pressure\":1031,\"humidity\":70},\"weather\":[{\"id\":803,\"main\":\"Clouds\","
        + "\"description\":\"broken clouds\",\"icon\":\"04d\"}],\"pop\":0.1,\"wind\":{\"speed\":2.5,\"deg\":240}},"
        // 2026-01-04 18:30 UTC = 00:30 local on the 5th: by UTC reckoning the 4th, by the city's own clock the
        // 5th. The day must answer to its own midnight.
        + "{\"dt\":1767551400,\"main\":{\"temp\":1.0,\"feels_like\":-2.0,\"temp_min\":-1.4,\"temp_max\":1.4,"
        + "\"pressure\":1028,\"humidity\":92},\"weather\":[{\"id\":600,\"main\":\"Snow\","
        + "\"description\":\"snow\",\"icon\":\"13d\"}],\"pop\":0.6,\"wind\":{\"speed\":4.0,\"deg\":320}},"
        // 2026-01-05 06:00 UTC = 12:00 local: the second day's own midday.
        + "{\"dt\":1767592800,\"main\":{\"temp\":3.5,\"feels_like\":1.0,\"temp_min\":0.5,\"temp_max\":3.9,"
        + "\"pressure\":1029,\"humidity\":80},\"weather\":[{\"id\":600,\"main\":\"Snow\","
        + "\"description\":\"light snow\",\"icon\":\"13d\"}],\"pop\":0.4,\"wind\":{\"speed\":3.5,\"deg\":340}}"
        + "]}";

    private static readonly WeatherCityBookmark BERLIN =
        new WeatherCityBookmark("Berlin", "DE", 52.517, 13.389);

    /// <summary>
    /// OpenWeatherMap, scripted, with three moods: everything answers, the weather endpoint answers 500, or the
    /// first N requests are refused as spent keys before the answers start. It remembers the URLs it was asked.
    /// </summary>
    private class WeatherServer : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();

      private readonly bool _currentRefuses;
      private int _refuseFirst;

      internal WeatherServer(bool currentRefuses = false, int refuseFirstRequests = 0)
      {
        _currentRefuses = currentRefuses;
        _refuseFirst = refuseFirstRequests;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        AskedPaths.Add(request.RequestUri!.AbsolutePath);
        AskedQueries.Add(request.RequestUri.Query);

        if (_refuseFirst > 0)
        {
          _refuseFirst--;
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
          {
            RequestMessage = request
          });
        }

        string path = request.RequestUri.AbsolutePath;
        if (path.EndsWith("/weather", StringComparison.Ordinal) && _currentRefuses)
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
          {
            RequestMessage = request
          });

        string body = path.EndsWith("/direct", StringComparison.Ordinal) ? GEO_JSON
            : path.EndsWith("/weather", StringComparison.Ordinal) ? CURRENT_JSON
            : path.EndsWith("/forecast", StringComparison.Ordinal) ? FORECAST_JSON
            : null;
        if (body == null)
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition())
          return;
      }
      Assert.Fail("Timed out after 15 seconds waiting for " + because);
    }

    private static string FreshSettingsFile()
    {
      return Path.Combine(Path.GetTempPath(),
          "mp2-weather-test-" + Guid.NewGuid().ToString("N") + ".json");
    }

    private static WeatherViewModel ScreenOn(WeatherServer server, string culture, string settingsFile)
    {
      return new WeatherViewModel(
          new WeatherApiClient(new HttpClient(server), cultureName: culture), settingsFile, culture);
    }

    #endregion

    #region The places

    [Test]
    [AvaloniaTest]
    public void TheSearchFindsPlacesAndKeepingOneSurvivesARestart()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      WeatherViewModel screen = ScreenOn(internet, "en", settings);

      Assert.That(screen.Status, Does.Contain("No place watched yet"),
          "a television nobody has told where it stands says that, and does not pretend to have weather");

      screen.SearchText = "Berlin";
      screen.SearchCommand.Execute(null);
      WaitUntil("the geocoder to answer", () => screen.Found.Count == 2);

      Assert.That(internet.AskedPaths[0], Is.EqualTo("/geo/1.0/direct"),
          "places are looked up with the geocoder, which is the search the API still gives everybody");
      Assert.That(screen.Found[1].Detail, Is.EqualTo("New Hampshire, US"),
          "the state and the country are the second line that tells two Berlins apart");

      screen.SelectedFound = screen.Found[1];
      screen.AddSelectedFoundCommand.Execute(null);
      Pump();

      Assert.That(screen.Places.Select(row => row.Name), Is.EqualTo(new[] { "Berlin" }),
          "one kept place is on the list");
      Assert.That(screen.SelectedPlace!.Bookmark.Lat, Is.EqualTo(44.469),
          "the American Berlin, the one that was picked, and the one now being read");
      Assert.That(screen.Status, Does.Contain("added and saved"));

      WeatherViewModel restarted = ScreenOn(internet, "en", settings);
      Assert.That(restarted.Places.Count, Is.EqualTo(1),
          "and the next start of the screen knows the place too - the viewer picked it once from a couch and "
          + "is not asked to type it again");
      Assert.That(restarted.Places[0].Detail, Is.EqualTo("New Hampshire, US"));

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void ARefusedSearchIsNamedAndTheKeysStepAside()
    {
      // The first request answers "unauthorised" - a spent key, which is the normal fate of shared demo keys -
      // and the client has to try the next key before anybody is told the town does not exist.
      WeatherServer internet = new WeatherServer(refuseFirstRequests: 1);
      string settings = FreshSettingsFile();
      WeatherViewModel screen = ScreenOn(internet, "en", settings);

      screen.SearchText = "Berlin";
      screen.SearchCommand.Execute(null);
      WaitUntil("the second key to answer", () => screen.Found.Count == 2);

      Assert.That(internet.AskedPaths.Count, Is.EqualTo(2),
          "one refused, one asked again with the next key - the rotation happens before the viewer hears "
          + "anything about keys");

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void TheWeatherTileIsOpenableWithoutAServer()
    {
      HomeViewModel home = new HomeViewModel();
      MainMenuEntry weather = home.Menu.Single(entry => entry.Label == "Weather");

      Assert.That(weather.ScreenKey, Is.EqualTo("weather"),
          "the tile leads to the weather screen");
      Assert.That(weather.Missing, Is.Null,
          "the weather never came from the MP2 server even in the old MP2 - the plugin fetched it from "
          + "OpenWeatherMap itself - so the tile has no reason to apologise, on a machine with no server and no "
          + "media both of it still work");
      Assert.That(weather.Openable, Is.True);
    }

    #endregion

    #region The sky

    [Test]
    [AvaloniaTest]
    public void TheCurrentSkyArrivesInTheViewersUnits()
    {
      string viennaSettings = FreshSettingsFile();
      File.WriteAllText(viennaSettings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherServer viennasInternet = new WeatherServer();
      WeatherViewModel vienna = ScreenOn(viennasInternet, "de-AT", viennaSettings);
      WaitUntil("Vienna's Berlin to be read", () => vienna.CurrentLine.Contains("°C", StringComparison.Ordinal));

      Assert.That(vienna.CurrentLine, Does.StartWith("8°C"),
          "eight degrees, rounded the way the viewer rounds them, with the unit their culture asks for");
      Assert.That(vienna.CurrentLine, Does.Contain("few clouds"));
      Assert.That(vienna.CurrentDetail, Does.Contain("feels like 6°C"));
      Assert.That(vienna.CurrentDetail, Does.Contain("78% humidity"));
      Assert.That(vienna.CurrentDetail, Does.Contain("NE 3,1 m/s"),
          "the German screen carries the German comma, and sixty degrees of wind is a north-easterly - the "
          + "direction as the word a person reads, not the number on a dial");
      string viennasWeatherQuery = viennasInternet.AskedQueries.First(q => q.Contains("units="));
      Assert.That(viennasWeatherQuery, Does.Contain("units=metric"));
      Assert.That(viennasWeatherQuery, Does.Contain("lang=de"));

      string texasSettings = FreshSettingsFile();
      File.WriteAllText(texasSettings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherServer texansInternet = new WeatherServer();
      WeatherViewModel texas = ScreenOn(texansInternet, "en-US", texasSettings);
      WaitUntil("the Texan Berlin to be read", () => texas.CurrentLine.Contains("°F", StringComparison.Ordinal));

      Assert.That(texas.CurrentLine, Does.StartWith("8°F"),
          "the same sky in Fahrenheit for a viewer whose culture never learned the other scale, and the "
          + "front end has no settings screen for it because it never needs one");
      Assert.That(texansInternet.AskedQueries.First(q => q.Contains("units=")), Does.Contain("units=imperial"));
      Assert.That(texansInternet.AskedQueries.First(q => q.Contains("lang=")), Does.Contain("lang=en"));
      Assert.That(texas.CurrentDetail, Does.Contain("3.1 mph"),
          "and the same wind in miles an hour - 3.09 of them either way, whatever the unit says");

      File.Delete(viennaSettings);
      File.Delete(texasSettings);
    }

    [Test]
    [AvaloniaTest]
    public void ADayIsItsOwnCitysLocalDay()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      File.WriteAllText(settings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherViewModel screen = ScreenOn(internet, "de-DE", settings);
      WaitUntil("the two days to arrive", () => screen.Days.Count == 2);

      WeatherDayRow day = screen.Days[0];
      Assert.That(day.Day.Date.Date, Is.EqualTo(new DateTime(2026, 1, 3)),
          "the first day is the city's 3rd of January; two of the entries are past that day's UTC midnight but "
          + "before the city's own, and grouping by UTC would start the day one entry late");

      Assert.That(day.Overview, Is.EqualTo("broken clouds"),
          "a day's words are its midday's, not its first entry's - the morning rain is true at ten and not what"
          + " anybody means by 'what is tomorrow like'");
      Assert.That(day.Range, Is.EqualTo("6°C / 1°C"),
          "and its span is the day's own extremes across all its entries");
      Assert.That(day.Rain, Is.EqualTo("rain 20%"),
          "and its rain chance is the biggest the day carries");

      WeatherDayRow next = screen.Days[1];
      Assert.That(next.Day.Date.Date, Is.EqualTo(new DateTime(2026, 1, 5)),
          "the city's 5th, not the 4th the UTC clock says: the entry at midnight UTC is the city's noon on the"
          + " 5th's neighbour already past");
      Assert.That(next.Overview, Is.EqualTo("light snow"));

      Assert.That(screen.Status, Does.Contain("Berlin: few clouds 8°C, 2 days ahead"));

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void ARefusedCityIsNamedAndTheOtherPlacesStay()
    {
      WeatherServer internet = new WeatherServer(currentRefuses: true);
      string settings = FreshSettingsFile();
      File.WriteAllText(settings, "{\"places\":["
          + "{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,\"lon\":13.389},"
          + "{\"name\":\"Vienna\",\"detail\":\"AT\",\"lat\":48.208,\"lon\":16.373}]}");
      WeatherViewModel screen = ScreenOn(internet, "en", settings);
      WaitUntil("the refusal to be said", () => screen.Status.Contains("did not answer", StringComparison.Ordinal));

      Assert.That(screen.Status, Does.Contain("Berlin did not answer"),
          "the place names the failure - 'could not read the weather' from a screen with two places on it "
          + "leaves the viewer guessing which one went quiet");
      Assert.That(screen.Days.Count, Is.EqualTo(0),
          "and nothing is invented under a refusal - an empty panel and a named reason, not yesterday's numbers");

      Assert.That(screen.Places.Count, Is.EqualTo(2), "the other place is still on the list");
      screen.SelectedPlace = screen.Places[1];
      Assert.That(screen.SelectedPlace!.Name, Is.EqualTo("Vienna"),
          "the walker through the column can still walk - the second place gets its own try at its own weather");

      File.Delete(settings);
    }

    [Test]
    public void TheCompassHasEightPointsAndWrapsAround()
    {
      Assert.That(WeatherApiClient.WindDirectionOf(0), Is.EqualTo("N"));
      Assert.That(WeatherApiClient.WindDirectionOf(60), Is.EqualTo("NE"));
      Assert.That(WeatherApiClient.WindDirectionOf(180), Is.EqualTo("S"));
      Assert.That(WeatherApiClient.WindDirectionOf(292), Is.EqualTo("W"));
      Assert.That(WeatherApiClient.WindDirectionOf(359), Is.EqualTo("N"));
      Assert.That(WeatherApiClient.WindDirectionOf(-10), Is.EqualTo("N"),
          "and a nonsense negative reading still lands on the compass instead of throwing off a weather screen");
    }

    #endregion
  }
}
