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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;
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
      // Windows a test finished on but never closed: a stray window holds the keyboard globally, the cross-
      // window ceasefire in ShellWindow correctly refuses to fight it back, and the next test times out on
      // keys nobody receives. Every shell window enrolls itself in ShellWindow.StandingWindows; whatever
      // still stands when a test ends is swept here, before the servers go.
      //
      // Only from the thread the windows belong to.  A plain [Test] - a view model asked a question, no screen
      // stood up - is run off the UI thread, and asking a window whether it is visible from such a thread is
      // answered with "the calling thread cannot access this object" rather than with the sweep.  That fault
      // then lands on a test that never touched a window at all, over a window some OTHER test left standing,
      // and the real failure is buried under it.  A thread that owns no window has none to sweep either; the
      // hand-off below is for the ones it cannot reach, and it is the UI thread that closes them.
      if (Dispatcher.UIThread.CheckAccess())
      {
        foreach (ShellWindow stray in ShellWindow.StandingWindows.ToArray())
        {
          if (stray.IsVisible)
            stray.Close();
        }
        Pump();
      }
      else if (ShellWindow.StandingWindows.Count > 0)
      {
        Dispatcher.UIThread.Post(delegate
        {
          foreach (ShellWindow stray in ShellWindow.StandingWindows.ToArray())
          {
            if (stray.IsVisible)
              stray.Close();
          }
        });
      }

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
    /// titles themselves live on. Every query the server passes through on its way to this shelf is recorded in
    /// the returned list, because "what did the screen actually ask for" is sometimes the only interesting answer.
    /// </summary>
    /// <remarks>
    /// The titles it hands out and the number it counts depend on the filter it was asked with - see
    /// <see cref="Described"/>. That is not decoration: a screen that filters by watched state cannot be told
    /// apart from one that does not by a shelf that answers every question with the same four titles.
    /// </remarks>
    private static List<MediaItemQuery> RegisterLibrary(params MediaItem[] items)
    {
      var asked = new List<MediaItemQuery>();
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(ShelfAspects());
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Returns((MediaItemQuery query, bool onlyOnline, Guid? profile, bool virtualItems) =>
          {
            asked.Add(query);
            return DescribedItems(items, query.Filter);
          });
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false,
              It.IsAny<bool>()))
          .Returns((IEnumerable<Guid> types, IFilter filter, bool onlyOnline, bool virtualItems) =>
              DescribedItems(items, filter).Count);
      InProcessApi.RegisterMediaLibrary(library.Object);
      return asked;
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

    /// <summary>
    /// A film standing on a share of another machine is the one path a player will not take with the slashes the
    /// library stores. Measured with VLC 3.0.20: given <c>//HTPC-SERVER/Video/Film.mkv</c> as a <i>path</i> it
    /// builds <c>file:///HTPC-SERVER/Video/Film.mkv</c> - a folder called HTPC-SERVER at the root of the current
    /// drive - and answers with "No such file or directory", which on the television is a black screen and the
    /// word "stopped". Windows calls a share <c>\\server\share</c>, so that is what the player is handed.
    /// </summary>
    [AvaloniaTest]
    public void AShareOfAnotherMachineIsSpelledTheWayThisMachineCallsAShare()
    {
      string share = RESOURCE_SYSTEM_ID.ToString("B") + "://///HTPC-SERVER/Video/Movies/Film.mkv";
      Assert.That(MediaLibraryPath.TryAsLocalFile(share, out string path, out string reason), Is.True, reason);
      Assert.That(Path.IsPathFullyQualified(path), Is.True);
      if (Path.DirectorySeparatorChar != '/')
      {
        Assert.That(path, Is.EqualTo(@"\\HTPC-SERVER\Video\Movies\Film.mkv"),
            "a share kept its slashes, so the player looks for it on this machine's own drive");
      }
      else
      {
        Assert.That(path, Is.EqualTo("//HTPC-SERVER/Video/Movies/Film.mkv"),
            "on a machine whose separators are slashes there is nothing to translate");
      }
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
    public void AServerWithNothingTheViewerCanWatchOffersNothingToOpenRatherThanEmptyTiles()
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

      // MP2's menu is still on the screen - it is the same menu whether or not this server can fill it - but
      // none of it opens, because there is no aspect on the server that a viewer can watch titles from.
      Assert.That(home.Sections, Is.Empty);
      Assert.That(home.Menu, Is.Not.Empty);
      Assert.That(home.Open(EntryNamed(home.Menu, "Series")), Is.Null,
          "a tile that would open onto nothing is refused, and the status line says so");
      Assert.That(home.Status, Does.Contain("not on this server"));
    }

    [AvaloniaTest]
    public void PressingOKOnATileOpensTheTitlesOfThatSection()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      var opened = new List<MediaBrowserViewModel>();
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "home")
        {
          return new HomeView(home, workflow, null, null,
              delegate (HomeSection section)
              {
                var model = new MediaBrowserViewModel(home.Client, section);
                opened.Add(model);
                return new WorkflowFrame("titles", section.Label, model);
              });
        }
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      workflow.Push(new WorkflowFrame("home", "Home", home));
      window.Show();
      home.RefreshCommand.Execute(null);
      WaitUntil("the tiles to appear", delegate { return home.Sections.Count > 0; });

      ListBox tiles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "menuList");
      Assert.That(tiles.Items.Count, Is.EqualTo(home.Menu.Count),
          "the whole menu is on the screen, not only the part of it this server can fill");

      tiles.Focus();
      tiles.SelectedItem = EntryNamed(home.Menu, "Movies");
      tiles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = tiles
      });
      Pump();

      Assert.That(opened, Has.Count.EqualTo(1), "the tile opened a screen");
      MediaBrowserViewModel browser = opened[0];
      Assert.That(browser.Heading, Is.EqualTo("Movies"));
      window.Close();
    }

    /// <summary>
    /// The way out of a shelf. A viewer who opens a library and presses back expects to be standing in front of
    /// the library again, however often they pressed - the screen's own layers first (the details, the watched
    /// filter, a search, the screen's menu) and then the screen itself. The key has to travel through the shelf,
    /// through the screen and into the workflow behind it, so this is proven over a real window with a real
    /// workflow under it and not against a screen held on its own.
    /// </summary>
    [AvaloniaTest]
    public void BackOutOfAShelfWalksOutOfTheScreenWhenItsOwnLayersAreAway()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      var opened = new List<MediaBrowserViewModel>();
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "home")
        {
          return new HomeView(home, workflow, null, null,
              delegate (HomeSection section)
              {
                var model = new MediaBrowserViewModel(home.Client, section);
                opened.Add(model);
                return new WorkflowFrame("titles", section.Label, model);
              });
        }
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      workflow.Push(new WorkflowFrame("home", "Home", home));
      window.Show();
      home.RefreshCommand.Execute(null);
      WaitUntil("the tiles to appear", delegate { return home.Sections.Count > 0; });

      ListBox tiles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "menuList");
      tiles.Focus();
      tiles.SelectedItem = EntryNamed(home.Menu, "Movies");
      tiles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = tiles
      });
      Pump();
      Assert.That(opened, Has.Count.EqualTo(1), "the tile opened the shelf");

      ListBox shelf = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      shelf.Focus();

      // Back is answered by whatever stands nearest: the screen undoes its own layers first, so a viewer pressing
      // it a few times is undoing a few layers. Four presses is one more than this screen has layers to undo, so
      // after them the key cannot still be the screen's business.
      for (int presses = 0; presses < 4 && window.Screen is MediaBrowserView; presses++)
      {
        shelf.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Escape,
          Source = shelf
        });
        Pump();
      }

      Assert.That(window.Screen, Is.Not.InstanceOf<MediaBrowserView>(),
          "the back key walked out of the shelf and showed the home screen again");
      window.Close();
    }

    /// <summary>
    /// Back out of every layer the screen can be wearing at once. A viewer who has the details up, a watched
    /// filter on, a search going and the screen's menu open presses back and expects the newest thing to go, then
    /// the next, and eventually to be standing outside the screen again. If one of those layers ever answers the
    /// key without putting itself away, the viewer is shut in: the key never reaches the workflow behind the
    /// screen, and no number of presses helps. So the screen is dressed in everything it can wear at once and
    /// then pressed back until it lets go.
    /// </summary>
    [AvaloniaTest]
    public void BackLetsTheScreenGoEvenWhenEveryLayerIsUpAtOnce()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      var opened = new List<MediaBrowserViewModel>();
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "home")
        {
          return new HomeView(home, workflow, null, null,
              delegate (HomeSection section)
              {
                var model = new MediaBrowserViewModel(home.Client, section);
                opened.Add(model);
                return new WorkflowFrame("titles", section.Label, model);
              });
        }
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      workflow.Push(new WorkflowFrame("home", "Home", home));
      window.Show();
      home.RefreshCommand.Execute(null);
      WaitUntil("the tiles to appear", delegate { return home.Sections.Count > 0; });

      ListBox tiles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "menuList");
      tiles.Focus();
      tiles.SelectedItem = EntryNamed(home.Menu, "Movies");
      tiles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = tiles
      });
      Pump();
      MediaBrowserViewModel browser = opened[0];
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 0; });

      ListBox shelf = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      shelf.Focus();

      // Everything the screen can wear, worn at once, the way a viewer ends up wearing it: the details, the
      // watched filter, a search, the screen's own menu and the title's menu.
      Press(shelf, MediaPortal.Frontend.Settings.KeyAction.Details);
      Press(shelf, MediaPortal.Frontend.Settings.KeyAction.Watched);
      browser.OpenSearch();
      browser.FindCommand.Execute(null);
      // The info key is the title's dialog now and the rail comes out on its own command; the test wants both
      // worn at once, so it opens the dialog first and the rail over it - the rail hides neither.
      Press(shelf, MediaPortal.Frontend.Settings.KeyAction.MediaMenu);
      WaitUntil("the title's dialog to stand open", delegate { return browser.MenuVisible; });
      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to be out", delegate { return browser.MediaMenuVisible; });

      int presses = 0;
      while (window.Screen is MediaBrowserView && presses < 10)
      {
        shelf.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Escape,
          Source = shelf
        });
        Pump();
        presses++;
      }

      Assert.That(window.Screen, Is.Not.InstanceOf<MediaBrowserView>(),
          "after " + presses + " presses of back the screen still would not let the viewer go; it is wearing "
          + "details: " + browser.DetailsVisible + ", menu: " + browser.MediaMenuVisible
          + ", title menu: " + browser.MenuVisible + ", search: " + browser.HasSearch
          + ", watched filter: " + browser.HasSeenFilter);
      window.Close();
    }

    /// <summary>
    /// Back out of a shelf whose details the viewer had put away.  The screen used to answer a details layer it
    /// could not actually put away - the strip along its foot, which was made of the same reading as the panel
    /// and could not be dismissed - and a viewer pressing Escape on the 26th of October stayed shut in.  That
    /// layer is gone: the shelf has one details layer, the panel beside it (or above its covers), and Back puts
    /// that away first and then leaves, however many times it is pressed.
    /// </summary>
    [AvaloniaTest]
    public void BackOutOfAShelfWearingDetailsNobodyAskedForLetsTheViewerGo()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      var opened = new List<MediaBrowserViewModel>();
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "home")
        {
          return new HomeView(home, workflow, null, null,
              delegate (HomeSection section)
              {
                var model = new MediaBrowserViewModel(home.Client, section);
                model.ShowDetails = false;
                opened.Add(model);
                return new WorkflowFrame("titles", section.Label, model);
              });
        }
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      workflow.Push(new WorkflowFrame("home", "Home", home));
      window.Show();
      home.RefreshCommand.Execute(null);
      WaitUntil("the tiles to appear", delegate { return home.Sections.Count > 0; });

      ListBox tiles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "menuList");
      tiles.Focus();
      tiles.SelectedItem = EntryNamed(home.Menu, "Movies");
      tiles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = tiles
      });
      Pump();
      MediaBrowserViewModel browser = opened[0];
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 0; });

      // The state in question: the viewer put the details away, and a title is highlighted.
      browser.Selected = browser.Titles[0];
      Pump();
      Assert.That(browser.ShowDetails, Is.False, "the viewer never asked for the panel");

      ListBox shelf = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      shelf.Focus();

      int presses = 0;
      while (window.Screen is MediaBrowserView && presses < 5)
      {
        shelf.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Escape,
          Source = shelf
        });
        Pump();
        presses++;
      }

      Assert.That(window.Screen, Is.Not.InstanceOf<MediaBrowserView>(),
          "back shut the viewer in: the screen kept answering a details layer it could not put away (presses: "
          + presses + ", panel: " + browser.ShowDetails + ", read about a title: " + browser.DetailsVisible + ")");
      window.Close();
    }

    /// <summary>
    /// One press of back on a wall wearing details leaves the screen.  The screen used to answer that press by
    /// taking the details away and swallowing the key, so that a viewer who wanted out pressed a key they had
    /// already pressed once and stayed exactly where they were, looking at the same wall minus a panel.  The
    /// reference binds Escape to nothing on this screen - its one Escape binding (master_mediamenu.xaml:402) is
    /// enabled solely while the media menu is open - so back on a wall with details open means "away from this
    /// screen", and the details go out of the picture with the screen rather than one press ahead of it.
    /// </summary>
    [AvaloniaTest]
    public void BackLeavesAWallWearingDetailsInOnePress()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      var opened = new List<MediaBrowserViewModel>();
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "home")
        {
          return new HomeView(home, workflow, null, null,
              delegate (HomeSection section)
              {
                var model = new MediaBrowserViewModel(home.Client, section);
                opened.Add(model);
                return new WorkflowFrame("titles", section.Label, model);
              });
        }
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      workflow.Push(new WorkflowFrame("home", "Home", home));
      window.Show();
      home.RefreshCommand.Execute(null);
      WaitUntil("the tiles to appear", delegate { return home.Sections.Count > 0; });

      ListBox tiles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "menuList");
      tiles.Focus();
      tiles.SelectedItem = EntryNamed(home.Menu, "Movies");
      tiles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = tiles
      });
      Pump();
      MediaBrowserViewModel browser = opened[0];
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 0; });

      ListBox shelf = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      shelf.Focus();
      browser.Selected = browser.Titles[0];
      WaitUntil("the details to be read", delegate { return browser.DetailsVisible; });

      shelf.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = shelf
      });
      Pump();

      Assert.That(window.Screen, Is.Not.InstanceOf<MediaBrowserView>(),
          "one press of back on a wall wearing details left the viewer on the same wall, without a panel; the "
          + "key was pressed once and means one thing: away from this screen");
      window.Close();
    }

    /// <summary>Presses the key a bound action answers, into the control that really has the focus.</summary>
    private static void Press(Control source, MediaPortal.Frontend.Settings.KeyAction action)
    {
      Key key = MediaPortal.Frontend.Settings.KeyAction.Details == action ? Key.D
          : MediaPortal.Frontend.Settings.KeyAction.Watched == action ? Key.W
          : MediaPortal.Frontend.Settings.KeyAction.MediaMenu == action ? Key.I
          : MediaPortal.Frontend.Settings.KeyAction.Find == action ? Key.F
          : throw new ArgumentException("No key named for " + action, nameof(action));

      source.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = source
      });
      Pump();
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
      Assert.That(browser.HasMore, Is.False);
    }

    [AvaloniaTest]
    public void ATitleWhoseFileIsOnAnotherMachineIsStreamedAndNotRefused()
    {
      Guid elsewhereId = Guid.NewGuid();
      MediaItem reachable = Title("Reachable", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH);
      MediaItem elsewhere = new ItemWithPath(elsewhereId, "Elsewhere", 2025,
          RESOURCE_SYSTEM_ID.ToString("B") + "://Films/Movie.mkv");
      RegisterLibrary(reachable, elsewhere);
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser = new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      var played = new List<TitleViewModel>();
      browser.PlayRequested += delegate (object? sender, TitleViewModel title) { played.Add(title); };
      browser.OpenCommand.Execute(null);
      WaitUntil("the titles to arrive", delegate { return browser.Titles.Count == 2; });

      TitleViewModel streamed = browser.Titles.First(title => title.Name == "Elsewhere");
      Assert.That(streamed.LocalPath, Is.Null, "the file is not on this machine, and the row does not claim it is");
      Assert.That(streamed.StreamAddress, Is.Not.Null,
          "but the server that named the file can also carry it, so the row is not a dead end");
      Assert.That(streamed.StreamAddress!.ToString(), Does.StartWith(server.BaseUrl.ToString()),
          "and the address is on the server the titles were read from");
      Assert.That(streamed.StreamAddress,
          Is.EqualTo(new Uri(server.BaseUrl, "api/media/items/" + elsewhereId.ToString("D") + "/stream")),
          "it is the id of the title, not a path the viewer could guess at");
      Assert.That(streamed.Playable, Is.True);
      Assert.That(streamed.Detail, Is.Not.EqualTo("not playable here"),
          "the row does not warn about something that works");

      browser.Play(streamed);
      Assert.That(played, Has.Count.EqualTo(1));
      MediaLocation handed = played[0].PlaybackLocation!.Value;
      Assert.That(handed.IsFile, Is.False,
          "what the player is handed is the address, since the path would fail on this machine");
      Assert.That(handed.EngineString, Does.EndWith("/stream"),
          "and it is the stream address of the title, word for word");

      browser.Play(browser.Titles.First(title => title.Name == "Reachable"));
      Assert.That(played, Has.Count.EqualTo(2));
      MediaLocation local = played[1].PlaybackLocation!.Value;
      Assert.That(local.IsFile, Is.True,
          "a file that is on this machine is opened directly; it is not detoured through the server's socket");
      Assert.That(local.EngineString, Is.EqualTo(LOCAL_PATH));
      Assert.That(played[1].StreamAddress, Is.Not.Null,
          "the address is offered for every title, but the file wins where both exist");
    }

    [AvaloniaTest]
    public void ATitleTheServerHasNoFileForStillSaysWhyItCannotBeWatched()
    {
      MediaItem silent = new Item(Guid.NewGuid(), "Silent", 2025);
      RegisterLibrary(silent);
      InProcessApi server = StartServer();

      MediaBrowserViewModel browser = new MediaBrowserViewModel(new ServerApiClient(server.BaseUrl), MoviesSection);
      var played = new List<TitleViewModel>();
      browser.PlayRequested += delegate (object? sender, TitleViewModel title) { played.Add(title); };
      browser.OpenCommand.Execute(null);
      WaitUntil("the title to arrive", delegate { return browser.Titles.Count == 1; });

      TitleViewModel nothing = browser.Titles[0];
      Assert.That(nothing.StreamAddress, Is.Null, "a title without a file has no address either");
      Assert.That(nothing.Playable, Is.False);
      Assert.That(nothing.Detail, Is.EqualTo("not playable here"));

      browser.Play(nothing);
      Assert.That(played, Is.Empty);
      Assert.That(browser.Status, Does.Contain("cannot be watched from here"));
      Assert.That(browser.Status, Does.Contain("did not name a file"),
          "the viewer is told the server knows of no file for the title, which is the truth and not a complaint "
          + "about the player");
    }

    [Test]
    public void ARowWithNoServerBehindItCannotStreamAndSaysThePathIsNotOpenable()
    {
      MediaTitle elsewhere = new MediaTitle(Guid.NewGuid(), "Elsewhere", null, 2025, null,
          RESOURCE_SYSTEM_ID.ToString("B") + "://Films/Movie.mkv", false);

      TitleViewModel alone = new TitleViewModel(elsewhere);

      Assert.That(alone.StreamAddress, Is.Null);
      Assert.That(alone.Playable, Is.False, "without a server there is nothing that could carry the file");
      Assert.That(alone.UnplayableReason, Does.Contain("this machine can open"));
    }

    [Test]
    public void ATitleThatIsNotOneFileIsNotOfferedAsAStreamEither()
    {
      MediaTitle insideAnArchive = new MediaTitle(Guid.NewGuid(), "Inside", null, 2025, null,
          RESOURCE_SYSTEM_ID.ToString("B") + "://D:/films/films.zip>Films/Inside.mkv", false);
      ServerApiClient client = new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"));

      TitleViewModel row = new TitleViewModel(insideAnArchive, false, client);

      Assert.That(row.StreamAddress, Is.Null,
          "the endpoint sends the title's one file, and a title stacked out of several resources has no such file");
      Assert.That(row.Playable, Is.False);
      Assert.That(row.UnplayableReason, Does.Contain("more than one resource"),
          "and the row says that before anything is pressed, instead of starting a player that can only fail");
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
    public void AShelfThatWasGivenWholeIsNotReadOnInto()
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
      Assert.That(browser.HasMore, Is.False,
          "so there is nothing left to read; the paging itself is the server endpoint's own test");
    }

    /// <summary>
    /// The shelf on the screen is the shelf the server has - not the first screenful of it - and nobody had to
    /// scroll, press or wait for that to happen.
    /// </summary>
    [AvaloniaTest]
    public void AShelfReadsItselfWholeWithoutAnyoneAsking()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);

      WaitUntil("the whole shelf", delegate { return browser.Titles.Count == 130; });

      Assert.That(browser.Titles.Count, Is.EqualTo(130),
          "one hundred and thirty films were asked for and one hundred and thirty arrived, without a press");
      Assert.That(browser.HasMore, Is.False, "and the shelf knows it is whole");
      Assert.That(browser.Status, Is.EqualTo("130 of 130 movies, sorted by title"),
          "the count under the list speaks about the shelf, not about a page of it");
      Assert.That(server.TimesAsked, Is.EqualTo(2),
          "a screenful first so that names appear, then the rest in one question - two questions about a shelf "
          + "rather than three, because every question makes the server sort the shelf again");
    }

    /// <summary>
    /// The two questions a shelf costs, in the order they are asked, and what the answer must not do to the viewer
    /// standing in the middle of what was already there.
    /// </summary>
    [AvaloniaTest]
    public void TheOpeningScreenfulIsAskedFirstAndTheRestInOneBlowBehindIt()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the opening screenful",
          delegate { return browser.Titles.Count == MediaBrowserViewModel.OPENING_PAGE; });

      Assert.That(server.FirstQuestion, Does.Contain("limit=" + MediaBrowserViewModel.OPENING_PAGE),
          "the question that is answered first is the small one, because a television should have names on it "
          + "inside a sixth of a second");

      TitleViewModel standing = browser.Titles[3];
      browser.Selected = standing;

      WaitUntil("the rest of the shelf", delegate { return browser.Titles.Count == 130; });

      Assert.That(server.LastQuestion, Does.Contain("offset=" + MediaBrowserViewModel.OPENING_PAGE)
              .And.Contain("limit=" + MediaBrowserViewModel.REST_IN_ONE_ASK),
          "the second question carries on where the first stopped and asks for as much as the server will hand "
          + "over, which for eleven hundred films is everything that is left");
      Assert.That(browser.Selected, Is.SameAs(standing),
          "a shelf filling in under a viewer who is reading somewhere above it does not move them");
      Assert.That(browser.Status, Does.Contain("of 130"), "and the count still speaks about the whole shelf");
    }

    [AvaloniaTest]
    public void ARowTheScreenNeverDrewAskedForNoPicture()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the whole shelf", delegate { return browser.Titles.Count == 130; });

      TitleViewModel last = browser.Titles[browser.Titles.Count - 1];
      Assert.That(last.HasArtwork, Is.False,
          "a hundred and thirty rows are in memory and a television shows a dozen; nobody has laid this row out, "
          + "so nobody has asked what its poster looks like");
      Assert.That(server.PicturesAsked, Is.LessThan(10),
          "reading a shelf whole costs no picture per row - a shelf is not a hundred and thirty downloads wearing "
          + "a list");

      // The screen drew it, so the row asks. This is the call MediaBrowserView makes for the rows it can see.
      browser.PaintRow(last);
      WaitUntil("the row's picture", delegate { return last.HasArtwork; });

      Assert.That(last.HasArtwork, Is.True);
    }

    /// <summary>
    /// What a standing television asks of a shelf that is a thousand tiles long: the pictures of the tiles it can
    /// see, and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Eleven hundred tiles that each fetched a poster would be eleven hundred downloads wearing the shelf, which
    /// is exactly the thing that makes a big library stop feeling instant. The band the screen asks for is what it
    /// shows plus a screenful in each direction, so that scrolling arrives at pictures rather than at grey.
    /// </para>
    /// <para>
    /// The tile panel virtualises now, so the two facts that used to be far apart sit next to each other: only the
    /// rows the screen could be looking at are built at all, and of those rows the screen asks for the pictures it
    /// can see. Both are asserted below, together with the reading of the titles themselves: the whole shelf is
    /// already in the list when the viewer starts scrolling, so scrolling never waits.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void ATiledShelfAsksForNoPictureTheViewerCannotSee()
    {
      LongShelf server = new LongShelf { Total = 600 };
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the whole shelf", delegate { return browser.Titles.Count == 600 && !browser.IsBusy; });

      ListBox rows = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "titlesList");

      // Every shape the screen has worn lays its own set of frames out, and each of those sets is painted - a
      // shelf that is walked from rows into tiles has therefore asked for more pictures in total than the tiles
      // standing at the end of the walk. What may never happen is that it asks for the picture of a title it has
      // never laid a frame for, so the set of titles the screen ever had standing is what the count below is
      // measured against, phase by phase.
      HashSet<TitleViewModel> laid_out = new HashSet<TitleViewModel>();
      void NoteFramesStanding()
      {
        foreach (Control? container in rows.GetRealizedContainers())
          if (container?.DataContext is TitleViewModel title)
            laid_out.Add(title);

        // The drawn cover row lays its frames too - it paints the covers itself and asks their pictures by
        // itself (CoverCarousel.PictureAsked), while the list underneath it stands as caret only. A cover the
        // row drew is a frame the screen laid as much as a container of the list, so its title counts here.
        foreach (CoverCarousel row in window.GetVisualDescendants().OfType<CoverCarousel>())
          foreach (object cover in row.CoversAsked)
            if (cover is TitleViewModel cover_title)
              laid_out.Add(cover_title);
      }

      Pump();
      NoteFramesStanding();
      for (int press = 0; press < 3 && browser.Layout != BrowserLayout.Grid; press++)
      {
        browser.SwitchViewCommand.Execute(null);
        for (int pause = 0; pause < 10; pause++)
          Pump();
        NoteFramesStanding();
      }

      Assert.That(browser.Layout, Is.EqualTo(BrowserLayout.Grid), "the screen stands in tiles");

      WaitUntil("the tiles the screen shows to wear their pictures", delegate { return server.PicturesAsked > 0; });
      for (int pause = 0; pause < 20; pause++)
        Pump();
      NoteFramesStanding();

      int standing = rows.GetRealizedContainers().Count();
      Assert.That(standing, Is.LessThan(150),
          "six hundred titles were read, but only the rows the screen could be looking at were built: "
          + standing + " of 600");
      Assert.That(server.PicturesAskedForTitles, Is.LessThanOrEqualTo(laid_out.Count),
          "the screen asks for the picture of no title it has not laid a frame for: "
          + server.PicturesAskedForTitles + " titles asked for, " + laid_out.Count + " titles ever laid out ("
          + standing + " tiles standing now)");
      Assert.That(standing, Is.GreaterThan(0), "the tiles the viewer looks at are there");
      Assert.That(server.PicturesAsked, Is.LessThan(200),
          "a band of a few screenfuls around the eye, not a poster per tile of a shelf nobody will scroll to");

      ScrollViewer scroller = rows.GetVisualDescendants().OfType<ScrollViewer>().First();
      Assert.That(scroller.Extent.Height, Is.GreaterThan(scroller.Viewport.Height),
          "six hundred tiles do not fit on this screen, so there is a bottom to scroll to");

      int before = server.PicturesAsked;
      scroller.Offset = new Vector(0, scroller.Extent.Height);
      WaitUntil("the tiles at the bottom to ask for their pictures",
          delegate { return server.PicturesAsked > before; });
      WaitUntil("the last tile of the shelf to stand", delegate
      {
        return rows.GetRealizedContainers().OfType<Control>()
            .Any(tile => ReferenceEquals(tile.DataContext, browser.Titles[599]));
      });

      Assert.That(rows.GetRealizedContainers().Count(), Is.LessThan(150),
          "and rolling to the bottom of a six hundred tile shelf did not build it: "
          + rows.GetRealizedContainers().Count() + " tiles stand at the end of it");
      Assert.That(browser.Titles.Count, Is.EqualTo(600),
          "and the shelf did not need that scroll to be finished: it was whole before anybody moved");
    }

    [AvaloniaTest]
    public void AShelfThatGoesOnForeverIsStoppedAndSaysSo()
    {
      LongShelf server = new LongShelf();
      server.Total = 4 * MediaBrowserViewModel.MAX_ROWS;
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);

      WaitUntil("the ceiling", delegate { return !browser.HasMore; });

      Assert.That(browser.Titles.Count,
          Is.GreaterThanOrEqualTo(MediaBrowserViewModel.MAX_ROWS)
              .And.LessThan(MediaBrowserViewModel.MAX_ROWS + MediaBrowserViewModel.REST_IN_ONE_ASK),
          "the ceiling, with at most the question that walked over it - the reading stops on an answer boundary");
      Assert.That(browser.HasMore, Is.False);
      Assert.That(server.TimesAsked, Is.LessThanOrEqualTo(5),
          "twenty thousand rows are a screenful and a few big questions, not a hundred and forty - every question "
          + "makes the server sort the shelf again");
      Assert.That(browser.Status, Does.Contain("first 5000"),
          "a shelf that stopped short of the server's count says so instead of standing there looking finished");
    }

    /// <summary>
    /// A shelf that hands over the rows it was asked for and counts what it holds - the shape a real server's
    /// answer has, and the only way a front end learns that a shelf is longer than one page.
    /// </summary>
    private class LongShelf : HttpMessageHandler
    {
      /// <summary>How many titles this shelf holds in all.</summary>
      internal int Total { get; set; } = 130;

      /// <summary>How many times the shelf was asked for titles.</summary>
      internal int TimesAsked { get; private set; }

      /// <summary>
      /// How many pictures were asked for, which is the number a shelf of a hundred and thirty films must not
      /// reach while nobody has scrolled it.
      /// </summary>
      internal int PicturesAsked { get; private set; }

      /// <summary>
      /// How many <em>titles</em> were ever asked a picture for, however many kinds of picture they were asked
      /// for. A shelf walked from rows into tiles legitimately asks a poster of a title it had already asked a
      /// thumbnail of, so it is the titles and not the requests that a shelf may not exceed.
      /// </summary>
      internal int PicturesAskedForTitles
      {
        get { lock (_pictureGate) return _pictureTitles.Count; }
      }

      private readonly object _pictureGate = new object();
      private readonly HashSet<string> _pictureTitles = new HashSet<string>(StringComparer.Ordinal);

      internal bool AskedForPicture(Guid id)
      {
        lock (_pictureGate)
          return _pictureTitles.Contains(id.ToString("D"));
      }

      /// <summary>The last question about titles, so that a test can say what was asked for.</summary>
      internal string LastQuestion { get; private set; } = "";

      /// <summary>The very first question, which is the one that decides how fast names reach the screen.</summary>
      internal string FirstQuestion { get; private set; } = "";

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (IsPictureRequest(request))
        {
          PicturesAsked++;
          string[] parts = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
          for (int part = 0; part + 1 < parts.Length; part++)
            if (string.Equals(parts[part], "items", StringComparison.Ordinal))
            {
              lock (_pictureGate)
                _pictureTitles.Add(parts[part + 1]);
            }
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
          });
        }

        // A backdrop the shelf has none of, answered the way a server answers that.
        if (request.RequestUri!.AbsolutePath.Contains("/fanart", StringComparison.Ordinal))
          return Task.FromResult(NoPicture(request));

        // What the screen reads about one title is not a question about the shelf, but it is answered - a screen
        // that got a 404 for it would write that 404 into the status line the test is reading.
        if (request.RequestUri.AbsolutePath.Contains("/items/", StringComparison.Ordinal))
        {
          string path = request.RequestUri.AbsolutePath;
          string id = path.Substring(path.LastIndexOf('/') + 1);
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent("{\"id\":\"" + id + "\",\"title\":\"Title\",\"year\":2000}",
                Encoding.UTF8, "application/json")
          });
        }

        string query = request.RequestUri.Query;
        int askedFor = int.Parse(Of(query, "limit") ?? "60");
        int from = int.Parse(Of(query, "offset") ?? "0");
        TimesAsked++;
        LastQuestion = request.RequestUri.PathAndQuery;
        if (TimesAsked == 1)
          FirstQuestion = request.RequestUri.PathAndQuery;

        int rows = Math.Max(0, Math.Min(askedFor, Total - from));
        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(Total)
            .Append(",\"offset\":").Append(from).Append(",\"limit\":").Append(askedFor)
            .Append(",\"returned\":").Append(rows).Append(",\"sort\":\"title\"")
            .Append(",\"titles\":[");
        for (int i = 0; i < rows; i++)
        {
          if (i > 0)
            body.Append(',');
          body.Append("{\"id\":\"").Append(new Guid(from + i + 1, 0, 0, new byte[] { 128, 0, 0, 0, 0, 0, 0, 1 }))
              .Append("\",\"title\":\"Title ").Append((from + i).ToString("D4")).Append("\",\"year\":2000}");
        }
        body.Append("]}");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      /// <summary>One name out of a query string, or <c>null</c> when the question did not carry it.</summary>
      private static string? Of(string query, string name)
      {
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
          int equals = pair.IndexOf('=');
          if (equals > 0 && pair.Substring(0, equals) == name)
            return Uri.UnescapeDataString(pair.Substring(equals + 1));
        }
        return null;
      }
    }

    private static MediaBrowserViewModel BrowserOn(LongShelf server)
    {
      return new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);
    }

    /// <summary>
    /// A shelf that knows its genres: the counting question gets the categories with their numbers, and a
    /// question that names one value gets only that genre's own titles. A hundred and thirty films stand here -
    /// seventy Action, fifty Drama, ten Horror - which is enough for one genre to overflow the opening page
    /// when a test asks it to, and small enough to read whole in a blink.
    /// </summary>
    private class GroupedShelf : LongShelf
    {
      /// <summary>How many films carry each genre. The counting answers in this order, biggest first.</summary>
      internal int Action { get; set; } = 70;
      internal int Drama { get; set; } = 50;
      internal int Horror { get; set; } = 10;

      /// <summary>How many times the shelf was asked which genres there are.</summary>
      internal int GroupsAsked { get; private set; }

      /// <summary>The genres in the shelf's own order, as (word, count) pairs.</summary>
      private (string Value, int Count)[] Genres
      {
        get { return new[] { ("Action", Action), ("Drama", Drama), ("Horror", Horror) }; }
      }

      /// <summary>Where a genre's titles stand in the shelf's global numbering.</summary>
      private int FirstIndexOf(string value)
      {
        int first = 0;
        foreach ((string name, int count) in Genres)
        {
          if (name == value)
            return first;
          first += count;
        }
        return first;
      }

      private int TotalOf(string value)
      {
        foreach ((string name, int count) in Genres)
          if (name == value)
            return count;
        return 0;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        Total = Action + Drama + Horror;
        string path = request.RequestUri!.AbsolutePath;

        if (path.EndsWith("/groups", StringComparison.Ordinal))
        {
          GroupsAsked++;
          StringBuilder body = new StringBuilder("{\"aspect\":\"movies\",\"by\":\"genre\",\"groups\":[");
          bool first_group = true;
          foreach ((string name, int count) in Genres)
          {
            if (!first_group)
              body.Append(',');
            first_group = false;
            body.Append("{\"value\":\"").Append(name).Append("\",\"count\":").Append(count).Append('}');
          }
          body.Append("]}");

          return Task.FromResult(Json(request, body.ToString()));
        }

        string? by = Param(request.RequestUri.Query, "by");
        if (by != null)
        {
          string value = Param(request.RequestUri.Query, "value") ?? "";
          int asked_for = int.Parse(Param(request.RequestUri.Query, "limit") ?? "60");
          int from = int.Parse(Param(request.RequestUri.Query, "offset") ?? "0");
          int total = TotalOf(value);
          int rows = Math.Max(0, Math.Min(asked_for, total - from));
          int first = FirstIndexOf(value);

          StringBuilder body = new StringBuilder();
          body.Append("{\"total\":").Append(total)
              .Append(",\"offset\":").Append(from).Append(",\"limit\":").Append(asked_for)
              .Append(",\"returned\":").Append(rows).Append(",\"sort\":\"title\"")
              .Append(",\"titles\":[");
          for (int i = 0; i < rows; i++)
          {
            if (i > 0)
              body.Append(',');
            int index = first + from + i;
            body.Append("{\"id\":\"").Append(new Guid(index + 1, 0, 0, new byte[] { 128, 0, 0, 0, 0, 0, 0, 1 }))
                .Append("\",\"title\":\"").Append(value).Append(' ').Append(index.ToString("D4"))
                .Append("\",\"year\":2000}");
          }
          body.Append("]}");

          return Task.FromResult(Json(request, body.ToString()));
        }

        return base.SendAsync(request, cancellation);
      }

      private static HttpResponseMessage Json(HttpRequestMessage request, string body)
      {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
      }

      /// <summary>One name out of a query string, or <c>null</c> when the question did not carry it.</summary>
      private static string? Param(string query, string name)
      {
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
          int equals = pair.IndexOf('=');
          if (equals > 0 && pair.Substring(0, equals) == name)
            return Uri.UnescapeDataString(pair.Substring(equals + 1));
        }
        return null;
      }
    }

    /// <summary>
    /// Grouping is asked for where filtering is asked for: the screen's menu carries a "Group" row beside the
    /// "Filter" row, wearing grouping's own icon, and the row opens a page that names "None" and the five
    /// categories - because a grouping is the same counting asked the other way round, and a viewer who knows
    /// where one lives looks for the other exactly there.
    /// </summary>
    [AvaloniaTest]
    public void TheScreensMenuCarriesAGroupRowThatOpensTheListOfCategories()
    {
      MediaBrowserViewModel browser = BrowserOn(new GroupedShelf());
      browser.OpenCommand.Execute(null);
      WaitUntil("the titles to arrive", delegate { return browser.Titles.Count == 130; });

      browser.ShowMediaMenu();

      MediaBrowserMenuEntry filter = browser.MediaMenu.First(row => row.Icon == "filter");
      MediaBrowserMenuEntry group = browser.MediaMenu.First(row => row.Icon == "group");

      Assert.That(group.Label, Does.StartWith("Group"), "the row says what it does");
      Assert.That(browser.MediaMenu.IndexOf(group), Is.EqualTo(browser.MediaMenu.IndexOf(filter) + 1),
          "and it stands next to the filter, not buried under it");

      group.Command.Execute(null);
      WaitUntil("the group page to stand open",
          delegate { return browser.MediaMenu.Any(row => row.Label == "By genre"); });

      Assert.That(browser.MediaMenu.First(row => row.Label == "None").IsCurrent, Is.True,
          "no grouping is the grouping now, and the page says so on its None row");
      Assert.That(browser.MediaMenu.Count(row => row.Label.StartsWith("By ", StringComparison.Ordinal)),
          Is.EqualTo(5), "all five categories stand on the page, the same five the filter page offers");

      Assert.That(browser.IsGrouped, Is.False, "and nothing has been grouped until a row is pressed");
    }

    /// <summary>
    /// What pressing a category does to the shelf: every title stays - that is grouping's whole promise over
    /// filtering, which throws titles away - and each genre's run stands under its own headline, in the
    /// server's own order of how big each genre is.
    /// </summary>
    [AvaloniaTest]
    public void GroupingTheShelfStandsEveryTitleUnderItsOwnHeadline()
    {
      GroupedShelf server = new GroupedShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the flat shelf", delegate { return browser.Titles.Count == 130; });

      browser.ShowMediaMenu();
      browser.MediaMenu.First(row => row.Icon == "group").Command.Execute(null);
      WaitUntil("the group page", delegate { return browser.MediaMenu.Any(row => row.Label == "By genre"); });
      browser.MediaMenu.First(row => row.Label == "By genre").Command.Execute(null);

      WaitUntil("the shelf to stand grouped",
          delegate { return browser.IsGrouped && browser.Rows.Count == 133; });

      Assert.That(server.GroupsAsked, Is.EqualTo(1),
          "one question says which genres there are and how many each holds");
      Assert.That(browser.Titles.Count, Is.EqualTo(130),
          "every film that was on the shelf is still on it - grouping keeps the shelf, it only re-stands it");
      Assert.That(browser.Rows.Count, Is.EqualTo(133), "and three headlines stand among them on the wall");

      Assert.That(browser.Rows[0], Is.InstanceOf<GroupHeaderRow>(), "the shelf opens with its biggest headline");
      Assert.That(((GroupHeaderRow)browser.Rows[0]).Label, Is.EqualTo("Action"));
      Assert.That(((GroupHeaderRow)browser.Rows[0]).Count, Is.EqualTo(70),
          "and the headline says how many the server counted under it, because nothing narrows this shelf");
      Assert.That(browser.Rows[71], Is.InstanceOf<GroupHeaderRow>(),
          "the next headline stands where its own block begins, not bunched at the front with the others");
      Assert.That(browser.Rows[122], Is.InstanceOf<GroupHeaderRow>(), "and the third above its ten films");
      Assert.That(browser.Rows.OfType<GroupHeaderRow>().Count(), Is.EqualTo(3));
      Assert.That(browser.Rows.OfType<TitleViewModel>().Count(), Is.EqualTo(130),
          "the same title instances the shelf has always held, no more and none thrown away");

      Assert.That(browser.Selected, Is.InstanceOf<TitleViewModel>(),
          "a caret stands on a film, never on a headline - the wall cannot even put it anywhere else");
      Assert.That(browser.Status, Does.Contain("grouped by genre"), "and the foot says what stands there now");
    }

    /// <summary>
    /// An ungrouped wall shows the shelf with nothing in it but titles: the headlines are not decoration that
    /// stands around when nobody asked for them, and taking the grouping off with the None row puts the plain
    /// shelf back - same films, no plates.
    /// </summary>
    [AvaloniaTest]
    public void TakingTheGroupingOffPutsThePlainShelfBack()
    {
      MediaBrowserViewModel browser = BrowserOn(new GroupedShelf());
      browser.OpenCommand.Execute(null);
      WaitUntil("the flat shelf", delegate { return browser.Titles.Count == 130; });

      Assert.That(browser.Rows.Count, Is.EqualTo(browser.Titles.Count),
          "ungrouped, the wall is the shelf - plate for plate there are no plates");

      browser.ShowMediaMenu();
      browser.MediaMenu.First(row => row.Icon == "group").Command.Execute(null);
      WaitUntil("the group page", delegate { return browser.MediaMenu.Any(row => row.Label == "By genre"); });
      browser.MediaMenu.First(row => row.Label == "By genre").Command.Execute(null);
      WaitUntil("the grouped shelf", delegate { return browser.IsGrouped && browser.Rows.Count == 133; });

      browser.MediaMenu.First(row => row.Label == "None").Command.Execute(null);
      WaitUntil("the plain shelf back",
          delegate { return !browser.IsGrouped && browser.Rows.Count == 130; });

      Assert.That(browser.Rows.OfType<GroupHeaderRow>().Count(), Is.EqualTo(0),
          "the headlines are gone, and nothing else was disturbed on the way back");
      Assert.That(browser.Titles.Count, Is.EqualTo(130), "all the films are still there");
    }

    /// <summary>
    /// A grouped shelf still reads itself whole, and reading on must not disturb what grouping stood: the rest
    /// of a genre goes under that genre's own headline - which is the one thing a naive "append everything at
    /// the end" would get wrong, because the earlier genre finishes reading after the later ones already
    /// arrived.
    /// </summary>
    [AvaloniaTest]
    public async Task AGroupedShelfReadsItselfWholeKeepingEveryTitleUnderItsOwnHeadline()
    {
      GroupedShelf server = new GroupedShelf { Action = 700 };
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the opening screenful", delegate { return browser.Titles.Count >= 120; });

      await browser.PickGroupAsync("genre");

      WaitUntil("the whole grouped shelf",
          delegate { return browser.Titles.Count == 760 && !browser.IsBusy && browser.Rows.Count == 763; });

      Assert.That(browser.Rows.Count, Is.EqualTo(763), "three headlines and every one of the films");
      Assert.That(browser.Rows[0], Is.InstanceOf<GroupHeaderRow>(),
          "and after all the reading, each headline still stands where its block begins");
      Assert.That(browser.Rows[701], Is.InstanceOf<GroupHeaderRow>(),
          "the Drama headline sits behind all seven hundred Action films, not beside the first hundred and twenty");
      Assert.That(browser.Rows[752], Is.InstanceOf<GroupHeaderRow>());
      Assert.That(browser.Rows[700], Is.InstanceOf<TitleViewModel>(),
          "and it sits directly above the first film of its block, with nothing of another block between");
      Assert.That(browser.Rows[702], Is.InstanceOf<TitleViewModel>(),
          "and the block below the headline is its own genre's films read whole");
      Assert.That(browser.HasMore, Is.False, "the shelf knows it is whole");
    }

    /// <summary>
    /// A headline standing in the wall of tiles wears the tile it stands among - the poster's shape in the
    /// poster's measures, the banner's in the banner's - rather than naming a size of its own or shrinking to
    /// the size of its word. Proven on the screen: the plate's container stands as wide and as tall as the
    /// tile container next to it.
    /// </summary>
    [AvaloniaTest]
    public async Task TheGroupHeadlineWearsTheTileItStandsOn()
    {
      GroupedShelf server = new GroupedShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the flat shelf", delegate { return browser.Titles.Count == 130; });

      await browser.PickGroupAsync("genre");
      WaitUntil("the grouped shelf", delegate { return browser.IsGrouped && browser.Rows.Count == 133; });

      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      for (int pause = 0; pause < 10; pause++)
        Pump();

      for (int press = 0; press < 3 && browser.Layout != BrowserLayout.Grid; press++)
      {
        browser.SwitchViewCommand.Execute(null);
        for (int pause = 0; pause < 10; pause++)
          Pump();
      }
      Assert.That(browser.Layout, Is.EqualTo(BrowserLayout.Grid), "the wall stands in tiles");

      ListBox rows = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "titlesList");

      int plateAt = -1;
      for (int at = 0; at < browser.Rows.Count; at++)
        if (browser.Rows[at] is GroupHeaderRow)
        {
          plateAt = at;
          break;
        }
      Assert.That(plateAt, Is.GreaterThanOrEqualTo(0), "a grouped wall opens with a headline");

      int tileAt = plateAt;
      while (browser.Rows[++tileAt] is GroupHeaderRow)
      {
      }

      Control? plate = null;
      Control? tile = null;
      WaitUntil("the headline and its neighbour to stand", delegate
      {
        plate = rows.ContainerFromIndex(plateAt) as Control;
        tile = rows.ContainerFromIndex(tileAt) as Control;
        return plate != null && tile != null && plate.Bounds.Width > 0 && tile.Bounds.Width > 0;
      });

      Assert.That(plate!.Bounds.Width, Is.EqualTo(tile!.Bounds.Width).Within(1.0),
          "the headline wears the width the tiles wear");
      Assert.That(plate.Bounds.Height, Is.EqualTo(tile.Bounds.Height).Within(1.0),
          "and the height too - a poster wall gives it a poster's measures, a banner wall a banner's");

      // And not just the standing: the VISIBLE face - the framed card the item template produced - has to
      // measure what a tile's face measures, so the wall's gaps between media stay exactly what they were.
      Visual? plate_face = ItemFace(plate!);
      Visual? tile_face = ItemFace(tile!);
      Assert.That(plate_face, Is.Not.Null, "a tile wears its frame inside the container");
      Assert.That(tile_face, Is.Not.Null);
      Rect p = plate_face!.Bounds;
      Rect t = tile_face!.Bounds;
      Assert.That(p.Width, Is.EqualTo(t.Width).Within(1.0),
          "the plate's face is as wide as the tile's face - the same margin breathes around both");
      Assert.That(p.Height, Is.EqualTo(t.Height).Within(1.0),
          "and as tall");
      Assert.That(p.X, Is.EqualTo(t.X).Within(1.0),
          "and stands in the same gap from its container's left hand");
      Assert.That(p.Y, Is.EqualTo(t.Y).Within(1.0));

      // And the same law in the banner's and the thumbnail's measures: the plate follows whatever picture the
      // wall wears, because the wall measures itself by its tiles and the plate wears that measure.
      for (int press = 0; press < 2; press++)
      {
        browser.SwitchMedia();

        Control? again_plate = null;
        Control? again_tile = null;
        WaitUntil("the wall to stand in its next picture's tiles", delegate
        {
          again_plate = rows.ContainerFromIndex(plateAt) as Control;
          again_tile = rows.ContainerFromIndex(tileAt) as Control;
          return again_plate != null && again_tile != null && again_plate.Bounds.Width > 0
              && again_tile.Bounds.Width > 0;
        });

        Visual? next_face = ItemFace(again_plate!);
        Visual? next_tile_face = ItemFace(again_tile!);
        Assert.That(next_face, Is.Not.Null, "the rebuilt wall frames its plate");
        Assert.That(next_tile_face, Is.Not.Null, "and its tiles");
        Assert.That(next_face!.Bounds.Width, Is.EqualTo(next_tile_face!.Bounds.Width).Within(1.0),
            "the headline follows the picture the wall wears - here " + browser.GridTemplateKey);
        Assert.That(next_face.Bounds.Height, Is.EqualTo(next_tile_face.Bounds.Height).Within(1.0),
            "in height as well - here " + browser.GridTemplateKey);
      }
    }

    /// <summary>The framed card the item template produced: the first thing the container's ContentPresenter shows.</summary>
    private static Visual? ItemFace(Visual container)
    {
      ContentPresenter? presenter = container.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault();
      return presenter?.GetVisualChildren().OfType<Visual>().FirstOrDefault();
    }

    /// <summary>
    /// The framework's candidate walk loops while the panel keeps handing it containers it cannot stand on, and
    /// it only notices a cycle against WHERE IT SET OUT - not against where it currently is. So a headline, which
    /// no caret may stand on, must never be answered with itself when something asks it for the next step: the
    /// walk that steps onto a headline from the tile below and then asks the headline onward must be answered
    /// with NOTHING at the wall's edge, or the loop spins on the UI thread forever - which is how a viewer
    /// pressing UP under a headline lost the frontend without a crash.
    /// </summary>
    [AvaloniaTest]
    public async Task AHeadlineTheCaretCannotStandOnAnswersNoStepWithItself()
    {
      GroupedShelf server = new GroupedShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the flat shelf", delegate { return browser.Titles.Count == 130; });

      await browser.PickGroupAsync("genre");
      WaitUntil("the grouped shelf", delegate { return browser.IsGrouped && browser.Rows.Count == 133; });

      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      for (int pause = 0; pause < 10; pause++)
        Pump();

      for (int press = 0; press < 3 && browser.Layout != BrowserLayout.Grid; press++)
      {
        browser.SwitchViewCommand.Execute(null);
        for (int pause = 0; pause < 10; pause++)
          Pump();
      }
      Assert.That(browser.Layout, Is.EqualTo(BrowserLayout.Grid), "the wall stands in tiles");

      ListBox rows = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "titlesList");
      VirtualizingTilePanel? wall = rows.ItemsPanelRoot as VirtualizingTilePanel;
      Assert.That(wall, Is.Not.Null, "the tile wall lays its rows itself");
      INavigableContainer navigation = wall!;

      int plateAt = -1;
      for (int at = 0; at < browser.Rows.Count; at++)
        if (browser.Rows[at] is GroupHeaderRow)
        {
          plateAt = at;
          break;
        }

      Control? plate = null;
      WaitUntil("the headline to stand", delegate
      {
        plate = rows.ContainerFromIndex(plateAt) as Control;
        return plate != null && plate.Bounds.Width > 0;
      });

      Assert.That(plate!.Focusable, Is.False, "the wall disarmed the headline's container");

      // The exact question the dead walk asked: it stepped onto the headline and the headline was asked onward.
      // Over the top edge the answer must be nothing - answering with the headline itself is the spin.
      Assert.That(navigation.GetControl(NavigationDirection.Up, plate, false), Is.Null,
          "a walker stranded on a headline at the wall's edge is answered with nothing, not with the headline "
          + "again - the framework only notices a walk that returns to where it SET OUT, and this walk never did");

      // And the walk is not blinded either: the same question downward answers the tile under the headline.
      IInputElement? below = navigation.GetControl(NavigationDirection.Down, plate, false);
      Assert.That(below, Is.Not.Null.And.Not.SameAs(plate),
          "but downward the wall still answers the tile under the headline");
    }

    /// <summary>
    /// The drawn row and the caret list hand their selection to each other over a two-way binding, and that
    /// exchange is where opening a grouped wall used to die: the wall selected the headline at index zero, the
    /// view said no with -1, the drawn row clamps -1 onto its first cover - which is that same headline - and
    /// the "no" came back again until the stack was gone. A refusal now names the nearest film instead of -1,
    /// which the drawn row accepts as-is; this test says it out loud by assigning the headline directly, the
    /// very thing that used to start the echo.
    /// </summary>
    [AvaloniaTest]
    public void TheWallCannotStandItsCaretOnAHeadlineEvenWhenSomethingAssignsIt()
    {
      MediaBrowserViewModel browser = BrowserOn(new GroupedShelf());
      browser.OpenCommand.Execute(null);
      WaitUntil("the flat shelf", delegate { return browser.Titles.Count == 130; });

      browser.ShowMediaMenu();
      browser.MediaMenu.First(row => row.Icon == "group").Command.Execute(null);
      WaitUntil("the group page", delegate { return browser.MediaMenu.Any(row => row.Label == "By genre"); });
      browser.MediaMenu.First(row => row.Label == "By genre").Command.Execute(null);
      WaitUntil("the grouped shelf", delegate { return browser.IsGrouped && browser.Rows.Count == 133; });

      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the wall to stand", delegate { return browser.Rows.Count == 133 && !browser.IsBusy; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");

      int headline = -1;
      for (int i = 0; i < titles.Items.Count; i++)
      {
        if (titles.Items[i] is GroupHeaderRow)
        {
          headline = i;
          break;
        }
      }

      Assert.That(headline, Is.GreaterThanOrEqualTo(0), "the wall really has a headline to stand on");

      titles.SelectedIndex = headline;

      Assert.That(titles.SelectedItem, Is.Not.InstanceOf<GroupHeaderRow>(),
          "the wall refuses to keep a caret on the headline - it re-stands it on the film beside it");
      Assert.That(titles.SelectedIndex, Is.EqualTo(headline + 1),
          "which is the film under the headline, not the -1 the drawn row would clamp back onto it");
      Assert.That(browser.Selected, Is.Not.InstanceOf<GroupHeaderRow>(),
          "and nothing about the view model changed either");
    }

    /// <summary>
    /// A headline's number is the server counting the WHOLE shelf by the category. The moment a search narrows
    /// what the shelf reads, headline and shelf answer different questions - and a headline naming a number
    /// the shelf cannot show is a headline that lies - so a narrowed shelf shows its words bare.
    /// </summary>
    [AvaloniaTest]
    public async Task ANarrowedShelfShowsItsHeadlinesWithoutNumbers()
    {
      MediaBrowserViewModel browser = BrowserOn(new GroupedShelf());
      browser.OpenCommand.Execute(null);
      WaitUntil("the flat shelf", delegate { return browser.Titles.Count == 130; });

      browser.SearchText = "00";
      await browser.FindAsync();
      await browser.PickGroupAsync("genre");

      WaitUntil("the grouped narrowed shelf",
          delegate { return browser.IsGrouped && browser.Rows.OfType<GroupHeaderRow>().Any(); });

      foreach (GroupHeaderRow plate in browser.Rows.OfType<GroupHeaderRow>())
        Assert.That(plate.ShowsCount, Is.False,
            "the plate \"" + plate.Label + "\" keeps its number to itself while a search decides what the shelf holds");
    }

    /// <summary>
    /// The shelf's leading edge is where the screen's menu stands.  The reference hung that menu on the remote's
    /// Info key alone, but its own room says something else: the menu stands in the two hundred points the room
    /// keeps off the left edge of the screen, so a viewer walking left past the edge of the shelf walked at it.
    /// The edge is the whole first column, not the first title - a viewer standing on the fifth row and pointing
    /// left is pointing at the menu too - and in a list, whose rows are one column wide, every row touches it.
    /// A tile with tiles beside it is no edge, and the opposite key hands the arrows back.
    /// </summary>
    [AvaloniaTest]
    public void WalkingOffTheFrontOfTheShelfOpensTheScreensMenuAndTheOppositeKeyWalksBack()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");

      // A row of a list has nothing beside it, so it stands on the edge and reaches the menu.
      titles.Focus();
      titles.SelectedIndex = 40;
      SendKey(titles, Key.Left);
      WaitUntil("the menu to stand open for a row in the middle of the shelf",
          delegate { return browser.MediaMenuVisible; });

      SendKey(rail, Key.Right);
      WaitUntil("the menu to slide away", delegate { return !browser.MediaMenuVisible; });
      // Not merely "the list has the arrows" - the list holds the caret without wearing the frame.  The frame
      // lives on the ListBoxItem (:focus), so the caret has to be ON a tile or the viewer sees a dark wall.
      Assert.That(window.FocusManager.GetFocusedElement(), Is.InstanceOf<ListBoxItem>(),
          "the shelf has the arrows back ON A TILE, which is the tile that wears the frame");

      // A grid lays tiles beside one another, and only the column touching the shelf's left edge is an edge.
      browser.Layout = BrowserLayout.Grid;
      Pump();
      VirtualizingTilePanel tiles_panel = window.GetVisualDescendants().OfType<VirtualizingTilePanel>()
          .First();
      WaitUntil("the tiles to be laid across the shelf", delegate { return tiles_panel.Columns > 1; });
      int columns = tiles_panel.Columns;

      titles.Focus();
      titles.SelectedIndex = columns + 3;
      SendKey(titles, Key.Left);
      Assert.That(browser.MediaMenuVisible, Is.False,
          "a tile with tiles beside it is not an edge, and the menu stays off the screen");

      // The first tile of the second row is as much an edge as the first tile of the first one.
      titles.Focus();
      titles.SelectedIndex = columns;
      SendKey(titles, Key.Left);
      WaitUntil("the menu to stand open for the second row's first tile",
          delegate { return browser.MediaMenuVisible; });
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });
      WaitUntil("the room to stand aside for it", delegate { return view.SlideOffset >= 200; });
      Assert.That(rail.Items.Count, Is.GreaterThan(0), "and the viewer lands on a menu that has commands on it");

      SendKey(rail, Key.Right);
      WaitUntil("the menu to slide away", delegate { return !browser.MediaMenuVisible; });
      Assert.That(window.FocusManager.GetFocusedElement(), Is.InstanceOf<ListBoxItem>(),
          "the shelf has the arrows back ON A TILE, which is the tile that wears the frame");
      WaitUntil("the room to slide back into the space the menu stood in", delegate { return view.SlideOffset == 0; });
      window.Close();
    }

    /// <summary>
    /// The edge of a row of covers that reaches the screen's menu is its BOTTOM: the cover layout hangs that menu
    /// under the screen, so pointing down out of the row is what pointing left out of a grid is, and up is the way
    /// back.  Nodding up at a menu lying below the row is a key a viewer has to be told about.
    /// </summary>
    [AvaloniaTest]
    public void WalkingDownOutOfARowOfCoversOpensTheMenuUnderTheScreen()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      browser.Layout = BrowserLayout.Cover;
      WaitUntil("the covers to be laid out", delegate { return browser.IsCoverLayout; });
      titles.Focus();
      titles.SelectedIndex = 0;

      SendKey(titles, Key.Down);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox under = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuListBottom");
      WaitUntil("the row under the covers to be walked", delegate { return under.IsKeyboardFocusWithin; });
      WaitUntil("the room to slide up for the menu under it", delegate { return view.SlideOffset <= -200; });

      SendKey(under, Key.Up);
      WaitUntil("the menu to slide away", delegate { return !browser.MediaMenuVisible; });
      Assert.That(window.FocusManager.GetFocusedElement(), Is.InstanceOf<ListBoxItem>(),
          "and the covers have the arrows back ON A TILE, which is the cover that wears the frame");
      window.Close();
    }

    /// <summary>
    /// A filter picked from the rail closes the rail and re-reads the shelf, and the caret has to come back to
    /// a tile of the shelf it was rebuilt into. The viewer stood on a rail row when the shelf underneath them
    /// was thrown away and a new one asked for; a caret that went back to the tiles of the old shelf - or to
    /// the list that only waits for them - is a wall that looks alive and answers no key.
    /// </summary>
    [AvaloniaTest]
    public void PickingAFilterOffTheRailStandsTheCaretOnATileOfTheRebuiltShelf()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      titles.SelectedIndex = 1;
      Pump();

      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });

      MediaBrowserMenuEntry filter_page = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label.StartsWith("Filter", StringComparison.Ordinal));
      filter_page.Command.Execute(null);
      WaitUntil("the filter page to stand up", delegate { return browser.MediaMenuHeading == "Filter"; });

      MediaBrowserMenuEntry watched = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label == "All");
      watched.Command.Execute(null);
      WaitUntil("the rail to go away with the pick", delegate { return !browser.MediaMenuVisible; });
      WaitUntil("the filtered shelf to arrive", delegate { return browser.Titles.Count == 2; });
      Pump();
      Pump();

      object? caret = window.FocusManager.GetFocusedElement();
      bool on_a_tile_of_this_shelf = false;
      if (caret is ListBoxItem)
      {
        for (Visual? at = caret as Visual; at != null; at = at.GetVisualParent())
        {
          if (ReferenceEquals(at, titles))
            on_a_tile_of_this_shelf = true;
        }
      }

      Assert.That(on_a_tile_of_this_shelf, Is.True,
          "the rebuilt shelf wears its caret ON ONE OF ITS OWN TILES - the list alone holds no frame and "
          + "leaves the viewer with a closed rail and a wall that stands on nobody");
      window.Close();
    }

    /// <summary>
    /// The same pick as above, but with a server that takes its time - which is how a viewer meets it in the
    /// living room. The rail goes away the moment its row is pressed while the shelf is still wearing the old
    /// titles, so the caret is put on a tile of THAT shelf, and the rebuild then throws the tile away under it.
    /// A screen that asks for its caret once, at the moment the rail left, therefore ends up with a caret on a
    /// container that is no longer on the screen: the wall looks idle and the viewer stands on nothing.
    /// </summary>
    [AvaloniaTest]
    public void PickingAFilterWhileTheServerIsStillAnsweringStandsTheCaretOnATile()
    {
      SeenServer server = new SeenServer();
      server.DelayMs = 250;
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      titles.SelectedIndex = 1;
      Pump();

      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });

      MediaBrowserMenuEntry filter_page = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label.StartsWith("Filter", StringComparison.Ordinal));
      filter_page.Command.Execute(null);
      WaitUntil("the filter page to stand up", delegate { return browser.MediaMenuHeading == "Filter"; });

      MediaBrowserMenuEntry watched = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label == "All");
      watched.Command.Execute(null);

      // The shelf is emptied the same breath; the answer from the server takes a quarter of a second, and the
      // rail only follows when the new question is asked. During the waiting the caret can only stand on the
      // empty list - which wears no frame, and is why the tiles must be asked for again the moment they arrive
      // rather than only once when the rail left.
      WaitUntil("the shelf to be emptied for the new question", delegate { return browser.Titles.Count == 0; });

      WaitUntil("the filtered shelf to arrive", delegate { return browser.Titles.Count == 2; });
      WaitUntil("the rail to go away with the pick", delegate { return !browser.MediaMenuVisible; });
      Pump();
      Pump();

      object? caret = window.FocusManager.GetFocusedElement();
      bool on_a_tile_of_the_new_shelf = false;
      if (caret is ListBoxItem)
      {
        for (Visual? at = caret as Visual; at != null; at = at.GetVisualParent())
        {
          if (ReferenceEquals(at, titles))
            on_a_tile_of_the_new_shelf = true;
        }
      }

      Assert.That(on_a_tile_of_the_new_shelf, Is.True,
          "the answer arrived, the tiles it names are laid, and one of them wears the caret - a viewer who "
          + "presses the rail's filter and gets their arrows back must find them on a lit tile");
      window.Close();
    }

    /// <summary>
    /// The same pick again, on a wall big enough for the panel to recycle containers - and the caret is checked
    /// where the viewer's eye is, not merely in the focus manager's books. A caret on a tile the wall has laid
    /// outside the viewport answers "focused" to every question and shows no frame to anyone; the wall must
    /// bring the tile it stands on back INTO the view, not just back onto a container.
    /// </summary>
    [AvaloniaTest]
    public void PickingAFilterOnAWallThatRecyclesStandsTheCaretOnATileInsideTheView()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill beyond title seventy", delegate { return browser.Titles.Count > 70; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      titles.SelectedIndex = 45;
      titles.ScrollIntoView(titles.Items[45]);
      Pump();
      Pump();

      object stood_on = titles.SelectedItem;
      WaitUntil("the tile the test stands on to be one the wall has laid out",
          delegate { return titles.ContainerFromIndex(45) is ListBoxItem; });

      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });

      MediaBrowserMenuEntry filter_page = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label.StartsWith("Filter", StringComparison.Ordinal));
      filter_page.Command.Execute(null);
      WaitUntil("the filter page to stand up", delegate { return browser.MediaMenuHeading == "Filter"; });

      MediaBrowserMenuEntry all = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label == "All");
      int wall_before = browser.Titles.Count;
      all.Command.Execute(null);
      WaitUntil("the rail to go away with the pick", delegate { return !browser.MediaMenuVisible; });
      WaitUntil("the wall to be rebuilt", delegate { return browser.Titles.Count == wall_before; });
      Pump();
      Pump();

      WaitUntil("a tile of the rebuilt wall to wear the caret", delegate
      {
        return window.FocusManager.GetFocusedElement() is ListBoxItem tile
            && titles.ItemFromContainer(tile) != null;
      });

      object? caret = window.FocusManager.GetFocusedElement();
      ListBoxItem stood = (ListBoxItem)caret!;

      ScrollViewer viewport = titles.GetVisualDescendants().OfType<ScrollViewer>().First();

      // The wall travels to its caret with a smoothed offset, and the strip the tile ends up in only settles
      // when the ride is over - the question is asked until the glide has delivered, not one frame into it.
      bool inside_the_view = false;
      Point in_view = default;
      WaitUntil("the wall to glide the caret's tile into the view", delegate
      {
        in_view = stood.TranslatePoint(new Point(), viewport) ?? default;
        inside_the_view = in_view.Y + stood.Bounds.Height > 0 && in_view.Y < viewport.Bounds.Height;
        return inside_the_view;
      });

      Assert.That(inside_the_view, Is.True,
          "the caret's tile lies inside the strip of wall the viewer is looking at - a focus parked on a "
          + "container above or below the view is the same invisible caret the viewer keeps reporting; "
          + "y=" + in_view.Y + " off=" + viewport.Offset.Y + " sel=" + titles.SelectedIndex);
      window.Close();
    }

    /// <summary>
    /// Starts in the carousel so its geometry can be tested without an earlier shape masking a carousel failure.
    /// </summary>
    [AvaloniaTest]
    public void CarouselKeepsItsNeighboursMirrored()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Layout = BrowserLayout.Stacked;
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.SelectedIndex = 30;
      WaitUntil("the selection to reach title thirty", delegate { return titles.SelectedIndex == 30; });
      browser.SwitchView();
      WaitUntil("the carousel to replace the stack", delegate { return browser.Layout == BrowserLayout.Carousel; });
      ScrollViewer viewport = titles.GetVisualDescendants().OfType<ScrollViewer>().First();

      WearAndAskAboutTheRow(window, titles, viewport, browser, "carousel");
      titles.SelectedIndex = 70;
      WaitUntil("the selection to reach title seventy", delegate { return titles.SelectedIndex == 70; });
      WearAndAskAboutTheRow(window, titles, viewport, browser, "carousel");
      window.Close();
    }

    /// <summary>
    /// A shape has to fill the picture it is seen through with covers of its own.  A stack gathers its covers
    /// nearer one another than the panel laid them, so the covers the panel stands up - one picture's width of
    /// them, laid a whole cover apart - would cover only the middle of the television and leave both edges empty:
    /// the viewer walks the shelf and sees the stack fill up only when they reach the end of the wall, which is
    /// where the row is short enough to stop reaching out.  The panel is therefore asked to stand covers up beside
    /// the picture while a shape is worn, and the picture is asked whether it still cuts them off.
    /// </summary>
    [AvaloniaTest]
    public void AStackStandsItsCoversPastBothEdgesOfThePicture()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.SelectedIndex = 30;
      Pump();
      Pump();
      Assert.That(browser.Selected, Is.SameAs(browser.Titles[30]),
          "the list selection is bound to the ViewModel before changing layouts");

      ScrollViewer viewport = titles.GetVisualDescendants().OfType<ScrollViewer>().First();

      Assert.That(viewport.GetVisualDescendants().OfType<VirtualizingStackPanel>().Any(), Is.True,
          "the covers are laid in a panel that can stand covers up beside the picture");

      // The walk from rows over the wall and the covers to the first shape of them, and on to the second.
      browser.SwitchView();
      WaitUntil("the grid to keep the stood-on title", delegate { return titles.SelectedIndex == 30; });
      Assert.That(titles.SelectedIndex, Is.EqualTo(30),
          "changing to grid keeps the stood-on title (ViewModel index "
          + browser.Titles.IndexOf(browser.Selected!) + ")");
      Assert.That(browser.Selected, Is.SameAs(browser.Titles[30]), "the selection binding stays connected");
      browser.SwitchView();
      WaitUntil("the covers to keep the stood-on title", delegate { return titles.SelectedIndex == 30; });
      Assert.That(titles.SelectedIndex, Is.EqualTo(30),
          "changing to covers keeps the stood-on title (ViewModel index "
          + browser.Titles.IndexOf(browser.Selected!) + ")");
      Assert.That(browser.Selected, Is.SameAs(browser.Titles[30]), "the selection binding stays connected");
      browser.SwitchView();
      WaitUntil("the stack to keep the stood-on title", delegate { return titles.SelectedIndex == 30; });
      Assert.That(titles.SelectedIndex, Is.EqualTo(30),
          "changing to stacked keeps the stood-on title (ViewModel index "
          + browser.Titles.IndexOf(browser.Selected!) + ")");
      Assert.That(browser.Selected, Is.SameAs(browser.Titles[30]), "the selection binding stays connected");
      Assert.That(browser.Layout, Is.EqualTo(BrowserLayout.Stacked));
      WearAndAskAboutTheRow(window, titles, viewport, browser, "stack");
      browser.SwitchView();
      WaitUntil("the carousel to keep the stood-on title", delegate { return titles.SelectedIndex == 30; });
      Assert.That(titles.SelectedIndex, Is.EqualTo(30),
          "changing to carousel keeps the stood-on title (ViewModel index "
          + browser.Titles.IndexOf(browser.Selected!) + ")");
      Assert.That(browser.Selected, Is.SameAs(browser.Titles[30]), "the selection binding stays connected");
      Assert.That(browser.Layout, Is.EqualTo(BrowserLayout.Carousel));

      Guid title_past_sixty = browser.Titles[70].Title.Id;
      titles.SelectedIndex = 70;
      WaitUntil("a picture request past the sixtieth title",
          delegate { return server.AskedForPicture(title_past_sixty); });
      Assert.That(server.AskedForPicture(title_past_sixty), Is.True,
          "the realized cover row past title sixty is still offered its picture after the scroll offset grows");

      titles.SelectedIndex = 30;
      WaitUntil("the selection to return to title thirty", delegate { return titles.SelectedIndex == 30; });
      WearAndAskAboutTheRow(window, titles, viewport, browser, "carousel");

      // And a shelf walked plainly again is asked to stop: covers standing beside the picture of a list or a grid
      // are covers nobody looks at, and a wall that leans out of its window while it is walked vertically paints
      // rows over the whole screen.
      browser.SwitchView();
      Assert.That(browser.Layout, Is.EqualTo(BrowserLayout.List));
      Pump();
      Pump();
      Pump();

      VirtualizingStackPanel? naked = viewport.GetVisualDescendants().OfType<VirtualizingStackPanel>()
          .FirstOrDefault();
      if (naked != null)
        Assert.That(naked.CacheLength, Is.EqualTo(0.0),
            "a shelf walked in rows is asked to stand up no cover beside the picture");

      window.Close();
    }

    /// <summary>
    /// Asks a shelf wearing a shape the two questions the viewer asks it: does a cover of the shape reach past
    /// both edges of the picture, and does the cover being stood on stand in the middle of the picture?  Both are
    /// answered from the panel's own coordinates, which is where a dressed cover stands before the wall's travel
    /// is taken off it - <c>TransformToVisual</c> answers nonsense as soon as a perspective stands in the chain.
    /// </summary>
    private static void WearAndAskAboutTheRow(ShellWindow window, ListBox titles, ScrollViewer viewport,
        MediaBrowserViewModel browser, string shape)
    {
      Pump();
      Pump();
      Pump();

      VirtualizingStackPanel panel =
          viewport.GetVisualDescendants().OfType<VirtualizingStackPanel>().First();

      // How many trees the list is standing in right now.  A shelf that has just been put on can leave the panel
      // of the layout it came from standing beside the new one, and a shape that dresses the one while the screen
      // shows the other is a shape nobody can see - which is what a viewer sees as covers that never fill in.
      string trees = string.Join(" / ", viewport.GetVisualDescendants().OfType<VirtualizingStackPanel>()
          .Select((p, i) => "panel" + i + " cache=" + p.CacheLength + " bounds=" + p.Bounds
              + " alive=" + p.IsVisible));

      Assert.That(panel.CacheLength, Is.GreaterThan(0.0),
          "a gathered row is walked off covers the panel laid further out than the picture reaches, so the panel "
          + "is asked to stand them up while a shape is worn");

      Assert.That(
          viewport.GetVisualDescendants()
              .OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>().First().ClipToBounds,
          Is.False,
          "a cover standing taller than the slot the panel laid it in is not sawn off at the row's edges");
      Assert.That(viewport.ClipToBounds, Is.False,
          "the scroll viewer does not clip the 3D covers at the sides or floor after its presenter's clip is opened");

      double picture = viewport.Viewport.Width;

      // First the ride, then the question.  A layout that has just been put on has to grow its scroller before the
      // wall can be carried anywhere, so the wall is watched until it has STOOD STILL - a few samples in a row
      // saying the same place - and not merely until two samples agree before the ride has started.
      DateTimeOffset rest = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(8);
      double before = viewport.Offset.X;
      DateTimeOffset? standing_still_since = null;
      while (DateTimeOffset.UtcNow < rest
          && (standing_still_since == null
              || DateTimeOffset.UtcNow - standing_still_since < TimeSpan.FromMilliseconds(450)))
      {
        Pump();
        if (Math.Abs(viewport.Offset.X - before) < 0.5)
          standing_still_since ??= DateTimeOffset.UtcNow;
        else
          standing_still_since = null;

        before = viewport.Offset.X;
      }

      for (int settled = 0; settled < 3; ++settled)
        Pump();

      double left = double.MaxValue;
      double right = double.MinValue;
      double centre_of_the_stood_cover = double.NaN;
      string seen = "nothing";
      int stood_index = titles.SelectedIndex;
      int counted = 0;

      // Where the covers are PAINTED, read the way a viewer sees them: every cover's middle is translated into the
      // scroller's own space, which carries the wall's travel, the panel's place and every transform the shape wore
      // into the number.  Measuring out of a container's Bounds instead was measured to be wrong - a recycled
      // container answers with the Bounds of the place it came from (one stood at 118534 in its Bounds while it was
      // drawn at 1662), which made this test report a wall standing far off the picture while it was centred.
      for (int place_of_row = stood_index - 40; place_of_row <= stood_index + 40; ++place_of_row)
      {
        if (place_of_row < 0 || place_of_row >= titles.Items.Count)
          continue;

        if (titles.ContainerFromIndex(place_of_row) is not Control tile || tile.Bounds.Width <= 0.0)
          continue;

        Point? middle = tile.TranslatePoint(
            new Point(tile.Bounds.Width / 2.0, tile.Bounds.Height / 2.0), viewport);
        if (middle == null)
          continue;

        if (tile.RenderTransform is TransformGroup worn)
          seen = worn.ToString() ?? "no transform";

        double stood_at = middle.Value.X;
        double half_of_the_tile = tile.Bounds.Width / 2.0;
        if (tile.RenderTransform is TransformGroup shaped)
        {
          // A shape that shrinks a cover as it stands further off the middle paints a narrower cover than the one
          // it laid out, so the edge a viewer sees is the projected one, not the laid half width.
          Point? edge = tile.TranslatePoint(new Point(tile.Bounds.Width, 0.0), viewport);
          if (edge != null)
            half_of_the_tile = Math.Abs(edge.Value.X - stood_at);
          _ = shaped;
        }

        left = Math.Min(left, stood_at - half_of_the_tile);
        right = Math.Max(right, stood_at + half_of_the_tile);
        ++counted;

        if (place_of_row == stood_index)
          centre_of_the_stood_cover = stood_at;
      }

      string read = counted + " covers answered to an index, the leftmost standing at " + left
          + " and the rightmost ending at " + right + " of a picture running from 0 to " + picture
          + " (cache " + panel.CacheLength + ", extent " + viewport.Extent.Width + ", offset "
          + viewport.Offset.X + ", the panel trees: " + trees + ")"
          + "; the cover stood on stands at " + centre_of_the_stood_cover + " wearing " + seen;
      if (shape == "carousel")
      {
        Control? selected_tile = titles.ContainerFromIndex(stood_index) as Control;
        Point? selected_at = selected_tile?.TranslatePoint(new Point(), viewport);
        read += "; laid containers: " + string.Join(", ", titles.GetRealizedContainers()
            .OfType<Control>()
            .Select(tile => "i" + titles.IndexFromContainer(tile) + " x" + tile.Bounds.X
                + " w" + tile.Bounds.Width + " m" + tile.Margin.Left + "/" + tile.Margin.Right))
            + "; selected bounds=" + selected_tile?.Bounds + " translated=" + selected_at
            + " items=" + titles.Items.Count + " selected=" + stood_index;
      }
      Assert.That(counted, Is.GreaterThan(2), "the shape is worn over covers the list knows " + read);
      Assert.That(right - left, Is.LessThan(picture * 6.0),
          "a row of covers is as wide as the covers in it, not as wide as the whole wall - a row dressed while "
          + "its own measurements were still being changed has been seen standing eighty thousand pixels off the "
          + "picture, which is a shelf that shows nothing " + read);
      Assert.That(left, Is.LessThan(0.0), "a " + shape + " reaches past the left edge of the picture - " + read);
      Assert.That(right, Is.GreaterThan(picture), "a " + shape + " reaches past its right edge - " + read);

      if (shape == "carousel" && stood_index > 0 && stood_index + 1 < titles.Items.Count
          && titles.ContainerFromIndex(stood_index - 1) is Control left_neighbour
          && titles.ContainerFromIndex(stood_index + 1) is Control right_neighbour
          && left_neighbour.RenderTransform is TransformGroup left_group
          && right_neighbour.RenderTransform is TransformGroup right_group)
      {
        // What the viewer compares has never been the numbers written into a transform but the cover that comes out
        // of it, so both neighbours are projected here the way the compositor projects them: their four corners are
        // put through the whole transform the tile wears, and the points coming back are measured.  A turn whose
        // perspective is worked out wrongly for one sign of the angle survives every assertion on written values
        // and is caught by nothing else - it was measured at 143 px against 185 px for two covers written alike.
        Point[] left_seen = ProjectCorners(left_neighbour, left_group);
        Point[] right_seen = ProjectCorners(right_neighbour, right_group);

        double left_width = left_seen.Max(p => p.X) - left_seen.Min(p => p.X);
        double right_width = right_seen.Max(p => p.X) - right_seen.Min(p => p.X);
        Assert.That(left_width, Is.EqualTo(right_width).Within(1.0),
            "two covers standing equally far from the stood one are SEEN equally wide, not merely written so - "
            + CornersRead(left_seen, right_seen));

        // The vertical edge a neighbour shows the stood cover is the one nearer the viewer, so it must stand taller
        // than the edge turned away - on both sides, and by the same amount on both sides.
        double left_facing = Math.Abs(left_seen[3].Y - left_seen[1].Y);
        double left_away = Math.Abs(left_seen[2].Y - left_seen[0].Y);
        double right_facing = Math.Abs(right_seen[2].Y - right_seen[0].Y);
        double right_away = Math.Abs(right_seen[3].Y - right_seen[1].Y);
        Assert.That(left_facing, Is.GreaterThan(left_away),
            "the left neighbour turns its focus-facing edge toward the viewer - " + CornersRead(left_seen, right_seen));
        Assert.That(right_facing, Is.GreaterThan(right_away),
            "the right neighbour turns its focus-facing edge toward the viewer - " + CornersRead(left_seen, right_seen));
        Assert.That(left_facing, Is.EqualTo(right_facing).Within(1.0),
            "the two neighbours face the stood cover with equally tall edges - " + CornersRead(left_seen, right_seen));
        Assert.That(left_away, Is.EqualTo(right_away).Within(1.0),
            "the two neighbours turn away from the stood cover with equally short edges - "
            + CornersRead(left_seen, right_seen));

        // And the lean carries both of them the same little step toward the middle of the picture, which is what
        // closes a ring instead of leaving a hole where the stood cover stands.
        double left_drift = (left_seen.Max(p => p.X) + left_seen.Min(p => p.X)) / 2.0 - left_neighbour.Bounds.Width / 2.0;
        double right_drift = (right_seen.Max(p => p.X) + right_seen.Min(p => p.X)) / 2.0
            - right_neighbour.Bounds.Width / 2.0;
        Assert.That(left_drift, Is.EqualTo(-right_drift).Within(1.0),
            "the two neighbours lean toward the middle of the picture by equally much - "
            + CornersRead(left_seen, right_seen));

        ScaleTransform left_measure = left_group.Children.OfType<ScaleTransform>().Single();
        ScaleTransform right_measure = right_group.Children.OfType<ScaleTransform>().Single();
        Assert.That(left_measure.ScaleX, Is.EqualTo(right_measure.ScaleX).Within(0.001),
            "carousel neighbours at equal distances stand equally wide");
        Assert.That(left_measure.ScaleY, Is.EqualTo(right_measure.ScaleY).Within(0.001),
            "carousel neighbours at equal distances stand equally tall");
        Assert.That(left_neighbour.DataContext, Is.SameAs(titles.Items[stood_index - 1]),
            "recycled left containers wear the item at their current index");
        Assert.That(right_neighbour.DataContext, Is.SameAs(titles.Items[stood_index + 1]),
            "recycled right containers wear the item at their current index");
      }

      // The wall carries the stood cover to the middle of the picture and stops there; the row's own dressing must
      // not pull it away again, which is the re-centring the viewer felt as a jerk after every key.
      Assert.That(double.IsNaN(centre_of_the_stood_cover), Is.False,
          "the cover the shelf stands on is one the panel stood up while the shape was worn - " + read);
      // A carousel is built around the middle of the picture, so its stood cover answers to that middle: a stood
      // cover standing a hundred points to one side is a row that reads bigger on the other side, which the looser
      // band of the stacked shape has to keep allowing for.
      double within_reach_of_the_middle = shape == "carousel" ? 60.0 : picture / 4.0;
      Assert.That(Math.Abs(centre_of_the_stood_cover - picture / 2.0), Is.LessThan(within_reach_of_the_middle),
          "the cover being stood on answers to the middle of the picture rather than to one of its edges - " + read);
    }

    /// <summary>
    /// The four corners of a tile as the shape it wears puts them: the tile's own box, corner by corner, through
    /// the matrix the compositor draws with.  This is what a cover looks like, and it is not what was written.
    /// </summary>
    private static Point[] ProjectCorners(Control tile, Transform worn)
    {
      Matrix through = worn.Value;
      double width = tile.Bounds.Width;
      double height = tile.Bounds.Height;
      Point[] laid = { new Point(0.0, 0.0), new Point(width, 0.0), new Point(0.0, height), new Point(width, height) };
      return laid.Select(corner => through.Transform(corner)).ToArray();
    }

    /// <summary>How the two projected neighbours read, for the mouth of a failing assertion.</summary>
    private static string CornersRead(Point[] left, Point[] right)
        => "left corners " + string.Join(" ", left.Select(p => "(" + p.X.ToString("F1") + "|" + p.Y.ToString("F1") + ")"))
        + " against right corners " + string.Join(" ",
            right.Select(p => "(" + p.X.ToString("F1") + "|" + p.Y.ToString("F1") + ")"));

    /// <summary>
    /// The walls of the rail. A key that points past a wall - left past the rail's side, up past its head,
    /// down past its foot - is answered with a nod of the whole rail toward that wall and a spring back; the
    /// menu stays open, the caret stays on its row, and the viewer learns the wall is a wall rather than a
    /// stuck key.
    /// </summary>
    [AvaloniaTest]
    public void PointingPastAWallOfTheRailNodsTheRailAndKeepsEverythingWhereItIs()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      titles.SelectedIndex = 40;
      SendKey(titles, Key.Left);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });
      int rows = rail.Items.Count;

      // The side toward the room's edge: nothing stands left of the rail.  The nod's frames ride the render
      // timer, which the headless harness never turns - the very timer StandCaret asks for its caret on - so
      // what a headless test can prove is the decision: the key reaches the rail's ground and is answered
      // there, the menu stays open, and the caret stays put.  That the ground wears a nod transform afterwards
      // is the witness that it was a nod and not a swallow.
      Panel side_ground = window.GetVisualDescendants().OfType<Panel>()
          .First(panel => panel.Name == "sideMenu");
      SendKey(rail, Key.Left);
      Pump();
      Assert.That(side_ground.RenderTransform, Is.InstanceOf<TranslateTransform>(),
          "the wall key is answered by a nod of the rail's ground");
      Assert.That(browser.MediaMenuVisible, Is.True, "a wall does not put the menu away");
      Assert.That(rail.SelectedIndex, Is.EqualTo(0), "and a wall does not walk the caret off its row");

      // The head: up on the first row is a wall as well, and the caret keeps its row.
      SendKey(rail, Key.Up);
      Pump();
      Assert.That(browser.MediaMenuVisible, Is.True, "the head of the rail is a wall, not an exit");
      Assert.That(rail.SelectedIndex, Is.EqualTo(0), "and the head of the rail keeps its row");

      // The foot: down on the last row is the third wall.
      rail.SelectedIndex = rows - 1;
      if (rail.ContainerFromIndex(rows - 1) is ListBoxItem last_row)
        last_row.Focus();
      Pump();
      SendKey(rail, Key.Down);
      Pump();
      Assert.That(browser.MediaMenuVisible, Is.True, "the foot of the rail is a wall, not an exit");
      Assert.That(rail.SelectedIndex, Is.EqualTo(rows - 1), "and the foot of the rail keeps its row");

      // And a key with a road under it is still just walking: down on a row that is not the last one moves
      // the caret instead of stopping at a wall.
      rail.SelectedIndex = 0;
      if (rail.ContainerFromIndex(0) is ListBoxItem first_row)
        first_row.Focus();
      Pump();
      SendKey(rail, Key.Down);
      Pump();
      Assert.That(rail.SelectedIndex, Is.EqualTo(1), "a key with a row under it walks rather than nods");
      window.Close();
    }

    /// <summary>
    /// Climbing down into a page of the menu - over the "View" row onto the shapes - and back with the back key
    /// keeps a caret standing: the new page wears the frame on its first row, and the way back hands the frame
    /// to the row the viewer climbed down from.  A page is built by replacing every row of the rail underneath
    /// the caret, and a rail rebuilt under a caret that is not replaced stands dark - which is what the viewer
    /// sees as "no tile focused".
    /// </summary>
    [AvaloniaTest]
    public void ClimbingDownAndBackUpTheMenuPagesKeepsTheCaretStanding()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      titles.SelectedIndex = 0;
      Pump();
      SendKey(titles, Key.Left);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });

      // The row that opens the shapes page, walked to the way a viewer walks it.
      int view_row = -1;
      for (int i = 0; i < rail.Items.Count; i++)
      {
        if (((MediaBrowserMenuEntry) rail.Items[i]!).Label.StartsWith("View",
            StringComparison.OrdinalIgnoreCase))
        {
          view_row = i;
          break;
        }
      }

      Assert.That(view_row, Is.GreaterThan(0), "the commands carry a row that opens the shapes page");
      rail.SelectedIndex = view_row;
      Pump();
      SendKey(rail, Key.Enter);

      WaitUntil("the shapes page to stand up", delegate { return browser.MediaMenuHeading == "View"; });
      Assert.That(browser.MediaMenuVisible, Is.True, "climbing down keeps the menu open");
      WaitUntil("the page to stand on its first row", delegate { return rail.SelectedIndex == 0; });
      Assert.That(rail.IsKeyboardFocusWithin, Is.True, "and the caret stands inside the rail");

      // Back: the commands come back, and the caret travels back to the row the viewer climbed down from -
      // the same row, not merely some row.
      SendKey(rail, Key.Escape);
      WaitUntil("the commands to stand up again", delegate { return browser.MediaMenuHeading == "Menu"; });
      Assert.That(browser.MediaMenuVisible, Is.True, "back from a page keeps the menu open");
      WaitUntil("the caret back on the row the viewer climbed down from",
          delegate { return rail.SelectedIndex == view_row; });
      window.Close();
    }

    /// <summary>
    /// A wall walked fast is still standing on a tile.  What the viewer sees when this breaks is the frame vanishing
    /// while the key is held down: the caret is nowhere, so nothing wears the frame, and it only comes back when
    /// the walk stops.  The wall recycles the containers it walks past, and the focused container is one of them -
    /// so the promise this tests is the panel's hardest one, that the container holding the caret is never taken
    /// down while the wall travels.
    /// </summary>
    [AvaloniaTest]
    public void AWallWalkedFastStillStandsOnAFocusedTile()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      Pump();
      titles.SelectedIndex = 0;
      Pump();

      // Eight steps down, and the caret is asked after EVERY one of them before anything is allowed to lay out:
      // what the viewer sees is the frame missing while the wall is still travelling, so that instant is the one
      // that has to be answered.  Asking only after the walk settles hides the defect, because by then the wall
      // has caught up with the caret and stood a container under it again.
      for (int step = 1; step <= 8; step++)
      {
        titles.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Down,
          Source = titles
        });

        object? mid_walk = window.FocusManager.GetFocusedElement();
        string why = "step " + step + " of a fast walk left the caret on a tile; a caret that fell back to the "
            + "list wears no frame, which is the frame the viewer sees vanish while holding the key down";
        Assert.That(mid_walk, Is.InstanceOf<ListBoxItem>(), why);
      }

      object? caret = window.FocusManager.GetFocusedElement();
      Assert.That(caret, Is.InstanceOf<ListBoxItem>(),
          "the caret is still on a tile after a fast walk; a caret that fell back to the list wears no frame");

      Control stood = (Control)caret!;
      Assert.That(stood.GetVisualParent(), Is.Not.Null, "the tile wearing the frame is on the screen");
      Assert.That(titles.ContainerFromIndex(titles.SelectedIndex), Is.SameAs(stood),
          "and it is the tile the selection stands on");
      window.Close();
    }

    /// <summary>
    /// A wall walked fast and LEFT TO SETTLE still wears its frame.  The walk above asks after the caret between
    /// the steps; this one lets the wall actually travel - frames are pumped while the glide carries the offset,
    /// so rows fall out of the band and are built again underneath the walk, which is the hour in which the live
    /// screen was seen dropping the frame for good: the wall came to rest, no tile stood lit, and the caret had
    /// fallen back onto the list itself.  The promise is therefore the settled one - when the travel ends, the
    /// frame must still stand on the tile the selection stands on.
    /// </summary>
    [AvaloniaTest]
    public void AWallWalkedFastAndLeftToSettleStillWearsItsFrame()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ScrollViewer scroller = titles.GetVisualDescendants().OfType<ScrollViewer>().First();
      titles.Focus();
      Pump();
      titles.SelectedIndex = 0;
      Pump();

      // Twelve steps down, and between every one of them the wall is allowed to draw frames, the way a held key
      // lets it on a real machine.  The glide is a transition on the scroller's offset; only pumping lets it run.
      for (int step = 1; step <= 12; step++)
      {
        titles.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Down,
          Source = titles
        });

        for (int frame = 0; frame < 8; frame++)
          Pump();
      }

      // Let the travel end the way the viewer waits it out: the offset stops moving.
      Vector last = new Vector(-1, -1);
      WaitUntil("the glide to come to rest", delegate
      {
        Vector now = scroller.Offset;
        bool still_now = now == last;
        last = now;
        return still_now;
      });

      object? caret = window.FocusManager.GetFocusedElement();
      Assert.That(caret, Is.InstanceOf<ListBoxItem>(),
          "the wall came to rest and the caret is still on a tile; on the live screen it fell back to the list, "
          + "which wears no frame, and every tile went dark at once");
      Assert.That(titles.ContainerFromIndex(titles.SelectedIndex), Is.SameAs(caret),
          "and the tile wearing the frame is the tile the selection stands on");
      window.Close();
    }

    /// <summary>
    /// A walk keeps the watching of the shelf caret awake, which is the promise the live screen was seen
    /// without: a walk along the wall, a beat where the caret lands somewhere that wears no frame - on the
    /// live screen the screen's own rail, standing its full two hundred points off the left edge, where it sat
    /// for seven and a half seconds while nothing anywhere wore a frame - and nobody came to pick the caret
    /// up, because the watching that exists for exactly this had long since retired.  It wakes when the screen
    /// hands the caret to the shelf, it counts four quiet seconds of a still-standing tile and then lies down,
    /// and a walk - the one hour in which containers fall and carets get knocked - never once reached its
    /// ear.  A fallen caret cannot be dealt to the headless wall (its focus manager hands every such knock
    /// straight back, which is also why no headless test ever saw this bug), so what is pinned here is the
    /// waking itself: every step of the walk has to wake the watching anew, which leaves the mark of a fresh
    /// timer behind, and a step that walks past a watching nobody woke leaves that mark missing.
    /// </summary>
    [AvaloniaTest]
    public void AWalkKeepsTheWatchingOfTheShelfCaretAwake()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      titles.Focus();
      Pump();
      titles.SelectedIndex = 0;
      Pump();
      WaitUntil("the caret to stand on a tile",
          delegate { return window.FocusManager.GetFocusedElement() is ListBoxItem item
              && titles.ItemFromContainer(item) != null; });
      WaitUntil("the watching to wake with the caret on the shelf",
          delegate { return view.IsWatchingShelfCaret; });

      // What every step owes the watching is a fresh wake - the same promise the landing makes, renewed at
      // every step, so that whenever the watching finally lies down after its quiet seconds it was woken by
      // the LAST step and is still awake a fall could be caught in.  Whether a woken watching lies down
      // cannot be watched headlessly (the headless focus manager keeps handing carets back and every such
      // hand-back rewinds the quiet seconds), but the wake itself leaves a mark: each wake builds a new
      // timer, so a step that woke the watching can be told from a step that walked past it asleep.
      object? timer_before_step = ShelfWatchTimer(view);

      for (int step = 1; step <= 3; step++)
      {
        titles.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Down,
          Source = titles
        });
        for (int frame = 0; frame < 8; frame++)
          Pump();
        object? timer_after_step = ShelfWatchTimer(view);
        Assert.That(view.IsWatchingShelfCaret, Is.True,
            "step " + step + " of the walk left no watching standing at all");
        Assert.That(timer_after_step, Is.Not.SameAs(timer_before_step),
            "step " + step + " of the walk woke no fresh watching; a watching that is never woken by a step "
            + "has long lain down by the time a fast walk knocks its caret - which is the seven-and-a-half-"
            + "second frame the live screen lost");
        timer_before_step = timer_after_step;
      }
      window.Close();
    }

    /// <summary>
    /// The timer the shelf's caret watching currently runs on, which is a NEW object every time the watching
    /// is woken.  Read from outside only so that a test can tell a step that woke the watching from one that
    /// walked past it asleep.
    /// </summary>
    private static object? ShelfWatchTimer(MediaBrowserView view)
    {
      return typeof(MediaBrowserView)
          .GetField("_shelfTileTimer", System.Reflection.BindingFlags.NonPublic
              | System.Reflection.BindingFlags.Instance)!
          .GetValue(view);
    }

    /// <summary>
    /// The shelf fades where its wall has left a row out of the picture, and nowhere it has not.  Standing at its
    /// head the wall has left nothing above itself, so the first row reaches the top edge whole; the rest of the
    /// wall still stands below the window, so the foot dissolves.  Walked to its foot it is the other way round,
    /// and halfway down both ends fade.  This is the reference's own answer, which is to say no answer at all -
    /// <c>MediaStyles.xaml:5205</c>, <c>:5229</c> and <c>:5253</c> give their three media shapes a scroll margin
    /// and no mask whatever, and the one mask the home content wears (<c>HomeContentStyles.xaml:1868</c>) is black
    /// at every stop over a presenter wearing a solid black <c>DummyOpacityMask</c>, which hides nothing.  The
    /// blanket blend that used to be worn here faded a first row that had never been scrolled by anybody, which is
    /// exactly what it was blamed for.
    /// </summary>
    [AvaloniaTest]
    public void TheShelfFadesOnlyWhereTheWallHasLeftThePicture()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the whole wall to be read", delegate { return !browser.HasMore && browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ScrollViewer scroller = titles.GetVisualDescendants().OfType<ScrollViewer>().First();
      WaitUntil("the wall to be longer than its window",
          delegate { return scroller.Extent.Height > scroller.Viewport.Height + 1; });

      double bottom = scroller.Extent.Height - scroller.Viewport.Height;

      // How deeply an end of the wall is fading, read off the mask it wears: nought is an end whose row stands
      // whole against the edge of the screen, one is an end with a whole fade zone of wall standing outside the
      // picture.  An end with half a fade zone out of it is on the ladder in between.
      double Depth(bool head)
      {
        Assert.That(scroller.OpacityMask, Is.InstanceOf<LinearGradientBrush>(),
            "this wall has titles standing outside the picture at one end, so it wears its fade");
        LinearGradientBrush mask = (LinearGradientBrush)scroller.OpacityMask!;
        return 1.0 - mask.GradientStops[head ? 0 : 3].Color.A / 255.0;
      }

      void StandAt(double offset)
      {
        // The wheel answers a scroll question on the spot; the glide is a keyboard-and-time thing (see
        // ARollingWallKeepsItsFadeAndOnlyItsDepthFollowsTheOffset for how a travelling wall is tested).
        scroller.Offset = new Vector(scroller.Offset.X, offset);
        WaitUntil("the wall to stand at " + offset,
            delegate { return Math.Abs(scroller.Offset.Y - offset) < 1.0; });
        Pump();
      }

      // The head of an untouched wall: nothing has been walked out of the picture, so the head fades nothing.
      StandAt(0);
      Assert.That(Depth(true), Is.EqualTo(0),
          "the first row has never been scrolled out of the picture, so the head of the wall wears no fade");
      Assert.That(Depth(false), Is.EqualTo(1),
          "and the rows that still stand below the window are faded, which is the wall saying it goes on");

      StandAt(bottom / 2);
      Assert.That(Depth(true), Is.EqualTo(1), "rows have gone out of the picture above the wall's middle");
      Assert.That(Depth(false), Is.EqualTo(1), "and rows are waiting out of it below");

      StandAt(bottom);
      Assert.That(Depth(false), Is.EqualTo(0),
          "the wall has run out below, so its last row stands whole against the bottom edge");
      Assert.That(Depth(true), Is.EqualTo(1), "while its head stands deep out of the picture above");

      // And an end only a hand's breadth from the picture is faded a hand's breadth, not a whole strip at once:
      // half the fade zone of wall out of the picture is half the fade.
      StandAt(bottom - 0.06 * scroller.Viewport.Height);
      Assert.That(Depth(false), Is.EqualTo(0.5).Within(0.01),
          "the fade follows how much of the wall is out of the picture instead of arriving all at once");
      window.Close();
    }

    /// <summary>
    /// A wall shorter than the window it stands in has left nothing out of the picture at either end, and wears no
    /// mask at all - the mask is not taken off because it is in the way, it was never put on.
    /// </summary>
    [AvaloniaTest]
    public void AShelfShorterThanItsWindowWearsNoFade()
    {
      LongShelf server = new LongShelf();
      server.Total = 2;
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the shelf to hold its two titles", delegate { return browser.Titles.Count == 2; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ScrollViewer scroller = titles.GetVisualDescendants().OfType<ScrollViewer>().First();
      WaitUntil("the shelf to be laid out", delegate { return scroller.Viewport.Height > 0; });
      Pump();

      Assert.That(scroller.Extent.Height, Is.LessThanOrEqualTo(scroller.Viewport.Height),
          "two rows fit on this screen, which is the whole point of the question - the window is "
          + scroller.Viewport.Height + " tall and the wall is " + scroller.Extent.Height);
      Assert.That(scroller.OpacityMask, Is.Null,
          "a wall that stands wholly inside its window is faded at neither end");
      window.Close();
    }

    /// <summary>
    /// The cover wall is walked sideways, so the covers it leaves out of the picture stand out at its left and
    /// right hands and never above or below.  Its fade therefore runs along the axis the wall runs along; a mask
    /// drawn from top to bottom would dim the tops of covers that were never scrolled by anybody.
    /// </summary>
    [AvaloniaTest]
    public void AWallThatRunsSidewaysFadesAlongItsOwnAxis()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the whole wall to be read", delegate { return !browser.HasMore && browser.Titles.Count > 60; });

      browser.Layout = BrowserLayout.Cover;
      WaitUntil("the covers to be laid out", delegate { return browser.IsCoverLayout; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ScrollViewer scroller = titles.GetVisualDescendants().OfType<ScrollViewer>().First();
      WaitUntil("the cover row to be wider than its window",
          delegate { return scroller.Extent.Width > scroller.Viewport.Width + 1; });
      WaitUntil("the resting wall to take its fade back up",
          delegate { return scroller.OpacityMask is LinearGradientBrush; });

      Assert.That(scroller.Offset.X, Is.EqualTo(0), "the cover wall starts at its first cover");
      Assert.That(scroller.OpacityMask, Is.InstanceOf<LinearGradientBrush>(),
          "covers stand out of the picture to the right, so this wall wears its fade");
      LinearGradientBrush mask = (LinearGradientBrush)scroller.OpacityMask!;
      Assert.That(mask.StartPoint.Point.X, Is.Not.EqualTo(mask.EndPoint.Point.X),
          "the fade runs from side to side, because that is the way this wall travels");
      Assert.That(mask.StartPoint.Point.Y, Is.EqualTo(mask.EndPoint.Point.Y), "and not from top to bottom");
      Assert.That(mask.GradientStops[0].Color.A, Is.EqualTo((byte)255),
          "the first cover has not been walked out of the picture, so the left hand hides nothing");
      Assert.That(mask.GradientStops[3].Color.A, Is.EqualTo((byte)0),
          "while covers are still waiting to the right out of the picture");

      double furthest = scroller.Extent.Width - scroller.Viewport.Width;
      scroller.Offset = new Vector(furthest, scroller.Offset.Y);
      WaitUntil("the cover wall to stand at its last cover",
          delegate { return Math.Abs(scroller.Offset.X - furthest) < 1.0; });
      WaitUntil("the resting wall to take its fade back up",
          delegate { return scroller.OpacityMask is LinearGradientBrush; });

      mask = (LinearGradientBrush)scroller.OpacityMask!;
      Assert.That(mask.GradientStops[3].Color.A, Is.EqualTo((byte)255),
          "the wall has run out to the right, so its last cover stands whole against the edge");
      Assert.That(mask.GradientStops[0].Color.A, Is.EqualTo((byte)0),
          "and its first covers are deep out of the picture to the left now");
      window.Close();
    }

    /// <summary>
    /// A rolling wall keeps its fade.  An earlier answer here stowed the mask for the duration of a roll and
    /// handed it back a settle's wait later - and the viewer saw exactly that: a fade popping on a wall that
    /// had just stopped, a flicker at both edges after every scroll, worse than any mask cost.  The wall
    /// therefore wears its fade the way it wears its shadows - always, like the reference's baked dressing -
    /// and only the DEPTH of each end answers the scroll: nothing walked out of the picture at an end means
    /// that end stands fully opaque, a fade zone's worth out of it means the full fade, and a wheel notch
    /// walks between the two inside the running glide rather than through a bare-masked detour.
    /// </summary>
    [AvaloniaTest]
    public void ARollingWallKeepsItsFadeAndOnlyItsDepthFollowsTheOffset()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the whole wall to be read", delegate { return !browser.HasMore && browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ScrollViewer scroller = titles.GetVisualDescendants().OfType<ScrollViewer>().First();
      WaitUntil("the wall to be longer than its window",
          delegate { return scroller.Extent.Height > scroller.Viewport.Height + 1; });

      // At the very top nothing has walked out of the picture above: the head of the mask stands fully opaque,
      // while the far end below still fades because rows wait out of the picture there.
      Assert.That(scroller.Offset.Y, Is.LessThan(1.0), "the walk starts at the top");
      Assert.That(scroller.OpacityMask, Is.InstanceOf<LinearGradientBrush>(),
          "the wall already wears its fade before any hand moves it - there is no putting-on later");
      LinearGradientBrush atTop = (LinearGradientBrush)scroller.OpacityMask!;
      Assert.That(atTop.GradientStops[0].Color.A, Is.EqualTo((byte)255),
          "the first row has never been scrolled out of the picture, so the head fades nothing");

      // Which row of the shelf stands at or below a height of the wall - the tile a viewer would stand on to
      // bring the wall that far up.  Rows are found through the containers the list has laid, in order.
      static int FirstRowAtOrBelow(ScrollViewer wall, ListBox shelf, double height)
      {
        double best = double.MaxValue;
        int row = 0;
        int index = 0;
        foreach (Control? container in shelf.GetRealizedContainers())
        {
          Point? at = container?.TranslatePoint(new Point(), wall);
          if (at.HasValue && at.Value.Y >= height - 1 && at.Value.Y < best)
          {
            best = at.Value.Y;
            row = index;
          }

          index++;
        }

        return row;
      }

      // Move the wall into the middle of itself the way a viewer does - by standing on a title in its middle,
      // which is what the walk to the caret answers (see AWallWalkedFastStillStandsOnAFocusedTile).  Asking the
      // wall to stand somewhere by writing its offset is NOT such a travel: a scroller wearing a glide keeps
      // the old offset until the animation has had a clock to run on, and a test clock runs no frames.
      double middle = (scroller.Extent.Height - scroller.Viewport.Height) / 2;
      titles.Focus();
      Pump();
      titles.SelectedIndex = 0;
      Pump();
      int row = FirstRowAtOrBelow(scroller, titles, middle);
      for (int step = 0; step <= row; step++)
      {
        titles.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Down,
          Source = titles
        });
      }

      Pump();
      if (scroller.Offset.Y < middle / 2)
      {
        // Headless never let the wall travel - no frame came for the glide to move on.  A wheel answer - the
        // offset's own writing, without a glide worn - is just as good a movement for what this wall law says
        // about its mask, so take the glide off the scroller and write the place directly.
        scroller.Transitions = null;
        scroller.Offset = new Vector(scroller.Offset.X, middle);
      }

      Assert.That(scroller.OpacityMask, Is.InstanceOf<LinearGradientBrush>(),
          "the wall that just moved still wears its fade - a fade that blinked off and on is the flicker "
          + "the viewer sent this back for");

      // The head's fade follows its rows out of the picture - by depth, on every step of the way.
      WaitUntil("the head to fade as its rows walk out of the picture",
          delegate
          {
            return scroller.OpacityMask is LinearGradientBrush moving
                   && moving.GradientStops[0].Color.A < 255;
          });
      LinearGradientBrush mask = (LinearGradientBrush)scroller.OpacityMask!;
      Assert.That(mask.GradientStops[3].Color.A, Is.LessThan((byte)255),
          "and below it too");

      // And walking back to the top takes the head's fade off again by depth, not by a timer: an end that
      // hides nothing writes itself opaque again, with no blink between.
      scroller.Offset = new Vector(scroller.Offset.X, 0.0);
      WaitUntil("the head of the wall to fade nothing again",
          delegate
          {
            return scroller.OpacityMask is LinearGradientBrush back
                   && back.GradientStops[0].Color.A == 255;
          });
      window.Close();
    }

    /// <summary>
    /// The shelf wears the wall's tile. A viewer who has seen the home screen's covers knows what a title looks
    /// like on this skin, and the library's tiles are the same objects seen a metre closer - so they are built of
    /// the same parts and answer the walking the same way. What the shelf used to wear instead was a picture of a
    /// frame: the skin's empty poster frame lay under every tile, and where a title had no artwork that frame was
    /// the whole of the tile, which read as a coloured box rather than as a film.
    /// </summary>
    [AvaloniaTest]
    public void TheShelfWearsTheWallsTileRatherThanAPictureOfAFrame()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        LongShelf server = new LongShelf();
        MediaBrowserViewModel browser = BrowserOn(server);
        browser.Layout = BrowserLayout.Grid;
        window = new ShellWindow(new MediaBrowserView(browser));
        window.Show();
        WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

        ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
            .First(list => list.Name == "titlesList");
        Assert.That(titles.Classes.Contains("nereus-shelf-tiles"), Is.True,
            "the shelf of tiles tells its list that the tiles dress themselves");

        ListBoxItem? standing = titles.ContainerFromIndex(0) as ListBoxItem;
        Assert.That(standing, Is.Not.Null, "the first row of a shown shelf is laid out");

        foreach (string part in new[] { "home-tile-shadow", "home-tile-face", "home-tile-art", "home-tile-edge" })
          Assert.That(standing!.GetVisualDescendants().OfType<Control>()
              .Any(control => control.Classes.Contains(part)), Is.True,
              "a shelf tile is built of the wall's " + part);

        // No name lies over the picture.  The reference's tile prints its title only where the library keeps no
        // picture to print (MediaStyles.xaml:3295, IsVisible="!thumbnail.HasImage"), and a band of words across
        // the foot of a poster is a band of words across the poster.
        Assert.That(standing!.GetVisualDescendants().OfType<Control>()
            .Any(control => control.Classes.Contains("home-tile-caption")), Is.False,
            "a shelf tile says its name with its picture, not over it");

        // The container is the thing that used to paint the accent colour under a selected tile, and a coloured
        // box under a plate that is already a plate is a box with a tile floating in it.
        titles.SelectedIndex = 0;
        Pump();
        Assert.That(standing.Background, Is.EqualTo(null).Or.EqualTo(Brushes.Transparent),
            "the row a viewer stands on paints no selection box behind its own tile");

        // Rows are the other reading, and there the panel is how a list says which row it means.
        browser.Layout = BrowserLayout.List;
        WaitUntil("the rows to be laid out", delegate { return !titles.Classes.Contains("nereus-shelf-tiles"); });
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    /// <summary>
    /// Every row of the screen's menu carries its picture the moment the rail stands open.  The rows used to
    /// wait for a key press: the list's virtualizing panel - measured against the collapsed height a closing
    /// animation leaves behind while the room slides - realized the first row alone, and the rows below it
    /// stood as empty grey plates, which on a menu read as a menu of one command.  Eight rows is no reason
    /// to virtualize anything, and a menu whose commands hide until touched is the failure a viewer sees.
    /// </summary>
    [AvaloniaTest]
    public void EveryRowOfTheScreensMenuCarriesItsPictureWhileItStandsOpen()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        LongShelf server = new LongShelf();
        MediaBrowserViewModel browser = BrowserOn(server);
        window = new ShellWindow(new MediaBrowserView(browser));
        window.Show();
        WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

        ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
            .First(list => list.Name == "titlesList");
        ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
            .First(list => list.Name == "mediaMenuList");

        titles.Focus();
        browser.ShowMediaMenuCommand.Execute(null);
        WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
        WaitUntil("the rows of the menu to be there", delegate { return rail.Items.Count > 3; });
        Pump();

        Assert.That(rail.Items.Count, Is.GreaterThan(3), "the rail opened on a page of commands");
        for (int i = 0; i < rail.Items.Count; i++)
        {
          ListBoxItem? container = rail.ContainerFromIndex(i) as ListBoxItem;
          Assert.That(container, Is.Not.Null,
              "row " + i + " of the rail is laid out while the rail stands open, not only after a key wakes it");
          MediaBrowserMenuEntry? entry = container!.DataContext as MediaBrowserMenuEntry;
          Assert.That(entry, Is.Not.Null, "row " + entry!.Label + " knows the command it carries");

          // The row wears the shortcut's tile: a shape out of the skin's vectors where the command has an
          // icon, and its name standing in the band whether or not anybody stands on the row.
          List<Avalonia.Controls.Shapes.Path> shapes = container.GetVisualDescendants()
              .OfType<Avalonia.Controls.Shapes.Path>()
              .Where(shape => shape.Classes.Contains("media-menu-icon"))
              .ToList();
          Assert.That(shapes.Count, Is.EqualTo(1), "row '" + entry.Label + "' carries one command shape");
          if (entry.Icon.Length > 0)
            Assert.That(shapes[0].Data, Is.Not.Null,
                "the command '" + entry.Label + "' shows its own shape '" + entry.Icon + "'");

          TextBlock? name = container.GetVisualDescendants().OfType<TextBlock>()
              .FirstOrDefault(text => text.Text == entry.Label);
          Assert.That(name, Is.Not.Null, "row '" + entry.Label + "' says its name while it waits");
          Assert.That(name!.IsVisible, Is.True, "and nobody has to stand on the row to read it");
        }
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    /// <summary>
    /// The title's dialog lays its answers on a sheet: a plate one shade lighter than the panel, with a tighter
    /// corner than the panel itself, and every answer stretched across the sheet's width - the shape the viewer
    /// drew and signed off.  Before the sheet the rows lay shoulder to shoulder in the panel and each one measured
    /// only its own word, so the dialog read as a scatter of labels rather than as a question with answers; a
    /// dialog whose answers float at word-width inside a wide panel is the regression this holds shut.  The back
    /// answer keeps its word's width and stands under the sheet rather than among the answers, because it steps
    /// the dialog away instead of answering it.
    /// </summary>
    [AvaloniaTest]
    public void TheTitleDialogLaysItsAnswersStretchedOnASheetInsideThePanel()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");
        WornSkin worn = SkinEngine.Worn(app)!;

        LongShelf server = new LongShelf();
        MediaBrowserViewModel browser = BrowserOn(server);
        window = new ShellWindow(new MediaBrowserView(browser));
        window.Show();
        WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 0; });
        browser.Selected = browser.Titles[0];
        browser.ShowMenuCommand.Execute(null);
        WaitUntil("the title's dialog to stand open", delegate { return browser.MenuVisible; });
        Pump();

        Border plate = window.GetVisualDescendants().OfType<Border>().First(b => b.Name == "menuPlate");
        Border panel = window.GetVisualDescendants().OfType<Border>().First(b => b.Name == "menuDialog");
        ItemsControl answers = window.GetVisualDescendants().OfType<ItemsControl>()
            .First(c => c.Name == "menuActionList");
        Button back = window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "menuBackButton");

        // The dialog opens by growing, and a ruler held against a thing mid-grow measures a smaller thing.  The
        // grow is a plain scale on top of whatever the shell's own fit already scales the screen by, so every
        // distance here is the skin's point times one factor - the dialog's matrix, waited out until it settles
        // and read off the dialog itself instead of assumed.
        double settledOn = -1.0;
        WaitUntil("the dialog to finish growing", delegate
        {
          Matrix? grow = plate.TransformToVisual(window);
          if (!grow.HasValue)
            return false;
          bool settled = Math.Abs(grow.Value.M11 - settledOn) < 0.000001;
          settledOn = grow.Value.M11;
          return settled;
        });
        Pump();
        double scale = plate.TransformToVisual(window)!.Value.M11;
        Assert.That(scale, Is.GreaterThan(0.0), "the dialog stands on the screen through some scale");

        Point TopLeft(Visual visual)
        {
          return visual.TranslatePoint(new Point(0, 0), window) ?? new Point(-1, -1);
        }

        // The sheet is the skin's own shade and corner, brighter than the panel it lies in - which is what makes
        // the answers read as one body, and the panel as the frame around the body.
        ISolidColorBrush? sheet = plate.Background as ISolidColorBrush;
        ISolidColorBrush? ground = panel.Background as ISolidColorBrush;
        Assert.That(sheet, Is.Not.Null.And.Not.SameAs(ground), "the sheet wears its own one colour");
        Assert.That(sheet!.Color.A, Is.EqualTo((byte) 0xFF), "and nothing of the blur shows through it");
        Assert.That(sheet.Color.R, Is.GreaterThan(ground!.Color.R),
            "the sheet is a shade brighter than the panel, not a hole cut into it");

        CornerRadius sheetCorner = (CornerRadius) worn.Resources["DialogPlateCornerRadius"]!;
        CornerRadius frameCorner = (CornerRadius) worn.Resources["DialogFrameCornerRadius"]!;
        Assert.That(sheetCorner.TopLeft, Is.GreaterThan(0.0), "the palette says how round a dialog sheet is");
        Assert.That(frameCorner.TopLeft, Is.GreaterThan(sheetCorner.TopLeft),
            "and its corner is tighter than the panel's, so the nested roundings do not collide");
        Assert.That(plate.CornerRadius, Is.EqualTo(sheetCorner),
            "and the plate in the dialog wears that rounding");

        // The answers fill the sheet from inner edge to inner edge, with the sheet's own air above and below.
        Assert.That(answers.Items.Count, Is.GreaterThanOrEqualTo(2), "a title asks more than one question");
        List<Button> rows = new List<Button>();
        for (int i = 0; i < answers.Items.Count; i++)
        {
          Control? holder = answers.ContainerFromIndex(i) as Control;
          Assert.That(holder, Is.Not.Null, "answer " + i + " is laid out");
          Button? row = holder!.GetVisualDescendants().OfType<Button>().FirstOrDefault();
          Assert.That(row, Is.Not.Null, "answer " + i + " wears its button");
          rows.Add(row!);
        }

        double inner = plate.Bounds.Width - plate.Padding.Left - plate.Padding.Right;
        Assert.That(inner, Is.GreaterThan(0), "the sheet has an inside to fill");
        Point sheetTopLeft = TopLeft(plate);
        foreach (Button row in rows)
        {
          Assert.That(row.Bounds.Width, Is.EqualTo(inner).Within(1.0),
              "the answer '" + row.Content + "' stretches across the whole sheet instead of only its word");
          Assert.That(TopLeft(row).X, Is.EqualTo(sheetTopLeft.X + plate.Padding.Left * scale).Within(1.0),
              "and starts at the sheet's inner edge");
        }

        Assert.That(plate.Padding.Top, Is.GreaterThanOrEqualTo(10.0),
            "the sheet keeps air above the first answer, so the plate's corner reads as a corner");
        Assert.That(plate.Padding.Bottom, Is.GreaterThanOrEqualTo(10.0), "and below the last one");

        for (int i = 0; i + 1 < rows.Count; i++)
        {
          double foot = TopLeft(rows[i]).Y + rows[i].Bounds.Height * scale;
          Assert.That(TopLeft(rows[i + 1]).Y - foot, Is.EqualTo(10.0 * scale).Within(1.0),
              "answers stand " + (i + 1) + " steps apart on the sheet");
        }

        // The way out of the dialog stands under the sheet, in its own row, and keeps its word's width.
        double sheetBottom = sheetTopLeft.Y + plate.Bounds.Height * scale;
        Assert.That(TopLeft(back).Y, Is.GreaterThanOrEqualTo(sheetBottom - 1.0),
            "the way back is not one of the answers - it stands under the sheet");
        Assert.That(back.Bounds.Width, Is.LessThan(rows[0].Bounds.Width),
            "and it keeps its word's width instead of stretching across the dialog like an answer");
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    /// <summary>
    /// A shelf of ROWS wears the same law every other list wears: the screen's white sheet under the row a
    /// viewer stands on and the row's words in the screen's own black.  The reference had dressed this row in
    /// the skin's turquoise (<c>ButtonListViewControlStyle</c> laid the medium of the skin's colour over the
    /// whole row with a bar of its light eight points wide along the left edge, and <c>MediaStyles.xaml</c>
    /// brought the labels up to white); the viewer asked for the turquoise parts to go and the ground left
    /// the row's parts altogether.  What stays forbidden is what never belonged anyway: a platform rectangle
    /// or frame around a row that already wears its sheet.
    /// </summary>
    [AvaloniaTest]
    public void AShelfOfRowsWearsTheWhiteSheetAndNoTurquoiseDress()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        LongShelf server = new LongShelf();
        MediaBrowserViewModel browser = BrowserOn(server);
        browser.Layout = BrowserLayout.List;
        window = new ShellWindow(new MediaBrowserView(browser));
        window.Show();
        WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

        ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
            .First(list => list.Name == "titlesList");
        Assert.That(titles.Classes.Contains("nereus-shelf-rows"), Is.True,
            "the shelf of rows tells its list that the rows dress themselves");

        titles.SelectedIndex = 3;
        Pump();
        ListBoxItem? standing = titles.ContainerFromIndex(3) as ListBoxItem;
        Assert.That(standing, Is.Not.Null, "the row a viewer stands on is laid out");

        Assert.That(standing.Background, Is.EqualTo(null).Or.EqualTo(Brushes.Transparent),
            "the row carries no square of the platform behind its names");
        Assert.That(standing.GetVisualDescendants().OfType<Control>()
                .Any(control => control.Classes.Contains("nereus-row-ground")),
            Is.False, "the turquoise ground and its left bar are gone from the row's parts, not hidden");

        ContentPresenter? sheet = standing.GetVisualDescendants().OfType<ContentPresenter>()
            .FirstOrDefault(cp => cp.Name == "PART_ContentPresenter");
        Assert.That(sheet, Is.Not.Null);
        app.TryFindResource("TextBrush", out object? light);
        ISolidColorBrush? lightBrush = light as ISolidColorBrush;
        Assert.That(lightBrush, Is.Not.Null, "the palette owes the skin a TextBrush");
        ISolidColorBrush? sheetBrush = sheet!.Background as ISolidColorBrush;
        Assert.That(sheetBrush, Is.Not.Null);
        Color sheetColor = sheetBrush!.Color;
        Assert.That(sheetColor.R, Is.EqualTo(lightBrush!.Color.R),
            "the sheet a row stands on is the screen's own light in colour...");
        Assert.That(sheetColor.G, Is.EqualTo(lightBrush.Color.G));
        Assert.That(sheetColor.B, Is.EqualTo(lightBrush.Color.B));
        Assert.That(sheetColor.A, Is.GreaterThan((byte)0).And.LessThan((byte)255),
            " ... but see-through, so a shade of the ground behind the shelf reads faintly through it "
            + "(how faint is the viewer's dial - the law only binds it to be neither painted over nor gone)");

        Assert.That(standing.Foreground, Is.InstanceOf<ISolidColorBrush>(),
            "the row a viewer stands on speaks in one colour");
        app.TryFindResource("ScreenBackgroundBrush", out object? ground);
        ISolidColorBrush? groundBrush = ground as ISolidColorBrush;
        Assert.That(groundBrush, Is.Not.Null, "the palette owes the skin a ScreenBackgroundBrush");
        Assert.That(((ISolidColorBrush)standing.Foreground!).Color, Is.EqualTo(groundBrush!.Color),
            "and its words burn the screen's own black on the sheet - not the reference's white");

        // The row's colour is worth nothing if it never reaches the glyphs: what a viewer reads are the
        // labels inside the row's parts, and they may only learn their colour by inheritance.  A sheet
        // whose presenters do not pass the row's voice down leaves white words on a white sheet, which is
        // the very bug the viewer named twice - so the law asks the labels themselves.
        foreach (TextBlock label in standing.GetVisualDescendants().OfType<TextBlock>()
                     .Where(t => !string.IsNullOrEmpty(t.Text)))
        {
          Assert.That(((label.Foreground as ISolidColorBrush)?.Color).GetValueOrDefault(),
              Is.EqualTo(groundBrush!.Color),
              "the word \"" + label.Text + "\" on the stood-on row is black on the sheet");
          Assert.That(label.FontWeight, Is.EqualTo(Avalonia.Media.FontWeight.Bold),
              "the word \"" + label.Text + "\" stands bold on the sheet");
        }

        // The keyboard's row without a pick: a walked caret has the focus before it has a selection, and
        // the viewer's law says it wears the one effect the mouse wears - sheet and black words alike.
        titles.SelectedIndex = -1;
        ListBoxItem? walked = titles.ContainerFromIndex(5) as ListBoxItem;
        Assert.That(walked, Is.Not.Null);
        ((Control)walked!).Focus();
        Pump();
        ContentPresenter? walkedSheet = walked.GetVisualDescendants().OfType<ContentPresenter>()
            .FirstOrDefault(cp => cp.Name == "PART_ContentPresenter");
        Assert.That(((walkedSheet!.Background as ISolidColorBrush)?.Color).GetValueOrDefault(),
            Is.EqualTo(sheetColor),
            "the row the caret stands on wears the very same see-through sheet");
        Assert.That(((walked.Foreground as ISolidColorBrush)?.Color).GetValueOrDefault(),
            Is.EqualTo(groundBrush!.Color),
            "and its words are black on it too - white on white is not an effect");
        foreach (TextBlock label in walked.GetVisualDescendants().OfType<TextBlock>()
                     .Where(t => !string.IsNullOrEmpty(t.Text)))
        {
          Assert.That(((label.Foreground as ISolidColorBrush)?.Color).GetValueOrDefault(),
              Is.EqualTo(groundBrush!.Color),
              "the word \"" + label.Text + "\" on the caret's row is black on the sheet");
          Assert.That(label.FontWeight, Is.EqualTo(Avalonia.Media.FontWeight.Bold),
              "the word \"" + label.Text + "\" stands bold on the caret's sheet too");
        }

        // The control that keeps the law honest: a row nobody stands on keeps the screen's light.  Were
        // this one black too, the sheet would merely have traded who gets white on white.
        ListBoxItem? resting = titles.GetVisualDescendants().OfType<ListBoxItem>()
            .First(item => !item.IsSelected);
        Assert.That(resting, Is.Not.Null);
        Assert.That(resting.IsSelected, Is.False);
        Assert.That(((resting!.Foreground as ISolidColorBrush)?.Color).GetValueOrDefault(),
            Is.EqualTo(lightBrush!.Color),
            "a row at rest still speaks the screen's light");
        Assert.That(resting.FontWeight, Is.Not.EqualTo(Avalonia.Media.FontWeight.Bold),
            "and it keeps its resting weight - only the stood-on row speaks bold");
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    /// <summary>
    /// The row of a shelf speaks "watched" with the skin's own eye - the one the posters wear in their
    /// corner - and not with a tick of text: the viewer asked it by name ("statt des checkmark-symbol
    /// das watched oder partly watched icon"). A finished row wears the full eye, a begun row the eye
    /// fading in from its left, and a row nobody has opened wears neither; whichever eye stands there
    /// wears the row class the sheet paints black on the white ground.
    /// </summary>
    [AvaloniaTest]
    public void AWatchedRowWearsTheSkinsEyeAndNotATextTick()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        Avalonia.Controls.Templates.IDataTemplate? template = SkinEngine.TryTileTemplate("TitleRowTemplate");
        Assert.That(template, Is.Not.Null, "the worn skin owes the shelf a row to wear");

        //  The skin hands a template whose DataContext arrives by DelayedBinding when the row is attached
        //  under the list; a lone built row has no parent to take it from, so the test feeds it by hand -
        //  exactly what the shelf does for the row when it hangs it up.
        Control Row(MediaTitle title)
        {
          TitleViewModel model = new TitleViewModel(title, false);
          Control row = (Control)template!.Build(model)!;
          row.DataContext = model;
          row.Measure(new Size(400, 60));
          row.Arrange(new Rect(0, 0, 400, 60));
          return row;
        }
        MediaTitle ATitle(string name, int? percentage, int? resume) =>
            new MediaTitle(Guid.NewGuid(), name, null, 2025, null, null, false,
                Resume: resume, Percentage: percentage);

        Control finished = Row(ATitle("Finished", 100, null));
        Control half = Row(ATitle("Half way", 40, 40));
        Control untouched = Row(ATitle("Untouched", null, null));

        Assert.That(StandingEyes(finished).Count(eye => !eye.Classes.Contains("partly")), Is.EqualTo(1),
            "the finished row stands one whole eye, and no half one beside it");
        Assert.That(StandingEyes(half).Count(eye => eye.Classes.Contains("partly")), Is.EqualTo(1),
            "the begun row stands the eye that fades in from its left");
        Assert.That(StandingEyes(untouched), Is.Empty, "a row nobody has opened wears no eye at all");

        Assert.That(StandingEyes(finished).All(eye => eye.Classes.Contains("nereus-row-watched")),
            Is.True, "and the eye on a row wears the class that turns it black on the white sheet");

        //  The class means nothing until the sheet's rules are shown to REACH the eye through the row's
        //  presenter - the words taught that a row's voice does not travel down on its own.  So the law
        //  hangs the eyes on a living shelf and asks the stood-on, the walked-on and the resting row
        //  what colour their eye actually wears.
        TitleViewModel finishedModel = new TitleViewModel(ATitle("Shelf finished", 100, null), false);
        TitleViewModel halfModel = new TitleViewModel(ATitle("Shelf half way", 40, 40), false);
        TitleViewModel restModel = new TitleViewModel(ATitle("Shelf resting", 100, null), false);
        ListBox shelf = new ListBox();
        shelf.Classes.Add("nereus-shelf-rows");
        shelf.ItemTemplate = template!;
        shelf.ItemsSource = new object[] { finishedModel, halfModel, restModel };
        window = new ShellWindow(shelf);
        window.Show();
        Pump();

        app.TryFindResource("ScreenBackgroundBrush", out object? dark);
        ISolidColorBrush? darkBrush = dark as ISolidColorBrush;
        Assert.That(darkBrush, Is.Not.Null, "the palette owes the skin a ScreenBackgroundBrush");
        app.TryFindResource("TextBrush", out object? light);
        ISolidColorBrush? lightBrush = light as ISolidColorBrush;
        Assert.That(lightBrush, Is.Not.Null, "and a TextBrush for the rows at rest");

        shelf.SelectedIndex = 0;
        Pump();
        ListBoxItem chosen = (ListBoxItem)shelf.ContainerFromIndex(0)!;
        Avalonia.Controls.Shapes.Path chosenEye = StandingEyes(chosen).Single();
        Assert.That(((chosenEye.Fill as ISolidColorBrush)?.Color).GetValueOrDefault(),
            Is.EqualTo(darkBrush!.Color),
            "the eye on the chosen row burns the screen's black on the white sheet");

        shelf.SelectedIndex = 1;
        Pump();
        ListBoxItem chosenHalf = (ListBoxItem)shelf.ContainerFromIndex(1)!;
        Avalonia.Controls.Shapes.Path chosenHalfEye = StandingEyes(chosenHalf).Single();
        Assert.That(chosenHalfEye.Fill, Is.InstanceOf<IGradientBrush>(),
            "the half eye on the chosen row keeps its fade, ...");
        Assert.That(((IGradientBrush)chosenHalfEye.Fill!).GradientStops
                .Any(stop => stop.Color == Colors.Black && stop.Color.A == 255),
            Is.True, " ... and fades into the screen's black, not into its light");

        //  The keyboard's row without a pick - the very case the viewer named: walked onto, sheet and all,
        //  and the eye still shining light on light.
        shelf.SelectedIndex = -1;
        Pump();
        ListBoxItem walked = (ListBoxItem)shelf.ContainerFromIndex(0)!;
        ((Control)walked).Focus();
        Pump();
        Avalonia.Controls.Shapes.Path walkedEye = StandingEyes(walked).Single();
        Assert.That(((walkedEye.Fill as ISolidColorBrush)?.Color).GetValueOrDefault(),
            Is.EqualTo(darkBrush!.Color),
            "the eye on the row the caret stands on is black on the sheet too");

        ListBoxItem atRest = (ListBoxItem)shelf.ContainerFromIndex(2)!;
        Avalonia.Controls.Shapes.Path restEye = StandingEyes(atRest).Single();
        Assert.That(((restEye.Fill as ISolidColorBrush)?.Color).GetValueOrDefault(),
            Is.EqualTo(lightBrush!.Color),
            "and a row nobody stands on keeps its eye in the screen's light - the black belongs to the "
            + "sheet's rows only, and to the list view only");
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    /// <summary>The eyes a built row actually shows - the visible ones out of the pair the template holds.</summary>
    private static IEnumerable<Avalonia.Controls.Shapes.Path> StandingEyes(Control row)
    {
      return row.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
          .Where(eye => eye.Classes.Contains("nereus-watched") && eye.IsVisible);
    }

    /// <summary>
    /// A library that opens takes the caret onto its own shelf.  The reference kept it there with
    /// <c>RestoreFocus</c> and <c>SetFocusPrio</c>, and the port had <c>OnScreenActivated</c> ask for it too - but
    /// it asked while the shelf was still empty, because the titles were on their way from the server, and a caret
    /// handed to an unmeasured list is a caret nobody takes.  What a viewer then saw was a library with no tile
    /// standing lit, and the first arrow key fell to the next focusable thing in the room: the screen's own menu,
    /// which is standing two hundred points off the left edge precisely so that nobody sees it.
    /// </summary>
    [AvaloniaTest]
    public void TheShelfTakesTheCaretWhenTheScreenOpens()
    {
      CatalogueServer server = new CatalogueServer();
      MediaBrowserViewModel films = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);
      MediaBrowserView view = new MediaBrowserView(films);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      window.Focus();
      view.OnScreenActivated();
      WaitUntil("the films to arrive", delegate { return films.Titles.Count == 1; });
      Pump();

      Visual? caret = window.FocusManager.GetFocusedElement() as Visual;
      ListBox titles = view.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      Assert.That(StandsInside(caret, view), Is.True,
          "the caret of a library stands inside the library, not on the room around it");
      Assert.That(caret, Is.InstanceOf<ListBoxItem>(), "and it stands on a tile of the shelf");
      Assert.That(StandsInside(caret, titles), Is.True,
          "and the tile it stands on is one the shelf itself laid out");

      window.Close();
    }

    /// <summary>
    /// The shelf glides to the title being stood on, on the reference's own two numbers for its shape.  A shelf
    /// whose offset is written outright walks one row per key press and stops dead at every one of them, and it
    /// nudges a row's height every now and then instead of moving a whole row at a time; the reference answered
    /// both with an <c>AnimatedWrapPanel</c> that keeps the stood title inside a band of four hundred points either
    /// side over four hundred milliseconds (<c>MediaStyles.xaml:5230</c>), a stack of rows with a band of a hundred
    /// and thirty over three hundred (<c>:5205</c>) and a row of covers with two hundred and eighty before and two
    /// hundred and sixty after, also over four hundred (<c>:5253</c>).  Where a band cannot hold its own tile - a
    /// grid's four hundred points on a shelf of a thousand - the tile is stood at the band's middle, which is what
    /// makes a wall answer every step with the same glide.
    /// </summary>
    [AvaloniaTest]
    public void TheShelfGlidesToTheStoodTitleOnTheReferencesNumbers()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Layout = BrowserLayout.Grid;
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      window.Focus();
      view.OnScreenActivated();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = view.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ScrollViewer scroller = titles.GetVisualDescendants().OfType<ScrollViewer>().First();
      // The scroller wears the glide permanently now; the wait stands in for the dress being put on at Loaded
      // priority after the skin's template arrived.  The twentieth tile stands deep enough into the fifth row
      // of a grid that no band or middle rule can leave the wall where it is, and asking it to travel is how
      // the test finds the scroller the wall actually runs on.
      titles.SelectedIndex = 20;
      WaitUntil("the glide to stand on the scroller",
          delegate
          {
            return scroller.Transitions != null && scroller.Transitions.Count > 0;
          });

      ITransition glide = scroller.Transitions![0];
      Assert.That(glide, Is.InstanceOf<VectorTransition>(), "the offset the shelf travels by is animated");
      Assert.That(((VectorTransition)glide).Property, Is.EqualTo(ScrollViewer.OffsetProperty));
      Assert.That(((VectorTransition)glide).Duration, Is.EqualTo(TimeSpan.FromMilliseconds(400)),
          "a grid walks its wall in the reference's four hundred milliseconds");

      // Stand on the deepest tile the shelf has laid out: the wall is then asked to stand that tile at the middle
      // of its band, which is the same as saying its centre meets the middle of the shelf's own viewport.  The
      // tile is taken from what the shelf actually realized rather than from a number, because a tile deep enough
      // to be out of the viewport has no place to stand at yet - the wall has to move before it exists.
      int standing = -1;
      for (int i = 0; i < browser.Titles.Count; i++)
      {
        if (titles.ContainerFromIndex(i) != null)
          standing = i;
      }

      Assert.That(standing, Is.GreaterThan(0), "the shelf laid out more than the tile it opened on");
      Control? stood = titles.ContainerFromIndex(standing) as Control;
      Assert.That(stood, Is.Not.Null, "the tile stood on is laid out, or there is nothing to stand");

      // Where that tile stands in the wall itself, before anybody moved: the shelf may still scroll a step of its
      // own to answer the selection, which moves the tile and the offset by the same number of points and leaves
      // this one answer untouched.
      Point before = stood!.TranslatePoint(new Point(), scroller) ?? new Point();
      double stoodAt = scroller.Offset.Y + before.Y;
      double middle = stoodAt + stood.Bounds.Height / 2 - scroller.Viewport.Height / 2;
      double furthest = Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height);
      titles.SelectedIndex = standing;
      WaitUntil("the wall to be taken to the stood tile",
          delegate { return Math.Abs(view.ShelfGlideOffset - Math.Clamp(middle, 0, furthest)) < 1.0; });
      Assert.That(view.ShelfGlideOffset, Is.GreaterThan(0.0),
          "a wall deep in the shelf moves, and moves the whole way rather than a row at a time");

      // And back to the first title, where the band's middle lies before the shelf begins: the wall is walked back
      // to its beginning and no further.
      titles.SelectedIndex = 0;
      WaitUntil("the wall to be walked back to its beginning",
          delegate { return view.ShelfGlideOffset == 0.0; });

      // Rows are read a different way: the reference gave them a band of a hundred and thirty points and a shorter
      // walk, so that a shelf of rows only starts moving when the standing row reaches the band's edge.  Asking
      // the rows to travel is how the test sees the length of their walk on the retemplated scroller.
      browser.Layout = BrowserLayout.List;
      Pump();
      titles.SelectedIndex = titles.SelectedIndex == 1 ? 2 : 1;
      WaitUntil("the rows to be walked at three hundred milliseconds",
          delegate
          {
            ScrollViewer? again = titles.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            return again?.Transitions != null && again.Transitions!.Count > 0
                && ((VectorTransition)again.Transitions![0]).Duration == TimeSpan.FromMilliseconds(300);
          });

      window.Close();
    }

    /// <summary>
    /// A library lays the chrome header over its own head.  The reference hung its fanart over all three bands of
    /// its media master - head, client area, floor (<c>master_bare_media.xaml:57-59</c>, a <c>FanArtBackground</c>
    /// with RowSpan 3) - so its clock and its screen name stood on the picture and the picture went to the very
    /// edge of the television.  A header kept in a row above the screen does the opposite: the screen's fanart
    /// stops where its row starts and a black bar lies over the head of the screen, which is what a viewer of a
    /// 21:9 panel sees as the picture not filling the screen.  The home already asks the shell to float its header;
    /// so does a library now, and hands it back on the way out for whatever screen plans its space under a header.
    /// </summary>
    [AvaloniaTest]
    public void ALibraryLaysTheChromeHeaderOverItsOwnHead()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      view.OnScreenActivated();
      Pump();

      ContentControl chrome = window.GetVisualDescendants().OfType<ContentControl>()
          .First(control => control.Name == "ChromeHeader");
      Assert.That(Grid.GetRow(chrome), Is.EqualTo(1),
          "the header stands in the screens' row, so the screen's fanart reaches the top edge of the window");
      Assert.That(chrome.IsHitTestVisible, Is.False,
          "and a header that only decorates a screen never steals its clicks");

      window.Close();

      // A screen that never asked for the floating header finds its head band back where it was: the header is
      // floated by the screen that wants it and by nobody else, so a screen that plans its space under a header row
      // is not left with its top cut off.
      ShellWindow bare = new ShellWindow(new ContentControl());
      bare.Show();
      Pump();
      ContentControl rest = bare.GetVisualDescendants().OfType<ContentControl>()
          .First(control => control.Name == "ChromeHeader");
      Assert.That(Grid.GetRow(rest), Is.EqualTo(0),
          "with no screen asking, the header stands in a row of its own above the screens again");
      bare.Close();
    }

    /// <summary>Whether a control stands in the visual subtree of another, however deep.</summary>
    private static bool StandsInside(Visual? element, Visual ancestor)
    {
      for (Visual? at = element; at != null; at = at.GetVisualParent())
      {
        if (ReferenceEquals(at, ancestor))
          return true;
      }

      return false;
    }

    /// <summary>
    /// The tile a viewer stands on lights the wall's ring, zooms its picture and throws its shadow at full weight,
    /// exactly as the cover beside the shortcut glasses on the home screen does.  <c>MediaStyles.xaml</c> knew no
    /// second voice for a tile that is being walked on: <c>home-tile-edge</c> is a ghost of light at rest and the
    /// whole of it under a viewer, the artwork steps a tenth nearer, and the tile's own opacity comes up from
    /// .86 to 1.  A shelf whose tiles stay dressed as though nobody were standing on them is the wall's tile with
    /// its answer taken away, and the viewer cannot see where they are.
    /// </summary>
    [AvaloniaTest]
    public void ATileOfTheShelfLightsTheWallsRingWhenAViewerStandsOnIt()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        LongShelf server = new LongShelf();
        MediaBrowserViewModel browser = BrowserOn(server);
        browser.Layout = BrowserLayout.Grid;
        window = new ShellWindow(new MediaBrowserView(browser));
        window.Show();
        WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

        ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
            .First(list => list.Name == "titlesList");
        ListBoxItem? standing = titles.ContainerFromIndex(0) as ListBoxItem;
        Assert.That(standing, Is.Not.Null, "the tile a viewer stands on is laid out");

        Border edge = (Border)standing!.GetVisualDescendants().OfType<Control>()
            .First(control => control.Classes.Contains("home-tile-edge"));
        Image art = standing.GetVisualDescendants().OfType<Image>()
            .First(image => image.Classes.Contains("home-tile-art"));

        // At rest a tile is the wall's tile: the ring is a fifth of its light and the picture stands where it is.
        TestContext.Out.WriteLine($"rest edge={Describe(edge.BorderBrush)} transform={art.RenderTransform}");

        titles.SelectedIndex = 0;
        window.Focus();
        Assert.That(standing.Focus(), Is.True, "the tile a viewer walks onto takes the keyboard");
        Pump();
        Assert.That(standing.IsFocused, Is.True, "and keeps it");
        Assert.That(standing.Classes.Contains(":focus") || standing.IsFocused, Is.True);

        // And the ring stands at full light WITHOUT being waited for: a ring that has to be faded up is a ring
        // that is missing while it fades, which is exactly what a viewer walking two tiles in one breath sees.
        Pump();
        Assert.That(IsFullLightRing(edge.BorderBrush), Is.True,
            "the ring of the tile a viewer stands on is the wall's full light");
        // The picture does not jump; it walks a tenth nearer over half a second, so the reading waits for it.
        WaitUntil("the picture of the tile a viewer stands on to step a tenth nearer",
            delegate { return TransformScale(art.RenderTransform) > 1.09; });
        Assert.That(TransformScale(art.RenderTransform), Is.EqualTo(1.1).Within(0.01),
            "and its picture steps a tenth nearer, as the wall's covers do");
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    private static string Describe(IBrush? brush)
    {
      if (brush is LinearGradientBrush gradient && gradient.GradientStops.Count > 0)
      {
        return "gradient[" + string.Join(",",
            gradient.GradientStops.Select(stop => stop.Color.ToString())) + "]";
      }
      return brush?.ToString() ?? "null";
    }

    private static bool IsFullLightRing(IBrush? brush)
    {
      return brush is LinearGradientBrush gradient
          && gradient.GradientStops.Count > 0
          && gradient.GradientStops[0].Color == Colors.White;
    }

    private static double TransformScale(ITransform? transform)
    {
      if (transform is Avalonia.Media.Transformation.TransformOperations)
      {
        Matrix m = ((ITransform)transform).Value;
        return Math.Sqrt(m.M11 * m.M11 + m.M12 * m.M12);
      }
      if (transform is TransformGroup group)
      {
        double best = 1;
        foreach (ITransform child in group.Children)
          best = Math.Max(best, TransformScale(child));
        return best;
      }
      if (transform is ScaleTransform scale)
        return scale.ScaleX;
      if (transform is MatrixTransform matrix)
        return Math.Sqrt(matrix.Value.M11 * matrix.Value.M11 + matrix.Value.M12 * matrix.Value.M12);
      return 1;
    }

    /// <summary>
    /// The plot of the highlighted title stands beside the shelf, and never along its foot.  The reference knew a
    /// strip of details at the foot for the tile views whose side panel had been switched off
    /// (<c>BottomDetails_Template</c>), and the port had made that strip the normal case - so every library screen
    /// spoke underneath its shelf while <c>SimpleShowItems.xaml</c> laid its details in a column of a thousand
    /// points at the shelf's right hand.  The strip is gone; the column answers for grid and list, the cover view
    /// answers above its posters, and the screen has no third place for details left in it.
    /// </summary>
    [AvaloniaTest]
    public void TheDetailsStandBesideTheShelfAndNeverAlongItsFoot()
    {
      CatalogueServer server = new CatalogueServer();
      MediaBrowserViewModel films = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);
      MediaBrowserView view = new MediaBrowserView(films);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the films to arrive", delegate { return films.Titles.Count == 1; });

      Assert.That(films.ShowDetails, Is.True,
          "a shelf opens wearing its details, because the column beside it is where they belong");
      Assert.That(films.ShowsSideDetails, Is.True, "and the column beside the shelf is what it wears");

      Border[] panels = window.GetVisualDescendants().OfType<Border>()
          .Where(border => border.Name == "detailsPanel").ToArray();
      Assert.That(panels.Length, Is.EqualTo(1), "the screen has one column of details");
      Assert.That(panels[0].IsVisible, Is.True, "and it stands while the details are on");

      Assert.That(window.GetVisualDescendants().OfType<Control>()
          .Any(control => control.Name == "homeDetailsPanel"), Is.False,
          "nothing speaks along the foot of the shelf any more");

      films.Layout = BrowserLayout.Cover;
      Assert.That(films.ShowsCoverDetails, Is.True, "the covers wear their plot above the posters");
      Assert.That(films.ShowsSideDetails, Is.False, "and no column beside them");

      films.Layout = BrowserLayout.Grid;
      films.ShowDetails = false;
      Assert.That(panels[0].IsVisible, Is.False,
          "the viewer who puts the details away gets the shelf whole, with nothing at its foot instead");
      window.Close();
    }

    /// <summary>The folder the built frontend's sources were copied from, which is where the shipped skins are.</summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestMediaBrowser).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      return value?.Value ?? throw new InvalidOperationException("no " + name + " in the test assembly's metadata");
    }

    /// <summary>Presses a key into the control that really has the focus, the way a remote does.</summary>
    private static void SendKey(Control source, Key key)
    {
      source.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = source
      });
      Pump();
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
    /// <summary>
    /// Whether this request is the screen asking for a title's picture rather than for anything this fixture is
    /// about.
    /// </summary>
    /// <remarks>
    /// A shelf read now brings a page of picture requests with it, which is what a shelf does on a real server.
    /// The fakes below answer them with "no picture", for two reasons: the libraries they stand in for have none,
    /// and a fake that answered every URL with its titles JSON would be recorded as having been asked about
    /// titles, which would make a test that reads the question back from the server read a picture request
    /// instead.
    /// </remarks>
    private static bool IsPictureRequest(HttpRequestMessage request)
    {
      // Both of the server's picture doors answer here: the media item's own thumbnail and the fanart cache's
      // poster/banner/thumbnail, which is what a tile of those kinds wears.
      string path = request.RequestUri!.AbsolutePath;
      return path.EndsWith("/thumbnail", StringComparison.Ordinal)
          || path.Contains("/fanart", StringComparison.Ordinal);
    }

    private static HttpResponseMessage NoPicture(HttpRequestMessage request)
    {
      return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
    }

    private class DetailsServer : HttpMessageHandler
    {
      private readonly HttpStatusCode _code;
      private readonly string _details;
      private readonly string _row;
      private readonly string _tracks;
      private readonly string _persons;

      internal DetailsServer(HttpStatusCode code, string details)
          : this(code, details, null, null)
      {
      }

      /// <param name="row">
      /// The one row of the shelf above the details, as a server would answer it. Tests that ask about a row of
      /// some other kind - a season, whose row says something no film's row says - name their own.
      /// </param>
      internal DetailsServer(HttpStatusCode code, string details, string row)
          : this(code, details, row, null)
      {
      }

      /// <param name="tracks">
      /// What the file of this title carries: the answer of the tracks question. A test that asks about the tracks
      /// band names its own answer; the rest are served three empty lists, which is what a server that never
      /// looked inside the file answers.
      /// </param>
      internal DetailsServer(HttpStatusCode code, string details, string row, string tracks)
          : this(code, details, row, tracks, null)
      {
      }

      /// <param name="persons">
      /// The people of this title, as the server answers them. A test that asks about the two people bands names
      /// its own answer; the rest are served two empty groups, which is what a library that never named anybody
      /// for this title answers - and what leaves both bands away, so that a test about the tracks is not suddenly
      /// also a test about the people.
      /// </param>
      internal DetailsServer(HttpStatusCode code, string details, string row, string tracks, string persons)
      {
        _code = code;
        _details = details;
        _row = row ?? "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997}";
        _tracks = tracks ?? "{\"id\":\"" + TITANIC + "\",\"videos\":[],\"audios\":[],\"subtitles\":[]}";
        _persons = persons ?? "{\"id\":\"" + TITANIC + "\",\"groups\":[{\"role\":\"actor\",\"persons\":[]},"
            + "{\"role\":\"director\",\"persons\":[]}]}";
      }

      internal int TimesAsked { get; private set; }

      /// <summary>
      /// How long the answers about the title itself are held back. A test that asks what a reading does while
      /// the facts of a title are still travelling needs a server that is slow on purpose.
      /// </summary>
      internal int DelayMs { get; set; }

      /// <summary>
      /// An answer on its way, or already there when nothing was asked to wait.
      /// </summary>
      private Task<HttpResponseMessage> Later(HttpRequestMessage request, HttpStatusCode code, string body)
      {
        if (DelayMs <= 0)
          return Task.FromResult(Answer(request, code, body));

        return Task.Delay(DelayMs).ContinueWith(delegate { return Answer(request, code, body); });
      }

      /// <summary>How often the tracks question came in, so a test can wait for the answer it triggers.</summary>
      internal int TracksAsked { get; private set; }

      /// <summary>
      /// The one kind of lettering this server has a picture of, or <c>null</c> (which is what it answers by) when
      /// its libraries hold no lettering at all.  A test about the writing a record wears names the kind its library
      /// happens to keep and reads back which kinds the screen asked for, in which order.
      /// </summary>
      internal string? Lettering { get; set; }

      /// <summary>
      /// The kind of picture this server holds as a title's front picture, which is the kind the tiles wear and the
      /// details panel paints.  A test about the cover names it (<c>Poster</c>); every other test is served the
      /// refusal it was always served, and stands with an empty cover frame as before.
      /// </summary>
      internal string? Cover { get; set; }

      /// <summary>
      /// The kind of picture this server holds as the disc an album was pressed on, which the reference laid behind
      /// the cover of its album screen (<c>MediaItemListAudioAlbumDetails.inc:41-44</c>).  Nine of the household's
      /// forty three albums answer that question; a test about the disc names the kind, and a library that holds no
      /// disc answers with the refusal every other test was served before.
      /// </summary>
      internal string? Disc { get; set; }

      /// <summary>Every kind of picture the screen asked for, in the order the questions came in.</summary>
      internal List<string> LetteringAsked { get; } = new List<string>();

      /// <summary>How often the people question came in, for the same reason.</summary>
      internal int PeopleAsked { get; private set; }

      /// <summary>
      /// The whole query of the people question that last came in, so that a test can hear which roles the screen
      /// asked for.  A band that stands empty cannot be told apart from a band asked for under a name the library
      /// does not file its people under, and only the question shows the difference.
      /// </summary>
      internal string RolesAsked { get; private set; } = "";

      /// <summary>
      /// A picture for every kind this server holds - the writing, the front picture, the disc - and no picture for
      /// every other question about a picture, which is what this fixture answered before any test asked about one
      /// of them by name.
      /// </summary>
      private HttpResponseMessage PictureAnswer(HttpRequestMessage request)
      {
        string path = request.RequestUri!.AbsolutePath;
        string kind = path.Substring(path.LastIndexOf('/') + 1);
        lock (LetteringAsked)
          LetteringAsked.Add(kind);

        if (kind != Lettering && kind != Cover && kind != Disc)
          return NoPicture(request);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          // The bytes of the smallest whole picture there is: the test asks which name the screen called the
          // lettering by and what it did with the answer, and never how the pixels look.
          Content = new ByteArrayContent(Convert.FromBase64String(
              "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="))
        };
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (IsPictureRequest(request))
          return Task.FromResult(PictureAnswer(request));

        // Asked right after the details, about the same title, and with an answer of its own.
        if (request.RequestUri!.AbsolutePath.EndsWith("/streams"))
        {
          TracksAsked++;
          return Later(request, HttpStatusCode.OK, _tracks);
        }

        // Asked right after the tracks, about the same title, and with an answer of its own.
        if (request.RequestUri!.AbsolutePath.EndsWith("/persons"))
        {
          PeopleAsked++;
          RolesAsked = request.RequestUri!.Query;
          return Later(request, HttpStatusCode.OK, _persons);
        }

        if (request.RequestUri!.AbsolutePath.EndsWith("api/media/items/" + TITANIC))
        {
          TimesAsked++;
          if (DelayMs > 0)
            return Later(request, _code, _details);

          return Task.FromResult(new HttpResponseMessage(_code)
          {
            RequestMessage = request,
            Content = new StringContent(_details, Encoding.UTF8, "application/json")
          });
        }

        return Task.FromResult(Answer(request, HttpStatusCode.OK,
            "{\"total\":1,\"offset\":0,\"limit\":60,\"returned\":1,\"sort\":\"title\",\"titles\":"
            + "[" + _row + "]}"));
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

      // The shelf selects its first row as it fills, and a screen that wears its details reads the selected
      // row as the selection lands - nobody has to ask a screen to show what it is there to show.
      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsHeading, Is.EqualTo("Titanic"),
          "the heading says the name without the year; the year stands in the facts row below");
      Assert.That(browser.DetailsTagline, Is.EqualTo("Nothing on Earth could come between them."),
          "the reference detail screens put the studio's sentence under the title, and the server has always "
          + "carried it - a details panel that drops it is reading the library with half an eye");
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
    public void TheAgeCertificationSwitchCommandsTheDetailsLine()
    {
      // EnableAgeCertificationLogos hung the certification logo on the poster in the reference. This
      // machine's skins ship no logo pictures, so the certification stands in words - and the same switch
      // says yes or no to the same fact.
      string previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          Path.Combine(Path.GetTempPath(), "mp2-age-" + Guid.NewGuid().ToString("N")));
      string wasTheme = "";
      try
      {
        Application app = Application.Current!;
        wasTheme = FrontendTheme.CurrentName(app);
        FrontendTheme.Apply(app, "nereus", out _);

        string details =
            "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997,\"certification\":\"PG-13\"}";
        DetailsServer server = new DetailsServer(HttpStatusCode.OK, details);
        MediaBrowserViewModel browser = BrowserOn(server);
        browser.Selected = new TitleViewModel(
            new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

        WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
        Assert.That(browser.DetailsLine, Does.Contain("PG-13"),
            "the switch ships on, and the age the film carries is said");

        NereusSkinSettings off = new NereusSkinSettings();
        off.EnableAgeCertificationLogos = false;
        off.Save(null, out _);

        // A screen that reads the same title now - the switch is asked when the words are made, and a panel
        // that has already made its words for this title keeps them.
        MediaBrowserViewModel later = BrowserOn(server);
        later.Selected = new TitleViewModel(
            new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));
        WaitUntil("the second reading to land", delegate { return later.DetailsVisible; });
        Assert.That(later.DetailsLine, Does.Not.Contain("PG-13"),
            "off takes the age away - the whole of what the switch ever governed");
      }
      finally
      {
        if (wasTheme.Length > 0)
          FrontendTheme.Apply(Application.Current!, wasTheme, out _);
        Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", previousDir.Length == 0 ? null : previousDir);
      }
    }

    [AvaloniaTest]
    public void TheWordsFromFileReadTheWayTheReferenceReadsThem()
    {
      // A server whose numbers for this title came from the metadata files next to the media - the genres, the
      // first aired date and the crowd's rating are exactly the fields the reference's details screen draws.
      string details =
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien Earth\",\"year\":2025,"
          + "\"genres\":[\"Science-Fiction\",\"Drama\"],\"firstAired\":\"2025-08-12T00:00:00\","
          + "\"runtimeMinutes\":52,\"totalRating\":7.3,"
          + "\"overview\":\"A young woman and a band of soldiers face the planet's greatest threat.\"}";
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, details);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien Earth", null, 2025, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsLine, Does.Contain("Science-Fiction, Drama"),
          "the kinds of story the metadata file names are read as one line, in its order");
      Assert.That(browser.DetailsLine, Does.Contain("first aired 2025-08-12"),
          "the date the show first ran, told the way the panel tells every date");
      Assert.That(browser.DetailsLine, Does.Contain("rated 7.3/10"),
          "the tagging tool's decimal is shown rather than rounded away");
      Assert.That(browser.DetailsText, Does.StartWith("A young woman"));
    }

    /// <summary>
    /// What a server answers for a title nobody rated is not a missing number but a nought, and a panel that takes
    /// that for a rating stands "0.0" and five dark stars under every film in the library - a screen claiming each
    /// of them was rated nought.  The reference's own condition counts a nought as no answer (its stars and their
    /// number stand or fall with <c>TotalRating</c> through <c>ReferenceToBoolConverter</c>), so this screen counts
    /// it the same way and reads the number that IS there.
    /// </summary>
    [AvaloniaTest]
    public void ARatingOfNoughtIsNoRatingAndTheNumberThatIsThereIsTheOneThatShows()
    {
      string details =
          "{\"id\":\"" + TITANIC + "\",\"title\":\"The Creator\",\"year\":2023,\"runtimeMinutes\":134,"
          + "\"rating\":0,\"totalRating\":7.1,"
          + "\"overview\":\"A future war between the human race and the forces of artificial intelligence.\"}";
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, details);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "The Creator", null, 2023, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsRating, Is.EqualTo(7.1),
          "the nought the server writes for a title nobody rated is no rating; the library's own number is");
      Assert.That(browser.DetailsRatingWords, Is.EqualTo("(7.1)"),
          "and the reference's panel wrote that number after its stars, in brackets");
      Assert.That(browser.DetailsStars[0], Is.EqualTo(1).Within(0.001),
          "seven of ten fill the first star whole, since the reference lit five stars with the number halved");
      Assert.That(browser.DetailsStars[3], Is.GreaterThan(0.25).And.LessThan(1),
          "the fourth star is the one a rating of 7.1 left half lit");
      Assert.That(browser.DetailsStars[4], Is.EqualTo(0.25), "and the fifth waits dark");
    }

    [AvaloniaTest]
    public void ATitleNobodyRatedSpeaksNoRatingAtAll()
    {
      string details =
          "{\"id\":\"" + TITANIC + "\",\"title\":\"The Room\",\"year\":2003,\"runtimeMinutes\":99,"
          + "\"rating\":0,\"totalRating\":0,\"overview\":\"Oh, hi there.\"}";
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, details);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "The Room", null, 2003, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsRating, Is.Null,
          "a library that recorded nothing about a rating has nothing to show");
      Assert.That(browser.DetailsRatingWords, Is.Empty,
          "and the row of stars and number is hidden whole rather than showing five dark stars");
      Assert.That(browser.DetailsLine, Does.Not.Contain("rated"),
          "the sentence beside the plot keeps quiet about the number too, so that the panel cannot say two things");
    }

    /// <summary>
    /// What the server calls a title's path is a whole resource address: the id of the media source the file was
    /// imported from in braces, then the address.  The id is a key of the library's books and no path anybody
    /// reads, so the footnote speaks the path alone.
    /// </summary>
    [AvaloniaTest]
    public void ThePathIsSpokenWithoutTheSourceIdTheServerPutsInFrontOfIt()
    {
      string details =
          "{\"id\":\"" + TITANIC + "\",\"title\":\"The Creator\",\"year\":2023,"
          + "\"path\":\"{e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}://///HTPC-SERVER/Video/Movies/"
          + "The Creator (2023)/The Creator (2023) 1080p EAC3.mkv\"}";
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, details);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "The Creator", null, 2023, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsPath,
          Is.EqualTo("//HTPC-SERVER/Video/Movies/The Creator (2023)/The Creator (2023) 1080p EAC3.mkv"),
          "the file's own address, and no id of the library's books in braces in front of it");
    }

    [AvaloniaTest]
    public void AResourceAddressWithoutASourceIdInFrontOfItIsLeftAsTheServerWroteIt()
    {
      Assert.That(MediaBrowserViewModel.ShownPath("D:/Medien/Film.mkv"), Is.EqualTo("D:/Medien/Film.mkv"),
          "a path the server wrote without the source in front is not this screen's to rewrite");
      Assert.That(MediaBrowserViewModel.ShownPath(null),
          Is.EqualTo("This title has no resource the server can name."),
          "and a title the server cannot place is answered in words, not with a hole");
    }

    /// <summary>
    /// The screen's menu is a rail of commands, and a command nobody can press with the one button every input
    /// device carries is a command for the mouse only.  Two things had to come together for that: the caret, which
    /// a menu opened by the Info key never received - it stayed behind on a tile, and a tile answers that button
    /// by playing the film - and the row that button presses, which is the row the caret stands on rather than the
    /// row the rail happened to be opened on.
    /// </summary>
    [AvaloniaTest]
    public void ACommandOfTheScreensMenuIsPressedByTheButtonStandingOnItsRow()
    {
      LongShelf server = new LongShelf();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to fill", delegate { return browser.Titles.Count > 60; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");

      titles.Focus();
      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });

      // The door the reference hung its menu on (master_mediamenu.xaml:400) is the Info key, and whoever walks
      // through a door is standing on the other side of it: the caret goes with the menu.
      WaitUntil("the caret to be in the rail", delegate { return rail.IsKeyboardFocusWithin; });
      WaitUntil("the rows of the menu to be there", delegate { return rail.Items.Count > 3; });

      // The fourth row of the commands page, "View: ...", which is where a viewer who wants another layout stops.
      // Stood on by the list's own caret - the same state the arrow keys leave behind - and not reached by a click.
      rail.SelectedIndex = 3;
      Pump();
      Assert.That(rail.SelectedIndex, Is.EqualTo(3), "the caret stands on the fourth row of the rail");

      SendKey(rail, Key.Enter);
      WaitUntil("the page that row opens to be the page on the screen",
          delegate { return browser.MediaMenuHeading == "View"; });

      Assert.That(browser.ShowsSearch, Is.False,
          "the button pressed the row the caret stood on, not the first row of the rail - the first row would "
          + "have called for the search field instead of opening the view page");
      Assert.That(browser.MediaMenuVisible, Is.True, "and the rail stays open over its new page, as it did before");
      window.Close();
    }

    [AvaloniaTest]
    public void ThePanelClosesAndReopensWithoutAskingTheServerAgain()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, TITANIC_DETAILS);
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      browser.ToggleDetailsCommand.Execute(null);
      Pump();
      Assert.That(browser.DetailsVisible, Is.False, "the same key that showed the panel hides it");

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

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsLine, Does.Contain("S01E01"),
          "the number a viewer is looking for in a series, in the shape every series app uses");
      Assert.That(browser.DetailsLine, Does.Contain("Nimmerland"));
      Assert.That(browser.DetailsText, Is.EqualTo("Nothing was written about this title on this server."),
          "a series episode imported without a summary has none, and neither does its series on this fake, "
          + "so the panel says that instead of lying");
      // The importer files the series as "Alien Earth (2025)" and the row name carries it; the panel now says
      // the name without the year, because the facts row below names the year anyway.
      DetailsServer filed = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien Earth (2025)\",\"year\":2025}");
      MediaBrowserViewModel series = BrowserOn(filed);
      series.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien Earth (2025)", null, 2025, null, null, false));
      WaitUntil("the series details to open", delegate { return series.DetailsVisible; });
      Assert.That(series.DetailsHeading, Is.EqualTo("Alien Earth"),
          "the year does not belong in the heading; it stands in the details already");
    }

    /// <summary>
    /// A row of the series shelf is named by the name a show keeps on its own aspect - which the server answers as
    /// <c>seriesName</c> and the importers keep free of the year - and not by the library's title column, which
    /// carries the year welded on.  That is the name the reference's cover-details templates drew
    /// (<c>SeriesAspectWrapper.SeriesName</c>), and it is the law the viewer wrote for this screen: the heading
    /// says the name, the facts say the year, and neither of the two says the other's answer a second time.
    /// </summary>
    [AvaloniaTest]
    public void AShowRowTakesItsNameFromTheSeriesNameAndNotFromItsTitle()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien Earth (2025)\",\"year\":2025,"
          + "\"seriesName\":\"Alien: Earth\"}");
      MediaBrowserViewModel shows = BrowserOn(server);
      // A row read through the series shelf, which is the only shelf whose rows are shows themselves; the same
      // answer read under any other shelf is a film or an episode and keeps the name the server gave that title.
      shows.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien Earth (2025)", null, 2025, null, null, false),
          true, null, "SeriesItem");

      WaitUntil("the show's details to open", delegate { return shows.DetailsVisible; });

      Assert.That(shows.DetailsHeading, Is.EqualTo("Alien: Earth"),
          "the show's own aspect names the show, punctuation and all - the title column's spelling of the same "
          + "name is not what this line is drawn from");
      Assert.That(shows.DetailsHeading, Does.Not.Contain("2025"),
          "the year stands in the facts once, and never in the heading");
    }

    /// <summary>
    /// The tracks a file carries - its picture's facts, its audio tracks and its subtitle tracks - arrive under the
    /// story once the server has answered, which is the reference's information screen: its row of logos and its
    /// two lists of cards stood under the plot it showed (<c>MediaInfoVideo.xaml:144-380</c>).
    /// </summary>
    [AvaloniaTest]
    public void TheTracksTheFileCarriesStandUnderTheStory()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien\",\"year\":1979}",
          null,
          "{\"videos\":[{\"streamIndex\":0,\"videoType\":\"HD\",\"videoEncoding\":\"AVC\",\"width\":1920,"
          + "\"height\":1080,\"aspectRatio\":1.7778,\"bitRate\":12500,\"fps\":23.976,\"durationSeconds\":7080}],"
          + "\"audios\":[{\"streamIndex\":1,\"language\":\"de\",\"encoding\":\"AC3\",\"channels\":6,"
          + "\"bitRate\":640,\"sampleRate\":48000},"
          + "{\"streamIndex\":2,\"language\":\"en\",\"encoding\":\"DTS\",\"channels\":8,\"bitRate\":1509,"
          + "\"sampleRate\":48000}],"
          + "\"subtitles\":[{\"streamIndex\":3,\"language\":\"de\",\"format\":\"SRT\",\"isInternal\":true,"
          + "\"isForced\":false}]}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien", null, 1979, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the tracks to arrive", delegate { return browser.ShowsAudioTracks; });

      Assert.That(browser.DetailsPicture.CodecToken, Is.EqualTo("AVC"),
          "the row the reference drew as six logos, asked for by the names its own folders filed them under");
      Assert.That(browser.DetailsPicture.Resolution, Is.EqualTo("1080p"));
      Assert.That(browser.DetailsPicture.RatioClass, Is.EqualTo("16:9"));
      Assert.That(browser.DetailsPicture.RatioFigure, Is.EqualTo("1.78:1"));
      Assert.That(browser.DetailsPicture.RateText, Is.EqualTo("12500 kbps · 24 fps"),
          "the last plaque says both figures on one line, with the frame glyph in front of them - and it says the "
          + "frame rate as a whole number, though the file above was read as 23.976 frames a second");
      Assert.That(browser.DetailsPicture.FrameRateText, Is.EqualTo("24 fps"),
          "the reference stacked these two rather than writing them as one line, and so are they stacked here; the "
          + "three decimals it wrote the rate with are rounded away, since nobody chooses a television by 23.976");
      Assert.That(browser.DetailsAudioTracks, Has.Count.EqualTo(2),
          "every track the file carries is offered, not the first one only");
      Assert.That(browser.DetailsAudioTracks[0].Language, Is.EqualTo("German"),
          "the card reads its language by name; the library wrote the two letters \"de\"");
      Assert.That(browser.DetailsAudioTracks[0].FlagToken, Is.EqualTo("de"),
          "the flag the reference stood beside that word, asked for by the language as the library wrote it");
      Assert.That(browser.DetailsAudioTracks[0].Channels, Is.EqualTo("5.1"));
      Assert.That(browser.DetailsAudioTracks[0].ChannelsToken, Is.EqualTo("6"),
          "the loudspeaker picture of six channels, filed by the count rather than by the trade's word");
      Assert.That(browser.DetailsAudioTracks[1].Detail, Is.EqualTo("1509 kbps · 48000 Hz"),
          "the library's figures arrive in the unit the importer wrote them in");
      Assert.That(browser.DetailsSubtitleTracks[0].CarriageToken, Is.EqualTo("Internal"),
          "the picture of a track lying inside the file");
      Assert.That(browser.ShowsTracks, Is.True);
    }

    /// <summary>
    /// The arrows walk OUT of a band of track cards.  A first version put the caret on a card and never let it go
    /// again, which the viewer caught the same evening (2026-11-02); the reading of a title is a stack of bands of
    /// cards and nothing else, so an arrow at the edge of a band has to lead into the band next to it - and where
    /// the reading has no band left, the arrow is left alone rather than walking the caret off the screen.  Without
    /// that, the screen is a shelf with one tile on it.  Measured over a real window, because a caret is a thing a
    /// window holds and no view model has one.
    /// </summary>
    [AvaloniaTest]
    public async Task TheArrowsWalkOutOfABandOfTrackCardsIntoTheNextThingOnTheScreen()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien\",\"year\":1979}",
          null,
          "{\"videos\":[{\"streamIndex\":0,\"videoType\":\"HD\",\"videoEncoding\":\"AVC\",\"width\":1920,"
          + "\"height\":1080,\"aspectRatio\":1.7778,\"bitRate\":12500,\"fps\":24,\"durationSeconds\":7080}],"
          + "\"audios\":[{\"streamIndex\":1,\"language\":\"de\",\"encoding\":\"AC3\",\"channels\":6,"
          + "\"bitRate\":640,\"sampleRate\":48000},"
          + "{\"streamIndex\":2,\"language\":\"en\",\"encoding\":\"DTS\",\"channels\":8,\"bitRate\":1509,"
          + "\"sampleRate\":48000}],"
          + "\"subtitles\":[{\"streamIndex\":3,\"language\":\"de\",\"format\":\"SRT\",\"isInternal\":true,"
          + "\"isForced\":false},"
          + "{\"streamIndex\":4,\"language\":\"en\",\"format\":\"SRT\",\"isInternal\":false,\"isForced\":true}]}");
      MediaBrowserViewModel browser = BrowserOn(server);
      TitleViewModel alien = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien", null, 1979, null, null, false));
      browser.Titles.Add(alien);
      browser.Selected = alien;
      WaitUntil("the tracks to arrive", delegate { return browser.ShowsAudioTracks; });

      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      workflow.Push(new WorkflowFrame("titles", "Movies", browser));
      window.Show();
      Pump();

      MediaTitleAction reading = MediaTitleActions.For(alien, true, true)
          .First(action => action.Kind == MediaTitleActionKind.Information);
      await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>) browser.RunTitleActionCommand)
          .ExecuteAsync(reading);
      Pump();
      Assert.That(browser.InfoShown, Is.True, "the reading stands up, which is where the two bands are");

      List<Border> audio = TrackCardsOf(window, "audioTracksScroller");
      List<Border> subtitles = TrackCardsOf(window, "subtitleTracksScroller");
      Assert.That(audio, Has.Count.EqualTo(2),
          "the band really did lay its cards out, or the walk below would be a walk over nothing");
      Assert.That(subtitles, Has.Count.EqualTo(2));

      // The walk a viewer makes with the arrows, one press at a time, from the first card of the first band down
      // to the last card of the reading and back up.
      audio[0].Focus();
      Pump();
      Assert.That(audio[0].IsKeyboardFocusWithin, Is.True, "a card takes the caret");
      Assert.That(audio[0].BoxShadow, Is.Not.Null,
          "and a card stands in the skin's shared shadow - the same Border.home-tile-shadow the wall's tiles, the "
          + "news cards and the weather cards stand in, so that a card of this screen is no flatter than a tile of "
          + "the wall");

      Press(audio[0], Key.Right);
      Assert.That(audio[1].IsKeyboardFocusWithin, Is.True,
          "along a band the arrows step one card at a time");

      Press(audio[1], Key.Down);
      Assert.That(subtitles[1].IsKeyboardFocusWithin, Is.True,
          "down out of a band walks into the band beneath it, at the same place ALONG it: a viewer who came down "
          + "at the second card comes back up at the second card, not at the band's first - this is the one press "
          + "the viewer asked for and the first version refused");

      Press(subtitles[1], Key.Up);
      Assert.That(audio[1].IsKeyboardFocusWithin, Is.True,
          "and up walks back the same way");

      Press(audio[1], Key.Left);
      Assert.That(audio[0].IsKeyboardFocusWithin, Is.True);

      Press(audio[0], Key.Down);
      Assert.That(subtitles[0].IsKeyboardFocusWithin, Is.True);

      // The head of the first band is the head of the whole reading, and the question about it is asked THERE: up
      // out of the band below walks the caret back to it (the same walk three presses above, read at the head this
      // time), and only then does an arrow pointing further left meet the end of the row.  Its honest answer is the
      // band leaning against that end (see BounceBand) while the caret stays on the card it was asked on - not a
      // caret that walked off the screen.  Standing elsewhere when that key is pressed proves nothing: the caret
      // was told to stay where it stands, and it stood in the band below.
      Press(subtitles[0], Key.Up);
      Assert.That(audio[0].IsKeyboardFocusWithin, Is.True,
          "up out of the band below comes back to the head of the first band, at the same place along it");

      Press(audio[0], Key.Left);
      Assert.That(audio[0].IsKeyboardFocusWithin, Is.True,
          "an arrow past the head of the first band leaves the caret where it stands and leans the band instead");

      // Down to the last band again - the walk down was measured above and is only walked here to stand the caret
      // where the last question is asked.
      Press(audio[0], Key.Down);
      Assert.That(subtitles[0].IsKeyboardFocusWithin, Is.True);

      // And below the last band the reading has nothing left for a caret to stand on - the button that used to wait
      // at its foot is gone, and a viewer leaves the reading the way they leave every other screen - so the arrow
      // is left alone there rather than walking the caret off the screen.
      Press(subtitles[0], Key.Down);
      Assert.That(subtitles[0].IsKeyboardFocusWithin, Is.True,
          "down out of the last band of the reading leaves the caret on its card: an arrow that walked off the last "
          + "card would leave the viewer with a screen that answers nobody");

      subtitles[1].Focus();
      Pump();

      Press(subtitles[1], Key.Enter);
      Assert.That(browser.InfoShown, Is.True,
          "Enter on a card is taken and answered with nothing: this screen hands Enter to \"play this\" wherever "
          + "else it lands, and a film starting because a viewer looked at an audio track answers no question");
      Assert.That(subtitles[1].IsKeyboardFocusWithin, Is.True,
          "the caret stays on the card it was asked on");

      window.Close();
    }

    /// <summary>
    /// The track cards of one band of the reading, in the order they stand on the screen - which is the order a
    /// viewer walks them.
    /// </summary>
    private static List<Border> TrackCardsOf(Window window, string band) =>
        window.GetVisualDescendants().OfType<ScrollViewer>()
            .First(scroller => scroller.Name == band)
            .GetVisualDescendants().OfType<Border>()
            .Where(card => card.Classes.Contains("trackcard"))
            .ToList();

    /// <summary>Presses one key on a control the way a viewer does, and lets the screen answer it.</summary>
    private static void Press(Control control, Key key)
    {
      control.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = control
      });
      Pump();
    }

    /// <summary>
    /// A card's shadow belongs to the card.  Every tile of this front end stands in the skin's shared
    /// <c>Border.home-tile-shadow</c>, whose shadow lies three points to the right and four below the tile and
    /// blurs sixteen points past that - which is to say it needs its room OUTSIDE the tile's own bounds, and
    /// whatever clips at the tile's bounds cuts it off and leaves the card sitting flat.  Measured rather than
    /// looked at, because the shadow is exactly what a headless render cannot show: this walks from a card up to
    /// the window and asks every ancestor that clips anything whether the room the shadow needs is inside what it
    /// leaves visible.  The first version of the bands failed this: the items panel clipped at the exact height of
    /// a card and the scroller left the shadow's last twenty points twenty points of need.
    /// </summary>
    [AvaloniaTest]
    public void TheShadowOfATrackCardIsNotCutByTheBandItStandsIn()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Dune\",\"year\":2021}",
          null,
          "{\"videos\":[{\"streamIndex\":0,\"videoType\":\"HD\",\"videoEncoding\":\"HEVC\",\"width\":1920,"
          + "\"height\":1080,\"aspectRatio\":2.39,\"bitRate\":30000,\"fps\":24,\"durationSeconds\":7080}],"
          + "\"audios\":[{\"streamIndex\":1,\"language\":\"de\",\"encoding\":\"AC3\",\"channels\":6,"
          + "\"bitRate\":640,\"sampleRate\":48000},"
          + "{\"streamIndex\":2,\"language\":\"en\",\"encoding\":\"DTS\",\"channels\":8,\"bitRate\":1509,"
          + "\"sampleRate\":48000}],"
          + "\"subtitles\":[{\"streamIndex\":3,\"language\":\"de\",\"format\":\"SRT\",\"isInternal\":true,"
          + "\"isForced\":false}]}",
          // The people too, so that this measures every card of the reading at once and not the four bands of a
          // title whose library named nobody - which is the state the bands were first measured in.
          "{\"id\":\"" + TITANIC + "\",\"groups\":[{\"role\":\"actor\",\"persons\":["
          + "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Timothee Chalamet\"},"
          + "{\"id\":\"22222222-2222-2222-2222-222222222222\",\"name\":\"Rebecca Ferguson\"},"
          + "{\"id\":\"33333333-3333-3333-3333-333333333333\",\"name\":\"Josh Brolin\"}]},"
          + "{\"role\":\"director\",\"persons\":[{\"id\":\"44444444-4444-4444-4444-444444444444\","
          + "\"name\":\"Denis Villeneuve\"}]}]}");
      MediaBrowserViewModel browser = BrowserOn(server);
      TitleViewModel title = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Dune", null, 2021, null, null, false));
      browser.Titles.Add(title);
      browser.Selected = title;
      WaitUntil("the tracks and the people to arrive",
          delegate { return browser.ShowsAudioTracks && browser.ShowsActors && browser.ShowsDirectors; });

      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Filme", browser));
      window.Show();
      Pump();

      MediaTitleAction reading = MediaTitleActions.For(title, true, true)
          .First(action => action.Kind == MediaTitleActionKind.Information);
      ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>) browser.RunTitleActionCommand)
          .ExecuteAsync(reading).Wait();
      Pump();

      // How far the skin's shared shadow reaches outside the card it is painted on: Border.home-tile-shadow
      // carries "3 4 16 0", which is three points to the right and four below, blurred by sixteen - so the
      // faintest trace of it stands 19 points right of the card, 20 below it, 13 left of it and 12 above it.
      // Three points of slack on each side, because a shadow whose last faint line lands exactly on a cut is a
      // shadow a viewer sees as cut.
      const double airLeft = 16;
      const double airRight = 22;
      const double airTop = 15;
      const double airBottom = 23;
      List<string> cut = new List<string>();
      foreach (Border card in window.GetVisualDescendants().OfType<Border>()
          .Where(b => b.Classes.Contains("trackcard")))
      {
        Assert.That(card.BoxShadow, Is.Not.Null, "a track card wears the skin's shared shadow");
        Point? at = card.TranslatePoint(new Point(0, 0), window);
        Assert.That(at, Is.Not.Null, "a card on the screen knows where it stands");
        Rect needs = new Rect(at.Value.X - airLeft, at.Value.Y - airTop,
            card.Bounds.Width + airLeft + airRight, card.Bounds.Height + airTop + airBottom);

        for (Visual? ancestor = card.GetVisualParent();
            ancestor != null && ancestor != window;
            ancestor = ancestor.GetVisualParent())
        {
          var control = ancestor as Control;
          bool clips = control != null && (control.ClipToBounds || control.Clip != null);
          if (!clips)
            continue;

          Point? here = ancestor.TranslatePoint(new Point(0, 0), window);
          Assert.That(here, Is.Not.Null);
          Rect leaves = new Rect(here.Value.X, here.Value.Y,
              control.Bounds.Width, control.Bounds.Height);

          // A card that does not itself stand inside what this ancestor leaves visible is a card the band scrolls
          // to: three actors do not fit beside one another in a band of half the reading's width, and the third is
          // meant to run past its edge.  Its shadow is cut by the edge of the screen and not by the band, so
          // asking for the air around it would measure the wrong thing.
          Rect cardHere = new Rect(at.Value.X, at.Value.Y, card.Bounds.Width, card.Bounds.Height);
          if (!leaves.Contains(cardHere.TopLeft) || !leaves.Contains(cardHere.BottomRight))
            continue;

          if (!leaves.Contains(new Point(needs.Right, needs.Bottom))
              || !leaves.Contains(new Point(needs.Left, needs.Top)))
          {
            cut.Add((string.IsNullOrEmpty(control.Name) ? "unnamed" : control.Name) + " ("
                + control.GetType().Name + ") leaves "
                + leaves.Width.ToString("F0") + "x" + leaves.Height.ToString("F0") + " at "
                + leaves.X.ToString("F0") + "," + leaves.Y.ToString("F0")
                + " while the card needs " + needs.Width.ToString("F0") + "x" + needs.Height.ToString("F0")
                + " at " + needs.X.ToString("F0") + "," + needs.Y.ToString("F0"));
          }
        }
      }

      Assert.That(cut, Is.Empty,
          "the shadow of a track card is cut off by " + string.Join(" | ", cut));

      // And the room the shadow bought must not be bought from the screen: both bands, air included, still stand
      // whole inside the window.
      ScrollViewer first = window.GetVisualDescendants().OfType<ScrollViewer>()
          .First(s => s.Name == "audioTracksScroller");
      // The last band of cards the reading draws, asked of the screen: the people row stands last now, and for a
      // library that named nobody it is the subtitles again - the two states have to be measured, not assumed.
      ScrollViewer last = new[]
          { "directorsScroller", "actorsScroller", "subtitleTracksScroller", "audioTracksScroller" }
          .Select(name => window.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == name))
          .First(band => band.IsEffectivelyVisible);
      Point? topOfFirst = first.TranslatePoint(new Point(0, 0), window);
      Point? bottomOfLast = last.TranslatePoint(new Point(0, last.Bounds.Height), window);
      Assert.That(topOfFirst, Is.Not.Null);
      Assert.That(bottomOfLast, Is.Not.Null);
      Assert.That(topOfFirst!.Value.Y, Is.GreaterThanOrEqualTo(0),
          "the first band stands whole on the screen");
      Assert.That(bottomOfLast!.Value.Y, Is.LessThanOrEqualTo(window.Bounds.Height),
          "the last band stands whole on the screen - a band that grows past the foot of the window is a band that "
          + "hides the last of its cards behind an edge the viewer cannot scroll past");
      window.Close();
    }

    /// <summary>
    /// The people a title names stand in two bands <b>beside</b> one another - one row of the reader's side, not
    /// two rows of it, which is what the viewer agreed to on 2026-11-03 when it turned out that the reference's
    /// four stacked bands do not stand above the foot of a window a thousand and eighty lines tall - and the arrows
    /// walk that row the way they walk everything else on this screen: along it, into the band above it, and no
    /// further.  Measured on a laid out window because "beside", "whole" and "reachable" are all about where a card
    /// stands, and because the first version of a band that could not be walked out of was exactly the mistake the
    /// viewer sent back once already.
    /// </summary>
    [AvaloniaTest]
    public void ThePeopleOfATitleStandInTwoBandsBesideOneAnotherAndAreWalkedWithTheArrows()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien\",\"year\":1979}",
          null,
          "{\"videos\":[],\"audios\":[{\"streamIndex\":1,\"language\":\"de\",\"encoding\":\"AC3\",\"channels\":6,"
          + "\"bitRate\":640,\"sampleRate\":48000}],\"subtitles\":[{\"streamIndex\":2,\"language\":\"en\","
          + "\"format\":\"SRT\",\"isInternal\":false,\"isForced\":false}]}",
          "{\"id\":\"" + TITANIC + "\",\"groups\":[{\"role\":\"actor\",\"persons\":["
          + "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Sigourney Weaver\"},"
          + "{\"id\":\"22222222-2222-2222-2222-222222222222\",\"name\":\"John Hurt\"},"
          + "{\"id\":\"33333333-3333-3333-3333-333333333333\",\"name\":\"Ian Holm\"}]},"
          + "{\"role\":\"director\",\"persons\":[{\"id\":\"44444444-4444-4444-4444-444444444444\","
          + "\"name\":\"Ridley Scott\"}]}]}");
      MediaBrowserViewModel browser = BrowserOn(server);
      TitleViewModel alien = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien", null, 1979, null, null, false));
      browser.Titles.Add(alien);
      browser.Selected = alien;
      WaitUntil("the people to arrive", delegate { return browser.ShowsActors && browser.ShowsDirectors; });

      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Movies", browser));
      window.Show();
      Pump();

      MediaTitleAction reading = MediaTitleActions.For(alien, true, true)
          .First(action => action.Kind == MediaTitleActionKind.Information);
      ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>) browser.RunTitleActionCommand)
          .ExecuteAsync(reading).Wait();
      Pump();

      List<Border> actors = TrackCardsOf(window, "actorsScroller");
      List<Border> directors = TrackCardsOf(window, "directorsScroller");
      List<Border> subtitles = TrackCardsOf(window, "subtitleTracksScroller");
      Assert.That(actors, Has.Count.EqualTo(3),
          "a card per person the library named, in the order it named them");
      Assert.That(directors, Has.Count.EqualTo(1));
      Assert.That(subtitles, Has.Count.EqualTo(1));

      List<string> read = window.GetVisualDescendants().OfType<TextBlock>()
          .Select(text => text.Text ?? "").ToList();
      Assert.That(read, Does.Contain("Sigourney Weaver"),
          "a name is read on the card, and not hidden behind a picture of a person");
      Assert.That(read, Does.Contain("Ridley Scott"));
      Assert.That(read, Does.Contain("Actors"), "the heads carry the reference's own words");
      Assert.That(read, Does.Contain("Director"));

      ScrollViewer actorsBand = window.GetVisualDescendants().OfType<ScrollViewer>()
          .First(band => band.Name == "actorsScroller");
      ScrollViewer directorsBand = window.GetVisualDescendants().OfType<ScrollViewer>()
          .First(band => band.Name == "directorsScroller");
      Point? atActors = actorsBand.TranslatePoint(new Point(0, 0), window);
      Point? atDirectors = directorsBand.TranslatePoint(new Point(0, 0), window);
      Assert.That(atActors, Is.Not.Null);
      Assert.That(atDirectors, Is.Not.Null);
      Assert.That(atActors!.Value.Y, Is.EqualTo(atDirectors!.Value.Y).Within(1),
          "the two bands are one row of the screen, which is the whole point of putting them beside one another");
      Assert.That(atActors.Value.X, Is.LessThan(atDirectors.Value.X),
          "the actors at the reading side, the director to their right, as the words are read");

      // The row must not buy its place from the window.
      Point? belowRow = directorsBand.TranslatePoint(new Point(0, directorsBand.Bounds.Height), window);
      Assert.That(belowRow, Is.Not.Null);
      Assert.That(belowRow!.Value.Y, Is.LessThanOrEqualTo(window.Bounds.Height),
          "the people row stands whole on a window of a thousand and eighty lines - a band that runs past the foot "
          + "of the window is a band a viewer has to guess at");

      // The walk, one press at a time, from the band of subtitles down into the row and back.
      subtitles[0].Focus();
      Pump();
      Press(subtitles[0], Key.Down);
      Assert.That(actors[0].IsKeyboardFocusWithin, Is.True,
          "down out of the last band of cards before the people goes into the people row at the same place along "
          + "it - the row a viewer can get into but not out of was the very thing sent back once");

      Press(actors[0], Key.Right);
      Assert.That(actors[1].IsKeyboardFocusWithin, Is.True);
      Press(actors[1], Key.Right);
      Assert.That(actors[2].IsKeyboardFocusWithin, Is.True);
      Press(actors[2], Key.Right);
      Assert.That(directors[0].IsKeyboardFocusWithin, Is.True,
          "past the tail of the actors the caret steps into the band beside it, because the two bands are one row "
          + "and a row is walked along, not jumped over");

      Press(directors[0], Key.Down);
      Assert.That(directors[0].IsKeyboardFocusWithin, Is.True,
          "and below the last band of the reading the arrow is left alone: the people row is the last thing this "
          + "screen has for a caret to stand on, so the caret keeps its card instead of walking off the screen - "
          + "the button that used to wait at the foot of the reading is gone, and a viewer leaves the reading the "
          + "way they leave every other screen");

      Press(directors[0], Key.Left);
      Assert.That(actors[2].IsKeyboardFocusWithin, Is.True,
          "back past the head of the director band comes to the tail of the actors band");

      Press(actors[2], Key.Up);
      Assert.That(subtitles[0].IsKeyboardFocusWithin, Is.True,
          "up out of the people row walks into the band above it, as far along that band as the row of people "
          + "reaches - a band with one card cannot be walked to its third place, and its last card answers");

      window.Close();
    }

    /// <summary>
    /// A reading that stands up before the server has answered what the file holds still takes the caret onto
    /// the card at the head of its first band, as soon as there is one to stand on.
    /// </summary>
    /// <remarks>
    /// A reading says it is up the moment it is asked for, because waiting for the answer would leave the viewer
    /// staring at the wall for as long as the server thinks. So the ask that stands the caret at the head of the
    /// first band meets empty lists and finds nothing to stand on - the right answer for a moment, and a screen
    /// nobody can walk for the rest of them: every arrow meets a screen that answers nobody. Measured on a server
    /// that is slow on purpose.
    /// </remarks>
    [AvaloniaTest]
    public void AReadingThatOutrunsItsOwnFactsTakesTheCaretOntoTheFirstCardWhenTheyArrive()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien\",\"year\":1979}",
          null,
          "{\"videos\":[],\"audios\":[{\"streamIndex\":1,\"language\":\"de\",\"encoding\":\"AC3\",\"channels\":6,"
          + "\"bitRate\":640,\"sampleRate\":48000}],\"subtitles\":[]}",
          "{\"id\":\"" + TITANIC + "\",\"groups\":[{\"role\":\"actor\",\"persons\":["
          + "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Sigourney Weaver\"}]},"
          + "{\"role\":\"director\",\"persons\":[]}]}");
      server.DelayMs = 400;

      MediaBrowserViewModel browser = BrowserOn(server);
      TitleViewModel alien = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien", null, 1979, null, null, false));
      browser.Titles.Add(alien);
      browser.Selected = alien;

      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Movies", browser));
      window.Show();
      Pump();

      MediaTitleAction reading = MediaTitleActions.For(alien, true, true)
          .First(action => action.Kind == MediaTitleActionKind.Information);
      ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>) browser.RunTitleActionCommand)
          .ExecuteAsync(reading);

      WaitUntil("the reading to stand open", delegate { return browser.InfoShown; });

      // Nothing the caret could stand on is drawn yet, so it stands on nothing at all: the reading keeps no button
      // of its own for a waiting caret to sit on.
      Pump();
      Assert.That(TrackCardsOf(window, "audioTracksScroller"), Has.Count.EqualTo(0),
          "the slow server has not answered yet, so the first band has no card to take the caret");

      WaitUntil("the bands to answer", delegate { return browser.ShowsAudioTracks; });
      Pump();
      Pump();

      List<Border> audios = TrackCardsOf(window, "audioTracksScroller");
      Assert.That(audios, Has.Count.EqualTo(1),
          "one audio track is one card - and not one card per question the slow server happened to be asked "
          + "(cards drawn: " + audios.Count + ", the reader's list holds "
          + browser.DetailsAudioTracks.Count + ")");
      Assert.That(audios[0].IsKeyboardFocusWithin, Is.True,
          "the facts arrived, the first band has its head card, and the caret that was left waiting on nothing "
          + "walks onto it - a viewer whose arrows come back must find them on a card they can see");

      // One reading is one read of the title's answers, however many times the shelf settling and the reading
      // opening both asked for them while the first question was still travelling.
      Assert.That(server.TimesAsked, Is.EqualTo(1),
          "the questions about the title itself, asked while an earlier one was still on its way, are not asked "
          + "a second time: two reads empty the bands on the way out and fill them on the way back in, which is "
          + "how a track comes to be drawn twice");

      // Nothing may still be on its way when the test ends.  This server is slow on purpose, so an answer that
      // lands after the window is gone hands the dispatcher a job that the next test's teardown would run.
      WaitUntil("the people of the title to have arrived as well", delegate { return browser.ShowsActors; });
      for (int round = 0; round < 60; round++)
        Pump();

      window.Close();
    }

    /// <summary>
    /// What a file that is only sound says about itself, read on the reading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The worn skin gives such a file a row of its own: the packing, the loudspeakers, the disc, and under them
    /// the two rates the file was written at (<c>MediaInfoAudio.xaml:115-170</c>), and it counts the length in
    /// minutes and whole seconds rather than in the rounded minutes of a film
    /// (<c>MediaInfoAudioDetails.inc:308-315</c>).
    /// </para>
    /// <para>
    /// <b>Why this had to be asked of the server.</b> All six figures stand on the file's own record in the
    /// library, in the aspect the importer filled when it looked inside the file (<c>AudioAspect.cs:47-77</c>).
    /// The answer about a single title had never carried them, so a song stood in front of its viewer with the
    /// row empty however carefully the library had read its file - and the pictures the row wears are named by
    /// the library's own wording, which is the second thing this test pins: the skin's file for the packing is
    /// called by exactly the name the library's tag reader writes.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void AFileOfPureSoundSaysItsPackingItsLoudspeakersItsDiscAndItsTwoRates()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Blue in Green\",\"year\":1959,\"durationSeconds\":206,"
          + "\"audioEncoding\":\"MPEG Version 1 Audio, Layer 3\",\"audioChannels\":2,\"audioBitRate\":320,"
          + "\"audioSampleRate\":44100,\"discId\":1,\"discCount\":2}");
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AudioItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Blue in Green", null, 1959, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.ShowsAudioLine, Is.True,
          "a file whose library looked inside it says what it is packed in - the row is there to be read");
      Assert.That(browser.DetailsAudio!.CodecWord, Is.EqualTo("MPEG Version 1 Audio, Layer 3"),
          "the packing is said in the library's own words, and not as an abbreviation the viewer has to learn");
      Assert.That(browser.DetailsAudio.CodecToken, Is.EqualTo("MPEG Version 1 Audio, Layer 3"),
          "the picture is asked for by the very name the library's tag reader writes, which is the name the "
          + "worn skin's pictures carry - a name this front end invented would find no picture at all");
      Assert.That(browser.DetailsAudio.ChannelsToken, Is.EqualTo("2"),
          "the loudspeaker picture is named by the plain count, as the reference named it");
      Assert.That(browser.DetailsAudio.ChannelsWord, Is.EqualTo("2.0"),
          "and under it stands the arrangement in the trade's spelling, which is what a viewer compares");
      Assert.That(browser.DetailsAudio.DiscText, Is.EqualTo("1 | 2"),
          "the disc is said as the disc of a set, which is the shape the reference wrote");
      Assert.That(browser.DetailsAudio.RatesText, Is.EqualTo("320 kbps · 44100 Hz"),
          "the two rates stand on one line under the three plaques, as they did in the skin");

      MediaBrowserFact? length = browser.DetailsFacts.FirstOrDefault(
          delegate (MediaBrowserFact fact) { return fact.Label == "Length"; });
      Assert.That(length, Is.Not.Null, "the reading says how long the piece is");
      Assert.That(length!.Value, Is.EqualTo("03:26"),
          "three minutes and twenty six seconds, in the shape the skin wrote for sound: a song rounded to "
          + "\"4 min\" is a song the viewer cannot find again");

      // The video row is filled by the second question about the file, which is still on its way at this point.
      WaitUntil("the answer about the file to have been read", delegate { return browser.DetailsPicture != null; });
      Assert.That(browser.DetailsPicture!.ShowsAnything, Is.False,
          "a file of pure sound wears no video row - the tracks question comes back empty for such a file, and a "
          + "row of blanks where a picture should be is worse than the row's being away");

      // Drawn, and drawn ONCE.  A row that is inserted twice into the view answers its questions twice, and every
      // property in the world still reads correct - only the laid out screen can be asked how often a band stands
      // in the reading.  (Which is exactly what happened to this row: two versions of it were written into the
      // view on the same evening, so a song wore its "Audio" heading twice, one under the other.)
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Musik", browser));
      window.Show();
      Pump();

      List<TextBlock> audioHeads = window.GetVisualDescendants().OfType<TextBlock>()
          .Where(delegate (TextBlock word) { return word.Text == "Audio"; })
          .ToList();
      Assert.That(audioHeads, Has.Count.EqualTo(1),
          "one audio file is one row about its sound, standing once in the reading (headings found: "
          + audioHeads.Count + ")");

      window.Close();
    }

    /// <summary>
    /// The four answers an album gives about itself and no shelf gave it before: how long the record runs, the
    /// kinds of story it is told in, how many tracks stand on it, and how many discs the set has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Measured over the household's library on 2026-11-03: 43 albums, 1018 tracks, every track carrying a duration
    /// and every album naming a genre on its shelf - and the album's own screen standing there with two facts, one
    /// of them a day nobody knew.  The band is the film band; the album is now read through the same words
    /// <em>Length</em> and <em>Genre</em>, plus the two the record answers by.
    /// </para>
    /// <para>
    /// The length is spelled as a record and not as a stopwatch: 6345 seconds are "1 h 45 min" here, while the very
    /// same figure on a track is "03:26" (the track's own test above pins that).  The minutes are cut whole, so a
    /// screen never promises a minute the record does not have.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void AnAlbumSaysHowLongItRunsItsKindsItsTracksAndItsDiscs()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Born This Way (Special Edition)\",\"year\":2011,"
          + "\"genres\":[\"Dance\"],\"durationSeconds\":6345,\"trackCount\":23,\"discCount\":2,"
          + "\"audioEncoding\":\"MPEG Version 1 Audio, Layer 3\",\"audioChannels\":2}");
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Born This Way (Special Edition)", null, 2011, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(AnswerOf(browser, "Length"), Is.EqualTo("1 h 45 min"),
          "the record is said in hours, which is how anybody says how long an album takes; the same figure on a "
          + "track is still 03:26 because a song is about to be listened to");
      Assert.That(AnswerOf(browser, "Genre"), Is.EqualTo("Dance"),
          "the kind the shelf named a moment before is still named here");
      Assert.That(AnswerOf(browser, "Tracks"), Is.EqualTo("23"),
          "the tracks standing on the record, which is what the library holds - the 17 its tag remembers are the "
          + "title list's answer");
      Assert.That(AnswerOf(browser, "Discs"), Is.EqualTo("2"),
          "the two discs of the set, which the same answer has carried all along");
      Assert.That(browser.DetailsFacts.Any(
              delegate (MediaBrowserFact fact) { return fact.Label == "Runtime"; }), Is.False,
          "music says its length as music does, whatever a film's word would have been");
    }

    /// <summary>
    /// A set of one disc says no "Discs", because a number that chooses nothing is not an answer.
    /// </summary>
    [AvaloniaTest]
    public void AnAlbumOfASingleDiscSaysNothingAboutDiscs()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"amo\",\"year\":2019,\"genres\":[\"Rock\"],"
          + "\"durationSeconds\":3114,\"trackCount\":13,\"discCount\":1}");
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "amo", null, 2019, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(AnswerOf(browser, "Length"), Is.EqualTo("51 min"),
          "fifty one minutes and fifty four seconds are fifty one minutes, and not a promised fifty second");
      Assert.That(AnswerOf(browser, "Tracks"), Is.EqualTo("13"));
      Assert.That(browser.DetailsFacts.Any(
              delegate (MediaBrowserFact fact) { return fact.Label == "Discs"; }), Is.False,
          "one disc is the absence of a set, so nothing is said about one");
    }

    /// <summary>
    /// The answer one of the fact band's words got, or null when the word stands for nothing.
    /// </summary>
    private static string? AnswerOf(MediaBrowserViewModel browser, string label)
    {
      MediaBrowserFact? fact = browser.DetailsFacts.FirstOrDefault(
          delegate (MediaBrowserFact candidate) { return candidate.Label == label; });
      return fact?.Value;
    }

    /// <summary>
    /// That a sound file's plaques are copied onto the details panel's plaque line rather than moved there: the
    /// information screen still wears its own row, and the album now wears the same answers at the end of the line
    /// under its name.
    /// </summary>
    /// <remarks>
    /// The sound answers had always been read for the details panel too - <c>DetailsAudio</c> arrives with the
    /// details answer - but were drawn on the information screen only, so an album stood at its own screen with an
    /// empty end of the line while its packing, its loudspeakers and its two rates were in the answer in hand.
    /// The viewer asked for a copy on 2026-11-03: "COPY them, not move!" - and, seeing the screen afterwards,
    /// asked for the disc to stay out of that line, because the facts band under the name already says it.
    /// </remarks>
    [AvaloniaTest]
    public void AnAlbumWearsItsSoundPlaquesAtTheEndOfTheLineUnderItsName()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"amo\",\"year\":2019,\"genres\":[\"Rock\"],"
          + "\"durationSeconds\":3114,\"trackCount\":13,\"audioEncoding\":\"MPEG Version 1 Audio, Layer 3\","
          + "\"audioChannels\":2,\"audioBitRate\":320,\"audioSampleRate\":44100,\"discId\":1,\"discCount\":2}");
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "amo", null, 2019, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      Assert.That(browser.ShowsAudioLine, Is.True);

      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Musik", browser));
      window.Show();
      Pump();

      Border copied = window.GetVisualDescendants().OfType<Border>()
          .FirstOrDefault(delegate (Border plaque) { return plaque.Name == "detailsAudioCodecPlaque"; });
      Assert.That(copied, Is.Not.Null, "the details panel's plaque line wears the packing of the record now");
      Assert.That(copied.IsVisible, Is.True,
          "and it is shown for an album, whose line used to end in nothing at all");

      StackPanel line = (StackPanel) copied.Parent!;
      Panel row = (Panel) line.Parent!;

      // Copied and not moved: the information screen's own row is still built, with its own plaque of the same
      // answer.  Named on the viewer's word, because "copied, not moved" is exactly the question a screen can be
      // asked and a property cannot answer.
      Border[] originals = window.GetVisualDescendants().OfType<Border>()
          .Where(delegate (Border plaque) { return plaque.Name == "infoAudioCodecPlaque"; })
          .ToArray();
      Assert.That(originals, Has.Length.EqualTo(1),
          "the row about sound keeps its own plaque of the packing - the information screen is a screen a viewer "
          + "opens to read a file, and it was not robbed of its answer to give this one a second");

      // The DISC is the one answer that was NOT copied: the facts band under the name already says it as "Discs",
      // and the viewer pointed at the "1 | 2" plaque standing over that fact on 2026-11-03 - "ist falsch und gehört
      // da nicht hin. Es steht schon in der tabelle."  So the figure is said once, in the facts.
      Assert.That(AnswerOf(browser, "Discs"), Is.EqualTo("2"), "the table says the disc of the set");
      Assert.That(line.GetVisualDescendants().OfType<TextBlock>()
              .Any(delegate (TextBlock word) { return word.Text == "1 | 2"; }), Is.False,
          "and the line under the name does not say the same figure a second time");
      TextBlock[] rates = line.GetVisualDescendants().OfType<TextBlock>()
          .Where(delegate (TextBlock word) { return word.Text == "320 kbps · 44100 Hz"; })
          .ToArray();
      Assert.That(rates, Has.Length.EqualTo(1),
          "the two rates are said on the line, as the mockup's last two plaques said them");

      // And the copied plaque stands where the line's other answers stand: at the right edge of the line, and the
      // line at the right edge of the panel.  Bounds are spoken in the parent's coordinates, so the line's right
      // edge is measured against the width of the row it stands in.
      Assert.That(line.Bounds.Right, Is.EqualTo(row.Bounds.Width).Within(1),
          "the plaques are read at the right edge of the line under the name, as the film's plaques always were");
      Assert.That(copied.Bounds.Width, Is.GreaterThan(0),
          "and the plaque of the packing is laid out, not merely present in a tree nobody sees");
      Assert.That(copied.Padding, Is.EqualTo(new Thickness(20, 0)),
          "and it wears its inner ground as the line's other plaques do, not as the information screen's row does");
      window.Close();
    }

    /// <summary>
    /// The writing a record wears is asked for under the two names this server's libraries keep it under, and it is
    /// worn over the story - never in the row under the play button, where a record's name would stand twice.
    /// </summary>
    /// <remarks>
    /// Measured over the household's library on 2026-11-03, one request per album: <c>ClearArt</c> answers for three
    /// albums, <c>Logo</c> for thirty nine, and <c>ClearLogo</c> - the name the reference's film screen wrote
    /// (<c>MediaItemListMovieDetails.inc:208-213</c>) - answers with a refusal for every title, because that is not
    /// one of this server's kinds (<c>FanArtConstants.cs:123</c> names the kind <c>Logo</c>).  The album therefore
    /// asks the reference's first question (<c>MediaItemListAudioAlbumDetails.inc:26-31</c>) and, when it is answered
    /// with nothing, the library's second.  A film keeps its lettering in the head's row and asks once.
    /// </remarks>
    [AvaloniaTest]
    public void ARecordWearsTheLetteringItsLibraryKeepsUnderThatName()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Born This Way\",\"year\":2011,\"genres\":[\"Dance\"],"
          + "\"durationSeconds\":3114,\"trackCount\":23,\"discCount\":2");
      server.Lettering = "ClearArt";
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Born This Way", null, 2011, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the lettering to arrive", delegate { return browser.DetailsAlbumLettering != null; });

      Assert.That(LettereingsAsked(server).First(), Is.EqualTo("ClearArt"),
          "the record's own art is asked for first, which is the name the reference's album screen wrote");
      Assert.That(LettereingsAsked(server), Has.None.EqualTo("Logo"),
          "the band's logo is the SECOND question and has nothing to look for once the first was answered");
      Assert.That(LettereingsAsked(server), Has.None.EqualTo("ClearLogo"),
          "the film screen's name for its lettering is not one of this server's kinds, and asking for it is "
          + "answered with a refusal for every title in the library");
      Assert.That(browser.DetailsClearArt, Is.Null,
          "and the record's writing is not the second clear art of the row under the play button, which would "
          + "stand the same name twice on one screen - once of it without a bar underneath");
    }

    /// <summary>
    /// A record whose library keeps no clear art asks the library's second question and wears that answer: thirty
    /// nine of the household's forty three albums are filed under <c>Logo</c> and would stand bare otherwise.
    /// </summary>
    [AvaloniaTest]
    public void ARecordWithoutClearArtAsksForTheBandsLogo()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"amo\",\"year\":2019,\"genres\":[\"Rock\"],"
          + "\"durationSeconds\":3114,\"trackCount\":13");
      server.Lettering = "Logo";
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "amo", null, 2019, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the lettering to arrive", delegate { return browser.DetailsAlbumLettering != null; });

      string[] asked = LettereingsAsked(server);
      Assert.That(asked.First(), Is.EqualTo("ClearArt"), "the reference's name is asked first, as ever");
      Assert.That(asked, Contains.Item("Logo"),
          "and when that is answered with nothing the library's own name is asked, which is where the great "
          + "majority of the records on this server have their writing");
      Assert.That(asked, Has.None.EqualTo("ClearLogo"), "the film screen's name is never asked");
      Assert.That(browser.DetailsClearArt, Is.Null, "and it is worn over the story, not under the play button");
    }

    /// <summary>
    /// Which kinds of picture the screen asked this server for, in the order the questions came in.
    /// </summary>
    private static string[] LettereingsAsked(DetailsServer server)
    {
      lock (server.LetteringAsked)
        return server.LetteringAsked.ToArray();
    }

    /// <summary>
    /// A film asks once about its lettering, under the one name its screen always wrote, and takes the answer into
    /// the row under the play button - which is where a record's writing must NOT go.
    /// </summary>
    [AvaloniaTest]
    public void AFilmAsksOnceAboutItsLetteringAndTakesItIntoThePlayersRow()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997}");
      server.Lettering = "ClearArt";
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the lettering to arrive", delegate { return browser.DetailsClearArt != null; });

      Assert.That(browser.DetailsAlbumLettering, Is.Null,
            "a film's lettering is not a record's writing: the two are worn at different places of the screen");
      Assert.That(LettereingsAsked(server), Has.None.EqualTo("Logo"),
          "a film never asks the record's second question - no film in this library is filed under it");
      Assert.That(LettereingsAsked(server), Has.None.EqualTo("DiscArt"),
          "and a film never asks about a disc either: a poster of a film has nothing round to stand on, and only "
          + "a record lays one under its cover");
      Assert.That(browser.DetailsAlbumDisc, Is.Null,
          "so there is no disc to lay anywhere on a film's screen");
    }

    /// <summary>
    /// An album whose library keeps a picture of its disc lays that disc <em>behind</em> its cover, lets forty
    /// percent of it look out over the cover's left edge, and tells its story under the two - in the whole width
    /// the cover leaves behind.
    /// </summary>
    /// <remarks>
    /// The reference laid both pictures into the cover's own box and let the disc look out from under the cover:
    /// four hundred twenty lines of disc behind four hundred forty of cover in a box of four hundred fifty six,
    /// pushed a hundred ninety of its own four hundred twenty beyond the cover it lay under
    /// (<c>MediaItemListAudioAlbumDetails.inc:41-44</c>, the cover at <c>:52</c>, the box at <c>:33</c>) - five
    /// parts of eleven showing.  This panel turns that to the left and shows four parts of ten, which is what the
    /// viewer asked for on 2026-11-03; the size of the two, the order they are painted in and the fact that the
    /// story leaves the disc's column are the reference's and the viewer's.
    /// <para>
    /// Measured over the household's library on 2026-11-03, one request per album: nine of forty three albums
    /// answer <c>DiscArt</c>, and their files are square scans of a printed disc (1000 × 1000), which is why the
    /// frame below cuts them round - a square with a picture of a disc on it reads as a second cover.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void AnAlbumWithADiscLaysItBehindItsCoverAndTellsItsStoryUnderneath()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Farbenspiel\",\"year\":2013,\"genres\":[\"Deutscher Schlager\"],"
          + "\"durationSeconds\":4213,\"trackCount\":18,"
          + "\"overview\":\"Zwolf Lieder, von einem Farbenstreifen zusammengehalten.\"}");
      server.Cover = "Poster";
      server.Disc = "DiscArt";
      server.Lettering = "Logo";
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Farbenspiel", null, 2013, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the disc to arrive", delegate { return browser.DetailsAlbumDisc != null; });
      WaitUntil("the cover to arrive", delegate { return browser.DetailsArtwork != null; });

      Assert.That(LettereingsAsked(server), Contains.Item("DiscArt"),
          "the disc is asked of the same picture door the cover and the writing come out of");

      WorkflowManager workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Musik", browser));
      window.Show();
      Pump();

      (double X, double Y, double Right, double Bottom) Frame(Visual picture)
      {
        Point top = picture.TranslatePoint(new Point(0, 0), window) ?? new Point(-1, -1);
        return (top.X, top.Y, top.X + picture.Bounds.Width, top.Y + picture.Bounds.Height);
      }

      Border disc = window.GetVisualDescendants().OfType<Border>().FirstOrDefault(
          delegate (Border plate) { return plate.Name == "discBehindCover" && plate.IsVisible; });
      Border cover = window.GetVisualDescendants().OfType<Border>().FirstOrDefault(
          delegate (Border plate) { return plate.Name == "coverOnDisc" && plate.IsVisible; });
      Assert.That(disc, Is.Not.Null, "the disc of the record is drawn");
      Assert.That(cover, Is.Not.Null, "and its cover is drawn over it");

      Border alone = window.GetVisualDescendants().OfType<Border>().First(
          delegate (Border plate) { return plate.Name == "coverAlone"; });
      Assert.That(alone.IsVisible, Is.False,
          "and the cover is drawn once: not once plain at the old place and once again over its disc");

      (double X, double Y, double Right, double Bottom) laid = Frame(disc);
      (double X, double Y, double Right, double Bottom) over = Frame(cover);

      Assert.That(disc.CornerRadius.TopLeft, Is.EqualTo(disc.Bounds.Width / 2).Within(1),
          "the disc is cut round, because the file is a square scan of a printed disc and a square of it would "
          + "read as a second cover rather than as the disc the record was pressed on");
      double showing = over.X - laid.X;
      Assert.That(showing, Is.GreaterThan(0.0), "the disc looks out past the cover's left edge");
      Assert.That(showing / (laid.Right - laid.X), Is.EqualTo(0.4).Within(0.02),
          "and it looks out by forty percent of itself, which is the reference's measure of five parts in eleven "
          + "turned to the side the story can be moved out of");
      Assert.That((laid.Y + laid.Bottom) / 2, Is.EqualTo((over.Y + over.Bottom) / 2).Within(2),
          "the disc's middle stands on the cover's middle, as the reference centred the two in one box");
      Assert.That(laid.X, Is.LessThan(over.X), "and it looks out to the left, not into the panel's edge");

      ScrollViewer story = window.GetVisualDescendants().OfType<ScrollViewer>().First(
          delegate (ScrollViewer reading) { return reading.Name == "plotScroller"; });
      (double X, double Y, double Right, double Bottom) told = Frame(story);
      Assert.That(told.Y, Is.GreaterThanOrEqualTo(over.Bottom - 1),
          "the story has moved under the cover, because the disc needs the column the story stood in");
      Assert.That(told.Right, Is.GreaterThanOrEqualTo(over.Right - 2),
          "and it is told over the whole width the cover left, which is what the column beside the cover never was");
      window.Close();
    }

    /// <summary>
    /// An album whose library keeps no disc - thirty four of the household's forty three - keeps its cover standing
    /// alone and its story standing beside it, exactly as both stood before the disc was ever asked about.
    /// </summary>
    [AvaloniaTest]
    public void AnAlbumWithoutADiscKeepsItsCoverAloneAndItsStoryBesideIt()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Born This Way\",\"year\":2011,\"genres\":[\"Dance\"],"
          + "\"durationSeconds\":6300,\"trackCount\":23,\"discCount\":2,"
          + "\"overview\":\"Zwanzig Lieder, geschrieben auf den Hochzeitsreisen einer Band.\"}");
      server.Cover = "Poster";
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Born This Way", null, 2011, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the cover to arrive", delegate { return browser.DetailsArtwork != null; });

      Assert.That(browser.DetailsAlbumDisc, Is.Null, "a library that keeps no disc is answered with a refusal");
      Assert.That(browser.ShowsDiscBehindCover, Is.False, "and the panel then says nothing about a disc");

      WorkflowManager workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Musik", browser));
      window.Show();
      Pump();

      (double X, double Y, double Right, double Bottom) Frame(Visual picture)
      {
        Point top = picture.TranslatePoint(new Point(0, 0), window) ?? new Point(-1, -1);
        return (top.X, top.Y, top.X + picture.Bounds.Width, top.Y + picture.Bounds.Height);
      }

      Assert.That(window.GetVisualDescendants().OfType<Panel>().FirstOrDefault(
          delegate (Panel pair) { return pair.Name == "discAndCover" && pair.IsVisible; }), Is.Null,
          "no disc is drawn where the library keeps none");

      Border cover = window.GetVisualDescendants().OfType<Border>().First(
          delegate (Border plate) { return plate.Name == "coverAlone" && plate.IsVisible; });
      Assert.That(cover, Is.Not.Null, "the cover stands by itself, as it always did");

      ScrollViewer story = window.GetVisualDescendants().OfType<ScrollViewer>().First(
          delegate (ScrollViewer reading) { return reading.Name == "plotScroller"; });
      (double X, double Y, double Right, double Bottom) told = Frame(story);
      (double X, double Y, double Right, double Bottom) over = Frame(cover);
      Assert.That(told.Right, Is.LessThanOrEqualTo(over.X + 1),
          "the story keeps the column beside the cover and does not run under it");
      Assert.That(told.Y, Is.LessThan(over.Bottom - 100),
          "and it begins at the cover's top, which is where a story beside a cover begins");
      window.Close();
    }

    /// <summary>
    /// A reading of sound never asks whether the file carries subtitles, on either of its two shelves.
    /// </summary>
    /// <remarks>
    /// The plaque answers the question "does this file carry text?" with a framed "CC" or with the same frame
    /// crossed out, and a sound file has no answer to give: it cannot carry subtitles, so the crossed frame would
    /// deny a possibility nobody proposed (the viewer's word on 2026-11-03: "meaningless, because audiofiles never
    /// have subtitles").  The reference asked the question in its video screens only
    /// (<c>MediaInfoVideo.xaml:157-174</c>, <c>CurrentlyPlayingVideo.xaml:164-181</c>); its audio screen asks about
    /// the packing, the loudspeakers, the disc and the two rates, and about nothing else
    /// (<c>MediaInfoAudio.xaml:115-170</c>).
    /// </remarks>
    [AvaloniaTest]
    public void AReadingOfSoundNeverAsksAboutSubtitles()
    {
      foreach (string shelf in new[] { "AudioItem", "AlbumItem" })
      {
        DetailsServer server = new DetailsServer(HttpStatusCode.OK,
            "{\"id\":\"" + TITANIC + "\",\"title\":\"So What\",\"year\":1959,\"audioEncoding\":\"Flac Audio\","
            + "\"audioChannels\":2,\"audioBitRate\":1023,\"audioSampleRate\":44100,\"discId\":1,\"discCount\":1}");
        MediaBrowserViewModel browser = new MediaBrowserViewModel(
            new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled(shelf));
        browser.Selected = new TitleViewModel(
            new MediaTitle(new Guid(TITANIC), "So What", null, 1959, null, null, false));

        WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

        Assert.That(browser.ShowsAudioLine, Is.True, "the shelf " + shelf + " did read its own row of sound");
        Assert.That(browser.ShowsSubtitlePlaque, Is.False,
            "a shelf of sound asks no question about subtitles, and neither does its answer");

        var workflow = new WorkflowManager();
        ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
        {
          return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
        });
        window.Width = 1920;
        window.Height = 1080;
        workflow.Push(new WorkflowFrame("titles", "Musik", browser));
        window.Show();
        Pump();

        TextBlock head = window.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(
            delegate (TextBlock word) { return word.Text == "Audio" && word.IsVisible; });
        Assert.That(head, Is.Not.Null,
            "the reading of sound was drawn, so what follows is said about a screen and not about an empty one");

        Border[] plaques = window.GetVisualDescendants().OfType<Border>()
            .Where(delegate (Border plaque) { return plaque.Name == "subtitlesPlaque"; })
            .ToArray();
        Assert.That(plaques, Has.Length.EqualTo(1),
            "the plaque is still built into the view - a film wears it, and this is the same view");
        Assert.That(plaques[0].IsVisible, Is.False,
            "but on a reading of sound it is not shown, so no song is asked about subtitles");
        window.Close();
      }
    }

    /// <summary>
    /// That the film keeps the question the song lost: a picture is still asked whether it carries text.
    /// </summary>
    [AvaloniaTest]
    public void AReadingOfAPictureStillAsksAboutSubtitles()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997,\"runtimeMinutes\":195}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.ShowsSubtitlePlaque, Is.True,
          "a film wears the plaque whether its file carries subtitles or not - the denial is an answer there");
    }

    /// <summary>
    /// That a piece of music nobody wrote about says nothing at all, while a picture still admits the silence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sentence "Nothing was written about this title on this server." answers a question a viewer of music
    /// never asked: the reference kept a story block in its VIDEO screens (StoryPlot through the VideoAspectWrapper
    /// at <c>CurrentlyPlayingVideoDetails.inc:59</c>, <c>CurrentlyPlayingMovieDetails.inc:88</c>,
    /// <c>CurrentlyPlayingSeriesDetails.inc:339</c>) and had none on its audio screen
    /// (<c>CurrentlyPlayingAudioDetails.inc</c>).  Where a record does carry prose - an album description a tag
    /// reader wrote - it is still said, in the album's own screens
    /// (<c>MediaItemListAudioAlbumDetails.inc:307</c>).
    /// </para>
    /// <para>
    /// "Empty means empty" (the viewer's word on 2026-11-03), and the empty story block costs its room too: an
    /// empty strip under the facts reads as a broken panel.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void ASongNothingWasWrittenAboutSaysNothingAtAll()
    {
      foreach (string shelf in new[] { "AlbumItem", "AudioItem" })
      {
          DetailsServer unwritten = new DetailsServer(HttpStatusCode.OK,
              "{\"id\":\"" + TITANIC + "\",\"title\":\"So What\",\"year\":1959,"
              + "\"audioEncoding\":\"Flac Audio\",\"audioChannels\":2}");
          MediaBrowserViewModel browser = new MediaBrowserViewModel(
              new ServerApiClient(SIGN_IN_URL, new HttpClient(unwritten)), ShelfCalled(shelf));
          browser.Selected = new TitleViewModel(
              new MediaTitle(new Guid(TITANIC), "So What", null, 1959, null, null, false));

          WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

          Assert.That(browser.DetailsText, Is.EqualTo(string.Empty),
              "the shelf " + shelf + " has no story to tell and says so by saying nothing, instead of announcing "
              + "that this song is missing something a song cannot have");

          Assert.That(DrawnStoryTakesRoom(browser, shelf), Is.False,
              "with nothing to read the story block takes no room, since an empty strip under the facts is read "
              + "as a broken panel");
      }

      // Prose that exists is still said, in the library's own words, and takes its room back.
      DetailsServer written = new DetailsServer(HttpStatusCode.OK,
            "{\"id\":\"" + TITANIC + "\",\"title\":\"Kind of Blue\",\"year\":1959,"
            + "\"overview\":\"Recorded in two sessions at Columbia's 30th Street studio.\"}");
      MediaBrowserViewModel described = new MediaBrowserViewModel(
            new ServerApiClient(SIGN_IN_URL, new HttpClient(written)), ShelfCalled("AlbumItem"));
      described.Selected = new TitleViewModel(
            new MediaTitle(new Guid(TITANIC), "Kind of Blue", null, 1959, null, null, false));

      WaitUntil("the album's own prose to arrive", delegate { return described.DetailsText.Length > 0; });
      Assert.That(described.DetailsText,
            Is.EqualTo("Recorded in two sessions at Columbia's 30th Street studio."),
            "holding its tongue about an undescribed record must not silence the records somebody described");
      Assert.That(DrawnStoryTakesRoom(described, "AlbumItem"), Is.True,
            "and the prose a record does carry is drawn in the block the silent one vacated");

      // The admission the music lost stays where it always stood: on a picture, whose story is the question.
      DetailsServer film = new DetailsServer(HttpStatusCode.OK,
            "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997,\"runtimeMinutes\":195}");
      MediaBrowserViewModel picture = BrowserOn(film);
      picture.Selected = new TitleViewModel(
            new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      WaitUntil("the film's admission", delegate { return picture.DetailsText.Length > 0; });
      Assert.That(picture.DetailsText, Is.EqualTo("Nothing was written about this title on this server."),
            "a film without a plot is missing something, and that is worth saying");
    }

    /// <summary>
    /// Whether the story block of a reading is drawn and takes room on the screen.
    /// </summary>
    /// <remarks>
    /// Asked of the scroller and not of the letters inside it: the block is declared in the view, while its text
    /// is only realized once the scroller is shown, so an absent TextBlock would prove nothing on its own.
    /// </remarks>
    private static bool DrawnStoryTakesRoom(MediaBrowserViewModel browser, string aspect)
    {
      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Musik", browser));
      window.Show();
      Pump();

      ScrollViewer story = window.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(
            delegate (ScrollViewer scroller) { return scroller.Name == "plotScroller"; });
      Assert.That(story, Is.Not.Null,
            "the story block is still built into the view - a film reads its plot in this very one " + aspect);
      bool takesRoom = story.IsVisible;
      window.Close();
      return takesRoom;
    }

    /// <summary>
    /// The three bands of people a piece of music names, and the roles they are asked for.
    /// </summary>
    /// <remarks>
    /// The reference's audio screen asks for artist, album artist and composer and lays all three out under each
    /// other, in three columns (<c>MediaInfoAudio.xaml:204-296</c>), while its film screens ask for actors and
    /// directors.  A song read as a film names nobody at all, because the library files the musician under a
    /// relationship the film question never asks about.
    /// </remarks>
    [AvaloniaTest]
    public void ASongNamesItsArtistItsAlbumArtistAndItsComposer()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Blue in Green\",\"year\":1959}",
          null!, null!,
          "{\"id\":\"" + TITANIC + "\",\"groups\":["
          + "{\"role\":\"artist\",\"persons\":[{\"id\":\"11111111-1111-1111-1111-111111111111\","
          + "\"name\":\"Miles Davis\"}]},"
          + "{\"role\":\"albumartist\",\"persons\":[{\"id\":\"22222222-2222-2222-2222-222222222222\","
          + "\"name\":\"Columbia Records\"}]},"
          + "{\"role\":\"composer\",\"persons\":[{\"id\":\"33333333-3333-3333-3333-333333333333\","
          + "\"name\":\"Bill Evans\"}]}]}");
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Blue in Green", null, 1959, null, null, false));

      WaitUntil("the three bands to fill", delegate { return browser.ShowsMusicPersons; });

      Assert.That(browser.DetailsArtists.Select(delegate (PersonCard face) { return face.Name; }),
          Is.EqualTo(new[] { "Miles Davis" }), "who sang it");
      Assert.That(browser.DetailsAlbumArtists.Select(delegate (PersonCard face) { return face.Name; }),
          Is.EqualTo(new[] { "Columbia Records" }), "who published it, which is whom a collector looks for");
      Assert.That(browser.DetailsComposers.Select(delegate (PersonCard face) { return face.Name; }),
          Is.EqualTo(new[] { "Bill Evans" }), "who wrote it, which for a standard is somebody else again");
      Assert.That(browser.ShowsPersons, Is.False,
          "a song names no actors and no director, so the two bands a film fills stay away from it");

      Assert.That(server.RolesAsked, Is.EqualTo("?roles=artist,albumartist,composer"),
          "the three roles are asked for by the names the library files its relationships under - asking for "
          + "them by any other name comes back empty, and an empty band cannot be told from a song without "
          + "musicians");
    }

    /// <summary>
    /// That the film's reading is untouched by the song's: the two people it asks for, and no row of sound.
    /// </summary>
    [AvaloniaTest]
    public void AFilmStillNamesItsActorsAndItsDirectorAndWearsNoRowOfSound()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997,\"runtimeMinutes\":195}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      WaitUntil("the people to have been asked for", delegate { return server.PeopleAsked > 0; });
      Pump();

      Assert.That(server.RolesAsked, Is.EqualTo("?roles=actor,director"),
          "the film's question is still the film's question");
      Assert.That(browser.ShowsAudioLine, Is.False,
          "a film's answer carries no figures of its own, so no row of blanks stands where the sound row would "
          + "stand");
      Assert.That(browser.ShowsMusicPersons, Is.False, "and no band of musicians stands under it either");

      MediaBrowserFact? length = browser.DetailsFacts.FirstOrDefault(
          delegate (MediaBrowserFact fact) { return fact.Label == "Runtime"; });
      Assert.That(length, Is.Not.Null, "a film is asked for its length under the word a film has always used");
      Assert.That(length!.Value, Is.EqualTo("3 h 15 min"),
          "and in the film's own spelling of the same length: the song's shape came in for the song, not to "
          + "rewrite the film");
    }

    /// <summary>
    /// Where the answers about the file are read, measured on the laid out screen and not off a property list.
    /// Asked for by the viewer on 2026-11-02: the picture is read on the reader's own side of the reading, under
    /// the word "Video", while five answers - the codec, the size, the HDR, the first track's channels and whether
    /// the file carries text at all - are read at the right end of the rating's line, the rating itself keeping
    /// the line's left end.
    /// </summary>
    [AvaloniaTest]
    public void ThePictureIsReadOnTheReadersSideAndFiveAnswersAreReadBehindTheRating()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Dune\",\"year\":2021,\"rating\":0,\"totalRating\":7.3}",
          null,
          "{\"videos\":[{\"streamIndex\":0,\"videoType\":\"HD\",\"videoEncoding\":\"HEVC\",\"width\":1920,"
          + "\"height\":1080,\"aspectRatio\":2.39,\"bitRate\":30000,\"fps\":23.976,\"durationSeconds\":7080}],"
          + "\"audios\":[{\"streamIndex\":1,\"language\":\"de\",\"encoding\":\"AC3\",\"channels\":6,"
          + "\"bitRate\":640,\"sampleRate\":48000}],"
          + "\"subtitles\":[{\"streamIndex\":2,\"language\":\"de\",\"format\":\"SRT\",\"isInternal\":true,"
          + "\"isForced\":false}]}");
      MediaBrowserViewModel browser = BrowserOn(server);
      TitleViewModel title = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Dune", null, 2021, null, null, false));
      browser.Titles.Add(title);
      browser.Selected = title;
      WaitUntil("the tracks to arrive", delegate { return browser.ShowsAudioTracks; });

      var workflow = new WorkflowManager();
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        return new MediaBrowserView((MediaBrowserViewModel) frame.ViewModel);
      });
      window.Width = 1920;
      window.Height = 1080;
      workflow.Push(new WorkflowFrame("titles", "Filme", browser));
      window.Show();
      Pump();

      MediaTitleAction reading = MediaTitleActions.For(title, true, true)
          .First(action => action.Kind == MediaTitleActionKind.Information);
      ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>) browser.RunTitleActionCommand)
          .ExecuteAsync(reading).Wait();
      Pump();

      // Where a thing is drawn, which is the whole point of the move: a property that holds the figure says
      // nothing about the place the viewer reads it from.  Measured against the window, which is the frame a
      // viewer's eye has; an unattached visual answers no question about a place at all.
      (double X, double Y, double Right, double Bottom) Frame(Visual visual)
      {
        Point top = visual.TranslatePoint(new Point(0, 0), window) ?? new Point(-1, -1);
        return (top.X, top.Y, top.X + visual.Bounds.Width, top.Y + visual.Bounds.Height);
      }

      // The three headings are words, in the order the viewer gave them, and they stand on the reader's side.
      // They are the one place of this screen where a localized word is written into the skin on purpose: the
      // viewer asked for these three words on 2026-11-02, having first been shown headings made of glyphs.
      (string Word, double X, double Y)[] headings = window.GetVisualDescendants()
          .OfType<TextBlock>()
          .Where(t => t.Text is "Video" or "Audiotrack" or "Subtitle")
          .Select(t => (t.Text!, Frame(t).X, Frame(t).Y))
          .OrderBy(h => h.Y)
          .ToArray();
      Assert.That(headings.Select(h => h.Word), Is.EqualTo(new[] { "Video", "Audiotrack", "Subtitle" }),
          "the three headings stand in the order the viewer gave, got "
          + string.Join(", ", headings.Select(h => "'" + h.Word + "' at " + h.Y.ToString("F0"))));
      Assert.That(headings, Has.Length.EqualTo(3),
          "three headings are written, and no fourth word crept in beside them");

      // The rate of the picture - an answer that used to stand in the details - is read on that side too.  Sought
      // by its figure and not by the whole line, because a file that knows its bitrate says that on the same line.
      TextBlock rate = window.GetVisualDescendants().OfType<TextBlock>()
          .Single(t => (t.Text ?? "").Contains("24 fps", StringComparison.Ordinal));
      Assert.That(Frame(rate).X, Is.LessThan(window.Width / 2),
          "the figure travelled out of the details column to the reader's side");

      // All seven answers of the picture in one row of a narrower column is a row that can run over its edge, so
      // the row is measured against the column it must stay inside of: the title of the reading names where the
      // words' column begins, and no plaque of the reader's side may reach into it.
      TextBlock name = window.GetVisualDescendants().OfType<TextBlock>()
          .First(t => t.Text == "Dune");
      foreach (Border plaque in window.GetVisualDescendants().OfType<Border>()
                   .Where(b => b.Classes.Contains("details-logo") && Frame(b).X < Frame(name).X))
      {
        Assert.That(Frame(plaque).Right, Is.LessThanOrEqualTo(Frame(name).X),
            "a plaque of the reader's side reaches into the column of words - the row of the picture's answers "
            + "is wider than the side it stands on");
      }

      // And what is kept in the details is kept there IN ADDITION TO being read here - the viewer had to say so
      // twice, because a first reading of the request moved the answers instead of copying them.  The codec is the
      // one to measure with: it is read on the reader's side and behind the rating, and nowhere else.
      TextBlock[] codecs = window.GetVisualDescendants().OfType<TextBlock>()
          .Where(t => t.Text == "HEVC")
          .ToArray();
      Assert.That(codecs, Has.Length.EqualTo(2),
          "the codec is read twice over, once under the word \"Video\" and once behind the rating, got "
          + codecs.Length.ToString());

      // And what stayed, stayed ON the row of the rating: behind it, with ground between, on the same line.  The
      // row outlives a title without stars - which is why the rating stands in a panel of its own inside it -
      // but where a rating is read, the plaques are read behind it.
      TextBlock rating = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "(7.3)");
      Border[] plaques = window.GetVisualDescendants().OfType<Border>()
          .Where(b => b.Classes.Contains("details-logo"))
          .ToArray();
      Border first = plaques.FirstOrDefault(b => Frame(b).X > Frame(rating).Right)!;
      Assert.That(first, Is.Not.Null,
          "no plaque of the row stands right of the rating - the rating is at "
          + Frame(rating).X.ToString("F0") + "," + Frame(rating).Y.ToString("F0")
          + " and the plaques of the screen are at "
          + string.Join(" | ", plaques.Select(p => Frame(p).X.ToString("F0") + "," + Frame(p).Y.ToString("F0"))));

      (double X, double Y, double Right, double Bottom) words = Frame(rating);
      (double X, double Y, double Right, double Bottom) kept = Frame(first);
      Assert.That(kept.X, Is.GreaterThan(words.Right),
          "the plaques stand behind the rating and not in front of it");
      Assert.That(kept.X - words.Right, Is.GreaterThanOrEqualTo(8),
          "a bit of ground lies between the rating and the plaques, as asked");
      Assert.That(kept.Bottom > words.Y && kept.Y < words.Bottom, Is.True,
          "the plaques are on the rating's own row, not under it");

      // The plaques of that row wear their inner ground on themselves - twenty points on each side, written on
      // every one of them - so that a logo of one width and a word of another stand in boxes that are measured
      // the same way.  Asked for on 2026-11-02 for these four alone; the reader's own row keeps the style's padding.
      foreach (Border plaque in window.GetVisualDescendants().OfType<Border>()
                   .Where(b => b.Classes.Contains("details-logo") && Frame(b).X > Frame(rating).Right))
      {
        Assert.That(plaque.Padding, Is.EqualTo(new Thickness(20, 0)),
            "the plaque of the rating's row carries its padding as its own, and not from the style");
      }

      // The two ends of the line: the plaques are pushed to the right edge of the line while the rating keeps the
      // left edge it has always had (the viewer's word on 2026-11-02, and not a centre they crowd around).
      StackPanel plaqueRow = (StackPanel) first.Parent!;
      Visual line = (Visual) plaqueRow.Parent!;
      Assert.That(Frame(plaqueRow).Right, Is.EqualTo(Frame(line).Right).Within(1),
          "the plaques are read at the right edge of the line");
      Visual ratingGroup = (Visual) rating.Parent!;
      Assert.That(Frame(ratingGroup).X, Is.EqualTo(Frame(line).X).Within(1),
          "and the rating keeps the left edge");

      // The framed "CC" travelled from the reader's side to this row and was NOT left behind there, which is what
      // the viewer insisted on: "richtig, hier nicht kopieren".  One plaque of that name on the whole screen.
      Border[] subtitles = window.GetVisualDescendants().OfType<Border>()
          .Where(b => b.Name == "subtitlesPlaque")
          .ToArray();
      Assert.That(subtitles, Has.Length.EqualTo(1),
          "the subtitle plaque is moved, not copied - one on the screen, got " + subtitles.Length.ToString());
      (double X, double Y, double Right, double Bottom) text = Frame(subtitles[0]);
      Assert.That(text.Bottom > words.Y && text.Y < words.Bottom, Is.True,
          "and it stands on the rating's own row, with the rest of the row's answers");
      window.Close();
    }

    /// <summary>
    /// A title whose server never looked inside its file says nothing about tracks, and the band under the story
    /// costs no room at all - an empty frame there would only show the viewer that something is missing.
    /// </summary>
    [AvaloniaTest]
    public void ATitleNobodyLookedInsideSaysNothingAboutItsTracks()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Titanic\",\"year\":1997}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Titanic", null, 1997, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });
      WaitUntil("the tracks to be asked and answered", delegate { return server.TracksAsked > 0; });

      Assert.That(browser.DetailsPicture, Is.SameAs(PictureLine.Empty),
          "nothing was ever measured, so the row above the story is not asked for at all");
      Assert.That(browser.ShowsTracks, Is.False,
          "three empty lists are the ordinary answer of a library and not a hole to draw a frame around");
    }

    /// <summary>
    /// The subtitle answer of the tag band is the skin's own glyph, and both of its faces are on disk as shapes.
    /// A file the reader cannot parse is worse than a missing one: the chip stands there and draws nothing, and a
    /// picture line whose first cell is empty reads as a film without subtitles rather than as a picture the file
    /// carries.
    /// </summary>
    [AvaloniaTest]
    public void TheSubtitleGlyphOfTheTagBandIsAShapeTheSkinCarries()
    {
      Application app = Application.Current!;
      string previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? _);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");
        WornSkin? worn = SkinEngine.Worn(app);
        Assert.That(worn, Is.Not.Null, "the shipped skin came off the disk and onto the application");

        foreach (string face in new[] { "PlayerControl/subtitles", "PlayerControl/subtitles_off" })
        {
          Assert.That(worn!.Vectors.Has(face), Is.True,
              "the tag band asks the skin for '" + face + "' and the skin carries no icon of that name");

          Geometry? shape = worn.Vectors.Load(face);
          Assert.That(shape, Is.Not.Null, "'" + face + "' is on disk and did not read as a shape");

          Rect bounds = shape!.Bounds;
          Assert.That(bounds.Width, Is.GreaterThan(0.0), face + " read as a shape with nothing in it");
          Assert.That(bounds.Height, Is.GreaterThan(0.0), face + " read as a shape with nothing in it");
          Assert.That(bounds.X, Is.GreaterThanOrEqualTo(-0.5), face + " stands left of the square");
          Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(-0.5), face + " stands above the square");
          Assert.That(bounds.Right, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
              face + " reaches out of the square to the right");
          Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
              face + " reaches out of the square downwards");
        }
      }
      finally
      {
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    /// <summary>
    /// The row of pictures above the story and the corners of the track cards ask the skin for the names the old
    /// skin filed its logos under, and for every figure this library actually holds the skin carries the picture -
    /// except where the old catalog itself never had one, which is the case the cells answer in words instead of
    /// drawing a hole.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinCarriesTheLogoOfEveryFactTheLibraryWrites()
    {
      Application app = Application.Current!;
      string previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? _);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");
        WornSkin? worn = SkinEngine.Worn(app);
        Assert.That(worn, Is.Not.Null, "the shipped skin came off the disk and onto the application");

        // The names are what MediaStreamLogos makes out of the figures this library holds: its languages ("de"
        // written for audio, "DE" for subtitles, which is one and the same file to the catalog), its channel
        // counts, the codecs the importer records since the fallback was added, the sizes the reference's own
        // bounds name, and the three pictures the reference fixed in its skin.
        foreach (string face in new[]
        {
            "MediaLogos/flags/de", "MediaLogos/flags/en", "MediaLogos/flags/es", "MediaLogos/flags/tr",
            "MediaLogos/flags/Unknown",
            "MediaLogos/audio/6", "MediaLogos/audio/2", "MediaLogos/audio/8", "MediaLogos/audio/DTS",
            "MediaLogos/audio/AAC", "MediaLogos/audio/mp3", "MediaLogos/audio/flac",
            "MediaLogos/video/AVC", "MediaLogos/video/HEVC", "MediaLogos/video/XviD", "MediaLogos/video/3d",
            "MediaLogos/resolution/576", "MediaLogos/resolution/720p", "MediaLogos/resolution/1080p",
            "MediaLogos/resolution/2160p",
            "MediaLogos/subtitle/Internal", "MediaLogos/subtitle/External", "MediaLogos/subtitle/Frame"
        })
        {
          Assert.That(worn!.Images.Has(face), Is.True,
              "the details ask the skin for '" + face + "' and the skin carries no picture of that name");
          Assert.That(worn.Images.Load(face), Is.Not.Null, face + " is on disk and did not read as a picture");
        }

        // The one honest hole, and the reason the two tables in MediaStreamLogos exist: the old catalog filed
        // Dolby Digital under Dolby's name and the Japanese flag under the country's code, while the reference
        // bound the library's own value ("AC-3", "ja") straight into the name - so it drew nothing for the most
        // common audio codec in a German library and for every language its folder did not spell in ISO 639-1.
        // A value no row speaks for still ends in a name the skin has no picture of, and the word fallback is
        // what the card draws then; this sentence is that sentence.
        Assert.That(worn.Images.Has("MediaLogos/audio/AC-3"), Is.False,
            "if the skin ever carries this picture, the audio table's row for it is doing nothing and has to be "
            + "told again");
        Assert.That(worn.Images.Has("MediaLogos/flags/" + MediaStreamLogos.Flag("sq")), Is.False,
            "a language no row speaks for asks the skin for a picture it has none of, which is what the word on "
            + "the card is for");

        // The proportion's glyph is not a picture but a shape, and the row asks for it by name the same way.
        // Without it the cell stands with its figure and no frame, which is a hole in the row rather than a
        // figure written in words: a glyph is the one answer no fallback can spell out.
        Assert.That(worn.Vectors.Has("MediaInfo/aspect_ratio"), Is.True,
            "the proportion's cell draws the frame glyph the viewer drew for it, which this skin ships as a "
            + "vector under MediaInfo/aspect_ratio");
        Assert.That(worn.Vectors.Load("MediaInfo/aspect_ratio"), Is.Not.Null,
            "the file is on disk and did not read as a shape");
        Assert.That(worn.Vectors.Has("MediaInfo/frames"), Is.True,
            "the last plaque of the row leads its two figures with the frame glyph, which this skin ships as a "
            + "vector under MediaInfo/frames");
        Assert.That(worn.Vectors.Load("MediaInfo/frames"), Is.Not.Null,
            "the file is on disk and did not read as a shape");
        // The audio band is no longer headed by the track glyph - the viewer asked for the word on 2026-11-02 -
        // but the picture stays in the skin: it is the viewer's own drawing, and a band that later wants a glyph
        // should reach for that one rather than for a new file.

        // What the library actually writes, run through the model and asked of the skin: this is the coverage
        // that was measured by hand once, and it is the promise the cards make - a figure the importer records
        // is drawn as a picture or written as a word, but does not stand there empty.
        foreach (string language in new[]
        {
            "de", "en", "es", "fr", "hu", "it", "nl", "ru", "tr", "ja", "ZH", "da", "el", "cs", "ko", "uk", "he",
            "hi", "nb", "te"
        })
        {
          string flag = "MediaLogos/flags/" + MediaStreamLogos.Flag(language);
          Assert.That(worn.Images.Has(flag), Is.True,
              "the library writes the language '" + language + "' and the skin carries no picture for '" + flag + "'");
        }

        foreach (string codec in new[] { "DTS", "AC-3", "E-AC-3", "MLP FBA", "AAC", "MP3", "FLAC" })
        {
          string logo = "MediaLogos/audio/" + MediaStreamLogos.AudioCodec(codec);
          Assert.That(worn.Images.Has(logo), Is.True,
              "the library writes the codec '" + codec + "' and the skin carries no picture for '" + logo + "'");
        }
      }
      finally
      {
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
      }
    }

    [AvaloniaTest]
    public void AnEpisodeWithoutItsOwnSummaryReadsItsSeriesSentence()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien Earth\",\"year\":2025,"
          + "\"seriesName\":\"Alien Earth\",\"season\":1,\"episode\":1,\"episodeName\":\"Nimmerland\","
          + "\"seriesOverview\":\"A family on a distant world.\"}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien Earth", null, 2025, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsText, Is.EqualTo("A family on a distant world."),
          "when the importer numbered the episodes but never described them, the series' own sentence - the one "
          + "the viewer read one shelf up - is the honest answer, and \"nothing was written\" stays reserved "
          + "for titles where even the series has nothing");
    }

    [AvaloniaTest]
    public void AnEpisodeKeepsItsOwnSentenceWhenTheSeriesLendsOneToo()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Alien Earth\",\"year\":2025,"
          + "\"overview\":\"Troops wake up a century too late.\","
          + "\"seriesName\":\"Alien Earth\",\"season\":1,\"episode\":1,\"episodeName\":\"Nimmerland\","
          + "\"seriesOverview\":\"A family on a distant world.\"}");
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.Selected = new TitleViewModel(
          new MediaTitle(new Guid(TITANIC), "Alien Earth", null, 2025, null, null, false));

      WaitUntil("the details to open", delegate { return browser.DetailsVisible; });

      Assert.That(browser.DetailsText, Is.EqualTo("Troops wake up a century too late."),
          "the lent sentence only speaks where the episode is silent; an episode with a summary of its own is "
          + "never talked over by its series");
    }

    [AvaloniaTest]
    public void TheScreenWearsItsDetailsAndTheKeysTurnThemOffAndOn()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, TITANIC_DETAILS);
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the row to arrive", delegate { return browser.Titles.Count == 1; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      Border panel = window.GetVisualDescendants().OfType<Border>()
          .First(border => border.Name == "detailsPanel");

      // The row is selected as it arrives and the screen wears its details from the start - the panel is
      // what the screen shows, not a favour a key grants.
      titles.Focus();
      WaitUntil("the details to be worn", delegate { return panel.IsVisible; });
      Assert.That(browser.DetailsHeading, Is.EqualTo("Titanic"));
      Assert.That(browser.ShowDetails, Is.True, "a screen wears its details until its viewer says otherwise");
      Assert.That(titles.Items.Count, Is.EqualTo(1), "the list stayed where it was behind the panel");

      // The rail is the screen's own menu, and the room slides aside for it; the info key of the remote is the
      // title's own dialog now, so the rail is asked the way its button asks it. The details keep their own key.
      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      Assert.That(browser.ShowDetails, Is.True, "and the rail left the details alone");
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the menu to fill with its commands", delegate { return rail.Items.Count > 0; });

      // Nothing is painted over the rows for this menu: the room slides the reference's two hundred points out of
      // the way and the menu walks into the space that sliding makes (Animations.xaml:8-38). The sliding itself
      // belongs to the transition on the room's own transform - see the room in the markup for why it is not
      // driven from a timer - so what the screen is asked here is where the room was told to stand.
      WaitUntil("the room to stand aside for the menu", delegate { return view.SlideOffset >= 200; });

      // And the same transform is what the menu's other axis uses: walking from a grid into covers hands the menu
      // from the left edge of the room to its bottom edge, and the room is put where the new menu wants it without
      // sliding across a diagonal (see SlideMenu's atOnce).
      browser.Layout = BrowserLayout.Cover;
      Pump();
      Assert.That(view.SlideOffset, Is.EqualTo(-200),
          "with the menu under a row of covers the room stands two hundred points up");
      browser.Layout = BrowserLayout.Grid;
      Pump();
      Assert.That(view.SlideOffset, Is.EqualTo(200),
          "and two hundred points to the right when the menu is back beside a grid");

      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      WaitUntil("the menu to go away", delegate { return !browser.MediaMenuVisible; });
      WaitUntil("the room to slide back", delegate { return view.SlideOffset == 0; });
      Assert.That(browser.MediaMenuVisible, Is.False, "back puts the menu away again");

      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.D,
        Source = titles
      });
      Pump();
      Assert.That(panel.IsVisible, Is.False, "the details key turns the details away");

      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.D,
        Source = titles
      });
      WaitUntil("the details to come back", delegate { return panel.IsVisible; });
      Assert.That(server.TimesAsked, Is.EqualTo(1),
          "and they come back from what was already read, not from a second question to the server");

      // Back is not the panel's key, and one press of it is one decision: away from this screen.  The wall used
      // to answer the press by taking the details away and swallowing the key, so the viewer stood on the same
      // wall looking at the same wall minus a panel, and had to press a key they had already pressed once a
      // second time to get out.  Nereus binds Escape to nothing on this screen at all (master_mediamenu.xaml:402
      // enables its only Escape binding while the media menu stands open), and the key that turns the panel off
      // is the details key, which the two presses of D above just demonstrated.
      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      Pump();
      Assert.That(panel.IsVisible, Is.True,
          "one press of back is not a press of the details key: the panel is still standing on the wall");
      Assert.That(window.IsVisible, Is.False,
          "and that one press is the way out of the screen, not the second one");
    }

    [AvaloniaTest]
    public void TheWashBehindTheDetailsDarkensTheCornerTheFanartShinesThrough()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK, TITANIC_DETAILS);
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the row to arrive", delegate { return browser.Titles.Count == 1; });

      Border panel = window.GetVisualDescendants().OfType<Border>()
          .First(border => border.Name == "detailsPanel");
      Assert.That(panel.Background, Is.InstanceOf<LinearGradientBrush>(),
          "the details stand on a wash of their own, not on a flat plate");
      LinearGradientBrush wash = (LinearGradientBrush)panel.Background;

      // Avalonia reads a bare number in a gradient point as an amount in device independent pixels, while the
      // reference skin is WPF markup where the same number means the fraction of the panel. Written the WPF way
      // the wash is a line one pixel long in the top left corner and the panel is painted one flat colour, which
      // is what its designer ran into. Only the '%' suffix makes a point follow the panel.
      Assert.That(wash.StartPoint.Unit, Is.EqualTo(RelativeUnit.Relative),
          "the wash starts on a corner of the panel, not on a pixel amount inside it");
      Assert.That(wash.EndPoint.Unit, Is.EqualTo(RelativeUnit.Relative),
          "and it ends on a corner too");
      Assert.That(wash.StartPoint.Point.X, Is.EqualTo(1.0), "it starts at the right edge");
      Assert.That(wash.StartPoint.Point.Y, Is.EqualTo(0.45), "a little under the middle of its top half");
      Assert.That(wash.EndPoint.Point.X, Is.EqualTo(0.03), "and runs to the left edge");
      Assert.That(wash.EndPoint.Point.Y, Is.EqualTo(0.5), "at its middle");

      // Sixty per cent black standing level from the light's corner out to sixty hundredths of the line, so the
      // words sit on an even ground, thinning to nothing only at the reader's own edge.
      Assert.That(((GradientStop)wash.GradientStops![0]!).Color.ToUInt32(), Is.EqualTo(0x99000000),
          "the corner the light comes from is darkened to sixty per cent, which is #99");
      Assert.That(((GradientStop)wash.GradientStops![1]!).Color.ToUInt32(), Is.EqualTo(0x99000000),
          "and it stands at that darkness out to sixty hundredths of the line, rather than starting to thin "
          + "behind the words");
      Assert.That(((GradientStop)wash.GradientStops![wash.GradientStops.Count - 1]!).Color.ToUInt32(),
          Is.EqualTo(0x00000000), "and the far corner is left alone completely");
      window.Close();
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
        return client.GetTitlesAsync("MediaItem", null, false, 60, 0, null, null, null, null, null, cancellation: CancellationToken.None);
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

      /// <summary>
      /// Whether the shelf says a whole page is still behind what it hands over. Off by default, because a shelf
      /// that was given whole must not be read on into; tests about reading on turn it on.
      /// </summary>
      internal bool AnotherPage { get; set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (IsPictureRequest(request))
          return Task.FromResult(NoPicture(request));

        // The screen's details read for the selected row lands here too; it is answered from the shelf body,
        // but it is not a question about the shelf and does not count as one.
        if (!request.RequestUri.AbsoluteUri.Contains("/items/", StringComparison.OrdinalIgnoreCase))
        {
          TimesAsked++;
          LastUrl = request.RequestUri.PathAndQuery;
          Question = QuestionIn(request.RequestUri.Query);
        }

        List<string> matching = new List<string>();
        foreach (string name in SHELF)
        {
          if (Question == null || name.IndexOf(Question, StringComparison.OrdinalIgnoreCase) >= 0)
            matching.Add(name);
        }

        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(matching.Count + (AnotherPage ? 60 : 0))
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
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, "alien", null, null, null, null, cancellation: CancellationToken.None);
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
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, "R&D 2 + 2", null, null, null, null, cancellation: CancellationToken.None);
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

      // The server says another page is behind this one. Nothing has to be pressed for it - there is no "more"
      // row to press any more - so the walk to the bottom of what is on screen is what asks, and the words that
      // got this list are the words the next page is asked with.
      server.AnotherPage = true;
      browser.SearchText = "a";
      browser.FindCommand.Execute(null);
      WaitUntil("the matches to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      browser.Selected = browser.Titles[browser.Titles.Count - 1];
      WaitUntil("the second page", delegate { return server.TimesAsked == 3; });

      Assert.That(server.LastUrl, Does.Contain("search=a"),
          "the shelf reads on through the matches; a second page of the whole shelf would be a different list");
      Assert.That(server.LastUrl, Does.Contain("offset="));
    }

    [AvaloniaTest]
    public void TheFindKeyHandsTheKeyboardToTheBoxAndBackGivesTheShelfBack()
    {
      SearchServer server = new SearchServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      ShellWindow window = new ShellWindow(new MediaBrowserView(browser));
      window.Show();
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SHELF.Length; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      TextBox box = window.GetVisualDescendants().OfType<TextBox>()
          .First(field => field.Name == "searchBox");

      titles.Focus();
      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.F,
        Source = titles
      });
      Pump();
      Assert.That(box.IsFocused, Is.True, "one key is all it takes to start typing a title");

      box.Text = "alien";
      box.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = box
      });
      WaitUntil("the matches to arrive", delegate { return browser.Titles.Count == 2; });
      // ON A TILE, not merely in the list: the caret that came out of the box is the caret the viewer walks the
      // results with, and a list holding it wears no frame - which is a wall of results with nothing lit.
      Assert.That(window.FocusManager.GetFocusedElement(), Is.InstanceOf<ListBoxItem>(),
          "Enter searched, so the arrows belong to a result - not to the box they came from");

      titles.RaiseEvent(new KeyEventArgs
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

      /// <summary>
      /// How long the shelf makes everybody wait for its answer. A real server answers a library over the
      /// network and its database, which takes longer than one dispatcher turn, and the caret's journey after
      /// a rail pick crosses exactly that waiting - a fake that answers within the turn hides every hole the
      /// waiting leaves behind.
      /// </summary>
      internal int DelayMs { get; set; }

      /// <summary>
      /// How many titles the shelf hands out, when a test needs a wall big enough for the panel to recycle
      /// containers under the caret. Zero keeps the four-title shelf body; a bigger number trades it for
      /// generated titles, which a recycling test cares nothing about the names of.
      /// </summary>
      internal int Size { get; set; }

      /// <summary>
      /// Whether the shelf says a whole page is still behind what it hands over. See the search shelf's own
      /// version of this for why a test needs to say so.
      /// </summary>
      internal bool AnotherPage { get; set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (IsPictureRequest(request))
          return Task.FromResult(NoPicture(request));

        // The screen's details read for the selected row lands here too; it is answered from the shelf body,
        // but it is not a question about the shelf and does not count as one.
        if (!request.RequestUri.AbsoluteUri.Contains("/items/", StringComparison.OrdinalIgnoreCase))
        {
          TimesAsked++;
          LastUrl = request.RequestUri.PathAndQuery;
          Filter = ParameterIn(request.RequestUri.Query, "seen");
        }

        List<ShelfTitle> shelf = new List<ShelfTitle>();
        if (Size > 0)
        {
          for (int i = 0; i < Size; i++)
            shelf.Add(new ShelfTitle("Title " + i.ToString("D4"), 0, null));
        }
        else
        {
          shelf.AddRange(SEEN_SHELF);
        }

        List<ShelfTitle> matching = new List<ShelfTitle>();
        foreach (ShelfTitle title in shelf)
        {
          if (Filter == "watched" && title.PlayCount <= 0)
            continue;
          if (Filter == "unwatched" && title.PlayCount > 0)
            continue;
          matching.Add(title);
        }

        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(matching.Count + (AnotherPage ? 60 : 0))
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

        HttpResponseMessage answer = new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        };

        // A server that answers within the dispatcher turn cannot show what the waiting does to the caret.
        return DelayMs <= 0
            ? Task.FromResult(answer)
            : Task.Delay(DelayMs).ContinueWith(_ => answer, TaskScheduler.Default);
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
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, null, "watched", null, null, null, cancellation: CancellationToken.None);
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
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, null, null, null, null, null, cancellation: CancellationToken.None);
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

      // The filtered shelf is two rows long, the server says it goes on, and the walk of the selection onto it
      // is enough to ask for what comes next. The next page has to carry the filter: without it the shelf would
      // append the watched titles the viewer had just asked not to see.
      server.AnotherPage = true;
      browser.CycleSeenCommand.Execute(null);
      WaitUntil("the unwatched titles", delegate { return browser.Titles.Count == 2; });
      Assert.That(browser.HasMore, Is.True, "the server says the filtered shelf goes on");

      // The filtered shelf is two rows long, the server says it goes on, and the shelf finishes itself - which is
      // the whole point: the next part has to carry the filter, because without it the shelf would append the
      // watched titles the viewer had just asked not to see.
      WaitUntil("the filtered shelf to go on reading", delegate { return server.TimesAsked >= 2; });

      Assert.That(server.LastUrl, Does.Contain("seen=unwatched"),
          "the shelf reads on through the filtered titles; an unfiltered second page would append titles the "
          + "viewer had asked not to see");
    }

    [AvaloniaTest]
    public void TheWatchedKeyFiltersTheScreenAndBackTakesItOffAgain()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      MediaBrowserView view = new MediaBrowserView(browser);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      ListBox titles = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "titlesList");
      Assert.That(browser.SeenLabel, Is.EqualTo("All titles"));

      titles.Focus();
      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.W,
        Source = titles
      });
      WaitUntil("the filtered list", delegate { return browser.Titles.Count == 2; });
      Assert.That(browser.SeenLabel, Is.EqualTo("Unwatched only"),
          "the screen says the state the list is in, so the filter is visible without asking");

      // The state is said on the screen by the screen's own menu, which is where the reference kept these commands
      // (master_mediamenu.xaml): the row that cycles the filter wears the label of the state it is in, so a viewer
      // who opens the menu can read what the shelf is showing before pressing anything.  The rail comes out the
      // way its button brings it - the info key is the title's dialog now, not the rail.
      browser.ShowMediaMenuCommand.Execute(null);
      WaitUntil("the menu to stand open", delegate { return browser.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "mediaMenuList");
      WaitUntil("the menu to fill", delegate { return rail.Items.Count > 0; });
      // The watched row stands on the "Filter" page now, beside the other filters it belongs to, so the test
      // steps there the way a viewer does.
      MediaBrowserMenuEntry filterPage = rail.Items.OfType<MediaBrowserMenuEntry>()
          .First(row => row.Label.StartsWith("Filter", StringComparison.Ordinal));
      filterPage.Command.Execute(null);
      WaitUntil("the filter page to stand up", delegate { return browser.MediaMenuHeading == "Filter"; });
      Assert.That(rail.Items.OfType<MediaBrowserMenuEntry>()
              .Any(row => row.Label == "Unwatched only"), Is.True,
          "the filter page says which watched filter is on");

      // One back steps from the filter page to the commands, the next puts the menu away - the way a viewer
      // climbing out of a submenu does it.
      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      WaitUntil("the commands page to stand up again", delegate { return browser.MediaMenuHeading == "Menu"; });

      titles.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = titles
      });
      WaitUntil("the menu to go away", delegate { return !browser.MediaMenuVisible; });

      titles.RaiseEvent(new KeyEventArgs
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
    /// The four titles the pane's continue-playing test runs on. Every question the screen can ask has a different answer here, so
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

    /// <summary>
    /// The rows the pane wrote under one of its own headings: every row between that heading and the next, which
    /// is the list the heading names and nothing else. Asking the whole pane instead would let a title of
    /// "Latest added" pass for a title of "Continue playing".
    /// </summary>
    private static List<HomeRow> RowsUnder(HomeViewModel home, string heading)
    {
      Assert.That(home.ContentRows.Any(row => row.IsHeading && row.Text == heading), Is.True,
          "the pane carried no \"" + heading + "\" heading at all: " + string.Join(" | ", PaneRows(home)));

      var rows = new List<HomeRow>();
      bool under = false;
      foreach (HomeRow row in home.ContentRows)
      {
        if (row.IsHeading)
          under = row.Text == heading;
        else if (under)
          rows.Add(row);
      }
      return rows;
    }

    [AvaloniaTest]
    public async Task TheHomePaneOffersThisSectionsOwnContinuePlayingTitles()
    {
      RegisterLibrary(HalfWayShelf());
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      await home.ShowContentAsync(EntryNamed(home.Menu, "Movies"));

      string[] titles = RowsUnder(home, "Continue playing")
          .Where(row => row.IsTitle).Select(row => row.Title!.Name).ToArray();

      Assert.That(titles, Is.EqualTo(new[] { "Alien Earth", "Army of Thieves" }),
          "the pane of the films shelf offers the list Nereus' MovieHomeContent called ContinuePlayMovies - "
          + "\"where was I\", answered for the shelf the viewer is standing on, which is the shape MP2's home "
          + "always had. The two titles that were opened and not finished, the one stopped most recently first: "
          + "not the film that was watched through (its last-played date is what a finished viewing leaves "
          + "behind) and not the one nobody ever opened. Four titles are on the shelf, so this answer is only "
          + "possible if the pane asked for \"started\" - every other question has a different answer here, and "
          + "one merged list beside the whole menu could only ever answer with the same titles from every shelf "
          + "on the screen at once");
    }

    #endregion

    #region What a screen does with a viewing the library has just taken

    /// <summary>
    /// What the server answers a report with, for the tests below. The figures are the library's own, gathered on
    /// the way back from the report rather than asked for again - which is the whole reason a row can be rewritten
    /// the moment a film ends instead of waiting for somebody to press Refresh.
    /// </summary>
    private static MediaPlayback LibrarySays(TitleViewModel row, int playCount, bool watched, int? resume)
    {
      return new MediaPlayback(row.Title.Id, playCount, new DateTime(2026, 2, 14, 22, 5, 0), watched, null, resume);
    }

    [AvaloniaTest]
    public void ARowTheViewerJustFinishedSaysSoWithoutWaitingForARefresh()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      TitleViewModel stopped = browser.Titles.Single(title => title.Name == "Army of Thieves");
      Assert.That(stopped.Seen, Is.False);
      Assert.That(stopped.ResumePercent, Is.EqualTo(42));

      int announced = 0;
      stopped.PropertyChanged += delegate { announced++; };
      int askedBefore = server.TimesAsked;

      Assert.That(browser.ApplyPlayback(LibrarySays(stopped, 1, true, null)), Is.True);

      Assert.That(stopped.Seen, Is.True, "the tick arrived with the report instead of with a refresh");
      Assert.That(stopped.SeenMark, Is.EqualTo("\u2713"));
      Assert.That(stopped.ResumePercent, Is.Null,
          "and the position went away with it: a title that is finished has nothing left to resume, and a row "
          + "showing both is describing two viewings at once");
      Assert.That(stopped.LastPlayedText, Is.EqualTo("2026-02-14 22:05"));
      Assert.That(announced, Is.EqualTo(1), "the row said so itself, which is what a bound screen has to hear");
      Assert.That(server.TimesAsked, Is.EqualTo(askedBefore),
          "and the server was not asked again about something it has just answered");
    }

    [AvaloniaTest]
    public void ATitleStoppedAgainKeepsItsRowAndGainsTheNewerFigure()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      TitleViewModel stopped = browser.Titles.Single(title => title.Name == "Army of Thieves");
      Assert.That(browser.ApplyPlayback(LibrarySays(stopped, 0, false, 61)), Is.True);

      Assert.That(stopped.ResumePercent, Is.EqualTo(61), "the newer position, because the newer viewing is the "
          + "one a viewer would be coming back to");
      Assert.That(stopped.ResumeText, Is.EqualTo("61% watched"));
      Assert.That(stopped.Seen, Is.False, "stopped again rather than finished, and the library said so");
    }

    [AvaloniaTest]
    public void AReportAboutATitleThatIsNotOnThisListChangesNothing()
    {
      SeenServer server = new SeenServer();
      MediaBrowserViewModel browser = BrowserOn(server);
      browser.OpenCommand.Execute(null);
      WaitUntil("the shelf to arrive", delegate { return browser.Titles.Count == SEEN_SHELF.Length; });

      // The ordinary case for a film picked on the home screen's strip while a library list of something else is
      // open: the report happened, and this list has nothing of it to show.
      MediaPlayback elsewhere = new MediaPlayback(new Guid("77777777-7777-7777-7777-777777777777"), 1,
          new DateTime(2026, 2, 14, 22, 5, 0), true, null, null);

      string[] seenBefore = browser.Titles.Where(title => title.Seen).Select(title => title.Name).ToArray();
      Assert.That(browser.ApplyPlayback(elsewhere), Is.False);

      Assert.That(browser.Titles.Where(title => title.Seen).Select(title => title.Name).ToArray(),
          Is.EqualTo(seenBefore),
          "no row of this list was rewritten to agree with a title it is not showing");
    }

    #endregion
    #region The titles inside a title

    /// <summary>
    /// A tile for a shelf named by its aspect, so that one helper speaks for albums, series and films alike.
    /// </summary>
    private static HomeSection ShelfCalled(string aspectName)
    {
      return new HomeSection(HomeSection.LabelFor(aspectName),
          new AspectViewModel(new FrontendAspect(Guid.Empty, aspectName, false, false, null,
              Array.Empty<AspectAttribute>())));
    }

    /// <summary>One row as a server that knows what is inside what answers it.</summary>
    private static MediaTitle ARow(string name, string? path = null, int? year = null, int? minutes = null,
        int? track = null, string? artist = null, int? season = null, int? episode = null,
        int? trackCount = null, int? episodes = null, Guid? parentId = null)
    {
      return new MediaTitle(new Guid(7, 0, 1, new byte[] { 128, 0, 0, 0, 0, 0, 0, (byte) name.Length }), name, null,
          year, minutes, path, false, Season: season, Episode: episode, Track: track, Artist: artist,
          TrackCount: trackCount, Episodes: episodes, ParentId: parentId);
    }

    internal static readonly Guid ALBUM_ID = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static string RowJson(string id, string title, string extra)
    {
      return "{\"id\":\"" + id + "\",\"title\":\"" + title + "\"" + extra + "}";
    }

    /// <summary>
    /// A server that hands out the titles of whichever shelf it is asked about and remembers the question. The rows
    /// differ per aspect on purpose: a test can then tell a track list from a film list by what came back, not by
    /// repeating what the code under test was supposed to do.
    /// </summary>
    private class CatalogueServer : HttpMessageHandler
    {
      internal string? Aspect { get; private set; }

      internal string? Inside { get; private set; }

      internal string? Sort { get; private set; }

      /// <summary>The season the shelf was narrowed to, which is how a counted season is opened.</summary>
      internal string? Season { get; private set; }

      /// <summary>
      /// Hands out a shelf of seasons counted off episode numbers instead of seasons the library holds: rows whose
      /// ids mean nothing outside the shelf, each saying which series it was counted from.
      /// </summary>
      internal bool Counted { get; set; }

      internal string? LastUrl { get; private set; }

      /// <summary>
      /// Where the screen last asked for one title's own story - its details or its pictures - which is how a test
      /// sees that a row the library only counts is read through the title it was counted from.
      /// </summary>
      internal string? ItemUrl { get; private set; }

      /// <summary>Hands out empty shelves, for the difference between "nothing here" and "nothing matched".</summary>
      internal bool Empty { get; set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        if (IsPictureRequest(request))
          return Task.FromResult(NoPicture(request));

        // The tracks the file carries are asked of the same title just after its details, but they are a second
        // answer and not the details themselves. Served apart, so that this question does not write itself into
        // ItemUrl - which stands for the one question a test is waiting for - and answered with three empty lists
        // because the stand holds no file.
        if (request.RequestUri.AbsoluteUri.EndsWith("/streams", StringComparison.OrdinalIgnoreCase))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent("{\"id\":\"" + new Guid(1, 0, 4,
                new byte[] { 128, 0, 0, 0, 0, 0, 0, 4 }).ToString("D")
                + "\",\"videos\":[],\"audios\":[],\"subtitles\":[]}",
                Encoding.UTF8, "application/json")
          });
        }

        // The details read the screen fires for the selected row arrives here too. It is answered - the
        // shelf's own list body parses well enough for the panel to stand - but it is not the shelf
        // question, so it does not overwrite what the shelf was asked with.
        if (request.RequestUri.AbsoluteUri.Contains("/items/", StringComparison.OrdinalIgnoreCase))
        {
          ItemUrl = request.RequestUri.PathAndQuery;
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent("{\"id\":\"" + new Guid(1, 0, 4,
                new byte[] { 128, 0, 0, 0, 0, 0, 0, 4 }).ToString("D") + "\",\"title\":\"Alien\",\"year\":1979}",
                Encoding.UTF8, "application/json")
          });
        }

        LastUrl = request.RequestUri.PathAndQuery;
        Aspect = Asked(request.RequestUri.Query, "aspect");
        Inside = Asked(request.RequestUri.Query, "inside");
        Sort = Asked(request.RequestUri.Query, "sort");
        Season = Asked(request.RequestUri.Query, "season");

        string playable = ",\"path\":\"" + (RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH)
            .Replace("\\", "\\\\") + "\"";
        List<string> rows = new List<string>();
        if (!Empty)
        {
          if (Aspect == "AlbumItem")
          {
            rows.Add(RowJson(ALBUM_ID.ToString("D"), "Kind of Blue", ",\"year\":1959,\"trackCount\":5"));
          }
          else if (Aspect == "AudioItem")
          {
            rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 1 }).ToString("D"),
                "Blue in Green", ",\"track\":4,\"artist\":\"Miles Davis\"" + playable));
            rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 2 }).ToString("D"),
                "Flamenco Sketches", ",\"track\":5,\"artist\":\"Miles Davis\"" + playable));
          }
          else if (Aspect == "EpisodeItem")
          {
            // The number the shelf was narrowed to is spelled back into the row, so a test can tell a shelf asked
            // for one season from the whole series' episodes by what came over the wire.
            rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 3 }).ToString("D"),
                "Low Tide", ",\"season\":" + (Season ?? "1") + ",\"episode\":2" + playable));
          }
          else if (Aspect == "SeriesItem")
          {
            rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 5 }).ToString("D"),
                "Bones", ",\"year\":2005"));
          }
          else if (Aspect == "SeasonItem")
          {
            if (Counted)
            {
              // What a server answers when the library holds no seasons: rows whose ids are keys into this shelf
              // only, and a "parentId" saying where the season's contents really have to be asked from.
              rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 8 }).ToString("D"),
                  "Bones (2005) S01", ",\"season\":1,\"episodes\":24,\"parentId\":\"" + Inside + '"'));
              rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 9 }).ToString("D"),
                  "Bones (2005) S02", ",\"season\":2,\"episodes\":26,\"parentId\":\"" + Inside + '"'));
            }
            else
            {
              // Spelled the way MP2 spells a season ("<series> (<year>) S<nn>", SeasonInfo.SEASON_FORMAT_STR), which
              // is a name written for a shelf holding every season the house has - so a test can see the front end
              // making the row say what the viewer is standing in instead.
              rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 6 }).ToString("D"),
                  "Bones (2005) S01", ",\"season\":1,\"episodes\":24"));
              rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 7 }).ToString("D"),
                  "Bones (2005) S02", ",\"season\":2,\"episodes\":26"));
            }
          }
          else
          {
            rows.Add(RowJson(new Guid(1, 0, 4, new byte[] { 128, 0, 0, 0, 0, 0, 0, 4 }).ToString("D"),
                "Alien", ",\"year\":1979,\"runtimeMinutes\":117" + playable));
          }
        }

        StringBuilder body = new StringBuilder();
        body.Append("{\"total\":").Append(rows.Count)
            .Append(",\"offset\":0,\"limit\":60,\"returned\":").Append(rows.Count)
            .Append(",\"sort\":\"").Append(Sort ?? "title").Append('"');
        if (Inside != null)
          body.Append(",\"inside\":\"").Append(Inside).Append('"');
        body.Append(",\"titles\":[");
        for (int i = 0; i < rows.Count; i++)
        {
          if (i > 0)
            body.Append(',');
          body.Append(rows[i]);
        }
        body.Append("]}");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      /// <summary>The value of one query parameter, or <c>null</c> when the request did not carry it.</summary>
      private static string? Asked(string query, string name)
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

    [Test]
    public void ATitleThatHoldsTitlesIsCountedRatherThanTimed()
    {
      TitleViewModel album = new TitleViewModel(ARow("Kind of Blue", trackCount: 5),
          MediaCatalog.HoldsTitles("AlbumItem"));

      Assert.That(album.Detail, Is.EqualTo("5 tracks"),
          "an album is opened, not played, so how long it is has no meaning; how many things are on it does");
      Assert.That(album.Detail, Does.Not.Contain("not playable"),
          "refusing to play a row the viewer was never meant to play is not an answer, it is a bug");
    }

    [Test]
    public void AContainerNobodyCountedSaysNothingRatherThanTheWrongThing()
    {
      TitleViewModel album = new TitleViewModel(ARow("Kind of Blue"), MediaCatalog.HoldsTitles("AlbumItem"));

      Assert.That(album.Detail, Is.Empty,
          "a server that never stored how many tracks an album has leaves the row blank; guessing zero would be "
          + "saying something the library does not know");
    }

    [Test]
    public void ARowThatHoldsNothingAndHasNoFileSaysItCannotBeWatched()
    {
      TitleViewModel film = new TitleViewModel(ARow("Alien"));

      Assert.That(film.Detail, Is.EqualTo("not playable here"));
    }

    [Test]
    public void ATrackRowCarriesItsNumberAndWhoIsCredited()
    {
      TitleViewModel track = new TitleViewModel(ARow("Blue in Green", track: 4, artist: "Miles Davis"));

      Assert.That(track.Position, Is.EqualTo("4."));
      Assert.That(track.Line, Is.EqualTo("4. Blue in Green"),
          "a listener looks for the fourth track by its number, and two tracks called 'Untitled' are only "
          + "distinguishable by that number");
      Assert.That(track.Artist, Is.EqualTo("Miles Davis"),
          "on its own line, because the name is what is scanned and the artist is what is checked afterwards");
    }

    [Test]
    public void AnEpisodeRowSaysWhichOneItIs()
    {
      TitleViewModel episode = new TitleViewModel(ARow("Low Tide", season: 1, episode: 2));

      Assert.That(episode.Position, Is.EqualTo("S01E02"));
      Assert.That(episode.Line, Is.EqualTo("S01E02 Low Tide"),
          "in the same shape as every other episode row, which is what makes a row scanable without reading it");
    }

    [Test]
    public void AFilmRowCarriesNoPosition()
    {
      TitleViewModel film = new TitleViewModel(ARow("Alien", year: 1979));

      Assert.That(film.Position, Is.Empty);
      Assert.That(film.Line, Is.EqualTo("Alien (1979)"));
    }

    [Test]
    public void ARowWhoseNameBearsTheYearWearsItOnce()
    {
      // The taggers file series under the name WITH the year on the end and the year column agrees; the row
      // that printed the year again read "Alien Earth (2025) (2025)", which is one fact standing in two places.
      TitleViewModel worn = new TitleViewModel(ARow("Alien Earth (2025)", year: 2025));
      Assert.That(worn.Line, Is.EqualTo("Alien Earth (2025)"),
          "a name that has said its year does not hear it said back to it");

      TitleViewModel bare = new TitleViewModel(ARow("Alien Earth", year: 2025));
      Assert.That(bare.Line, Is.EqualTo("Alien Earth (2025)"),
          "a name that has not said its year still hears it on the row");

      TitleViewModel other = new TitleViewModel(ARow("Alien Earth (2024)", year: 2025));
      Assert.That(other.Line, Is.EqualTo("Alien Earth (2024) (2025)"),
          "and a name ending in a DIFFERENT year keeps both: that ending is part of the name, not the year");
    }

    [Test]
    public void ASeriesRowIsCountedByItsEpisodes()
    {
      // The right of a series row says what an album's row says about its tracks - how many things are inside.
      TitleViewModel series = new TitleViewModel(ARow("Alien Earth (2025)", year: 2025, episodes: 8),
          MediaCatalog.HoldsTitles("SeriesItem"));

      Assert.That(series.Detail, Is.EqualTo("8 episodes"),
          "the server counts the episodes under the series and the row says it on the right, where the counts live");
    }

    [Test]
    public void OnlyAlbumsSeriesSeasonsAndCollectionsAreOpened()
    {
      Assert.That(MediaCatalog.HoldsTitles("AlbumItem"), Is.True);
      Assert.That(MediaCatalog.HoldsTitles("SeriesItem"), Is.True);
      Assert.That(MediaCatalog.HoldsTitles("SeasonItem"), Is.True);
      Assert.That(MediaCatalog.HoldsTitles("MovieCollectionItem"), Is.True);

      Assert.That(MediaCatalog.HoldsTitles("MovieItem"), Is.False,
          "a film is watched, and a screen that opened something when a viewer pressed OK on the film they came for "
          + "would make the film unwatchable");
      Assert.That(MediaCatalog.HoldsTitles("EpisodeItem"), Is.False);
      Assert.That(MediaCatalog.HoldsTitles("AudioItem"), Is.False);
      Assert.That(MediaCatalog.HoldsTitles("VideoItem"), Is.False);
      Assert.That(MediaCatalog.HoldsTitles("ImageItem"), Is.False);
      Assert.That(MediaCatalog.HoldsTitles("WhateverItemANewServerInvented"), Is.False,
          "an aspect this front end has never met is played, which is what every aspect MP2 ever had is or holds");
      Assert.That(MediaCatalog.HoldsTitles(null), Is.False);
    }

    [Test]
    public void EveryKindOfContainerSaysWhatIsFiledInsideItAndInWhatOrder()
    {
      Assert.That(MediaCatalog.ChildAspectFor("AlbumItem"), Is.EqualTo("AudioItem"));
      Assert.That(MediaCatalog.ChildSortFor("AlbumItem"), Is.EqualTo("track"),
          "an album is listened to in the order its tracks were pressed, not in alphabetical order");
      Assert.That(MediaCatalog.ChildAspectFor("SeriesItem"), Is.EqualTo("SeasonItem"),
          "a series leads to its seasons, which is what the reference put between the two: MP2 files a season as "
          + "its own media item (SeasonAspect, ROLE_SEASON) and Nereus has whole season screens, so the level is "
          + "in the library and not invented by the skin");
      Assert.That(MediaCatalog.ChildSortFor("SeriesItem"), Is.EqualTo("season"));
      Assert.That(MediaCatalog.ChildAspectFor("SeasonItem"), Is.EqualTo("EpisodeItem"),
          "a season leads to its episodes, which is the last step of the way down");
      Assert.That(MediaCatalog.ChildSortFor("SeasonItem"), Is.EqualTo("episode"));
      Assert.That(MediaCatalog.ChildAspectFor("MovieCollectionItem"), Is.EqualTo("MovieItem"));
      Assert.That(MediaCatalog.ChildSortFor("MovieCollectionItem"), Is.EqualTo("title"));

      Assert.That(MediaCatalog.ChildAspectFor("MovieItem"), Is.EqualTo("MovieItem"),
          "nothing is filed inside a film, so asking what is inside one gives the same kind back");
      Assert.That(MediaCatalog.ChildSortFor("MovieItem"), Is.EqualTo("title"));
      Assert.That(MediaCatalog.ChildAspectFor(null), Is.Empty);
    }

    [Test]
    public void TheRowsOfAListAreNamedForTheStatusLine()
    {
      Assert.That(MediaCatalog.RowNounFor("AudioItem"), Is.EqualTo("tracks"));
      Assert.That(MediaCatalog.RowNounFor("EpisodeItem"), Is.EqualTo("episodes"));
      Assert.That(MediaCatalog.RowNounFor("MovieItem"), Is.EqualTo("films"));
      Assert.That(MediaCatalog.RowNounFor("AlbumItem"), Is.EqualTo("albums"));
      Assert.That(MediaCatalog.RowNounFor("ImageItem"), Is.EqualTo("pictures"));
      Assert.That(MediaCatalog.RowNounFor("WhateverItemANewServerInvented"), Is.EqualTo("whateveritemanewserverinvented"),
          "an unknown aspect still gets a word, and it is the word the server's own tile uses");
    }

    [AvaloniaTest]
    public void PressingAnAlbumAsksForWhatIsOnItInsteadOfPlayingIt()
    {
      CatalogueServer server = new CatalogueServer();
      MediaBrowserViewModel albums = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("AlbumItem"));

      TitleViewModel? opened = null;
      bool played = false;
      albums.ChildrenRequested += delegate(object? sender, TitleViewModel row) { opened = row; };
      albums.PlayRequested += delegate { played = true; };

      albums.OpenCommand.Execute(null);
      WaitUntil("the albums to arrive", delegate { return albums.Titles.Count == 1; });

      Assert.That(albums.DrillsIn, Is.True, "a shelf of albums is a shelf of things to open");
      Assert.That(albums.Titles[0].Detail, Is.EqualTo("5 tracks"));

      albums.Play(albums.Titles[0]);

      Assert.That(opened, Is.SameAs(albums.Titles[0]), "the viewer pressed OK on an album, so: what is on it?");
      Assert.That(played, Is.False, "and nothing was played, because an album has no file to play");
      Assert.That(albums.Selected, Is.SameAs(albums.Titles[0]),
          "the row stays highlighted, because that is the row the next screen is about");
      Assert.That(albums.Status, Does.Not.Contain("cannot be watched"),
          "the album was not refused; it was understood");
    }

    [AvaloniaTest]
    public void TheTracksOfAnAlbumAreAskedForAsTheTracksOfThatAlbum()
    {
      CatalogueServer server = new CatalogueServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel albums = new MediaBrowserViewModel(client, ShelfCalled("AlbumItem"));
        albums.OpenCommand.Execute(null);
        WaitUntil("the album to arrive", delegate { return albums.Titles.Count == 1; });

        MediaBrowserViewModel tracks =
            new MediaBrowserViewModel(client, ShelfCalled("AlbumItem"), albums.Titles[0]);
        tracks.OpenCommand.Execute(null);
        WaitUntil("the tracks to arrive", delegate { return tracks.Titles.Count == 2; });

        Assert.That(server.Aspect, Is.EqualTo("AudioItem"),
            "the tracks were asked for, not another page of albums");
        Assert.That(server.Inside, Is.EqualTo(ALBUM_ID.ToString("D")), "of the album that was pressed, and of no other");
        Assert.That(server.Sort, Is.EqualTo("track"));
        Assert.That(tracks.Inside, Is.EqualTo(ALBUM_ID));
        Assert.That(tracks.SortOrder, Is.EqualTo("track"));

        Assert.That(tracks.Heading, Is.EqualTo("Kind of Blue"),
            "headed by the album, because that is where the viewer is");
        Assert.That(tracks.Titles[0].Line, Is.EqualTo("4. Blue in Green"));
        Assert.That(tracks.Status, Is.EqualTo("2 of 2 tracks, sorted by track"),
            "the rows are counted as what they are: tracks, not two of Kind of Blue");
        Assert.That(tracks.DrillsIn, Is.False, "a track is listened to; a screen that opened another list from it "
            + "would never let anyone hear anything");
      }
    }

    [AvaloniaTest]
    public void ATrackIsPlayedRatherThanOpened()
    {
      CatalogueServer server = new CatalogueServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel albums = new MediaBrowserViewModel(client, ShelfCalled("AlbumItem"));
        albums.OpenCommand.Execute(null);
        WaitUntil("the album to arrive", delegate { return albums.Titles.Count == 1; });

        MediaBrowserViewModel tracks = new MediaBrowserViewModel(client, ShelfCalled("AlbumItem"), albums.Titles[0]);
        TitleViewModel? opened = null;
        TitleViewModel? played = null;
        tracks.ChildrenRequested += delegate(object? sender, TitleViewModel row) { opened = row; };
        tracks.PlayRequested += delegate(object? sender, TitleViewModel row) { played = row; };
        tracks.OpenCommand.Execute(null);
        WaitUntil("the tracks to arrive", delegate { return tracks.Titles.Count == 2; });

        tracks.Play(tracks.Titles[0]);

        Assert.That(played, Is.SameAs(tracks.Titles[0]), "the drill-down ends at the leaf, or it goes nowhere");
        Assert.That(opened, Is.Null);
      }
    }

    [AvaloniaTest]
    public void AFilmIsStillPlayedRatherThanOpened()
    {
      CatalogueServer server = new CatalogueServer();
      MediaBrowserViewModel films = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);

      TitleViewModel? opened = null;
      TitleViewModel? played = null;
      films.ChildrenRequested += delegate(object? sender, TitleViewModel row) { opened = row; };
      films.PlayRequested += delegate(object? sender, TitleViewModel row) { played = row; };

      films.OpenCommand.Execute(null);
      WaitUntil("the films to arrive", delegate { return films.Titles.Count == 1; });
      films.Play(films.Titles[0]);

      Assert.That(played, Is.SameAs(films.Titles[0]));
      Assert.That(opened, Is.Null, "nothing about a film was opened, so the viewer who came to watch can watch");
      Assert.That(films.Status, Is.EqualTo("1 of 1 movies, sorted by title"),
          "a whole shelf is counted with the word on the tile it came from; only a list read out of one title is "
          + "counted with the word for what it holds");
    }

    [AvaloniaTest]
    public void TheFootOfTheShelfCountsWhatTheReferenceCounted()
    {
      // MediaItemsList.inc drew two numbers at the foot of every shelf - where the highlight stands, counted
      // from one, and how many titles the server counted in all.  The dark band rising from the bottom edge is
      // the master's own (master_bare_media.xaml:66), and the master wore it under grid and list whatever the
      // details said; only the covers, which lay their own shadows over the wall of posters, went without it.
      CatalogueServer server = new CatalogueServer();
      MediaBrowserViewModel films = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), MoviesSection);

      films.OpenCommand.Execute(null);
      WaitUntil("the films to arrive", delegate { return films.Titles.Count == 1; });

      Assert.That(films.HasTitles, Is.True);
      Assert.That(films.SelectedNumber, Is.EqualTo(1),
          "the shelf selects its first row as it fills, and the reference counted that row as one, not zero");
      Assert.That(films.TotalNumber, Is.EqualTo(1), "the number behind the bar is the server's total");

      films.ShowDetails = false;
      Assert.That(films.ShowsFloorGradient, Is.True,
          "the band is the master's, not the details column's: a shelf whose details are away still darkens its "
          + "foot, because nothing else stands there to lift the names off the fanart");

      films.Layout = BrowserLayout.Cover;
      Assert.That(films.ShowsFloorGradient, Is.False, "covers draw no floor beneath themselves");

      films.Layout = BrowserLayout.Grid;
      Assert.That(films.ShowsFloorGradient, Is.True,
          "the band comes back with the grid, whatever view was walked through before");

      films.ShowDetails = true;
      Assert.That(films.ShowsFloorGradient, Is.True, "under grid and list the band rises with the details");

      films.Layout = BrowserLayout.Cover;
      Assert.That(films.ShowsFloorGradient, Is.False,
          "and a cover shelf wears no band, because its details are centred above it rather than beside it");

      films.Layout = BrowserLayout.Grid;
      films.ShowDetails = false;
      films.Selected = null;
      Assert.That(films.SelectedNumber, Is.EqualTo(0), "no highlight is counted as nothing, not as a row");
    }

    [AvaloniaTest]
    public void PressingIntoASeriesOpensItsSeasonsAndPressingIntoASeasonOpensItsEpisodes()
    {
      CatalogueServer server = new CatalogueServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel series = new MediaBrowserViewModel(client, ShelfCalled("SeriesItem"));
        series.OpenCommand.Execute(null);
        WaitUntil("the series to arrive", delegate { return series.Titles.Count == 1; });

        MediaBrowserViewModel seasons =
            new MediaBrowserViewModel(client, ShelfCalled("SeriesItem"), series.Titles[0]);
        seasons.OpenCommand.Execute(null);
        WaitUntil("the seasons to arrive", delegate { return seasons.Titles.Count == 2; });

        Assert.That(server.Aspect, Is.EqualTo("SeasonItem"),
            "MP2 files a season as a title in its own right - its own aspect, its own role, its own relationship "
            + "extractor - and the reference has whole season screens, so a viewer pressing into a series stands "
            + "above the seasons, not above the episodes");
        Assert.That(server.Inside, Is.EqualTo(series.Titles[0].Title.Id.ToString("D")));
        Assert.That(seasons.SortOrder, Is.EqualTo("season"),
            "a season is watched in the order it was made, which is the number the importer wrote and not the "
            + "name it wrote above the number");
        Assert.That(seasons.Heading, Is.EqualTo("Bones"),
            "headed by the series the viewer pressed, because that is where they are");
        Assert.That(seasons.Titles[0].Line, Is.EqualTo("Season 1"),
            "the row says the season and nothing else: the name MP2 builds for a season spells out the series and "
            + "its year, both of which the heading already said, and the row is not addressed inside anything the "
            + "way an episode is");
        Assert.That(seasons.Titles[0].Detail, Is.EqualTo("24 episodes"),
            "how much is inside is the one thing a season has to say about itself, which is what an album's row "
            + "answers with its tracks");
        Assert.That(seasons.Status, Is.EqualTo("2 of 2 seasons, sorted by season"));
        Assert.That(seasons.DrillsIn, Is.True);

        MediaBrowserViewModel episodes =
            new MediaBrowserViewModel(client, ShelfCalled("SeriesItem"), seasons.Titles[0]);
        episodes.OpenCommand.Execute(null);
        WaitUntil("the episodes to arrive", delegate { return episodes.Titles.Count == 1; });

        Assert.That(server.LastUrl, Does.Contain("aspect=EpisodeItem"),
            "and the way down ends where the reference's own screens end: a season holds episodes");
        Assert.That(server.LastUrl, Does.Contain("inside=" + seasons.Titles[0].Title.Id.ToString("D")),
            "the episodes of the season that was pressed, not the whole series' - a screen that derived its "
            + "contents from the tile it was opened from would read one series' seasons twice and never reach an "
            + "episode, however often the viewer pressed OK");
        Assert.That(episodes.SortOrder, Is.EqualTo("episode"));
        Assert.That(episodes.Titles[0].Line, Is.EqualTo("S01E02 Low Tide"),
            "an episode is addressed, and stays addressed however many levels it took to reach it");
        Assert.That(episodes.DrillsIn, Is.False, "an episode is watched; the way down ends here");
      }
    }

    [AvaloniaTest]
    public void ASeasonTheLibraryOnlyCountsIsOpenedByItsSeriesAndItsNumber()
    {
      CatalogueServer server = new CatalogueServer { Counted = true };
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel series = new MediaBrowserViewModel(client, ShelfCalled("SeriesItem"));
        series.OpenCommand.Execute(null);
        WaitUntil("the series to arrive", delegate { return series.Titles.Count == 1; });

        MediaBrowserViewModel seasons =
            new MediaBrowserViewModel(client, ShelfCalled("SeriesItem"), series.Titles[0]);
        seasons.OpenCommand.Execute(null);
        WaitUntil("the counted seasons to arrive", delegate { return seasons.Titles.Count == 2; });

        Assert.That(seasons.Titles[1].Line, Is.EqualTo("Season 2"),
            "a season the server counted off its episodes' numbers is shown exactly like a season the library "
            + "holds: the row is not addressed inside anything the way an episode is, and the name the server built "
            + "for a shelf of every season in the house is not what the viewer is standing in");
        Assert.That(seasons.Titles[1].Detail, Is.EqualTo("26 episodes"));

        WaitUntil("the counted season to be read",
            delegate { return server.ItemUrl != null && server.ItemUrl.Contains("season=", StringComparison.Ordinal); });
        Assert.That(server.ItemUrl, Does.Contain("/items/" + series.Titles[0].Title.Id.ToString("D")),
            "a counted season has no id of its own that means anything to the server - the row's key is a stand-in "
            + "for a title that does not exist - so its story is read of the series it was counted from");
        Assert.That(server.ItemUrl, Does.Match("season=[12]"),
            "and with the number of one of the two counted seasons, because the series on its own cannot say which "
            + "of its seasons the screen was standing on");

        MediaBrowserViewModel episodes =
            new MediaBrowserViewModel(client, ShelfCalled("SeriesItem"), seasons.Titles[1]);
        episodes.OpenCommand.Execute(null);
        WaitUntil("the season's episodes to arrive", delegate { return episodes.Titles.Count == 1; });

        Assert.That(server.Inside, Is.EqualTo(series.Titles[0].Title.Id.ToString("D")),
            "pressing into a counted season asks the series it was counted from, which is the only title the shelf "
            + "inside it can be asked of");
        Assert.That(server.Season, Is.EqualTo("2"),
            "and it says which number it means, or the row of season two opens the whole series' episodes and the "
            + "level the viewer pressed into is gone again");
        Assert.That(episodes.Titles[0].Line, Is.EqualTo("S02E02 Low Tide"),
            "the row shows what the narrowed question brought back, so a viewer who pressed season two and got "
            + "season one's episodes can see it");
      }
    }

    [Test]
    public void ACountedSeasonRowIsReadThroughTheSeriesItWasCountedFrom()
    {
      TitleViewModel counted = new TitleViewModel(
          ARow("Bones (2005) S02", season: 2, episodes: 26, parentId: ALBUM_ID), true, null, "SeasonItem");

      Assert.That(counted.AskId, Is.EqualTo(ALBUM_ID),
          "the two parts a season the library does not hold consists of are its series and its number, and "
          + "everything about the row - its story, its pictures, the shelf inside it - has to be asked of the part "
          + "that really exists");
      Assert.That(counted.AskSeason, Is.EqualTo(2),
          "with the number, which is what tells one row of the shelf from the next");
    }

    [Test]
    public void ASeasonTheLibraryHoldsIsReadThroughItsOwnId()
    {
      TitleViewModel held = new TitleViewModel(ARow("Bones (2005) S03", season: 3, episodes: 15), true, null,
          "SeasonItem");

      Assert.That(held.AskId, Is.EqualTo(held.Title.Id),
          "a season the library holds as a title is asked about by its own id, the way every other row is; the "
          + "counting is a stand-in for libraries that have nothing else, and must not reach back and reinterpret a "
          + "title that exists");
      Assert.That(held.AskSeason, Is.Null,
          "and no number goes with the question, which would ask the same season twice over");
    }

    [Test]
    public void ASeasonRowSaysWhichSeasonItIsAndHowManyEpisodesItHolds()
    {
      TitleViewModel season = new TitleViewModel(ARow("Bones (2005) S03", season: 3, episodes: 15), true,
          null, "SeasonItem");

      Assert.That(season.Name, Is.EqualTo("Season 3"));
      Assert.That(season.Position, Is.Empty,
          "a season is not filed inside anything, so it has no address; writing the number in front of the name "
          + "would print it twice");
      Assert.That(season.Line, Is.EqualTo("Season 3"));
      Assert.That(season.Detail, Is.EqualTo("15 episodes"));

      TitleViewModel unnumbered = new TitleViewModel(ARow("Alien Earth"), true, null, "SeasonItem");
      Assert.That(unnumbered.Name, Is.EqualTo("Alien Earth"),
          "a season the importer never numbered keeps the name it does have rather than becoming 'Season '");
      Assert.That(unnumbered.Detail, Is.Empty, "and a count the server never stored is said as nothing");
    }

    [Test]
    public void ASeasonIsOnTheSeriesShelfBecauseOneShelfLeadsIntoTheNext()
    {
      Assert.That(MediaCatalog.SameShelf("SeriesItem", "SeasonItem"), Is.True);
      Assert.That(MediaCatalog.SameShelf("SeasonItem", "EpisodeItem"), Is.True);
      Assert.That(MediaCatalog.SameShelf("SeriesItem", "EpisodeItem"), Is.True,
          "the tile a viewer came from and the episode they ended up watching are the same part of the library, "
          + "or the home screen would dim the tile the viewer is standing in the middle of");
      Assert.That(MediaCatalog.SameShelf("EpisodeItem", "SeriesItem"), Is.True,
          "and it is the same answer whichever way the question is asked");

      Assert.That(MediaCatalog.SameShelf("SeriesItem", "AudioItem"), Is.False,
          "but a shelf is not the same shelf as one it has no road to");
      Assert.That(MediaCatalog.SameShelf("SeriesItem", "SeriesItem"), Is.True);
      Assert.That(MediaCatalog.SameShelf(null, "SeriesItem"), Is.False);
    }

    [AvaloniaTest]
    public void ASeasonsDetailsSayWhichSeriesItBelongsToAndHowMuchIsInside()
    {
      DetailsServer server = new DetailsServer(HttpStatusCode.OK,
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Bones (2005) S03\",\"year\":2005,\"seriesName\":\"Bones\","
          + "\"season\":3,\"episodes\":15,\"overview\":\"The team adjusts to running the lab without its lead.\"}",
          "{\"id\":\"" + TITANIC + "\",\"title\":\"Bones (2005) S03\",\"year\":2005,\"season\":3,\"episodes\":15}");

      // The shelf is the seasons of one series, which is what makes its rows seasons rather than episodes - the
      // two arrive with the same number on them and only the shelf they were read out of knows which is which.
      MediaBrowserViewModel seasons = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)), ShelfCalled("SeriesItem"),
          new TitleViewModel(ARow("Bones", year: 2005), true, null, "SeriesItem"));
      seasons.OpenCommand.Execute(null);
      WaitUntil("the details to open", delegate { return seasons.DetailsVisible; });

      Assert.That(seasons.DetailsHeading, Is.EqualTo("Season 3"),
          "the panel is standing above one season, and the name MP2 wrote for a season says the series and its "
          + "year - which the viewer is inside of and did not ask to read twice");
      Assert.That(seasons.DetailsLine, Does.Contain("Bones"), "the series the season belongs to");
      Assert.That(seasons.DetailsLine, Does.Contain("15 episodes"), "and how many episodes the library counted");
      Assert.That(seasons.DetailsLine, Does.Not.Contain("S03E00"),
          "a season has no episode nought; the number the panel would build from an episode's aspect is an "
          + "invention here");
      Assert.That(seasons.DetailsText, Does.StartWith("The team adjusts"),
          "the prose the reference's season panel showed is on the season aspect, and it is the reason a viewer "
          + "opened the season at all");
    }

    [AvaloniaTest]
    public void AnEmptyAlbumSaysTheServerHasNoTracks()
    {
      CatalogueServer server = new CatalogueServer();
      server.Empty = true;
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel tracks = new MediaBrowserViewModel(client, ShelfCalled("AlbumItem"),
            new TitleViewModel(ARow("Kind of Blue", trackCount: 0), true));
        tracks.OpenCommand.Execute(null);
        WaitUntil("the empty answer", delegate { return tracks.Status.StartsWith("This server"); });

        Assert.That(tracks.Status, Is.EqualTo("This server has no tracks"),
          "the album was empty, and saying 'no tracks' is the truth a viewer can act on; 'no Kind of Blue' is not");
      }
    }

    #endregion
    #region The home screen's main menu

    /// <summary>
    /// An aspect the way the front end holds one, for a menu built without a server in the way.
    /// </summary>
    private static AspectViewModel AspectCalled(string name)
    {
      return new AspectViewModel(new FrontendAspect(Guid.NewGuid(), name, false, false, null,
          Array.Empty<AspectAttribute>()));
    }

    /// <summary>
    /// The menu entry with this label, with a failure that says what the menu did offer - because "Movies is not
    /// in the menu" is the one thing a reader cannot check for themselves from the assertion.
    /// </summary>
    private static MainMenuEntry EntryNamed(IEnumerable<MainMenuEntry> menu, string label)
    {
      MainMenuEntry? entry = menu.FirstOrDefault(candidate => candidate.Label == label);
      Assert.That(entry, Is.Not.Null, "the menu offered: " + string.Join(", ",
          menu.Select(candidate => candidate.Label)));
      return entry!;
    }

    [Test]
    public void TheMenuIsThereBeforeAnyServerHasAnswered()
    {
      HomeViewModel home = new HomeViewModel();

      Assert.That(home.Menu, Is.Not.Empty,
          "a home screen that waits for a server before it offers anything to press is a dark rectangle with a "
          + "clock on it; the menu is what a media centre is, so it is on screen from the first frame");
      Assert.That(home.Menu.Count(entry => entry.Openable), Is.EqualTo(9),
          "nothing but the nine screens the front end owns - the folders the library comes from, the picture "
          + "folders, the browse screen, which reads whatever the server turns out to hold, the news and "
          + "weather screens, which talk to the internet instead of the server, the party screen, which "
          + "only queues until a server answers, the settings screen, which needs nothing from anywhere "
          + "because it only changes what this box looks like, and the television and radio guides, which ask a "
          + "TV backend rather than this server and say so themselves when that has not been written down either "
          + "- can be opened, because there is "
          + "no library to open and no server to ask about one. Those nine are openable on purpose: telling a "
          + "viewer where the media should be is most useful of an empty media centre, and the screens say "
          + "\"no server connected\" instead of an empty rectangle");
      Assert.That(EntryNamed(home.Menu, "Movies").Detail, Is.EqualTo("no server connected"),
          "the tile says that out loud rather than leaving the viewer to work it out from an empty list");
    }

    [Test]
    public void TheMenuIsMP2sOwnEntriesInMP2sOwnOrder()
    {
      HomeViewModel home = new HomeViewModel();

      Assert.That(home.Menu.Select(entry => entry.Label), Is.EqualTo(new[]
      {
          "Local media", "Browse media", "Audio", "Videos", "Movies", "Series", "Images",
          "News", "TV", "Radio", "Weather", "Party music player", "Settings", "Utilities"
      }), "the names and the order are the entries MP2's own plugins contribute to the home state - "
          + "media-actions.xml for the seven media ones, SlimTvClient-actions.xml for television and radio, and "
          + "the news, weather, party, settings and utilities plugins' own files for the rest - and not a set of "
          + "words invented for this front end. TestHomeMenuContributions reads those files on every run and says "
          + "which line of this table no longer matches what the previous code assembles");
    }

    [Test]
    public void TheReasonOnATileIsShortEnoughToBeReadOffIt()
    {
      // The menu is two rows of five across the 1184 lines a 1280-wide screen leaves after its margins, so a tile
      // is about 237 lines wide; at the 14-point size the detail is drawn in, a character costs some seven lines,
      // which puts the limit near thirty characters. The cap is that arithmetic with a little left over, so that a
      // reason ends on its own tile instead of running into the tile beside it. The long form of every reason is
      // in MainMenu's remarks, where it can be as long as it needs to be.
      foreach (MainMenuEntry entry in MainMenu.Build(Array.Empty<AspectViewModel>(), false))
        Assert.That(entry.Detail, Has.Length.LessThanOrEqualTo(30),
            entry.Label + " says more than its tile can hold");
    }

    [Test]
    public void AnEntryThisBuildHasNothingBehindSaysWhatIsMissing()
    {
      HomeViewModel home = new HomeViewModel();
      MainMenuEntry utilities = EntryNamed(home.Menu, "Utilities");

      Assert.That(utilities.Openable, Is.False);
      Assert.That(utilities.Detail, Does.Contain("its tools went with the TV"),
          "an entry that leads nowhere explains itself, because a silent one is read as a broken front end");
      Assert.That(home.Open(utilities), Is.Null);
      Assert.That(home.Status, Does.Contain("its tools went with the TV"),
          "and pressing it says the same sentence the tile already says, rather than nothing at all");
    }

    [Test]
    public void TheTelevisionAndRadioTilesNameAScreenNowThatAGuideExists()
    {
      // Both tiles carried their apology - "no TV backend" - for as long as this front end existed, because what
      // MP2 launched behind them was a TV service that had been deleted from this tree. A TvHeadend is the
      // television of a multiplatform centre now, and its guide is behind both tiles, so neither says sorry.
      HomeViewModel home = new HomeViewModel();

      MainMenuEntry television = EntryNamed(home.Menu, "TV");
      MainMenuEntry radio = EntryNamed(home.Menu, "Radio");

      Assert.That(television.Missing, Is.Null);
      Assert.That(radio.Missing, Is.Null);
      Assert.That(television.Openable, Is.True);
      Assert.That(radio.Openable, Is.True);
    }

    [Test]
    public void TheAudioTileOpensAlbumsBecauseThatIsWhatHoldsTheTracks()
    {
      List<MainMenuEntry> menu = MainMenu.Build(
          new[] { AspectCalled("AudioItem"), AspectCalled("AlbumItem") }, true);

      Assert.That(EntryNamed(menu, "Audio").Section!.Aspect.Name, Is.EqualTo("AlbumItem"),
          "the tile MP2 calls Audio opens the shelf that has something inside it, because that is the shelf a "
          + "viewer can go on from");
    }

    [Test]
    public void TheAudioTileFallsBackToTracksWhenTheServerHasNoAlbums()
    {
      List<MainMenuEntry> menu = MainMenu.Build(new[] { AspectCalled("AudioItem") }, true);

      Assert.That(EntryNamed(menu, "Audio").Section!.Aspect.Name, Is.EqualTo("AudioItem"),
          "a library imported without albums still has its music on the screen");
    }

    [Test]
    public void EveryCataloguedPartOfTheServerStillGetsATile()
    {
      List<MainMenuEntry> menu = MainMenu.Build(
          new[] { AspectCalled("MovieItem"), AspectCalled("EpisodeItem") }, true);

      Assert.That(menu.All(entry => entry.Label != "Episodes"), Is.True,
          "episodes are not one of MP2's home entries and they stay that way: a viewer reaches them through their "
          + "series, the shelf the drilldown table still leads from Series to EpisodeItem, the way Nereus reached "
          + "ContinuePlaySeries inside the series entry - so a server that holds them gains no tile of its own, "
          + "and the menu stays MP2's own entries in MP2's own order");
      Assert.That(menu.Count(entry => entry.Openable), Is.EqualTo(10),
          "the films, and the nine screens that are the front end's own rather than parts of the "
          + "library: the folders the media comes from, the picture folders, the browse screen, the news, "
          + "the weather, the party, the settings its own look, and the two guides - which open whatever "
          + "television the household has hooked up, so a server with nothing on it is no reason to lock them");
    }

    [AvaloniaTest]
    public void TheVideoShelfIsOfferedOnceAndOpensByName()
    {
      List<MainMenuEntry> menu = MainMenu.Build(new[] { AspectCalled("VideoItem") }, true);

      Assert.That(menu.Count(entry => entry.Section != null && entry.Section.Aspect.Name == "VideoItem"),
          Is.EqualTo(1),
          "the videos have one door. MP2 offered a second one - the \"Latest media\" tile, which was the same "
          + "VideoItem shelf wearing an arrival order - and that tile, its order and everything behind it were "
          + "removed, so no second label opens this shelf any more");

      CatalogueServer server = new CatalogueServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel videos = new MediaBrowserViewModel(client, EntryNamed(menu, "Videos").Section);
        videos.OpenCommand.Execute(null);
        WaitUntil("the videos to arrive", delegate { return videos.Titles.Count == 1; });

        Assert.That(server.LastUrl, Does.Contain("sort=title").And.Not.Contain("order=desc"),
            "a shelf opens by name, which is the order it has always had; the newest-first order was asked for by "
            + "the removed tile alone, and nothing asks for it now");
        Assert.That(videos.Status, Does.EndWith("sorted by title"),
            "and the line under the list says which order the server gave");
      }
    }

    [Test]
    public void ThePicturesHaveOneTileAndItWearsMP2sOwnWord()
    {
      List<MainMenuEntry> menu = MainMenu.Build(new[] { AspectCalled("ImageItem") }, true);

      Assert.That(menu.Count(entry => entry.Label == "Images"), Is.EqualTo(1),
          "photographs have one door. This front end had invented a second word for them - the shelf label "
          + "\"Pictures\" - and because the Images tile claims no shelf of its own, a server that had filed image "
          + "titles was handed a second tile beside MP2's one, which every viewer read as a second plugin");
      Assert.That(menu.Select(entry => entry.Label), Does.Not.Contain("Pictures"),
          "and the invented word is gone from the menu rather than hidden behind the same tile");

      MainMenuEntry images = EntryNamed(menu, "Images");
      Assert.That(images.Openable, Is.True, "the tile opens whatever the server has or has not filed");
      Assert.That(images.ScreenKey, Is.EqualTo("pictures"));
      Assert.That(images.Section, Is.Null,
          "and claiming the aspect does not turn the tile into a shelf of titles, which is the empty shelf over "
          + "thousands of photographs that the folders were put in its place for");
      Assert.That(HomeSection.LabelFor("ImageItem"), Is.EqualTo("Images"),
          "the browse screen's shelf for filed photographs wears the tile's word, so one media has one word");
    }

    [Test]
    public void ThePartsOfTheSchemaNobodySitsDownToWatchStayOffTheMenu()
    {
      List<MainMenuEntry> menu = MainMenu.Build(
          new[] { AspectCalled("PersonItem"), AspectCalled("GenreItem") }, true);

      Assert.That(menu.Count(entry => entry.Openable), Is.EqualTo(9),
          "an actor and a genre are how the library is organised inside, not a part of it to watch; the nine "
          + "screens that stay openable are the folders one, the pictures one, the browse screen, the news, "
          + "the weather, the party, the settings and the two guides, and none of the nine is a shelf - the first "
          + "two are about where the "
          + "library's files are, the third asks the server to group whatever it holds, the next two ask the "
          + "internet, the party waits for a shelf of its own politely, the settings only repaint the box, and "
          + "the guides ask a different machine altogether");
      Assert.That(menu.Select(entry => entry.Label), Does.Not.Contain("PersonItem"));
    }

    [AvaloniaTest]
    public void TheMenuCountsWhatTheServerHasAndNamesWhatItHasNot()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      Refreshed(home);

      Assert.That(EntryNamed(home.Menu, "Movies").Detail, Is.EqualTo("1"),
          "the tile carries the server's own count of its own aspect");
      MainMenuEntry audio = EntryNamed(home.Menu, "Audio");
      Assert.That(audio.Openable, Is.False);
      Assert.That(audio.Detail, Is.EqualTo("not on this server"),
          "this server has no music at all, which is a different answer from 'no server' and from 'nothing to "
          + "watch', and the tile says which of the three it is");
      Assert.That(home.Open(audio), Is.Null);
      Assert.That(home.Status, Does.Contain("not on this server"));
    }

    [AvaloniaTest]
    public void AServerThatWentAwayIsNotPretendedToStillBeThere()
    {
      HomeViewModel home = new HomeViewModel(new ServerApiClient(SIGN_IN_URL));
      Assert.That(EntryNamed(home.Menu, "Movies").Detail, Is.EqualTo("no server connected"),
          "having an address is not having a server; until one has answered, the menu says no server");
    }

    [AvaloniaTest]
    public void PointingAtANewServerEmptiesTheMenuBackToNoServer()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = new HomeViewModel(new ServerApiClient(server.BaseUrl));
      home.RefreshCommand.Execute(null);
      // Waited for a section rather than for anything openable, because the folders screen is openable whatever
      // the server does; what this test is about is the library, and the library arrives as sections.
      WaitUntil("the menu to fill", delegate { return home.Menu.Any(entry => entry.Section != null); });

      home.UseServer(new ServerApiClient(new Uri("http://127.0.0.1:2/MediaPortal/Server/")));

      Assert.That(home.Menu.Count(entry => entry.Section != null), Is.Zero,
          "the films of the old server are not films of the new one, and a menu still offering them would be "
          + "offering titles that are not there");
      Assert.That(home.Menu.Count(entry => entry.Openable), Is.EqualTo(9),
          "only the nine screens that need nothing from a library - the folders, the pictures, the browse "
          + "screen, the news, the weather, the party, the settings and the two guides - survive a server that "
          + "went away, and the guides survive it because a television does not live in the library");
      Assert.That(EntryNamed(home.Menu, "Movies").Detail, Is.EqualTo("no server connected"));
    }

    /// <summary>
    /// The content pane written out as one sequence of lines: what the viewer walks, in the order the walk goes.
    /// Titles say only that they are titles, because which words a title carries is the shelf endpoint's claim,
    /// tested where the titles are read; here it is the shape of the pane that is in question.
    /// </summary>
    private static string[] PaneRows(HomeViewModel home)
    {
      return home.ContentRows.Select(row =>
          row.IsHeading ? "heading:" + row.Text
          : row.IsNote ? "note:" + row.Text
          : "title").ToArray();
    }

    [AvaloniaTest]
    public async Task ThePaneOfAShelfCarriesTheListsMP2sHomeHadAndSaysWhichOneCannotBeFilled()
    {
      List<MediaItemQuery> asked =
          RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      await home.ShowContentAsync(EntryNamed(home.Menu, "Movies"));

      Assert.That(PaneRows(home), Is.EqualTo(new[]
      {
          "heading:Latest added",
          "title",
          "heading:Favorites",
          "title",
          "heading:Unplayed",
          "title"
      }), "a list is only shown when it has items - AbstractHomeContent's one test, if (wrapper.HasItems). The "
          + "empty continue-playing shelf therefore takes itself off entirely, heading included, and no line of "
          + "apology stands where it was; the favourites stand, because the server answers that shelf the way the "
          + "reference's providers do - titles this viewer has played (BaseFavoriteMediaListProvider.cs:41-57)");

      Assert.That(home.ContentRows.First(row => row.IsTitle).Title!.Line, Does.Contain("Alien Earth"),
          "the titles under the first heading are the server's own, not placeholders");

      List<MediaItemQuery> arrivals = asked.Where(query => query.SortInformation.Count == 1
              && ((AttributeSortInformation)query.SortInformation[0]).AttributeType
                  == ImporterAspect.ATTR_DATEADDED)
          .ToList();
      Assert.That(arrivals, Has.Count.EqualTo(1),
          "exactly one of the pane's questions was about when the titles entered the library - the list under the "
          + "first heading - and it was the server that turned that word into the column, as its own test says");
      Assert.That(((AttributeSortInformation)arrivals[0].SortInformation[0]).Direction,
          Is.EqualTo(SortDirection.Descending),
          "newest arrivals first, asked of the database rather than sorted from one page at the end");

      Assert.That(home.HasContent, Is.True);
    }

    [AvaloniaTest]
    public async Task ThePaneOfAScreenSaysWhatPressingTheEntryOpens()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      await home.ShowContentAsync(EntryNamed(home.Menu, "Images"));

      Assert.That(PaneRows(home), Is.EqualTo(new[] { "note:Press Enter: the pictures, folder by folder" }),
          "an entry that opens a screen of its own has no lists to show, and the useful line is the one that says "
          + "what pressing it does - an empty column next to the menu reads as a broken front end");
    }

    [AvaloniaTest]
    public async Task HelpTextsOffSilencesTheNoteAndKeepsTheLists()
    {
      // EnableHelpTexts is Nereus' switch over exactly one thing - "help texts for menues on the home
      // screen", the reference's words - so off takes the explaining note away and leaves everything the
      // lists themselves say standing where it stood.
      string previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          Path.Combine(Path.GetTempPath(), "mp2-help-" + Guid.NewGuid().ToString("N")));
      string wasTheme = "";
      try
      {
        Application app = Application.Current!;
        wasTheme = FrontendTheme.CurrentName(app);

        NereusSkinSettings off = new NereusSkinSettings();
        off.EnableHelpTexts = false;
        off.Save(null, out _);
        FrontendTheme.Apply(app, "nereus", out _);

        RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
        InProcessApi server = StartServer();
        HomeViewModel home = HomeOn(server);
        Refreshed(home);

        await home.ShowContentAsync(EntryNamed(home.Menu, "Images"));
        Assert.That(PaneRows(home), Is.Empty,
            "the note is the help text the switch governs; the entry has no lists, so with help off the pane says nothing");
      }
      finally
      {
        if (wasTheme.Length > 0)
          FrontendTheme.Apply(Application.Current!, wasTheme, out _);
        Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", previousDir.Length == 0 ? null : previousDir);
      }
    }

    [AvaloniaTest]
    public async Task WalkingTheMenuLeavesThePaneShowingOnlyWhatTheLastStopAnswered()
    {
      // What this guards is the race a remote makes by walking: every stop fires a read, HTTP answers whenever it
      // likes, and an older answer landing under a newer heading tells the viewer about a shelf they have already
      // walked past. The generation guard says only the newest reading may keep writing.
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      Task first = home.ShowContentAsync(EntryNamed(home.Menu, "Movies"));
      await home.ShowContentAsync(EntryNamed(home.Menu, "Images"));
      await first;

      Assert.That(PaneRows(home), Is.EqualTo(new[] { "note:Press Enter: the pictures, folder by folder" }),
          "the films arrived after the viewer had already moved on, and the pane they would have written is the "
          + "pictures pane now - the late answer had to throw itself away rather than append to it");
    }

    /// <summary>
    /// The neighbour read, standing the wall ahead: while one wall is watched, the wall of the neighbour the walk
    /// would step onto next is read and laid out on the dark host, so the step that follows is the reference's
    /// pointer swap and nothing else - no reading, no building, no burst between the settling's end and the wall
    /// standing. The settling itself is not moved: the step still waits its full half second.
    /// </summary>
    [AvaloniaTest]
    public async Task WhileAWallStandsTheNeighboursWallIsBuiltInTheDarkHostAheadOfTheStep()
    {
      RegisterLibrary(Title("Alien Earth", 2025, RESOURCE_SYSTEM_ID.ToString("B") + "://" + LOCAL_PATH));
      InProcessApi server = StartServer();

      HomeViewModel home = HomeOn(server);
      Refreshed(home);

      await home.ShowContentAsync(EntryNamed(home.Menu, "Movies"));
      Assert.That(home.ContentGroups, Is.Not.Empty, "the standing wall carries its bands");
      int standing = home.WallIndex;
      var dark = standing == 0 ? home.WallGroups1 : home.WallGroups0;
      Assert.That(dark, Is.Empty, "the dark host starts out wearing nobody's wall");

      MainMenuEntry neighbour = (MainMenuEntry) typeof(HomeViewModel)
          .GetMethod("NeighbourToPreload", BindingFlags.NonPublic | BindingFlags.Instance)!
          .Invoke(home, new object?[] { EntryNamed(home.Menu, "Movies") })!;

      await Task.Delay(2500);

      Assert.That(home.WallIndex, Is.EqualTo(standing),
          "the probe builds into the dark host and never touches the light");
      Assert.That(dark, Is.Not.Empty,
          "the neighbour's wall did not come to stand in the dark host while the viewer had the floor");

      await home.ShowContentAsync(neighbour);

      Assert.That(ReferenceEquals(home.ContentGroups, dark), Is.True,
          "the step onto the pre-heated neighbour was the pointer swap the reference made it be: the very "
          + "collection the probe laid out stands now, not one emptied and filled again");
    }

    #endregion
  }
}
