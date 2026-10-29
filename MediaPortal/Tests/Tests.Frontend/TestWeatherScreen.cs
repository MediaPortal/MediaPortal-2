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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The weather screen and the settings page of its places: the geocoder's matches, the wall of bands the
  /// watched place is read into, and what the free three-hourly answer has to become - days of the city's own
  /// calendar standing as the forecast band.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nothing here waits for the actual weather or the actual API: OpenWeatherMap is scripted inside this file,
  /// answering with one fixed current sky and four three-hourly entries whose local times - in a city six hours
  /// east of Greenwich - deliberately split at a local midnight rather than at the UTC one. What is under test is
  /// this front end's half: the culture choosing units and language, coordinates replacing the city ids the old
  /// plugin saved because the geocoder hands out coordinates now, the watched places surviving a restart through
  /// their file, a spent key stepping aside for the next one, a refusal being said by the place that met it, the
  /// held brief answering a second showing, and "Refresh" being the one command that refuses to answer from it.
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

    private static WeatherPlacesEditorViewModel EditorOn(WeatherServer server, string settingsFile)
    {
      return new WeatherPlacesEditorViewModel(new WeatherApiClient(new HttpClient(server)), settingsFile);
    }

    #endregion

    #region The places, behind the settings door

    [Test]
    [AvaloniaTest]
    public void TheSearchFindsPlacesAndKeepingOneSurvivesARestart()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      WeatherPlacesEditorViewModel page = EditorOn(internet, settings);

      Assert.That(page.Status, Does.Contain("places watched"),
          "a television nobody has told where it stands says how few places it watches, and does not pretend "
          + "to have weather");

      page.SearchText = "Berlin";
      page.SearchCommand.Execute(null);
      WaitUntil("the geocoder to answer", () => page.Found.Count == 2);

      Assert.That(internet.AskedPaths[0], Is.EqualTo("/geo/1.0/direct"),
          "places are looked up with the geocoder, which is the search the API still gives everybody");
      Assert.That(page.Found[1].Detail, Is.EqualTo("New Hampshire, US"),
          "the state and the country are the second line that tells two Berlins apart");

      page.SelectedFound = page.Found[1];
      page.AddSelectedFoundCommand.Execute(null);
      Pump();

      Assert.That(page.Places.Select(row => row.Name), Is.EqualTo(new[] { "Berlin" }),
          "one kept place is on the list");
      Assert.That(page.Places[0].Bookmark.Lat, Is.EqualTo(44.469),
          "the American Berlin, the one that was picked");
      Assert.That(page.Status, Does.Contain("added and saved"));

      WeatherPlacesEditorViewModel restarted = EditorOn(internet, settings);
      Assert.That(restarted.Places.Count, Is.EqualTo(1),
          "and the next start of the page knows the place too - the viewer picked it once from a couch and "
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
      WeatherPlacesEditorViewModel page = EditorOn(internet, settings);

      page.SearchText = "Berlin";
      page.SearchCommand.Execute(null);
      WaitUntil("the second key to answer", () => page.Found.Count == 2);

      Assert.That(internet.AskedPaths.Count, Is.EqualTo(2),
          "one refused, one asked again with the next key - the rotation happens before the viewer hears "
          + "anything about keys");

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void TakingAPlaceOffTheListRewritesTheFile()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      File.WriteAllText(settings, "{\"places\":["
          + "{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,\"lon\":13.389},"
          + "{\"name\":\"Vienna\",\"detail\":\"AT\",\"lat\":48.208,\"lon\":16.373}]}");
      WeatherPlacesEditorViewModel page = EditorOn(internet, settings);

      page.SelectedPlace = page.Places[0];
      page.RemoveSelectedPlaceCommand.Execute(null);
      Pump();

      Assert.That(page.Places.Select(row => row.Name), Is.EqualTo(new[] { "Vienna" }),
          "the picked place is off the list, and the picker of what stays lands on the first of those that did");
      Assert.That(page.Status, Does.Contain("Berlin is off the list"));

      WeatherPlacesEditorViewModel restarted = EditorOn(internet, settings);
      Assert.That(restarted.Places.Count, Is.EqualTo(1),
          "and the file says the same - the page saves itself the moment an answer is given, and no Apply "
          + "waits to be pressed by nobody");

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

    #region The wall

    [Test]
    [AvaloniaTest]
    public void TheScreenReadsTheWatchedPlaceIntoItsBands()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      File.WriteAllText(settings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherViewModel screen = ScreenOn(internet, "de-DE", settings);

      Assert.That(screen.CurrentPlace?.Name, Is.EqualTo("Berlin"),
          "the first place on the list is home, and the screen opens speaking for it");

      screen.ScreenOpened();
      WaitUntil("the wall to rise", () => screen.Bands.Count == 4);

      Assert.That(screen.Bands.Select(band => band.Heading),
          Is.EqualTo(new[] { "Current weather", "Today", "Forecast", "Air & radar" }),
          "the four bands of the designed wall, in the design's own order");
      Assert.That(screen.Bands[0].Layout, Is.EqualTo("card1"),
          "today's card narrow as its content allows, the chart in the room its columns need");
      Assert.That(screen.Bands[1].OccupiedWidth, Is.EqualTo(980));
      Assert.That(screen.Bands[2].Tiles.Count, Is.EqualTo(2),
          "the two local days the three-hourly answer adds up to, each an upright of its own");
      Assert.That(screen.Status, Does.Contain("Berlin, DE: few clouds 8°C"),
          "and the line at the foot of the screen says which place and which sky");
      Assert.That(screen.Status, Does.Contain("2 days ahead"));

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void TheCurrentSkyArrivesInTheViewersUnits()
    {
      string viennaSettings = FreshSettingsFile();
      File.WriteAllText(viennaSettings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherServer viennasInternet = new WeatherServer();
      WeatherViewModel vienna = ScreenOn(viennasInternet, "de-AT", viennaSettings);
      vienna.ScreenOpened();
      WaitUntil("Vienna's Berlin to be read", () => vienna.Status.Contains("°C", StringComparison.Ordinal));

      Assert.That(vienna.Status, Does.Contain("8°C"),
          "eight degrees, rounded the way the viewer rounds them, with the unit their culture asks for");
      Assert.That(vienna.Status, Does.Contain("few clouds"));
      string viennasWeatherQuery = viennasInternet.AskedQueries.First(q => q.Contains("units="));
      Assert.That(viennasWeatherQuery, Does.Contain("units=metric"));
      Assert.That(viennasWeatherQuery, Does.Contain("lang=de"));

      string texasSettings = FreshSettingsFile();
      File.WriteAllText(texasSettings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherServer texansInternet = new WeatherServer();
      WeatherViewModel texas = ScreenOn(texansInternet, "en-US", texasSettings);
      texas.ScreenOpened();
      WaitUntil("the Texan Berlin to be read", () => texas.Status.Contains("°F", StringComparison.Ordinal));

      Assert.That(texas.Status, Does.Contain("8°F"),
          "the same sky in Fahrenheit for a viewer whose culture never learned the other scale, and the "
          + "front end has no settings screen for it because it never needs one");
      Assert.That(texansInternet.AskedQueries.First(q => q.Contains("units=")), Does.Contain("units=imperial"));
      Assert.That(texansInternet.AskedQueries.First(q => q.Contains("lang=")), Does.Contain("lang=en"));

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
      screen.ScreenOpened();
      WaitUntil("the forecast band to stand", () => screen.Bands.Any(band => band.Heading == "Forecast"));

      HomeContentGroup forecast = screen.Bands.First(band => band.Heading == "Forecast");

      // Grouping the four entries by the UTC date would stand THREE days on this band; the city's own
      // midnight gives TWO - the entry at 18:30 UTC on the 4th is already the city's 5th. The weekday is
      // the date wearing a name, so the band's two uprights say which calendar they were cut by.
      Assert.That(forecast.Tiles.Select(tile => tile.Card!.Label), Is.EqualTo(new[] { "Saturday", "Monday" }),
          "the city's 3rd and the city's 5th, not the 4th the UTC clock says: a forecast that renames its "
          + "days at a plane's take-off is no forecast anybody plans around");

      HomeInfoCard first = forecast.Tiles[0].Card!;
      Assert.That(first.Detail, Is.EqualTo("6°C / 1°C"),
          "the day's span is its own extremes across all its entries, unit on both ends");
      Assert.That(first.Sub, Is.EqualTo("20 %"),
          "and its rain chance is the biggest the day carries, not the noon's alone");

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
      screen.ScreenOpened();
      WaitUntil("the refusal to be said", () => screen.Status.Contains("did not answer", StringComparison.Ordinal));

      Assert.That(screen.Status, Does.Contain("Berlin did not answer"),
          "the place names the failure - 'could not read the weather' from a screen with two places on it "
          + "leaves the viewer guessing which one went quiet");
      Assert.That(screen.Bands.Count, Is.EqualTo(0),
          "and nothing is invented under a refusal - an empty wall and a named reason, not yesterday's numbers");

      Assert.That(screen.Places.Count, Is.EqualTo(2), "the other place is still on the list");
      screen.ChoosePlace(screen.Places[1]);
      WaitUntil("the second place's own try", () => screen.Status.Contains("Vienna", StringComparison.Ordinal));

      Assert.That(screen.Status, Does.Contain("Vienna did not answer"),
          "the walker through the picker gets their own try at their own weather - and the place that met "
          + "the refusal this time is named as the one that met it");

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void AShowingAnswersFromTheHeldBriefAndRefreshIsTheOneThatAsksAgain()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      File.WriteAllText(settings, "{\"places\":[{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,"
          + "\"lon\":13.389}]}");
      WeatherViewModel screen = ScreenOn(internet, "en", settings);
      screen.ScreenOpened();
      WaitUntil("the first read", () => screen.Bands.Count == 4);

      int askedFirst = internet.AskedPaths.Count;
      screen.ScreenOpened();
      Pump();
      Pump();

      Assert.That(internet.AskedPaths.Count, Is.EqualTo(askedFirst),
          "a screen shown again inside the wall's quarter answers from the brief the home wall would ask too - "
          + "the same sky is not asked twice in one minute");

      screen.RefreshCommand.Execute(null);
      WaitUntil("the refresh to have asked", () => internet.AskedPaths.Count > askedFirst);

      Assert.That(screen.Bands.Count, Is.EqualTo(4),
          "and the wall stands on the new reading, which is all Refresh ever promised");

      File.Delete(settings);
    }

    [Test]
    [AvaloniaTest]
    public void ChoosingAPlaceRereadsTheWallAndAScreenOpenedAfterwardsKnowsIt()
    {
      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      File.WriteAllText(settings, "{\"places\":["
          + "{\"name\":\"Berlin\",\"detail\":\"DE\",\"lat\":52.517,\"lon\":13.389},"
          + "{\"name\":\"Vienna\",\"detail\":\"AT\",\"lat\":48.208,\"lon\":16.373}]}");
      WeatherViewModel screen = ScreenOn(internet, "en", settings);
      screen.ScreenOpened();
      WaitUntil("Berlin's wall", () => screen.Status.Contains("Berlin, DE:", StringComparison.Ordinal));

      screen.ChoosePlace(screen.Places[1]);
      WaitUntil("Vienna's wall", () => screen.Status.Contains("Vienna, AT:", StringComparison.Ordinal));

      Assert.That(screen.CurrentPlace?.Name, Is.EqualTo("Vienna"),
          "the picker's answer is the screen's new standing place, not a one-off reading");

      // And the file may have changed while the dialog was open - the settings page edits the same list.
      File.WriteAllText(settings, "{\"places\":["
          + "{\"name\":\"Vienna\",\"detail\":\"AT\",\"lat\":48.208,\"lon\":16.373}]}");
      screen.ScreenOpened();
      Pump();

      Assert.That(screen.Places.Count, Is.EqualTo(1),
          "a showing takes its places off the file again, so a place removed behind the settings door is on "
          + "the picker gone without a restart");
      Assert.That(screen.CurrentPlace?.Name, Is.EqualTo("Vienna"),
          "and the place that survived the edit keeps the screen");

      File.Delete(settings);
    }

    #endregion

    #region The dialog that asks which place

    /// <summary>
    /// The question "which place does the wall speak for" as the machine raises it: over the weather screen,
    /// on the skin's headed frame, naming itself, offering the button that says what Enter does, and lighting
    /// every line of every row - because a second line left to its own colour inherits the platform's black
    /// the moment the row is selected, which on the dialog's dark grey ground is no letter at all.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void ThePlacesDialogNamesItselfOffersItsButtonAndLightsItsRows()
    {
      Application app = Application.Current;
      Assert.That(FrontendTheme.Apply(app, "nereus", out string themeProblem), Is.EqualTo("nereus"),
          themeProblem ?? "");
      Assert.That(SkinEngine.Apply(app, "nereus", out string skinProblem), Is.True, skinProblem ?? "");
      SkinEngine.SkinsFolder = Path.Combine(FrontendSource(), "Skins");

      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      WeatherPlacesEditorViewModel editor = EditorOn(internet, settings);
      editor.SearchText = "Berlin";
      editor.SearchCommand.Execute(null);
      WaitUntil("the geocoder to answer", () => editor.Found.Count == 2);
      editor.SelectedFound = editor.Found[0];
      editor.AddSelectedFoundCommand.Execute(null);
      Pump();

      WeatherViewModel screen = ScreenOn(internet, "de-DE", settings);
      WorkflowManager workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "weather")
          return new WeatherView(screen, workflow, null);
        if (frame.StateId == "weatherplaces")
          return new WeatherPlacePickerView((WeatherViewModel)frame.ViewModel, workflow);
        return null;
      });
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      window.Workflow.Push(new WorkflowFrame("weather", "Weather", screen));
      PumpRepeatedly();
      window.Workflow.Push(new WorkflowFrame("weatherplaces", "Weather places", screen));
      PumpRepeatedly();

      try
      {
        Assert.That(window.Workflow.Current?.StateId, Is.EqualTo("weatherplaces"),
            "the question is raised as a dialog of the weather screen, over the wall it changes");

        ListBox? places = window.GetVisualDescendants().OfType<ListBox>()
            .SingleOrDefault(list => list.Name == "placesList");
        Assert.That(places, Is.Not.Null, "the dialog of places did not even show its list");

        TextBlock? heading = window.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(line => line.Text == "Weather places");
        Assert.That(heading, Is.Not.Null,
            "a dialog that does not say what it asks is the strangeness the viewer complained of");

        Button? choose = window.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(button => button.Content as string == "Choose");
        Assert.That(choose, Is.Not.Null,
            "a row of places answers nothing by being looked at, so the dialog owes a button of its own");

        // Every row has to have arrived and every line of it has to stand lighter than the dialog's ground.
        WaitUntil("the place to stand on the dialog",
            () => places!.GetVisualDescendants().OfType<TextBlock>().Any(line => line.Text == "Berlin"));

        foreach (string line in new[] { "Berlin", "DE" })
        {
          TextBlock row = places!.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == line);
          ISolidColorBrush ink = row.Foreground as ISolidColorBrush;
          Assert.That(ink, Is.Not.Null, $"the line {line} must say its own colour");
          double light = (0.299 * ink.Color.R + 0.587 * ink.Color.G + 0.114 * ink.Color.B) / 255.0
              * ink.Opacity;
          Assert.That(light, Is.GreaterThan(0.5),
              $"the line {line} reads as dark on the dialog's dark grey ground, which is no letter at all");
        }

        // A developer's eye: MP2_SHOT names a file and the dialog is painted into it, because no screen
        // capture on this machine's display adapter can see the dialog layer through the shell's blur.
        string? shot = Environment.GetEnvironmentVariable("MP2_SHOT");
        if (!string.IsNullOrEmpty(shot))
        {
          RenderTargetBitmap frame = new RenderTargetBitmap(new PixelSize(1920, 1080), new Vector(96, 96));
          frame.Render(window);
          byte[] pixels = new byte[1920 * 1080 * 4];
          GCHandle pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
          try
          {
            frame.CopyPixels(new PixelRect(0, 0, 1920, 1080), pinned.AddrOfPinnedObject(), pixels.Length, 1920 * 4);
          }
          finally
          {
            pinned.Free();
          }
          File.WriteAllBytes(shot, pixels);
        }
      }
      finally
      {
        window.Close();
        File.Delete(settings);
      }
    }

    private static void PumpRepeatedly()
    {
      for (int i = 0; i < 60; i++)
      {
        Pump();
        Thread.Sleep(20);
      }
    }

    private static string FrontendSource()
    {
      AssemblyMetadataAttribute? value = typeof(TestWeatherScreen).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, "FrontendSource", StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty);
      return Path.GetFullPath(value!.Value!);
    }

    #endregion

    /// <summary>
    /// What the wall owes the keyboard when an answer arrives: the picked place's read rebuilds every band,
    /// and the card the viewer stood on goes with the old ones - a control already out of the tree cannot
    /// tell the window it took the focus with it. The keyboard must therefore come back to a card by itself,
    /// which is the complaint the viewer has now made by name more than once.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void APickedPlaceRebuildsTheWallAndStillLeavesAStandingCard()
    {
      Application app = Application.Current;
      Assert.That(FrontendTheme.Apply(app, "nereus", out string themeProblem), Is.EqualTo("nereus"),
          themeProblem ?? "");
      Assert.That(SkinEngine.Apply(app, "nereus", out string skinProblem), Is.True, skinProblem ?? "");
      SkinEngine.SkinsFolder = Path.Combine(FrontendSource(), "Skins");

      WeatherServer internet = new WeatherServer();
      string settings = FreshSettingsFile();
      WeatherPlacesEditorViewModel editor = EditorOn(internet, settings);
      editor.SearchText = "Berlin";
      editor.SearchCommand.Execute(null);
      WaitUntil("the geocoder to answer", () => editor.Found.Count == 2);
      editor.SelectedFound = editor.Found[0];
      editor.AddSelectedFoundCommand.Execute(null);
      Pump();

      WeatherViewModel screen = ScreenOn(internet, "de-DE", settings);
      WorkflowManager workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "weather")
          return new WeatherView(screen, workflow, null);
        if (frame.StateId == "weatherplaces")
          return new WeatherPlacePickerView((WeatherViewModel)frame.ViewModel, workflow);
        return null;
      });
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      window.Workflow.Push(new WorkflowFrame("weather", "Weather", screen));
      PumpRepeatedly();
      WaitUntil("the first wall to dress", () => screen.Bands.Count > 0);
      WaitUntil("the screen to hand its keys to a card",
          () => window.FocusManager.GetFocusedElement() is Button);
      Assert.That(window.FocusManager.GetFocusedElement(), Is.InstanceOf<Button>(),
          "the screen hands its keys to a card the moment its wall stands");

      // The dialog as the menu raises it, and as the viewer answers it: the dialog steps aside and the
      // picked place is handed to the screen - which is exactly when the wall is rebuilt from under the
      // keyboard.
      window.Workflow.Push(new WorkflowFrame("weatherplaces", "Weather places", screen));
      PumpRepeatedly();
      window.Workflow.Pop();
      screen.ChoosePlace(screen.Places[0]);

      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        object? standing = window.FocusManager.GetFocusedElement();
        if (standing is Button card && card.Classes.Contains("nereus-card-tile"))
        {
          window.Close();
          File.Delete(settings);
          return; // a card of the rebuilt wall stands on the keyboard - the debt is paid
        }
      }

      string held = window.FocusManager.GetFocusedElement()?.GetType().Name ?? "nothing";
      window.Close();
      File.Delete(settings);
      Assert.Fail("after the picked place rebuilt the wall the keyboard stood on " + held
          + " - the viewer is left with keys that buy nothing");
    }

    #region The compass

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
