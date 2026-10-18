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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using ServerMediaAspect = MediaPortal.Common.MediaManagement.DefaultItemAspects.MediaAspect;
using FrontendAspect = MediaPortal.Frontend.Api.MediaAspect;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The library as a viewer opens it: tiles for the parts of the collection, a list of titles behind each one,
  /// and the step from a title to the player.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The server is the one the other front-end tests use - the real controllers, listening on a real port - and
  /// the media library behind it is a mock that answers the same query the real database would. That combination
  /// is the point: the titles on screen have travelled through HTTP, JSON and the view model, so a test here
  /// fails when any of those three disagrees with the others about what a title is.
  /// </para>
  /// <para>
  /// No test awaits, for the reason spelled out in <c>TestHomeWindow</c>: the runner pumps this thread, so an
  /// awaited continuation is a deadlock. Work is started, the queue is run until the screen says it is done, and
  /// the result is looked at afterwards.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMediaBrowser
  {
    #region Fixtures

    private static readonly Guid RESOURCE_SYSTEM_ID = new Guid("44444444-4444-4444-4444-444444444444");

    /// <summary>
    /// A path this machine can name, on Windows and in a container alike: the temp directory exists on both, and
    /// a drive-less "/media/..." does not. The tests about playing use it because "can be opened here" is the
    /// question they ask, and an answer that only holds on one operating system would be testing the test. The
    /// file itself does not have to exist - the player is the one that would notice.
    /// </summary>
    private static readonly string LOCAL_PATH = Path.Combine(Path.GetTempPath(), "MediaPortal2-Film.mkv");

    private readonly List<IDisposable> _servers = new List<IDisposable>();
    private readonly List<string> _leftovers = new List<string>();

    [TearDown]
    public void TearDown()
    {
      foreach (IDisposable server in _servers)
      {
        if (!Task.Run(delegate { server.Dispose(); }).Wait(TimeSpan.FromSeconds(10)))
          _leftovers.Add("a server started by a test did not close within 10 seconds");
      }
      _servers.Clear();

      InProcessApi.RemoveMediaLibrary();
      if (_leftovers.Count > 0)
        Assert.Fail(string.Join("; ", _leftovers.ToArray()));
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

    /// <summary>
    /// A media item with a title, a year and a resource - as close to what the importer leaves behind as a test
    /// needs, and no closer.
    /// </summary>
    private class Item : MediaItem
    {
      public Item(Guid id, string title, int year)
          : base(id, Aspects(id, title, year))
      {
      }

      private static IDictionary<Guid, IList<MediaItemAspect>> Aspects(Guid id, string title, int year)
      {
        SingleMediaItemAspect media = new SingleMediaItemAspect(ServerMediaAspect.Metadata);
        media.SetAttribute(ServerMediaAspect.ATTR_TITLE, title);
        media.SetAttribute(ServerMediaAspect.ATTR_SORT_TITLE, title);
        media.SetAttribute(ServerMediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 6, 1));
        return new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { ServerMediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
        };
      }

      public override IResourceLocator GetResourceLocator()
      {
        return null;
      }
    }

    /// <summary>
    /// An item whose primary resource is the string the test names, which is what the front end reads.
    /// </summary>
    private class ItemWithPath : Item
    {
      private readonly string _serialized;

      public ItemWithPath(Guid id, string title, int year, string serialized)
          : base(id, title, year)
      {
        _serialized = serialized;
      }

      public override IResourceLocator GetResourceLocator()
      {
        return new StubLocator(ResourcePath.Deserialize(_serialized));
      }
    }

    /// <summary>
    /// A locator that names a path and nothing else. Opening it is not what the front end is asked about in
    /// these tests - only whether the path it was handed is one this machine could open.
    /// </summary>
    private class StubLocator : IResourceLocator
    {
      public StubLocator(ResourcePath path)
      {
        NativeResourcePath = path;
      }

      public string NativeSystemId
      {
        get { return RESOURCE_SYSTEM_ID.ToString("B"); }
      }

      public ResourcePath NativeResourcePath { get; }

      public IResourceAccessor CreateAccessor()
      {
        throw new NotSupportedException("a test does not open files through the locator");
      }

      public bool TryCreateLocalFsAccessor(out ILocalFsResourceAccessor accessor)
      {
        accessor = null;
        return false;
      }
    }

    private static MediaItem Title(string name, int year, string serializedPath)
    {
      return new ItemWithPath(Guid.NewGuid(), name, year, serializedPath);
    }

    /// <summary>
    /// The library behind the in-process server: a movie aspect with the given titles, and the media aspect the
    /// titles themselves live in.
    /// </summary>
    /// <remarks>
    /// The titles it hands out and the number it counts depend on the filter it was asked with - see
    /// <see cref="Described"/>. That is not decoration: a screen that filters by watched state cannot be told
    /// apart from one that does not by a shelf that answers every question with the same four titles.
    /// </remarks>
    private static void RegisterLibrary(params MediaItem[] items)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(ShelfAspects());
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Returns((MediaItemQuery query, bool onlyOnline, Guid? profile, bool virtualItems) =>
              DescribedItems(items, query.Filter));
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false,
              It.IsAny<bool>()))
          .Returns((IEnumerable<Guid> types, IFilter filter, bool onlyOnline, bool virtualItems) =>
              DescribedItems(items, filter).Count);
      InProcessApi.RegisterMediaLibrary(library.Object);
    }

    /// <summary>
    /// The aspects of the shelf: the media aspect the titles live on, the movie aspect that makes them films,
    /// and an aspect nothing on the home screen offers a tile for.
    /// </summary>
    private static IDictionary<Guid, MediaItemAspectMetadata> ShelfAspects()
    {
      return new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { ServerMediaAspect.ASPECT_ID, ServerMediaAspect.Metadata },
          { MovieAspect.ASPECT_ID, MovieAspect.Metadata },
          { InProcessApi.VIDEO_ASPECT_ID, InProcessApi.CreateAspect(InProcessApi.VIDEO_ASPECT_ID, "VideoFile") }
      };
    }

    /// <summary>
    /// The titles of the shelf that a filter describes.
    /// </summary>
    private static IList<MediaItem> DescribedItems(MediaItem[] shelf, IFilter filter)
    {
      return shelf.Where(item => Described(filter, item)).ToList();
    }

    /// <summary>
    /// Whether one title is what a filter describes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handful of conditions the watched questions are made of, evaluated here instead of by a database.
    /// Anything else is refused out loud, because a shelf that answered an unknown condition with "yes" would
    /// silently approve whatever filter replaces it.
    /// </para>
    /// <para>
    /// A condition about a value that was never stored is <em>not</em> satisfied - <c>NULL = 0</c> is unknown in
    /// SQL and an unknown throws the row out, which is the very trap the endpoint's second filter half exists
    /// for. Evaluating it the same way here is what makes "the endpoint forgot the NULL half" fail a test instead
    /// of passing one.
    /// </para>
    /// </remarks>
    private static bool Described(IFilter filter, MediaItem item)
    {
      switch (filter)
      {
        case null:
          return true;
        case BooleanCombinationFilter combined:
          return combined.Operator == BooleanOperator.And
              ? combined.Operands.All(operand => Described(operand, item))
              : combined.Operands.Any(operand => Described(operand, item));
        case NotFilter negated:
          return !Described(negated.InnerFilter, item);
        case MediaItemIdFilter byId:
          // The shape of "the title with this id, if there is one", which is what the details endpoint asks. It
          // arrives at the same shelf as the watched questions, so the shelf answers it too.
          return byId.MediaItemIds.Contains(item.MediaItemId);
        case EmptyFilter empty:
          return Stored(item, empty.AttributeType) == null;
        case RelationalFilter condition:
          return Holds(condition, item);
        default:
          throw new InvalidOperationException("this shelf cannot answer a " + filter.GetType().Name
              + "; a test that asks a new question has to say what its answer looks like");
      }
    }

    private static bool Holds(RelationalFilter condition, MediaItem item)
    {
      object stored = Stored(item, condition.AttributeType);
      switch (condition.Operator)
      {
        case RelationalOperator.EQ:
          return Equals(stored, condition.FilterValue);
        case RelationalOperator.GT:
          return stored is int number && condition.FilterValue is int wanted && number > wanted;
        default:
          throw new InvalidOperationException("this shelf cannot answer a " + condition.Operator + " condition");
      }
    }

    /// <summary>
    /// What the title's media aspect holds for one attribute, or <c>null</c> when nothing was ever written there.
    /// </summary>
    private static object Stored(MediaItem item, MediaItemAspectMetadata.AttributeSpecification attribute)
    {
      if (!item.Aspects.TryGetValue(ServerMediaAspect.ASPECT_ID, out IList<MediaItemAspect> aspects))
        return null;
      return aspects.OfType<SingleMediaItemAspect>().FirstOrDefault()?.GetAttributeValue(attribute);
    }

    /// <summary>
    /// A title the library was told about: a row of the shelf that also carries what watching it left behind.
    /// </summary>
    /// <param name="playCount">
    /// How often it was finished, or <c>null</c> for a title nobody ever opened - the two are different to the
    /// library, one is written and the other is an empty column.
    /// </param>
    /// <param name="lastPlayed">
    /// When it was last opened, or <see cref="DateTime.MinValue"/> for "never".
    /// </param>
    private static MediaItem WatchedTitle(string name, int year, int? playCount, DateTime lastPlayed)
    {
      return new WatchedItem(Guid.NewGuid(), name, year,
          RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH, playCount, lastPlayed);
    }

    private class WatchedItem : ItemWithPath
    {
      internal WatchedItem(Guid id, string title, int year, string serialized, int? playCount, DateTime lastPlayed)
          : base(id, title, year, serialized)
      {
        SingleMediaItemAspect media =
            (SingleMediaItemAspect) Aspects[ServerMediaAspect.ASPECT_ID][0];
        if (playCount.HasValue)
          media.SetAttribute(ServerMediaAspect.ATTR_PLAYCOUNT, playCount.Value);
        if (lastPlayed != DateTime.MinValue)
          media.SetAttribute(ServerMediaAspect.ATTR_LASTPLAYED, lastPlayed);
      }
    }

    private InProcessApi StartServer()
    {
      InProcessApi server = InProcessApi.Start();
      _servers.Add(server);
      return server;
    }

    private static HomeSection MoviesSection = new HomeSection("Movies",
        new AspectViewModel(new FrontendAspect(MovieAspect.ASPECT_ID, MovieAspect.Metadata.Name, false, false,
            null, Array.Empty<AspectAttribute>())));

    #endregion

    #region The path bridge

    [AvaloniaTest]
    public void AFilePathFromTheServerBecomesAFileThisMachineCanOpen()
    {
      Assert.That(MediaLibraryPath.TryAsLocalFile(
              RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH, out string path, out string reason),
          Is.True, reason);
      Assert.That(Path.IsPathFullyQualified(path), Is.True);
      Assert.That(reason, Is.Null);
    }

    [AvaloniaTest]
    public void AResourceMadeOfSeveralProvidersIsNotAFileAndSaysWhy()
    {
      string stacked = RESOURCE_SYSTEM_ID.ToString("B") + "://archive.zip>"
          + RESOURCE_SYSTEM_ID.ToString("B") + "://inside.mkv";
      string reason;
      Assert.That(MediaLibraryPath.TryAsLocalFile(stacked, out _, out reason), Is.False);
      Assert.That(reason, Does.Contain("more than one resource"));
    }

    [AvaloniaTest]
    public void APathThatIsNotAWholePathIsRefusedRatherThanGuessedAt()
    {
      string reason;
      Assert.That(MediaLibraryPath.TryAsLocalFile(
          RESOURCE_SYSTEM_ID.ToString("B") + "://Films/Movie.mkv", out _, out reason), Is.False);
      Assert.That(reason, Does.Contain("not a path this machine can open"));

      Assert.That(MediaLibraryPath.TryAsLocalFile(null, out _, out reason), Is.False);
      Assert.That(reason, Does.Contain("did not name a file"));
    }

    #endregion

    #region The tiles on the home screen

    [AvaloniaTest]
    public void TheHomeScreenOffersATileForEveryPartOfTheLibraryTheServerHas()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      home.RefreshCommand.Execute(null);
      WaitUntil("the aspects to arrive", delegate { return home.Aspects.Count > 0 && home.Sections.Count > 0; });

      Assert.That(home.Sections.Select(section => section.Label), Is.EqualTo(new[] { "Movies" }),
          "the server has movies, a video file aspect and the media aspect; only the first is something a "
          + "viewer sits down to watch");
      Assert.That(home.Sections[0].CountText, Does.Contain("1"),
          "and the tile already knows how many there are");
    }

    [AvaloniaTest]
    public void AServerWithNothingTheViewerCanWatchGetsNoTilesRatherThanEmptyOnes()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { InProcessApi.VIDEO_ASPECT_ID, InProcessApi.CreateAspect(InProcessApi.VIDEO_ASPECT_ID, "VideoFile") }
          });
      InProcessApi.RegisterMediaLibrary(library.Object);
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      home.RefreshCommand.Execute(null);
      WaitUntil("the aspects to arrive", delegate { return home.Aspects.Count > 0; });

      Assert.That(home.Sections, Is.Empty);
    }

    [AvaloniaTest]
    public void PressingOKOnATileOpensTheTitlesOfThatSection()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      var opened = new List<Window>();
      HomeWindow window = new HomeWindow(home, null, null,
          delegate (HomeSection section)
          {
            var screen = new MediaBrowserWindow(new MediaBrowserViewModel(home.Client, section));
            opened.Add(screen);
            return screen;
          });
      window.Show();
      home.RefreshCommand.Execute(null);
      WaitUntil("the tiles to appear", delegate { return home.Sections.Count > 0; });

      ListBox tiles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "sectionsList");
      Assert.That(tiles.Items.Count, Is.EqualTo(1));

      tiles.Focus();
      tiles.SelectedItem = home.Sections[0];
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = tiles
      });
      Pump();

      Assert.That(opened, Has.Count.EqualTo(1), "the tile opened a screen");
      MediaBrowserWindow browser = (MediaBrowserWindow) opened[0];
      Assert.That(browser.DataContext, Is.InstanceOf<MediaBrowserViewModel>());
      Assert.That(((MediaBrowserViewModel) browser.DataContext).Heading, Is.EqualTo("Movies"));
      browser.Close();
      window.Close();
    }

    #endregion

    #region The list of titles

    [AvaloniaTest]
    public void TheTitlesArriveInOrderAndSayHowManyThereAre()
    {
      RegisterLibrary(
          Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH),
          Title("Nimmerland", 2024, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser = new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      browser.OpenCommand.Execute(null);
      WaitUntil("the titles to arrive", delegate { return browser.Titles.Count == 2; });

      Assert.That(browser.Titles.Select(title => title.Name), Is.EqualTo(new[] { "Alien Earth", "Nimmerland" }));
      Assert.That(browser.Titles[0].Line, Is.EqualTo("Alien Earth (2025)"));
      Assert.That(browser.Status, Does.Contain("of 2"), "and the screen says how much there is");
      Assert.That(browser.Selected, Is.SameAs(browser.Titles[0]),
          "a list with nothing selected is a list whose OK key does nothing");
      Assert.That(browser.CanLoadMore, Is.False);
    }

    [AvaloniaTest]
    public void ATitleThisMachineCannotOpenSaysSoOnItsRowAndRefusesToPlay()
    {
      MediaItem reachable = Title("Reachable", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH);
      MediaItem elsewhere = Title("Elsewhere", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://Films/Movie.mkv");
      RegisterLibrary(reachable, elsewhere);
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser = new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      var played = new List<TitleViewModel>();
      browser.PlayRequested += delegate (object? sender, TitleViewModel title) { played.Add(title); };
      browser.OpenCommand.Execute(null);
      WaitUntil("the titles to arrive", delegate { return browser.Titles.Count == 2; });

      TitleViewModel unplayable = browser.Titles.First(title => title.Name == "Elsewhere");
      Assert.That(unplayable.Playable, Is.False);
      Assert.That(unplayable.Detail, Is.EqualTo("not playable here"),
          "the row says it before the viewer presses anything");

      browser.Play(unplayable);
      Assert.That(played, Is.Empty);
      Assert.That(browser.Status, Does.Contain("cannot be watched from here"),
          "and pressing OK says it again instead of doing nothing");

      browser.Play(browser.Titles.First(title => title.Name == "Reachable"));
      Assert.That(played, Has.Count.EqualTo(1));
      Assert.That(Path.IsPathFullyQualified(played[0].LocalPath), Is.True);
    }

    [AvaloniaTest]
    public void AMachineThatIsToldToSignInHearsThatRatherThanAnEmptyList()
    {
      InProcessApi server = StartServer();
      ServerApiClient client = new ServerApiClient(server.BaseUrl, new HttpClient(new AlwaysForbidden()));

      MediaBrowserViewModel browser = new MediaBrowserViewModel(client, MoviesSection);
      browser.OpenCommand.Execute(null);
      WaitUntil("the answer to come back", delegate { return !browser.IsBusy; });

      Assert.That(browser.Titles, Is.Empty);
      Assert.That(browser.Status, Does.Contain("signed-in"),
          "a server that wants to know who is asking has to be told in those words, or the viewer blames the "
          + "front end for an empty screen");
    }

    [AvaloniaTest]
    public void ASectionTheServerHasNothingForIsSaidRatherThanLeftBlank()
    {
      RegisterLibrary();
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser = new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      browser.OpenCommand.Execute(null);
      WaitUntil("the answer to come back", delegate { return !browser.IsBusy; });

      Assert.That(browser.Titles, Is.Empty);
      Assert.That(browser.Status, Does.Contain("no movies"));
    }

    [AvaloniaTest]
    public void MoreAsksForWhatComesAfterWhatIsOnScreen()
    {
      MediaItem[] items = new MediaItem[7];
      for (int i = 0; i < items.Length; i++)
        items[i] = Title("Title " + i, 2020 + i, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH);
      RegisterLibrary(items);
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser = new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      browser.OpenCommand.Execute(null);
      WaitUntil("the titles to arrive", delegate { return browser.Titles.Count == 7; });

      Assert.That(browser.Titles.Count, Is.EqualTo(7), "the mock answers every page with everything it holds,");
      Assert.That(browser.CanLoadMore, Is.False,
          "so there is nothing left to ask for; the paging itself is the server endpoint's own test");
    }

    #region What one title says about itself

    /// <summary>
    /// A title as an importer leaves it after reading the sidecar file next to the film: a plot, a length, a
    /// certificate, and the record of the evening it was last watched. None of it fits in a row of a list, which
    /// is exactly why a screen that only lists titles has to be able to ask about one.
    /// </summary>
    private class DescribedItem : MediaItem
    {
      private readonly string _serialized;

      internal DescribedItem(Guid id, string title, string serialized)
          : base(id, Describe(title))
      {
        _serialized = serialized;
      }

      private static IDictionary<Guid, IList<MediaItemAspect>> Describe(string title)
      {
        SingleMediaItemAspect media = new SingleMediaItemAspect(ServerMediaAspect.Metadata);
        media.SetAttribute(ServerMediaAspect.ATTR_TITLE, title);
        media.SetAttribute(ServerMediaAspect.ATTR_SORT_TITLE, title);
        media.SetAttribute(ServerMediaAspect.ATTR_RECORDINGTIME, new DateTime(1997, 12, 19));
        media.SetAttribute(ServerMediaAspect.ATTR_COMMENT,
            "A treasure hunter follows a drawing from 1912 to a wreck, and to the woman he met on it.");
        media.SetAttribute(ServerMediaAspect.ATTR_RATING, 9);
        media.SetAttribute(ServerMediaAspect.ATTR_PLAYCOUNT, 2);
        media.SetAttribute(ServerMediaAspect.ATTR_LASTPLAYED, new DateTime(2025, 3, 2));

        SingleMediaItemAspect movie = new SingleMediaItemAspect(MovieAspect.Metadata);
        movie.SetAttribute(MovieAspect.ATTR_RUNTIME_M, 195);
        movie.SetAttribute(MovieAspect.ATTR_CERTIFICATION, "PG-13");
        movie.SetAttribute(MovieAspect.ATTR_TAGLINE, "Nothing on Earth could come between them.");

        return new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { ServerMediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
            { MovieAspect.ASPECT_ID, new List<MediaItemAspect> { movie } }
        };
      }

      public override IResourceLocator GetResourceLocator()
      {
        return new StubLocator(ResourcePath.Deserialize(_serialized));
      }
    }

    private const string TITANIC = "11111111-1111-1111-1111-111111111111";

    /// <summary>
    /// One film as the details endpoint answers it, with nothing said about where it is kept - which is the
    /// situation a title on another machine puts the viewer in.
    /// </summary>
    private static readonly string TITANIC_DETAILS =
        "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997,\"runtimeMinutes\":195,"
        + "\"overview\":\"A drawing from 1912 leads a treasure hunter to a wreck.\","
        + "\"certification\":\"PG-13\",\"rating\":9,\"playCount\":2,"
        + "\"lastPlayed\":\"2025-03-02T00:00:00\"}";

    /// <summary>
    /// A server that answers the details question with what the test names, and counts how often it was asked.
    /// Every other URL is answered with the one-row title list, so that a screen can be opened on it.
    /// </summary>
    private class DetailsServer : HttpMessageHandler
    {
      private readonly HttpStatusCode _code;
      private readonly string _details;

      internal DetailsServer(HttpStatusCode code, string details)
      {
        _code = code;
        _details = details;
      }

      internal int TimesAsked { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (request.RequestUri!.AbsolutePath.EndsWith("api/media/items/" + TITANIC))
        {
          TimesAsked++;
          return Task.FromResult(new HttpResponseMessage(_code)
          {
            RequestMessage = request,
            Content = new StringContent(_details, Encoding.UTF8, "application/json")
          });
        }

        return Task.FromResult(Answer(request, HttpStatusCode.OK,
            "{\"total\":1,\"offset\":0,\"limit\":60,\"returned\":1,\"sort\":\"title\",\"titles\":"
            + "[{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997}]}"));
      }

      private static HttpResponseMessage Answer(HttpRequestMessage request, HttpStatusCode code, string body)
      {
        return new HttpResponseMessage(code)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
      }
    }

    private static MediaBrowserViewModel BrowserOn(DetailsServer server)
    {
      return new MediaBrowserViewModel(new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);
    }

    [AvaloniaTest]
    public void TheInfoKeyShowsWhatTheServerKnowsAboutTheSelectedTitle()
    {
      RegisterLibrary(new DescribedItem(new Guid(TITANIC), "Titanic",
          RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser =
          new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      browser.OpenCommand.Execute(null);
      WaitUntil("the title to arrive", delegate { return browser.Titles.Count == 1; });

      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsHeading, Is.EqualTo("Titanic (1997)"));
      Assert.That(browser.DetailsLine, Does.Contain("195 min"));
      Assert.That(browser.DetailsLine, Does.Contain("PG-13"),
          "how old a viewer has to be is the first thing two people argue about over a film");
      Assert.That(browser.DetailsLine, Does.Contain("rated 9/10"));
      Assert.That(browser.DetailsLine, Does.Contain("watched 2"), "and whether they have seen it");
      Assert.That(browser.DetailsLine, Does.Contain("2025-03-02"));
      Assert.That(browser.DetailsText, Does.StartWith("A treasure hunter"),
          "the prose the server holds is what the panel is there for");
      Assert.That(browser.DetailsPath, Does.Contain("MediaPortal2-Film.mkv"),
          "and where the file is, because a viewer who cannot find it wants to know why");
    }

    [AvaloniaTest]
    public void ThePanelClosesAndReopensWithoutAskingTheServerAgain()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, TITANIC_DETAILS);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      browser.ToggleDetailsCommand.Execute(null);
      Pump();
      Assert.That(browser.DetailsVisible, Is.False, "the same key that opened the panel closes it");

      browser.ToggleDetailsCommand.Execute(null);
      Pump();
      Assert.That(browser.DetailsVisible, Is.True, "and opens it again on the spot");
      Assert.That(server.TimesAsked, Is.EqualTo(1),
          "a viewer who presses the key twice to look and again to think does not wait twice");
    }

    [AvaloniaTest]
    public void ATitleWatchedAfterThePanelClosedIsReadAgainRatherThanShownFromMemory()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, TITANIC_DETAILS);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      browser.CloseDetails();

      browser.ForgetDetails(Guid.Empty);
      browser.ToggleDetailsCommand.Execute(null);
      Pump();
      Assert.That(server.TimesAsked, Is.EqualTo(1),
          "a viewing of some other title leaves what this panel knows about this one alone");
      browser.CloseDetails();

      browser.ForgetDetails(new Guid(TITANIC));
      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the second read", delegate { return server.TimesAsked == 2; });

      Assert.That(browser.DetailsLine, Does.Contain("watched 2"),
          "the panel comes back with whatever the server says now, not with the counters from before the film");
    }

    [AvaloniaTest]
    public void ATitleTheServerNoLongerHasSaysSoRatherThanOpeningAnEmptyPanel()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.NotFound,
          "{\"id\":\"" + TITANIC + "\",\"error\":\"this server has no title with the id " + TITANIC + "\"}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the server's answer", delegate { return browser.Status.Contains("no title"); });

      Assert.That(browser.DetailsVisible, Is.False,
          "an empty panel would be a second, worse answer to the question than the status line");
      Assert.That(browser.Status, Does.Contain("no title with the id"),
          "the server's own words are the honest ones: the library was rebuilt under the list");
    }

    [AvaloniaTest]
    public void ADetailTheServerKeepsForSignedInUsersSaysHowToSignIn()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.Forbidden, "");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the server's answer",
          delegate { return browser.Status.IndexOf("sign in", StringComparison.OrdinalIgnoreCase) >= 0; });
      Assert.That(browser.DetailsVisible, Is.False);
      Assert.That(browser.Status, Does.Contain("sign in").IgnoreCase,
          "a 403 without a way out of it reads as a broken server");
    }

    [AvaloniaTest]
    public void AnEpisodeSaysWhichEpisodeItIs()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien Earth\",\"year\":2025,"
          + "\"seriesName\":\"Alien Earth\",\"season\":1,\"episode\":1,"
          + "\"episodeName\":\"Nimmerland\",\"runtimeMinutes\":52}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien Earth", null, 2025, null, null, false));

      browser.ToggleDetailsCommand.Execute(null);
      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsLine, Does.Contain("S01E01"),
          "the number a viewer is looking for in a series, in the shape every series app uses");
      Assert.That(browser.DetailsLine, Does.Contain("Nimmerland"));
      Assert.That(browser.DetailsText, Is.EqualTo("Nothing was written about this title on this server."),
          "a series episode imported without a summary has none, and the panel says that instead of lying");
    }

    [AvaloniaTest]
    public void TheInfoKeyOpensThePanelAndBackClosesThePanelBeforeTheWindow()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, TITANIC_DETAILS);
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserWindow window = new MediaBrowserWindow(browser);
      window.Show();
      WaitUntil("the row to arrive", delegate { return browser.Titles.Count == 1; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      Border panel = window.GetVisualDescendants().OfType<Border>()
          .First(border => border.Name == "detailsPanel");
      Assert.That(panel.IsVisible, Is.False, "nothing is on screen until it is asked for");

      titles.Focus();
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.I,
        Source = titles
      });
      WaitUntil("the panel to open", delegate { return panel.IsVisible; });
      Assert.That(browser.DetailsHeading, Is.EqualTo("Titanic (1997)"));
      Assert.That(titles.Items.Count, Is.EqualTo(1), "the list stayed where it was behind the panel");

      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      Pump();
      Assert.That(panel.IsVisible, Is.False);
      Assert.That(window.IsVisible, Is.True, "back put the panel away, not the screen");
      Assert.That(browser.Selected, Is.Not.Null, "and the viewer is still on the same row");

      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      Pump();
      Assert.That(window.IsVisible, Is.False, "the second back is the way out of the screen");
    }

    #endregion

    #region Asking who is allowed

    /// <summary>
    /// The server that knows its users: it hands a ticket to the one who names themselves and answers anyone
    /// else with 403. Only the two answers the client has to cope with are played here; the accounts themselves
    /// belong to the server and are not this front end's business.
    /// </summary>
    private static readonly Uri SIGN_IN_URL = new Uri("http://127.0.0.1:1/MediaPortal/Server/");

    private class SignInServer : HttpMessageHandler
    {
      private readonly string _ticket;
      private readonly bool _accepts;

      internal SignInServer(string ticket, bool accepts)
      {
        _ticket = ticket;
        _accepts = accepts;
      }

      internal string? TokenBody { get; private set; }
      internal string? TicketSeenByTheLibrary { get; private set; }
      internal string? SchemeSeenByTheLibrary { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("Token"))
        {
          TokenBody = await request.Content!.ReadAsStringAsync(cancellation).ConfigureAwait(false);
          if (!_accepts)
            return Answer(request, HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_grant\",\"error_description\":\"The user name or password is incorrect.\"}");

          return Answer(request, HttpStatusCode.OK,
              "{\"access_token\":\"" + _ticket + "\",\"token_type\":\"bearer\",\"expires_in\":3600}");
        }

        SchemeSeenByTheLibrary = request.Headers.Authorization?.Scheme;
        TicketSeenByTheLibrary = request.Headers.Authorization?.Parameter;
        return Answer(request, HttpStatusCode.OK,
            "{\"total\":1,\"offset\":0,\"limit\":60,\"returned\":1,\"sort\":\"title\",\"titles\":"
            + "[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"Alien Earth\",\"year\":2025}]}");
      }

      private static HttpResponseMessage Answer(HttpRequestMessage request, HttpStatusCode code, string body)
      {
        return new HttpResponseMessage(code) { RequestMessage = request, Content = new StringContent(body) };
      }
    }

    [AvaloniaTest]
    public void ASignedInClientCarriesItsTicketToTheLibrary()
    {
      SignInServer server = new SignInServer("ticket-1", true);
      ServerApiClient client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server));

      string signedInAs = Task.Run(delegate { return client.SignInAsync("georg", "georgs-password", CancellationToken.None); })
          .GetAwaiter().GetResult();

      Assert.That(signedInAs, Is.EqualTo("georg"));
      Assert.That(server.TokenBody, Does.Contain("grant_type=password"));
      Assert.That(server.TokenBody, Does.Contain("username=georg"));
      Assert.That(Uri.UnescapeDataString(server.TokenBody!),
          Does.Contain("password=" + Convert.ToBase64String(Encoding.UTF8.GetBytes("georgs-password"))),
          "MP2 base64s the password before sending it - the server hands what arrives to FromBase64String, so a "
          + "plain-text password is not rejected, it is a format error on the other side");
      Assert.That(client.AccessToken, Is.EqualTo("ticket-1"), "the client remembers the ticket");

      MediaTitles titles = Task.Run(delegate
      {
        return client.GetTitlesAsync("MediaItem", null, false, 60, 0, null, null, CancellationToken.None);
      })
          .GetAwaiter().GetResult();

      Assert.That(titles.Titles, Has.Count.EqualTo(1));
      Assert.That(server.SchemeSeenByTheLibrary, Is.EqualTo("Bearer"));
      Assert.That(server.TicketSeenByTheLibrary, Is.EqualTo("ticket-1"),
          "the library is only reached with the ticket, or the answer is 403 and the screen is empty");
    }

    [AvaloniaTest]
    public void ARefusedSignInIsSaidInTheServersWordsAndLeavesTheClientAStranger()
    {
      SignInServer server = new SignInServer("ticket-1", false);
      ServerApiClient client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server));

      ServerApiException? refused = null;
      try
      {
        Task.Run(delegate { return client.SignInAsync("georg", "wrong", CancellationToken.None); }).GetAwaiter()
            .GetResult();
      }
      catch (ServerApiException ex)
      {
        refused = ex;
      }

      Assert.That(refused, Is.Not.Null, "a refused sign-in is an answer, not a silent no");
      Assert.That(refused!.Message, Does.Contain("password is incorrect"),
          "the reason the server gave is what the screen has to show, otherwise the viewer guesses");
      Assert.That(refused.StatusCode, Is.EqualTo(400));
      Assert.That(client.AccessToken, Is.Null, "and no ticket was handed out");

      client.SignOut();
      Assert.That(client.AccessToken, Is.Null);
    }

    [AvaloniaTest]
    public void AServerThatCannotLookAnybodyUpIsExplainedRatherThanQuoted()
    {
      ServerApiException refused = new ServerApiException(SIGN_IN_URL, HttpStatusCode.BadRequest,
          "{\"error\":\"invalid_grant\",\"error_description\":\"User management not available.\"}");

      string said = HomeViewModel.DescribeSignInFailure(refused);

      Assert.That(said, Does.Contain("profile"), "the screen has to say what to do, not repeat the server");
      Assert.That(said, Does.Contain("Web authorization"),
          "and the way out of it is a setting on the server, so it belongs in the sentence");
      Assert.That(said, Does.Not.Contain("User management not available"),
          "that string is the confusing half of the problem - it is what a server with no accounts answers");
    }

    [AvaloniaTest]
    public void AWrongPasswordIsStillAnsweredInTheServersOwnWords()
    {
      ServerApiException refused = new ServerApiException(SIGN_IN_URL, HttpStatusCode.BadRequest,
          "{\"error\":\"invalid_grant\",\"error_description\":\"The user name or password is incorrect.\"}");

      Assert.That(HomeViewModel.DescribeSignInFailure(refused), Does.Contain("password is incorrect"),
          "a wrong password needs no interpretation, and inventing one would hide what the server said");
    }

    #endregion

    private class AlwaysForbidden : HttpMessageHandler
    {
      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
          RequestMessage = request,
          Content = new StringContent("")
        });
      }
    }

    #endregion

    #region Finding a title by name

    private static readonly string[] SHELF = { "Alien", "Alien Earth", "Army of Thieves", "Nimmerland" };

    /// <summary>
    /// A server that answers the title question the way a library does - with only the titles carrying the words it
    /// was given - and remembers the words it heard, so that a test can read the question back rather than guess it
    /// from the answer.
    /// </summary>
    private class SearchServer : HttpMessageHandler
    {
      internal string Question { get; private set; }

      internal string LastUrl { get; private set; }

      internal int TimesAsked { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        TimesAsked++;
        LastUrl = request.RequestUri.PathAndQuery;
        Question = QuestionIn(request.RequestUri.Query);

        List<string> matching = new List<string>();
        foreach (string name in SHELF)
        {
          if (Question == null || name.IndexOf(Question, StringComparison.OrdinalIgnoreCase) >= 0)
            matching.Add(name);
        }

        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(matching.Count)
            .Append(",\"offset\":0,\"limit\":60,\"returned\":").Append(matching.Count)
            .Append(",\"sort\":\"title\"");
        if (Question != null)
          body.Append(",\"search\":\"").Append(Question).Append("\"");

        body.Append(",\"titles\":[");
        for (int i = 0; i < matching.Count; i++)
        {
          if (i > 0)
            body.Append(',');
          body.Append("{\"id\":\"").Append(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, (byte) i }))
              .Append("\",\"title\":\"").Append(matching[i]).Append("\",\"year\":2000}");
        }
        body.Append("]}");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      /// <summary>The words in <c>search=...</c>, or <c>null</c> when the request asked for no words.</summary>
      private static string QuestionIn(string query)
      {
        if (string.IsNullOrEmpty(query))
          return null;

        foreach (string pair in query.TrimStart('?').Split('&'))
        {
          int equals = pair.IndexOf('=');
          if (equals > 0 && pair.Substring(0, equals) == "search")
            return Uri.UnescapeDataString(pair.Substring(equals + 1));
        }
        return null;
      }
    }

    private static MediaBrowserViewModel BrowserOn(SearchServer server)
    {
      return new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);
    }

    [Test]
    public void TheWordsTravelToTheServerAndWhatItFoundTravelsBack()
    {
      SearchServer server = new SearchServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaTitles answer = Task.Run(delegate
        {
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, "alien", null, CancellationToken.None);
        }).GetAwaiter().GetResult();

        Assert.That(server.Question, Is.EqualTo("alien"));
        Assert.That(answer.Titles.Select(title => title.Title), Is.EqualTo(new[] { "Alien", "Alien Earth" }),
            "the library decided what matches, not a filter written here");
        Assert.That(answer.Search, Is.EqualTo("alien"), "and the answer says which question it is the answer to");
      }
    }

    [Test]
    public void TypingAnAmpersandIsATitleRatherThanASyntaxError()
    {
      SearchServer server = new SearchServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        Task.Run(delegate
        {
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, "R&D 2 + 2", null, CancellationToken.None);
        }).GetAwaiter().GetResult();

        Assert.That(server.LastUrl, Does.Contain("search=R%26D%202%20%2B%202"),
            "escaped, because an unescaped ampersand would end the search where a title has a comma in it");
        Assert.That(server.Question, Is.EqualTo("R&D 2 + 2"), "and the same words on the other side");
      }
    }

    [AvaloniaTest]
    public void TypingANameAndPressingEnterLeavesOnlyTheMatchingTitles()
    {
      SearchServer server = new SearchServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SHELF.Length; });
      Assert.That(browser.HasSearch, Is.False, "a list that was not searched does not offer to undo a search");

      browser.SearchText = "alien";
      browser.FindCommand.Execute(null);
      WaitUntil("the matches to arrive", delegate { return browser.Titles.Count == 2; });

      Assert.That(browser.Titles.Select(title => title.Name), Is.EqualTo(new[] { "Alien", "Alien Earth" }));
      Assert.That(browser.HasSearch, Is.True, "the Clear button appeared, because the list is no longer the shelf");
      Assert.That(browser.Status, Is.EqualTo("2 titles in movies match 'alien'"),
          "the status line says what was asked and what came of it, in that order");
    }

    [AvaloniaTest]
    public void ClearingTheBoxBringsTheWholeShelfBack()
    {
      SearchServer server = new SearchServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      browser.SearchText = "nimmerland";
      browser.FindCommand.Execute(null);
      WaitUntil("the one match", delegate { return browser.Titles.Count == 1; });

      browser.ClearSearchCommand.Execute(null);
      WaitUntil("the shelf back", delegate { return browser.Titles.Count == SHELF.Length; });

      Assert.That(server.Question, Is.Null, "clearing is not a search for the empty string");
      Assert.That(browser.HasSearch, Is.False);
      Assert.That(browser.SearchText, Is.Empty, "the box is empty too, or the next Find starts from a leftover");
    }

    [AvaloniaTest]
    public void AnEmptiedBoxFindsWhenEnteredAsAQuestionOfNothing()
    {
      SearchServer server = new SearchServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      browser.SearchText = "  ";
      browser.FindCommand.Execute(null);
      Pump();

      Assert.That(server.Question, Is.Null,
          "somebody who deleted the words and pressed Enter means the whole shelf, not the titles called '  '");
      Assert.That(browser.Titles, Has.Count.EqualTo(SHELF.Length));
    }

    [AvaloniaTest]
    public void TheNextPageIsAskedForWithTheSameWords()
    {
      SearchServer server = new SearchServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      browser.SearchText = "a";
      browser.FindCommand.Execute(null);
      WaitUntil("the matches to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      browser.LoadMoreCommand.Execute(null);
      WaitUntil("the second page", delegate { return server.TimesAsked == 3; });

      Assert.That(server.LastUrl, Does.Contain("search=a"),
          "More walks through the matches; a second page of the whole shelf would be a different list");
      Assert.That(server.LastUrl, Does.Contain("offset="));
    }

    [AvaloniaTest]
    public void TheFindKeyHandsTheKeyboardToTheBoxAndBackGivesTheShelfBack()
    {
      SearchServer server = new SearchServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserWindow window = new MediaBrowserWindow(browser);
      window.Show();
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      TextBox box = window.GetVisualDescendants().OfType<TextBox>()
          .First(field => field.Name == "searchBox");

      titles.Focus();
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.F,
        Source = titles
      });
      Pump();
      Assert.That(box.IsFocused, Is.True, "one key is all it takes to start typing a title");

      box.Text = "alien";
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = box
      });
      WaitUntil("the matches to arrive", delegate { return browser.Titles.Count == 2; });
      Assert.That(titles.IsFocused, Is.True,
          "Enter searched, so the arrows belong to the results again - not to the box they came from");

      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      WaitUntil("the shelf back", delegate { return browser.Titles.Count == SHELF.Length; });
      Assert.That(window.IsVisible, Is.True,
          "the first back undid the search rather than leaving the screen; the search was the newest thing");
    }

    #endregion

    #region What the list knows about what was watched

    /// <summary>
    /// One row of the shelf this region runs on, with the two figures the library keeps about watching it.
    /// </summary>
    private class ShelfTitle
    {
      internal ShelfTitle(string name, int playCount, string lastPlayed)
          : this(name, playCount, lastPlayed, null)
      {
      }

      /// <param name="resume">
      /// How far this viewer got, as the server reports it, or <c>null> for a title with no position on it - the
      /// answer a request from nobody gets for every title.
      /// </param>
      internal ShelfTitle(string name, int playCount, string lastPlayed, int? resume)
      {
        Name = name;
        PlayCount = playCount;
        LastPlayed = lastPlayed;
        Resume = resume;
      }

      internal string Name { get; }

      internal int PlayCount { get; }

      /// <summary>ISO date as JSON carries it, or <c>null</c> for a title the library was never told about.</summary>
      internal string LastPlayed { get; }

      internal int? Resume { get; }
    }

    private static readonly ShelfTitle[] SEEN_SHELF =
    {
        new ShelfTitle("Alien", 2, "1979-05-25T20:15:00"),
        new ShelfTitle("Alien Earth", 0, null),
        new ShelfTitle("Army of Thieves", 0, "2024-03-03T18:00:00", 42),
        new ShelfTitle("Nimmerland", 1, "2025-01-12T21:30:00")
    };

    /// <summary>
    /// A server that answers the watched filter the way the library does - by leaving out the titles the condition
    /// does not describe - and remembers the word it was asked with.
    /// </summary>
    private class SeenServer : HttpMessageHandler
    {
      internal string Filter { get; private set; }

      internal string LastUrl { get; private set; }

      internal int TimesAsked { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        TimesAsked++;
        LastUrl = request.RequestUri.PathAndQuery;
        Filter = ParameterIn(request.RequestUri.Query, "seen");

        List<ShelfTitle> matching = new List<ShelfTitle>();
        foreach (ShelfTitle title in SEEN_SHELF)
        {
          if (Filter == "watched" && title.PlayCount <= 0)
            continue;
          if (Filter == "unwatched" && title.PlayCount > 0)
            continue;
          matching.Add(title);
        }

        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(matching.Count)
            .Append(",\"offset\":0,\"limit\":60,\"returned\":").Append(matching.Count)
            .Append(",\"sort\":\"title\"");
        if (Filter != null)
          body.Append(",\"seen\":\"").Append(Filter).Append("\"");

        body.Append(",\"titles\":[");
        for (int i = 0; i < matching.Count; i++)
        {
          if (i > 0)
            body.Append(',');
          body.Append("{\"id\":\"").Append(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, (byte) i }))
              .Append("\",\"title\":\"").Append(matching[i].Name).Append("\"")
              .Append(",\"year\":2000")
              .Append(",\"playCount\":").Append(matching[i].PlayCount);
          if (matching[i].LastPlayed != null)
            body.Append(",\"lastPlayed\":\"").Append(matching[i].LastPlayed).Append("\"");
          if (matching[i].Resume != null)
            body.Append(",\"resume\":").Append(matching[i].Resume.Value);
          body.Append('}');
        }
        body.Append("]}");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      /// <summary>The value of one query parameter, or <c>null</c> when the request did not carry it.</summary>
      private static string ParameterIn(string query, string name)
      {
        if (string.IsNullOrEmpty(query))
          return null;

        foreach (string pair in query.TrimStart('?').Split('&'))
        {
          int equals = pair.IndexOf('=');
          if (equals > 0 && pair.Substring(0, equals) == name)
            return Uri.UnescapeDataString(pair.Substring(equals + 1));
        }
        return null;
      }
    }

    private static MediaBrowserViewModel BrowserOn(SeenServer server)
    {
      return new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);
    }

    [Test]
    public void TheWatchedFiguresArriveWithTheTitle()
    {
      SeenServer server = new SeenServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaTitles answer = Task.Run(delegate
        {
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, null, "watched", CancellationToken.None);
        }).GetAwaiter().GetResult();

        Assert.That(server.Filter, Is.EqualTo("watched"), "the word travels as the server's own two words");
        Assert.That(answer.Seen, Is.EqualTo("watched"), "and comes back attached to the answer");

        MediaTitle alien = answer.Titles.Single(title => title.Title == "Alien");
        Assert.That(alien.PlayCount, Is.EqualTo(2));
        Assert.That(alien.LastPlayed, Is.EqualTo(new DateTime(1979, 5, 25, 20, 15, 0)));
      }
    }

    [AvaloniaTest]
    public void ARowIsMarkedSeenOnlyWhenTheLibrarySaysItWasFinished()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      TitleViewModel twice = browser.Titles.Single(title => title.Name == "Alien");
      Assert.That(twice.Seen, Is.True);
      Assert.That(twice.SeenMark, Is.EqualTo("\u2713"), "a title finished twice is still one tick, not two");

      TitleViewModel started = browser.Titles.Single(title => title.Name == "Army of Thieves");
      Assert.That(started.Seen, Is.False,
          "a date in the library without a finished viewing behind it is somebody who stopped halfway, and a tick "
          + "on that row would be a lie about a title they still have to watch");
      Assert.That(started.SeenMark, Is.Empty);

      TitleViewModel never = browser.Titles.Single(title => title.Name == "Alien Earth");
      Assert.That(never.Seen, Is.False, "and a title the library never heard of plays the same way");
    }

    // ---- how far this person got ---------------------------------------------------------------------

    [Test]
    public void HowFarOnePersonGotArrivesWithTheTitle()
    {
      SeenServer server = new SeenServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaTitles answer = Task.Run(delegate
        {
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, null, null, CancellationToken.None);
        }).GetAwaiter().GetResult();

        MediaTitle stopped = answer.Titles.Single(title => title.Title == "Army of Thieves");
        Assert.That(stopped.Resume, Is.EqualTo(42),
            "the figure this person left behind is carried under the server's own word for it");

        MediaTitle finished = answer.Titles.Single(title => title.Title == "Alien");
        Assert.That(finished.Resume, Is.Null,
            "a title with no position on it answers with no figure, which is not the same figure as zero");
      }
    }

    [AvaloniaTest]
    public void ARowSaysHowFarItsViewerGotAndSaysNothingWhenTheyLeftNoPosition()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      TitleViewModel stopped = browser.Titles.Single(title => title.Name == "Army of Thieves");
      Assert.That(stopped.ResumePercent, Is.EqualTo(42));
      Assert.That(stopped.ResumeText, Is.EqualTo("42% watched"));

      TitleViewModel finished = browser.Titles.Single(title => title.Name == "Alien");
      Assert.That(finished.ResumePercent, Is.Null);
      Assert.That(finished.ResumeText, Is.Empty,
          "a row with nothing stored shows an empty space rather than a made-up figure or a bare percent sign");
    }

    [AvaloniaTest]
    public void OneKeyPressAsksTheServerForWhatIsLeftToWatch()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      browser.CycleSeenCommand.Execute(null);
      WaitUntil("the unwatched titles", delegate { return browser.Titles.Count == 2; });

      Assert.That(server.Filter, Is.EqualTo("unwatched"),
          "the shelf is filtered where it is stored, because the two titles left are on pages nobody has read");
      Assert.That(browser.Titles.Select(title => title.Name),
          Is.EqualTo(new[] { "Alien Earth", "Army of Thieves" }));
      Assert.That(browser.Status, Is.EqualTo("2 of 2 unwatched movies, sorted by title"),
          "and the status line admits it filtered, so a short list is never mistaken for a small library");
      Assert.That(browser.SeenLabel, Is.EqualTo("Unwatched only"));
    }

    [AvaloniaTest]
    public void TheFilterWalksRoundBackToEverything()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });
      Assert.That(browser.SeenLabel, Is.EqualTo("All titles"));

      browser.CycleSeenCommand.Execute(null);
      WaitUntil("the unwatched titles", delegate { return browser.Titles.Count == 2; });

      browser.CycleSeenCommand.Execute(null);
      WaitUntil("the watched titles", delegate { return browser.Titles.Count == 2 && server.Filter == "watched"; });
      Assert.That(browser.Titles.Select(title => title.Name), Is.EqualTo(new[] { "Alien", "Nimmerland" }));
      Assert.That(browser.SeenLabel, Is.EqualTo("Watched only"));

      browser.CycleSeenCommand.Execute(null);
      WaitUntil("the whole shelf back", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });
      Assert.That(server.Filter, Is.Null, "the third press asks the question the screen started with");
      Assert.That(browser.SeenLabel, Is.EqualTo("All titles"));
      Assert.That(browser.HasSeenFilter, Is.False);
    }

    [AvaloniaTest]
    public void TheWatchedFilterTravelsToTheNextPageToo()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      browser.CycleSeenCommand.Execute(null);
      WaitUntil("the unwatched titles", delegate { return browser.Titles.Count == 2; });

      browser.LoadMoreCommand.Execute(null);
      WaitUntil("the second page", delegate { return server.TimesAsked == 3; });

      Assert.That(server.LastUrl, Does.Contain("seen=unwatched"),
          "More walks through the filtered shelf; an unfiltered second page would append titles the viewer "
          + "had asked not to see");
    }

    [AvaloniaTest]
    public void TheWatchedKeyFiltersTheScreenAndBackTakesItOffAgain()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserWindow window = new MediaBrowserWindow(browser);
      window.Show();
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      Button seen = window.GetVisualDescendants().OfType<Button>()
          .First(button => button.Name == "seenButton");
      Assert.That(seen.Content, Is.EqualTo("All titles"));

      titles.Focus();
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.W,
        Source = titles
      });
      WaitUntil("the filtered list", delegate { return browser.Titles.Count == 2; });
      Assert.That(seen.Content, Is.EqualTo("Unwatched only"),
          "the button on the screen says the state the list is in, so the filter is visible without asking");

      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      WaitUntil("the shelf back", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });
      Assert.That(window.IsVisible, Is.True,
          "back undid the filter rather than leaving the screen, because the filter was the newest thing on it");
    }

    #endregion

    #region What the home screen offers from what was left half way

    /// <summary>
    /// The four titles the strip tests run on. Every question the screen can ask has a different answer here, so
    /// an unfiltered request cannot pass for a filtered one: the whole shelf is four, "watched" is one,
    /// "unwatched" is three and "started" is two.
    /// </summary>
    private static MediaItem[] HalfWayShelf()
    {
      return new[]
      {
          WatchedTitle("Alien", 1979, 2, new DateTime(2024, 5, 1, 20, 0, 0)),
          WatchedTitle("Nimmerland", 2025, null, DateTime.MinValue),
          WatchedTitle("Alien Earth", 2025, 0, new DateTime(2025, 4, 27, 21, 30, 0)),
          WatchedTitle("Army of Thieves", 2018, 0, new DateTime(2024, 3, 3, 18, 0, 0))
      };
    }

    private static HomeViewModel HomeOn(InProcessApi server)
    {
      return new HomeViewModel(new ServerApiClient(server.BaseUrl));
    }

    private static void Refreshed(HomeViewModel home)
    {
      home.RefreshCommand.Execute(null);
      WaitUntil("the read to finish",
          delegate { return home.RefreshCommand.ExecutionTask != null && home.RefreshCommand.ExecutionTask.IsCompleted; });
      Pump();
    }

    [AvaloniaTest]
    public void TheHomeScreenListsWhatWasLeftHalfWay()
    {
      RegisterLibrary(HalfWayShelf());
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      Assert.That(home.Unfinished.Select(row => row.Name),
          Is.EqualTo(new[] { "Alien Earth", "Army of Thieves" }),
          "the two titles that were opened and not finished, the one stopped most recently first: not the film "
          + "that was watched through (its last-played date is what a finished viewing leaves behind) and not the "
          + "one nobody ever opened. Four titles are on the shelf, so this answer is only possible if the screen "
          + "asked for \"started\" - every other question has a different answer here");
      Assert.That(home.UnfinishedStatus, Does.Contain("2 titles left half way"));
      Assert.That(home.HasUnfinished, Is.True);
      Assert.That(home.Unfinished[0].LastPlayedText, Is.EqualTo("2025-04-27 21:30"),
          "the row says when it was stopped, in the same words whatever the machine's regional settings are");
      Assert.That(home.Unfinished[0].Playable, Is.True,
          "and it is a row the viewer can actually start, not a name on a screen");
    }

    [AvaloniaTest]
    public void AHomeScreenWithNothingLeftHalfWayShowsNoStrip()
    {
      RegisterLibrary(WatchedTitle("Alien", 1979, 2, new DateTime(2024, 5, 1, 20, 0, 0)),
          WatchedTitle("Nimmerland", 2025, null, DateTime.MinValue));
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      Assert.That(home.Aspects, Is.Not.Empty, "the screen did read the server");
      Assert.That(home.Unfinished, Is.Empty);
      Assert.That(home.HasUnfinished, Is.False,
          "a strip holding nothing takes itself off the screen rather than asking the viewer to wonder about it");
      Assert.That(home.UnfinishedStatus, Is.Empty);
    }

    [AvaloniaTest]
    public void APartOfTheLibraryThatWillNotSayLeavesItsReasonOnTheStrip()
    {
      // The titles request refused while the aspect list and the counts answered. Silently hiding the strip here
      // would make "the server wouldn't tell" look exactly like "you have nothing to watch".
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(ShelfAspects());
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false,
              It.IsAny<bool>()))
          .Returns(4);
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Throws(new InvalidOperationException("this shelf will not hand out titles"));
      InProcessApi.RegisterMediaLibrary(library.Object);
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      Assert.That(home.Sections, Is.Not.Empty, "the tiles arrived, so this is not a screen that failed");
      Assert.That(home.Unfinished, Is.Empty);
      Assert.That(home.HasUnfinished, Is.True, "and the strip stays to say what went wrong");
      Assert.That(home.UnfinishedStatus, Does.Contain("Could not check what you left half way"));
      Assert.That(home.UnfinishedStatus, Does.Contain("did not answer"));
    }

    [AvaloniaTest]
    public void PressingARowOfTheStripIsPlayedLikeATitleInTheLibrary()
    {
      RegisterLibrary(HalfWayShelf());
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      var played = new List<TitleViewModel>();
      home.ResumeRequested += delegate (object? sender, TitleViewModel row) { played.Add(row); };
      Refreshed(home);

      Assert.That(home.Unfinished, Has.Count.EqualTo(2));
      home.ResumeCommand.Execute(home.Unfinished[0]);

      Assert.That(played, Has.Count.EqualTo(1),
          "the home screen does not own a player; it says which title, and the application opens it through the "
          + "one playback path the library already uses");
      Assert.That(played[0].Name, Is.EqualTo("Alien Earth"));
      Assert.That(played[0].Title.Id, Is.EqualTo(home.Unfinished[0].Title.Id),
          "the same title, so the viewing is reported against the same library entry the strip read from");
    }

    [AvaloniaTest]
    public void TheStripIsOnTheKeyboardRingAndStartsTheFilmFromThere()
    {
      RegisterLibrary(HalfWayShelf());
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Window playback = new Window();
      var opened = new List<Window>();
      var window = new HomeWindow(home, delegate
      {
        opened.Add(playback);
        return playback;
      });
      window.Show();
      Refreshed(home);
      Pump();

      ListBox strip = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "unfinishedList");
      Assert.That(strip.Items.Count, Is.EqualTo(2));

      strip.Focus();
      strip.SelectedIndex = 0;
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = strip
      });
      Pump();

      Assert.That(opened, Is.EqualTo(new[] { playback }),
          "OK on a row of the strip gets out of the way and shows the playback");
    }

    #endregion
  }
}
