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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using FrontendAspect = MediaPortal.Frontend.Api.MediaAspect;

namespace Tests.Frontend
{
  /// <summary>
  /// What a screen knows the next evening: the order its shelf is read in, which end it starts at, what the
  /// shelf stands under, and which value of which category it was cut to - the four the reference kept in
  /// <c>ViewSettings</c> beside the shape, and the four this front end had left to the visit until now.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Two keys, because the reference had two: the four belong to a screen, and a screen is one media kind seen
  /// through one filter class, while the filter itself belongs to the media kind and says which screen opens.
  /// The memories are also per person, which is what the reference's per-user skin files were.
  /// </para>
  /// <para>
  /// Every test names its own settings folder and its own screens file, and takes the signed-in person off
  /// again, so nothing here reads or writes what a real viewer on this machine picked.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestScreenMemory
  {
    private static readonly Uri SERVER_URL = new Uri("http://localhost:55555/MediaPortal/Server");

    private string _previousSettingsFile = "";
    private string _previousScreensFile = "";
    private string _previousSkinDir = "";
    private string _ownDir = "";
    private string? _ownerBefore;

    [SetUp]
    public void IsolateTheMachinesMemory()
    {
      _previousSettingsFile = Environment.GetEnvironmentVariable("MP2_SETTINGS_FILE") ?? "";
      _previousScreensFile = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE") ?? "";
      _previousSkinDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownerBefore = SkinSettingsBase.FileOwner;

      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-memory-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_ownDir);

      // Both names at once: the folder the settings of a person stand in, and - for every test but the one
      // about persons - the one file the screens' memories are told to use.
      Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", Path.Combine(_ownDir, "frontend.json"));
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownDir);
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", Path.Combine(_ownDir, "screens.json"));
      SkinSettingsBase.FileOwner = null;
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE",
          _previousSettingsFile.Length == 0 ? null : _previousSettingsFile);
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE",
          _previousScreensFile.Length == 0 ? null : _previousScreensFile);
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _previousSkinDir.Length == 0 ? null : _previousSkinDir);
      SkinSettingsBase.FileOwner = _ownerBefore;

      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    private static HomeSection Films()
    {
      return new HomeSection("Movies",
          new AspectViewModel(new FrontendAspect(MovieAspect.ASPECT_ID, MovieAspect.Metadata.Name, false, false,
              null, Array.Empty<AspectAttribute>())));
    }

    private static HomeSection Series()
    {
      return new HomeSection("Series",
          new AspectViewModel(new FrontendAspect(SeriesAspect.ASPECT_ID, SeriesAspect.Metadata.Name, false,
              false, null, Array.Empty<AspectAttribute>())));
    }

    private static MediaBrowserViewModel AShelf(HomeSection section, ServerApiClient? client = null)
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "a shelf needs the application to know what skin it wears");
      FrontendTheme.Apply(app!, FrontendTheme.DEFAULT_NAME, out _);
      return new MediaBrowserViewModel(client, section, null);
    }

    /// <summary>What the file holds for one screen, or nothing when nobody answered it.</summary>
    private static ScreenViewSettings.ScreenAnswer Written(string aspect, string filter_class)
    {
      ScreenViewSettings settings = ScreenViewSettings.Load(null, out _);
      Assert.That(settings.TryAnswer(aspect, filter_class, out ScreenViewSettings.ScreenAnswer answer), Is.True,
          "nothing was remembered about " + aspect + "#" + filter_class);
      return answer;
    }

    private static bool WasAskedAbout(string aspect, string filter_class)
    {
      return ScreenViewSettings.Load(null, out _).TryAnswer(aspect, filter_class, out _);
    }

    [AvaloniaTest]
    public async Task TheOrderAndTheEndItStartsAtComeBackToTheShelfTheyWereLeftOn()
    {
      MediaBrowserViewModel films = AShelf(Films());
      Assert.That(films.SortOrder, Is.EqualTo("title"), "a shelf nobody answered opens by name");

      await films.PickSortAsync("year");
      await films.PickSortAsync("year");
      // Asked twice is the shelf the other way round, and that answer is as much the viewer's as the first.

      MediaBrowserViewModel again = AShelf(Films());
      Assert.That(again.SortOrder, Is.EqualTo("year"),
          "the evening that read the films by year opens by year the next");
      Assert.That(again.SortDescending, Is.True, "and at the end it was started at");
      Assert.That(again.SortLabel, Is.EqualTo("Sort: year"));

      // A shelf of series was never asked anything, and the films' order is not its answer.
      Assert.That(AShelf(Series()).SortOrder, Is.EqualTo("title"));
    }

    [AvaloniaTest]
    public async Task TheGroupingComesBackAndTheAnswerOfNoneStands()
    {
      MediaBrowserViewModel films = AShelf(Films());
      await films.PickGroupAsync("genre");

      Assert.That(AShelf(Films()).IsGrouped, Is.True, "a shelf stood under plates the evening before");
      Assert.That(AShelf(Films()).GroupLabel, Is.EqualTo("Group: Genre"));

      // "None" is an answer like every other: the shelf that was put back on one shelf stays on it.
      MediaBrowserViewModel grouped = AShelf(Films());
      await grouped.PickGroupAsync(null);
      Assert.That(AShelf(Films()).IsGrouped, Is.False, "the viewer said none, and none is what they get");
    }

    [AvaloniaTest]
    public async Task TheCutComesBackWithItsValueAndTakesTheViewerToThatScreen()
    {
      MediaBrowserViewModel films = AShelf(Films());
      await films.PickFilterAsync("genre", "Action");

      MediaBrowserViewModel again = AShelf(Films());
      Assert.That(again.GroupBy, Is.EqualTo("genre"));
      Assert.That(again.GroupValue, Is.EqualTo("Action"),
          "a category with no value is a question the server refuses, so the value travels with it");
      Assert.That(again.HasShelfFilter, Is.True);
      Assert.That(again.FilterLabel, Is.EqualTo("Filter: genre = Action"));

      // And the order remembered there belongs to that screen: the shelf the landing opens on is the one
      // that answers, not the whole shelf behind it.
      await again.PickSortAsync("added");
      MediaBrowserViewModel third = AShelf(Films());
      Assert.That(third.GroupValue, Is.EqualTo("Action"));
      Assert.That(third.SortOrder, Is.EqualTo("added"));
      Assert.That(Written("MovieItem", "genre").Sort, Is.EqualTo("added"));
      Assert.That(WasAskedAbout("MovieItem", ScreenViewSettings.NO_FILTER), Is.False,
          "the whole shelf was never answered, and the genre's order is not its order");

      // Saying "all" is saying the shelf was the last one: the landing goes with the cut.
      await third.PickFilterAsync(null, null);
      Assert.That(AShelf(Films()).HasShelfFilter, Is.False);
    }

    [AvaloniaTest]
    public async Task OneFilterClassReadsInItsOwnOrderAndWearsItsOwnShape()
    {
      MediaBrowserViewModel films = AShelf(Films());
      await films.PickSortAsync("year");
      BrowserLayout whole_shelf = films.Layout;

      // Walking into a genre is walking onto another screen, which is what the reference meant by keeping an
      // order per screen: its own, and its own shape.
      await films.PickFilterAsync("genre", "Action");
      films.SwitchView();
      BrowserLayout worn = films.Layout;
      Assert.That(worn, Is.Not.EqualTo(whole_shelf), "the shelf really did change its shape on the way in");
      await films.PickSortAsync("title");

      Assert.That(Written("MovieItem", "genre").Layout, Is.EqualTo(worn.ToString()));
      Assert.That(Written("MovieItem", "genre").Sort, Is.EqualTo("title"));
      Assert.That(Written("MovieItem", ScreenViewSettings.NO_FILTER).Sort, Is.EqualTo("year"),
          "the whole shelf kept the order it was left in, undisturbed by the screen beside it");
      Assert.That(Written("MovieItem", ScreenViewSettings.NO_FILTER).Layout, Is.Null,
          "the whole shelf was never asked for a shape, so the genre's shape is not its answer");

      // Walking back out of the genre puts the whole shelf's own order back on it. Its shape stays as it
      // stands: a screen nobody ever answered about its shape wears the one its viewer is looking at, which
      // the next press then writes down as this screen's own answer.
      await films.PickFilterAsync(null, null);
      Assert.That(films.SortOrder, Is.EqualTo("year"));
      Assert.That(films.Layout, Is.EqualTo(worn));
      films.SwitchView();
      Assert.That(Written("MovieItem", ScreenViewSettings.NO_FILTER).Layout,
          Is.EqualTo(films.Layout.ToString()), "and the press belongs to the screen it was made on");
    }

    [AvaloniaTest]
    public async Task TwoViewersReadTheirFilmsInTwoOrders()
    {
      // The per-person file is only what the naming does when no single file was named, so this is the one
      // test that leaves MP2_SCREEN_VIEWS_FILE alone.
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", null);

      SkinSettingsBase.FileOwner = "Mother";
      await AShelf(Films()).PickSortAsync("year");
      Assert.That(File.Exists(Path.Combine(_ownDir, "screens-mother.json")), Is.True,
          "the mother's answers are her own file, the way her skin's are");

      SkinSettingsBase.FileOwner = "Father";
      Assert.That(AShelf(Films()).SortOrder, Is.EqualTo("title"),
          "the father never asked for an order, and his wife's is not his");

      SkinSettingsBase.FileOwner = "Mother";
      Assert.That(AShelf(Films()).SortOrder, Is.EqualTo("year"));
    }

    [Test]
    public void TheOldFileOfShapesIsReadAndWrittenTheNewWay()
    {
      string file = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE") ?? "";
      File.WriteAllText(file, "{\"layouts\": {\"MovieItem\": \"Cover\"}, \"media\": {\"MovieItem\": \"Banner\"},"
          + " \"details\": {\"MovieItem\": true}}");

      ScreenViewSettings read = ScreenViewSettings.Load(null, out string? problem);
      Assert.That(problem, Is.Null, "a file in the old shape is nobody's fault and says nothing about itself");
      Assert.That(read.TryAnswer("MovieItem", ScreenViewSettings.NO_FILTER,
          out ScreenViewSettings.ScreenAnswer answer), Is.True,
          "the shape a media kind was given is the shape of its screen with no filter in front of it");
      Assert.That(answer.Layout, Is.EqualTo("Cover"));
      Assert.That(answer.Media, Is.EqualTo("Banner"));
      Assert.That(answer.Details, Is.True);

      read.Save(null, out bool saved);
      Assert.That(saved, Is.True);
      string written = File.ReadAllText(file);
      Assert.That(written, Does.Contain("\"screens\""));
      Assert.That(written, Does.Not.Contain("layouts"),
          "the old shape is read once and never written again");
    }

    /// <summary>
    /// A library that stopped carrying what the memory says: the genre was retagged, the value left. The shelf
    /// is read whole without a word about it, because the sentence the read would otherwise say is about a
    /// library the viewer was not asking about.
    /// </summary>
    [AvaloniaTest]
    public async Task ACutTheLibraryNoLongerAnswersIsTakenOffWithoutAWord()
    {
      ScreenViewSettings store = ScreenViewSettings.Load(null, out _);
      store.RememberLanding("MovieItem", "genre", "Action");
      store.Save(null, out _);

      VanishedLibrary server = new VanishedLibrary();
      using (ServerApiClient client = new ServerApiClient(SERVER_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel films = AShelf(Films(), client);
        films.OpenCommand.Execute(null);
        WaitUntil("the whole shelf to arrive", delegate { return films.Titles.Count == 2; });

        Assert.That(films.GroupBy, Is.Null, "the cut is gone rather than the films");
        Assert.That(films.HasShelfFilter, Is.False);
        Assert.That(films.Status, Does.Not.Contain("This server has no"),
            "and nothing is said about a library the viewer was not asking about");
        Assert.That(films.Status, Does.Contain("2 of 2"));
        Assert.That(server.TitlesAsks, Is.EqualTo(2), "asked with the cut, refused by the answer, asked again");
      }

      Assert.That(ScreenViewSettings.Load(null, out _).LandingFor("MovieItem"), Is.Null,
          "a memory the library outgrew is not kept to be tripped over again");
    }

    [AvaloniaTest]
    public async Task AGroupingThatCarriesNothingIsTakenOffWithoutAWord()
    {
      ScreenViewSettings store = ScreenViewSettings.Load(null, out _);
      store.AnswerFor("MovieItem", ScreenViewSettings.NO_FILTER).Grouping = "genre";
      store.Save(null, out _);

      VanishedLibrary server = new VanishedLibrary();
      using (ServerApiClient client = new ServerApiClient(SERVER_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel films = AShelf(Films(), client);
        films.OpenCommand.Execute(null);
        WaitUntil("the whole shelf to arrive", delegate { return films.Titles.Count == 2; });

        Assert.That(films.IsGrouped, Is.False, "the plates that would have stood are none");
        Assert.That(films.GroupLabel, Is.EqualTo("Group: None"));
      }

      Assert.That(Written("MovieItem", ScreenViewSettings.NO_FILTER).Grouping, Is.Null);
    }

    [AvaloniaTest]
    public void AWordNoScreenOffersIsNoOrderAndNoGrouping()
    {
      // A memory from a build that offered more than this one does - an order the server would answer
      // silently by name, a category the counting refuses - is a memory to be left alone, not obeyed.
      ScreenViewSettings store = ScreenViewSettings.Load(null, out _);
      ScreenViewSettings.ScreenAnswer answers = store.AnswerFor("MovieItem", ScreenViewSettings.NO_FILTER);
      answers.Sort = "by_the_colour_of_the_poster";
      answers.Descending = true;
      answers.Grouping = "recording_time";
      store.Save(null, out _);

      MediaBrowserViewModel films = AShelf(Films());
      Assert.That(films.SortOrder, Is.EqualTo("title"), "a word the shelf cannot answer is not its order");
      Assert.That(films.SortDescending, Is.False, "and the end that went with it went with it");
      Assert.That(films.IsGrouped, Is.False);
    }

    /// <summary>
    /// A library that answers the cut it is asked with by nothing at all, and the counting of a category
    /// nobody carried any more: everything else is the shelf of two films it always was.
    /// </summary>
    private class VanishedLibrary : HttpMessageHandler
    {
      private const string SHELF = "{\"total\":2,\"offset\":0,\"limit\":120,\"returned\":2,\"sort\":\"title\","
          + "\"titles\":["
          + "{\"id\":\"00000001-0000-0000-0000-000000000000\",\"title\":\"Alien\",\"year\":1979},"
          + "{\"id\":\"00000002-0000-0000-0000-000000000000\",\"title\":\"Brand X\",\"year\":1973}]}";

      private const string NOTHING = "{\"total\":0,\"offset\":0,\"limit\":120,\"returned\":0,\"sort\":\"title\","
          + "\"by\":\"genre\",\"value\":\"Action\",\"titles\":[]}";

      private const string NO_GROUPS = "{\"aspect\":\"MovieItem\",\"by\":\"genre\",\"groups\":[]}";

      internal int TitlesAsks { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        string query = request.RequestUri.Query;

        if (path.EndsWith("/thumbnail", StringComparison.Ordinal)
            || path.EndsWith("/fanart", StringComparison.Ordinal)
            || path.Contains("/items/", StringComparison.Ordinal))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        string body;
        if (path.EndsWith("/groups", StringComparison.Ordinal))
        {
          body = NO_GROUPS;
        }
        else
        {
          TitlesAsks++;
          // The cut is answered with nothing, which is what a retagged genre looks like from here.
          body = query.Contains("by=", StringComparison.Ordinal) ? NOTHING : SHELF;
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
        if (condition())
          return;
      }
      Assert.Fail("Timed out after 15 seconds waiting for " + because);
    }
  }
}
