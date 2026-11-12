/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
    for more details.

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
using NUnit.Framework;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The wall of people: what it asks the server for, how it lays the names it gets, and which of its answers it
  /// remembers for the next evening.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every question here is asked of a server that answers in the same JSON the product's own controller writes,
  /// because the wall's whole job is that conversation: it asks for one shelf's people a page at a time, it asks
  /// for a letter rather than cutting one out of what arrived, and it asks again in the order the viewer picked.
  /// A fake library could not answer any of that honestly - the server decides which names a letter holds, and the
  /// wall has to believe it - while a canned server says exactly what the wall asked.
  /// </para>
  /// <para>
  /// The three walls share one class and one memory format, so the tests below spend their last two cases on the
  /// thing that could go wrong quietly: the films remembering an order the series then inherits.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheWallOfPeople
  {
    private static readonly Uri SERVER_URL = new Uri("http://localhost:55555/MediaPortal/Server");

    private string _previousSettingsFile = "";
    private string _previousScreensFile = "";
    private string _ownDir = "";
    private string? _ownerBefore;

    [SetUp]
    public void IsolateTheMachinesMemory()
    {
      _previousSettingsFile = Environment.GetEnvironmentVariable("MP2_SETTINGS_FILE") ?? "";
      _previousScreensFile = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE") ?? "";
      _ownerBefore = SkinSettingsBase.FileOwner;

      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-people-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_ownDir);
      Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", Path.Combine(_ownDir, "frontend.json"));
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
      SkinSettingsBase.FileOwner = _ownerBefore;
    }

    private static PeopleViewModel AWall(PeopleShelf shelf, WallServer server)
    {
      return new PeopleViewModel(new ServerApiClient(SERVER_URL, new HttpClient(server)), shelf);
    }

    /// <summary>
    /// Puts the worn skin back after a case that dressed a screen in one.  A wall read under Nereus leaves that
    /// skin's resources standing on the application, and the next screen laid out on this thread - a screen that
    /// never asked for a skin at all - found a worn-out compositor standing in its place.  Same reason the other
    /// fixtures that dress a screen wear the plain face again on the way out; asked here rather than from a TearDown,
    /// because a TearDown also runs after the cases that never touched the application at all.
    /// </summary>
    private static void WearThePlainFaceAgain()
    {
      Application? app = Application.Current;
      if (app == null)
        return;

      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    /// <summary>
    /// The one line a media screen has for a sentence stands at the room's foot, and the wall spends it on the name
    /// the caret is on - never on itself.  Which of the two it says is the shelf's law: the rail's word while the
    /// rail is out, the stood-on name while the details column is turned away, and nothing at all while the column
    /// is up and says the same thing better.
    /// </summary>
    [Test]
    public async Task TheFootLineSaysTheStoodOnNameAndNeverTheWallItself()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      await wall.ReadAsync(true);

      PersonTile stood = wall.Rows.OfType<PersonTile>().First();
      wall.ShowDetails = true;
      wall.Selected = stood;

      Assert.That(wall.ContentInfo, Does.Contain(stood.Name), "the line is about the name the caret stands on");
      Assert.That(wall.ContentInfo, Does.Contain(stood.BornWord), "and it carries the years the row carries");
      Assert.That(wall.ContentInfo, Does.Not.Contain("Film actors"),
          "the line says who is stood on, never which wall this is");
      Assert.That(wall.ShowsContentInfo, Is.False, "the column is up, and says all of this better");

      // The column turned away is not the knowledge turned away: the line takes the telling over.
      int asked_by_now = server.Asked.Count;
      wall.ShowDetails = false;
      Assert.That(wall.ShowsContentInfo, Is.True, "with the column away the line stands");
      Assert.That(server.Asked.Count, Is.EqualTo(asked_by_now),
          "and it asks nothing to say it - a foot line that asked would put a request behind every arrow key");

      // The rail speaks at this very place while it is out, and the line steps aside for it.
      wall.MediaMenuVisible = true;
      Assert.That(wall.ShowsContentInfo, Is.False, "the rail's word stands here, not the name");
      wall.MediaMenuVisible = false;
      Assert.That(wall.ShowsContentInfo, Is.True, "and it comes back with the rail");

      //  A caret off the wall leaves nothing to say.
      wall.Selected = null;
      Assert.That(wall.ContentInfo, Is.Empty, "nobody stood on is no line");
      Assert.That(wall.ShowsContentInfo, Is.False);
    }

    [Test]
    public async Task TheWallIsNamedAndSaysHowManyNamesItHolds()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      await wall.ReadAsync(true);

      Assert.That(wall.Rows.Count, Is.EqualTo(9), "every name the server answered stands on the wall");
      Assert.That(wall.Total, Is.EqualTo(9), "the count is the server's, not a count of the rows");
      Assert.That(wall.Headline, Does.StartWith("Film actors"), "the wall says whose names these are");
      Assert.That(wall.Headline, Does.Contain("9"), "and how many");
      Assert.That(wall.Notice, Is.Empty, "a wall that was answered has nothing to complain about");
      Assert.That(wall.AtEnd, Is.True, "nine names arrived out of nine, so there is nothing behind them");
    }

    [Test]
    public async Task EachWallAsksForThePeopleOfItsOwnShelf()
    {
      WallServer films = new WallServer(3);
      await AWall(PeopleShelf.FilmActors, films).ReadAsync(true);
      Assert.That(films.Asked[0], Does.Contain("aspect=MovieItem"));
      Assert.That(films.Asked[0], Does.Contain("roles=actor"));

      WallServer series = new WallServer(3);
      await AWall(PeopleShelf.SeriesActors, series).ReadAsync(true);
      Assert.That(series.Asked[0], Does.Contain("aspect=SeriesItem"),
          "the people who play in the series are asked of the series");

      WallServer records = new WallServer(3);
      await AWall(PeopleShelf.Artists, records).ReadAsync(true);
      Assert.That(records.Asked[0], Does.Contain("aspect=AlbumItem"));
      Assert.That(records.Asked[0], Does.Contain("roles=albumartist"),
          "a record's artist is the one who made the record, not the one who sang one song of it");
    }

    [Test]
    public async Task AWalkOverTheWallAsksForTheNamesBehindIt()
    {
      WallServer server = new WallServer(303);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      await wall.ReadAsync(true);
      Assert.That(wall.Rows.Count, Is.EqualTo(300), "a wall is opened a screenful at a time, never all seven "
          + "thousand names of it");
      Assert.That(wall.AtEnd, Is.False);

      await wall.KeepReadingAsync();
      Assert.That(wall.Rows.Count, Is.EqualTo(303), "the caret reaching the end of what stands there brings the "
          + "rest");
      Assert.That(wall.AtEnd, Is.True);
      Assert.That(server.WallAsks, Is.EqualTo(2), "and once the whole wall stands there, a caret walking on its "
          + "last row asks for nothing");

      await wall.KeepReadingAsync();
      Assert.That(server.WallAsks, Is.EqualTo(2));
    }

    [Test]
    public async Task OneLetterIsAskedOfTheServerAndNotCutOutOfTheNames()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      await wall.PickLetterAsync("B");

      Assert.That(server.LastWall, Does.Contain("letter=B"));
      Assert.That(wall.IsNarrowed, Is.True);
      foreach (object row in wall.Rows)
      {
        PersonTile tile = (PersonTile) row;
        Assert.That(tile.Name, Does.StartWith("B"), "the wall holds the names the server gave it for its letter");
      }

      await wall.ClearLetterAsync();
      Assert.That(server.LastWall, Does.Not.Contain("letter="),
          "every name is asked for by naming no letter at all");
      Assert.That(wall.Rows.Count, Is.EqualTo(9));
    }

    [Test]
    public async Task AGroupedWallStandsItsNamesUnderTheirLetters()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      await wall.PickGroupAsync("letter");

      Assert.That(wall.IsGrouped, Is.True);
      List<string> plates = new List<string>();
      foreach (object row in wall.Rows)
      {
        if (row is GroupHeaderRow plate)
          plates.Add(plate.Label);
      }

      Assert.That(plates, Is.EquivalentTo(new[] { "A", "B", "C" }), "one plate per letter the names fall under");
      foreach (object row in wall.Rows)
      {
        if (row is GroupHeaderRow plate)
        {
          Assert.That(plate.CountWord, Is.Empty, "a wall of names counts its own names, and does not put a number "
              + "on a plate the server never counted");
        }
      }

      Assert.That(wall.NamesUnderLetter("A"), Is.EqualTo(3));
    }

    /// <summary>
    /// The same wall plated by the decade its names were born in, measured against a library as sparse as this one:
    /// two birth years in its 7543 film actors.  A grouping that only plates what it has is a grouping that loses the
    /// rest of the wall, so the names nobody ever gave a year stand under one plate of their own, and the law the
    /// whole case rests on is that a plate CLOSES - the wall walks past a boundary once, not twenty times - which is
    /// why picking a plate brings the reading it needs along with it.
    /// </summary>
    [Test]
    public async Task GroupingByDecadeStandsTheYearsUnderDecadesAndTheNamesWithoutOneUnderTheirOwnPlate()
    {
      WallServer server = new WallServer(60);
      server.YearsHeld = 12;
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      await wall.PickGroupAsync("born");

      Assert.That(wall.Sort, Is.EqualTo("born"),
          "a wall plated by decade is read by decade, or every second name opens a new plate");
      Assert.That(server.LastWall, Does.Contain("sort=born"), "and it is asked of the server that way");

      List<string> plates = wall.Rows.OfType<GroupHeaderRow>().Select(plate => plate.Label).ToList();
      Assert.That(plates, Has.Some.EqualTo(PersonTile.NO_YEAR_PLATE),
          "the names nobody ever gave a year are not thrown away by a grouping, they are plated");
      Assert.That(plates.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(plates.Count),
          "one plate per decade and none of them reopening further down the wall");
      Assert.That(wall.Rows.OfType<PersonTile>().Count(), Is.EqualTo(60),
          "and every name of the wall is still on it");

      // Under each plate stand only the names of that plate, which is the only promise a grouping makes.
      string under = "";
      foreach (object row in wall.Rows)
      {
        if (row is GroupHeaderRow plate)
        {
          under = plate.Label;
          continue;
        }

        PersonTile name = (PersonTile)row;
        Assert.That(name.DecadeLabel, Is.EqualTo(under), name.Name + " stands under the wrong plate");
        Assert.That(under == PersonTile.NO_YEAR_PLATE ? name.BornDate == null : name.BornDate != null,
            Is.True, name.Name + " is plated by a year its record does not hold");
      }

      Assert.That(plates.Count(label => label != PersonTile.NO_YEAR_PLATE), Is.EqualTo(4),
          "twelve names born between 1900 and 1933 fall under four decade plates, and the plates are theirs alone");

      // Read from its other end, the plate of names without a year travels to the other side with them - the server
      // puts a name nobody gave a year at the front of an ascending reading and at the back of a descending one, and
      // a wall that plated them in the middle of either would open their plate twice.
      await wall.PickSortAsync("born");
      plates = wall.Rows.OfType<GroupHeaderRow>().Select(plate => plate.Label).ToList();
      Assert.That(plates[plates.Count - 1], Is.EqualTo(PersonTile.NO_YEAR_PLATE),
          "read the other way the yearless names stand last, where the server puts them");

      // The letters are still a plate this wall can stand under, and they plate by name again - a grouping that left
      // the wall read by birth year would letter-plate every second name.
      await wall.PickGroupAsync("letter");
      Assert.That(wall.Sort, Is.EqualTo("name"), "the letter plates ask for the wall read by name");
      Assert.That(wall.Rows.OfType<GroupHeaderRow>().Select(plate => plate.Label).Distinct().Count(),
          Is.EqualTo(3), "three letters, one plate each");
      Assert.That(wall.Rows.OfType<GroupHeaderRow>().Any(plate => plate.Label == PersonTile.NO_YEAR_PLATE),
          Is.False, "a wall plated by letter has nothing to say about years");

      // A plate this screen does not offer is no plate at all - the shelf's own test before it believes a grouping.
      await wall.PickGroupAsync("genre");
      Assert.That(wall.IsGrouped, Is.False, "a grouping the wall cannot fill leaves it ungathered");
    }

    /// <summary>
    /// The grouping page of the wall's rail names the plates it can actually stand under, and marks the one the wall
    /// is wearing.  A rail row that leads to plates the screen cannot lay is a row that should not be standing.
    /// </summary>
    [Test]
    public void TheGroupPageOfTheWallsRailNamesItsTwoPlates()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      wall.ShowMediaMenu();
      wall.MediaMenu.First(row => row.Icon == "group").Command.Execute(null);

      Assert.That(wall.MediaMenuHeading, Is.EqualTo("Group"));
      Assert.That(wall.MediaMenu.Select(row => row.Label),
          Is.EqualTo(new[] { "None", "First letter", "Birth decade" }));
      Assert.That(wall.MediaMenu.First(row => row.Label == "None").IsCurrent, Is.True,
          "an ungathered wall marks the row that says so");
      Assert.That(wall.MediaMenu.First(row => row.Label == "Birth decade").IsCurrent, Is.False);
    }

    [Test]
    public async Task ANameIsLookedForInTheWholeWall()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);

      await wall.FindAsync("Actor 002");

      string last = server.LastWall;
      Assert.That(last, Does.Contain("search="), "the word is carried to the server, which is the one that knows "
          + "every name it holds");
      Assert.That(wall.Rows.Count, Is.EqualTo(3), "one name of each letter carries the looked-for word");

      await wall.ClearSearchAsync();
      Assert.That(server.LastWall, Does.Not.Contain("search="));
      Assert.That(wall.Rows.Count, Is.EqualTo(9));
    }

    [Test]
    public async Task TheOrderAndTheEndAreAskedForAndRemembered()
    {
      WallServer first = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, first);
      await wall.ReadAsync(true);

      await wall.PickSortAsync("born");
      string last = first.LastWall;
      Assert.That(last, Does.Contain("sort=born"));

      await wall.PickSortAsync("born");
      last = first.LastWall;
      Assert.That(last, Does.Contain("order=desc"), "the same order pressed again reads the wall from its other end");

      // The next evening: a wall built again over the same settings file reads itself the way the viewer left it.
      WallServer next_evening = new WallServer(9);
      PeopleViewModel remembered = AWall(PeopleShelf.FilmActors, next_evening);
      await remembered.ReadAsync(true);

      Assert.That(remembered.Sort, Is.EqualTo("born"));
      Assert.That(remembered.SortDescending, Is.True);
      Assert.That(next_evening.Asked[0], Does.Contain("sort=born"), "the order is asked for, not applied afterwards");
      Assert.That(remembered.Rows.Count, Is.EqualTo(9));
    }

    [Test]
    public async Task EachWallRemembersItsOwnAnswer()
    {
      WallServer films = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, films);
      await wall.ReadAsync(true);
      // The plate first and the order after it, which is the only order a viewer can press them in that leaves the
      // wall standing as it was asked to: a plate brings the reading it needs with it, and an order picked on top of
      // a plated wall is the viewer's own doing and stays.
      await wall.PickGroupAsync("letter");
      await wall.PickSortAsync("born");

      // The three walls differ in one thing only - whose people they are - which is exactly the thing a shared
      // memory would get wrong: an order and a grouping picked for the films have no business standing under the
      // records.
      WallServer records = new WallServer(9);
      PeopleViewModel artists = AWall(PeopleShelf.Artists, records);
      await artists.ReadAsync(true);

      Assert.That(artists.Sort, Is.EqualTo("name"), "the records were never asked to be read by a birthday");
      Assert.That(artists.Grouping, Is.Empty);
      Assert.That(records.Asked[0], Does.Contain("sort=name"));

      ScreenViewSettings held = ScreenViewSettings.Load(null, out _);
      Assert.That(held.TryAnswer("MovieItem", "actors", out ScreenViewSettings.ScreenAnswer of_films), Is.True);
      Assert.That(of_films.Sort, Is.EqualTo("born"));
      Assert.That(held.TryAnswer("AlbumItem", "artists", out _), Is.False,
          "a wall that was never configured writes nothing for the next evening");
    }

    [Test]
    public void TheShortcutOnTheHomesArmNamesItsWall()
    {
      Assert.That(PeopleViewModel.IsWallShortcut("MediaMenu/actor", null, out PeopleShelf from_nothing), Is.True);
      Assert.That(from_nothing, Is.EqualTo(PeopleShelf.FilmActors));

      Assert.That(PeopleViewModel.IsWallShortcut("MediaMenu/actor", new HomeSection("TV Series",
          new AspectViewModel(new MediaAspect(Guid.Empty, "SeriesItem", false, false, null,
              Array.Empty<AspectAttribute>()))), out PeopleShelf under_tv), Is.True);
      Assert.That(under_tv, Is.EqualTo(PeopleShelf.SeriesActors),
          "the one tile the skin carries means the people of the shelf the viewer was standing on");

      Assert.That(PeopleViewModel.IsWallShortcut("MediaMenu/artist", null, out PeopleShelf of_records), Is.True);
      Assert.That(of_records, Is.EqualTo(PeopleShelf.Artists));

      Assert.That(PeopleViewModel.IsWallShortcut("MediaMenu/tv", null, out _), Is.False,
          "a shortcut for something else is not asked of this screen");
      Assert.That(PeopleViewModel.IsWallShortcut(null, null, out _), Is.False);
    }

    [AvaloniaTest]
    public async Task APressedPersonOpensTheirOwnWorks()
    {
      // The press itself is answered by the application, which is the one that can open a shelf of titles; what
      // this front end has to get right is the word a shelf carries when it stands under one person.
      WallServer server = new WallServer(3);
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "a shelf needs the application to know what skin it wears");
      FrontendTheme.Apply(app!, FrontendTheme.DEFAULT_NAME, out _);
      MediaBrowserViewModel works = new MediaBrowserViewModel(
          new ServerApiClient(SERVER_URL, new HttpClient(server)),
          new HomeSection("Movies", new AspectViewModel(new MediaAspect(Guid.Empty, "MovieItem", false, false,
              null, Array.Empty<AspectAttribute>()))));

      Guid person = Guid.Parse("00000001-0000-0000-0000-000000000000");
      works.WearsPerson(person, new[] { "actor" });
      await works.OpenAsync();

      Assert.That(server.TitlesAsks, Is.GreaterThan(0), "the shelf under a person is read like any other shelf");
      string asked = server.LastTitles;
      Assert.That(asked, Does.Contain("who=" + person.ToString("D")),
          "the shelf is cut to the one person the viewer pressed");
      Assert.That(asked, Does.Contain("roles=actor"));

      // The grouped shelf asks for each of its groups by itself, and a person who got lost on the way would widen
      // the wall back out under a heading that still names them.
      int asked_before = server.TitlesAsks;
      await works.PickGroupAsync("genre");
      Assert.That(server.TitlesAsks, Is.GreaterThan(asked_before), "the group of its own titles was asked for");
      Assert.That(server.LastTitles, Does.Contain("who=" + person.ToString("D")),
          "and the group stands under the same person");
    }

    /// <summary>
    /// The wall's leading edge is the whole first column of names, which is the law a shelf of titles already lives
    /// by (<c>MediaBrowserView.StandingOnTheLeadingEdge</c>) and for the shelf's reason: the rail stands in the strip
    /// of points the room keeps off the left edge of the screen, so a viewer pointing left out of the wall's first
    /// column is pointing at it.  A viewer standing in the middle of a row of tiles points at the name beside them,
    /// and a rail that jumped out on every left press would be a rail nobody could walk past.  Laid as rows the wall
    /// is one column of names, so there every row touches the edge.
    /// </summary>
    [AvaloniaTest]
    public void WalkingOffTheFrontOfTheWallOpensItsRailAndANameWithNamesBesideItDoesNot()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      int columns = TheWallColumns(names);

      // A name with names beside it is no edge: the caret steps sideways and the rail stays where it stands.  The
      // name is picked one place in from the edge of its row, which is a name no number of columns puts on it.
      WaitUntil("the whole wall to stand there", delegate { return wall.Rows.Count > 30 && wall.AtEnd; });
      int inside = columns * 2 + 1;
      names.SelectedIndex = inside;
      for (int settling = 0; settling < 30; settling++)
        Pump();
      Assert.That(names.SelectedIndex, Is.EqualTo(inside), "the caret stands on a name that has names beside it");
      Assert.That(wall.MediaMenuVisible, Is.False, "a wall that was only read carries no rail");

      SendKey(names, Key.Left);
      Assert.That(wall.MediaMenuVisible, Is.False,
          "a tile in the middle of a row means the name beside it, and the rail keeps out of the screen");
      Assert.That(names.SelectedIndex, Is.EqualTo(inside - 1),
          "the key walked the caret one name sideways instead");

      // The first name of the wall's second row is as much its edge as the very first name of the whole wall.
      names.SelectedIndex = columns * 3;
      for (int settling = 0; settling < 30; settling++)
        Pump();
      Assert.That(names.SelectedIndex % TheWallColumns(names), Is.EqualTo(0),
          "the caret stands on the wall's edge");
      SendKey(names, Key.Left);
      WaitUntil("the rail to stand open for the edge of a later row",
          delegate { return wall.MediaMenuVisible; });

      SendKey(window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "mediaMenuList"),
          Key.Right);
      WaitUntil("the rail to slide away again", delegate { return !wall.MediaMenuVisible; });

      // And laid as rows, where nothing stands beside a name, the key keeps the answer it always had.
      wall.Layout = PeopleViewModel.LAYOUT_LIST;
      WaitUntil("the wall to stand as rows", delegate { return names.Classes.Contains("nereus-shelf-rows"); });
      names.SelectedIndex = 5;
      SendKey(names, Key.Left);
      WaitUntil("the rail to stand open for a row in the middle of the wall",
          delegate { return wall.MediaMenuVisible; });

      window.Close();
    }

    /// <summary>
    /// The walls of a people wall's rail are the shelf's walls, and so is the answer to a key that points past one:
    /// the rail's ground nods toward that wall and springs back, the menu stays open, and the caret stays on its row.
    /// A rail that put its own menu away at the key beside it left the viewer with no menu and no caret at all,
    /// which is what the viewer asked about on 2026-11-12.
    /// </summary>
    [AvaloniaTest]
    public void PointingPastAWallOfTheWallsRailNodsTheRailAndKeepsEverythingWhereItIs()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });

      // The wall's leading edge, from which the pointing key brings the rail out.
      names.SelectedIndex = 0;
      for (int settling = 0; settling < 30; settling++)
        Pump();
      SendKey(names, Key.Left);
      WaitUntil("the rail to stand open", delegate { return wall.MediaMenuVisible; });

      ListBox rail = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });
      int rows = rail.Items.Count;

      // Nothing stands left of the rail.  The nod's frames ride the render timer, which the headless harness never
      // turns, so what a headless test can prove is the decision - the key reaches the rail's ground and is answered
      // there, the menu stays open, and the caret stays put.  That the ground wears a nod transform afterwards is the
      // witness that it was a nod and not a swallow, and that the caret is still inside the rail is the witness that
      // the answer was not "close the menu and hope".
      Panel side_ground = window.GetVisualDescendants().OfType<Panel>().First(panel => panel.Name == "sideMenu");
      SendKey(rail, Key.Left);
      Pump();
      Assert.That(side_ground.RenderTransform, Is.InstanceOf<TranslateTransform>(),
          "the wall key is answered by a nod of the rail's ground");
      Assert.That(wall.MediaMenuVisible, Is.True, "a wall does not put the menu away");
      Assert.That(rail.IsKeyboardFocusWithin, Is.True, "and a wall does not throw the caret out of the rail");
      Assert.That(rail.SelectedIndex, Is.EqualTo(0), "and a wall does not walk the caret off its row");

      // The head: up on the first row is a wall as well, and the caret keeps its row.
      SendKey(rail, Key.Up);
      Pump();
      Assert.That(wall.MediaMenuVisible, Is.True, "the head of the rail is a wall, not an exit");
      Assert.That(rail.SelectedIndex, Is.EqualTo(0), "and the head of the rail keeps its row");

      // The foot: down on the last row is the third wall.
      rail.SelectedIndex = rows - 1;
      if (rail.ContainerFromIndex(rows - 1) is ListBoxItem last_row)
        last_row.Focus();
      Pump();
      SendKey(rail, Key.Down);
      Pump();
      Assert.That(wall.MediaMenuVisible, Is.True, "the foot of the rail is a wall, not an exit");
      Assert.That(rail.SelectedIndex, Is.EqualTo(rows - 1), "and the foot of the rail keeps its row");

      // And a key with a road under it is still just walking.
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
    /// THE WALL ITSELF NODS AT ITS OWN EDGES, the way a shelf of titles has always nodded at its walls: an arrow
    /// that points past the last column, past the head or past the foot leans the wall twenty-two points that way
    /// and springs it back, instead of walking the caret off the wall or handing the key to the shell.  A wall of
    /// names that only nods its rail leaves the viewer no way to tell an edge from a broken key, which is what the
    /// viewer pointed out on 2026-11-13: "the actor media wall has not the same bounce effects as the other media
    /// screens - make a gap analysis of the differences".
    /// </summary>
    [AvaloniaTest]
    public void TheWallOfNamesNodsAtItsOwnEdges()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      int columns = TheWallColumns(names);

      // Every answer the wall gives to an arrow belongs to the caret standing inside it, so a case first stands the
      // caret on the tile it is about to ask about - on the container the wall recycled for that name, which is
      // where a shelf stands its caret too.
      void StandOn(int at)
      {
        names.SelectedIndex = at;
        WaitUntil("the wall to lay the tile " + at, delegate { return names.ContainerFromIndex(at) != null; });
        (names.ContainerFromIndex(at) as Control)!.Focus();
        Pump();
        Assert.That(names.IsKeyboardFocusWithin, Is.True, "the caret stands inside the wall");
      }

      // A key with a road under it is still ordinary walking and nods nothing.  This is checked first, while
      // nothing has been nodded yet: a nod is remembered by the wall for the tests to read, and the memory outlives
      // the spring.
      Assert.That(view.LastNodX, Is.EqualTo(0.0), "nobody has pointed at a wall yet");
      Assert.That(view.LastNodY, Is.EqualTo(0.0), "and nobody has pointed past its head or its foot either");
      StandOn(0);
      SendKey(names, Key.Down);
      Assert.That(names.SelectedIndex, Is.EqualTo(columns), "a Down with a row under it walks to that row");
      Assert.That(view.LastNodX, Is.EqualTo(0.0), "and a row under it is not a wall");
      Assert.That(view.LastNodY, Is.EqualTo(0.0), "which the wall says by not leaning anywhere");

      // The right hand wall: the last column of a row.
      StandOn(columns - 1);
      SendKey(names, Key.Right);
      Assert.That(view.LastNodX, Is.GreaterThan(0.0), "past the last column the wall leans right");
      Assert.That(names.SelectedIndex, Is.EqualTo(columns - 1), "and the caret stays on the tile it stood on");
      Assert.That(wall.MediaMenuVisible, Is.False, "nor does the key put the rail out of the wall's way");

      // The head: the first row of the wall.  Above it the wall keeps its nod - this wall wears no search field on
      // its head line, so there is nothing above it for the arrow to walk into.
      StandOn(0);
      SendKey(names, Key.Up);
      Assert.That(view.LastNodY, Is.LessThan(0.0), "above the head of the wall the wall leans up");
      Assert.That(names.SelectedIndex, Is.Zero, "and the head of the wall keeps its caret");

      // The foot: the last name of a wall that is read whole, which is the last row whatever the number of columns.
      int last = names.Items.Count - 1;
      StandOn(last);
      SendKey(names, Key.Down);
      Assert.That(view.LastNodY, Is.GreaterThan(0.0), "below the foot of the wall the wall leans down");
      Assert.That(names.SelectedIndex, Is.EqualTo(last), "and the foot of the wall keeps its caret");

      // And one row up from the foot the same key is a step, not a wall.
      StandOn(last - columns);
      int stepped_to = last - columns;
      SendKey(names, Key.Down);
      Assert.That(names.SelectedIndex, Is.EqualTo(stepped_to + columns),
          "a Down with a row under it walks to that row");

      // Behind the wall's leading edge the rail still waits: none of these walls took the Left away that brings it.
      StandOn(0);
      SendKey(names, Key.Left);
      WaitUntil("the rail to stand open", delegate { return wall.MediaMenuVisible; });

      window.Close();
    }

    /// <summary>
    /// A wall laid as rows is one name wide, so its right hand wall stands beside EVERY row - the shelf's law for
    /// its list (MediaBrowserView, the list branch of the wall's key handler) - while its head and foot stay the
    /// first and the last name.
    /// </summary>
    [AvaloniaTest]
    public void AWallLaidAsRowsNodsAtItsRightHandFromEveryRow()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });

      wall.Layout = PeopleViewModel.LAYOUT_LIST;
      WaitUntil("the wall to stand as rows", delegate { return names.Classes.Contains("nereus-shelf-rows"); });

      void StandOn(int at)
      {
        names.SelectedIndex = at;
        WaitUntil("the wall to lay the row " + at, delegate { return names.ContainerFromIndex(at) != null; });
        (names.ContainerFromIndex(at) as Control)!.Focus();
        Pump();
      }

      // The middle of the wall, where a tile-shaped wall has nothing to its right: a row is one name long, so the
      // right hand edge is reached from here too.
      Assert.That(view.LastNodX, Is.EqualTo(0.0), "nobody has pointed at a wall yet");
      StandOn(5);
      SendKey(names, Key.Right);
      Assert.That(view.LastNodX, Is.GreaterThan(0.0), "the right hand of a wall of rows is a wall from any row");
      Assert.That(names.SelectedIndex, Is.EqualTo(5), "and the row keeps its caret");
      Assert.That(wall.MediaMenuVisible, Is.False, "and the nod is not the rail jumping out");

      StandOn(0);
      SendKey(names, Key.Up);
      Assert.That(view.LastNodY, Is.LessThan(0.0), "the first row is the head of a wall of rows");

      int last = names.Items.Count - 1;
      StandOn(last);
      SendKey(names, Key.Down);
      Assert.That(view.LastNodY, Is.GreaterThan(0.0), "and the last one is its foot");
      Assert.That(names.SelectedIndex, Is.EqualTo(last), "with the caret left on the row it was on");

      window.Close();
    }

    /// <summary>
    /// THE RAIL'S "SEARCH" PLATE PUTS A FIELD ON THE HEAD BAND, hands it the caret, and the wall answers to the
    /// words - the law a shelf of titles has always worn (MediaBrowserView's searchStrip, and its viewer note
    /// 2026-11-10 that the head line wears no magnifier waiting to be clicked).  The plate stood on this rail and
    /// did nothing at all when pressed, which is what the viewer found on 2026-11-12: a promise the screen does not
    /// keep is worse than no promise.
    /// </summary>
    [AvaloniaTest]
    public void TheWallsSearchPlatePutsTheFieldOnTheScreenAndHandsItTheCaret()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });

      TextBox box = window.GetVisualDescendants().OfType<TextBox>().First(field => field.Name == "searchBox");
      Border plate = window.GetVisualDescendants().OfType<Border>().First(field => field.Name == "searchField");
      Button keys = window.GetVisualDescendants().OfType<Button>().First(mark => mark.Name == "keyboardToggle");

      Assert.That(plate.IsEffectivelyVisible, Is.False,
          "the head band carries no search while nobody has asked for one");
      Assert.That(keys.IsEffectivelyVisible, Is.False, "and no keyboard button beside it either");

      // The door the viewer walked up to: the first plate of the rail's commands.
      (names.ContainerFromIndex(0) as Control)?.Focus();
      SendKey(names, Key.Left);
      WaitUntil("the rail to stand open", delegate { return wall.MediaMenuVisible; });
      ListBox rail = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "mediaMenuList");
      WaitUntil("the rail to be walked", delegate { return rail.IsKeyboardFocusWithin; });
      Assert.That((rail.Items[0] as MediaBrowserMenuEntry)?.Label, Is.EqualTo("Search"),
          "the commands page begins with the search");

      SendKey(rail, Key.Enter);
      WaitUntil("the field to stand on the head band", delegate { return plate.IsEffectivelyVisible; });
      Assert.That(wall.ShowsSearch, Is.True, "the wall knows it is being looked through");
      Assert.That(wall.MediaMenuVisible, Is.False, "and the rail stepped out of the way of the field");
      WaitUntil("the caret to be handed to the field", delegate { return box.IsFocused; });
      Assert.That(keys.IsEffectivelyVisible, Is.True,
          "and the one keyboard button to the keys on the screen came up with the field");

      // The plate's edges nod, exactly as the wall's do, and no letter is lost by them.
      box.Text = "Actor";
      box.CaretIndex = 0;
      SendKey(box, Key.Left);
      Assert.That(view.LastNodX, Is.LessThan(0.0), "the arrow before the first letter leans the plate left");
      Assert.That(box.Text, Is.EqualTo("Actor"), "and the nod does not eat the word");
      box.CaretIndex = box.Text.Length;
      SendKey(box, Key.Up);
      Assert.That(view.LastNodY, Is.LessThan(0.0), "and the arrow above the plate leans it up");
      Assert.That(box.IsFocused, Is.True, "while the caret stays on the field");

      // Off the end of the words the caret steps onto the keyboard button, and back the same way.
      SendKey(box, Key.Right);
      Assert.That(keys.IsFocused, Is.True, "the mark beside the plate is a place the caret can stand on");
      SendKey(keys, Key.Left);
      Assert.That(box.IsFocused, Is.True, "and the arrow back takes it into the field the mark belongs to");

      // Down out of the field is down onto the wall, and it leaves the search standing.
      SendKey(box, Key.Down);
      WaitUntil("the caret to stand inside the wall", delegate { return names.IsKeyboardFocusWithin; });
      Assert.That(wall.ShowsSearch, Is.True, "pointing down at the matches is not asking for them to go away");

      // While the field stands, the arrow off the HEAD OF THE WALL steps into it - the shelf's law, whose other
      // half (the head keeps its nod while no field waits above it) is proved by the case above it.
      names.SelectedIndex = 0;
      (names.ContainerFromIndex(0) as Control)?.Focus();
      Pump();
      SendKey(names, Key.Up);
      WaitUntil("the caret to be back in the field", delegate { return box.IsFocused; });
      Assert.That(wall.ShowsSearch, Is.True, "walking into the field is not opening a search nobody asked for");

      // And one press of back is the whole way out: the field, its words and the question they made, with the
      // caret coming back to a tile.
      SendKey(box, Key.Escape);
      WaitUntil("the field to be gone", delegate { return !plate.IsEffectivelyVisible; });
      WaitUntil("the whole wall to stand again", delegate { return wall.Rows.Count > 30; });
      WaitUntil("and the caret to stand on a name again", delegate { return names.IsKeyboardFocusWithin; });
      Assert.That(wall.ShowsSearch, Is.False, "the field and its words left together");
      Assert.That(server.LastWall, Does.Not.Contain("search="),
          "and the wall was asked for again without the words standing on it");

      window.Close();
    }

    /// <summary>
    /// The field and its keyboard button are one plate worn by two screens, so the wall has to wear the SHELF's
    /// rules for them - the sizes, the glass, and the answers to the toolkit's own states.  A screen that carries
    /// the classes but not the rules that go with them wears the toolkit's defaults and looks like a field someone
    /// designed twice, which is the mistake this case stands watch over.
    /// </summary>
    [AvaloniaTest]
    public void TheWallsFieldAndKeyboardButtonWearTheShelfsOwnPlate()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      ShellWindow window = new ShellWindow(new PeopleView(wall, null, null));
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      wall.OpenSearch();
      TextBox box = window.GetVisualDescendants().OfType<TextBox>().First(field => field.Name == "searchBox");
      Border plaque = window.GetVisualDescendants().OfType<Border>().First(field => field.Name == "searchField");
      Button keys = window.GetVisualDescendants().OfType<Button>().First(mark => mark.Name == "keyboardToggle");
      WaitUntil("the row to be laid", delegate { return plaque.Bounds.Width > 0; });

      // The plate GROWS into the head band, so its numbers are only read once it has come to rest: a frame caught
      // mid-grow reports the height of the moment it was caught in rather than the height the plate was cut to -
      // which is why the render probe puts the transitions away before it reads a plate.
      LetThePlateComeToRest(plaque, keys);

      // The numbers below are the shelf's: the plaque is the details row's plate asked to hold a question, so it
      // is forty-eight less five and six of corner, and the keyboard button is the same plate fifty-eight wide.
      Assert.That(plaque.Bounds.Height, Is.EqualTo(43),
          "the wall's plaque is as tall as the shelf's, five under the details row's plaques");
      Assert.That(plaque.CornerRadius.TopLeft, Is.EqualTo(6), "and it wears the same corner as that plate");
      Assert.That(plaque.Background, Is.InstanceOf<ISolidColorBrush>(), "one ground, laid by one hand");
      Assert.That(((ISolidColorBrush)plaque.Background!).Color, Is.EqualTo(Color.Parse("#33FFFFFF")),
          "the wall's field is the shelf's third-of-white glass, not the toolkit's white slab");
      Assert.That(keys.Bounds.Height, Is.EqualTo(43), "the keyboard button stands as tall as the plate beside it");
      Assert.That(keys.Bounds.Width, Is.EqualTo(58), "and as wide as the shelf wears it");

      // The toolkit dresses a text box for its own face: a white plate, an edge, and a word colour that on a light
      // theme is near black.  Those three are written INSIDE the box's template, so they have to be answered there
      // - and a screen that never got the answer shows a white slab in the middle of a dark wall the moment the
      // caret stands in the field.
      box.Focus();
      Pump();
      Border plate = box.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_BorderElement");
      Assert.That(((ISolidColorBrush)plate.Background!).Color.A, Is.EqualTo(0),
          "the field's own ground stays away while the caret stands in it");
      Assert.That(plate.BorderThickness.Top, Is.EqualTo(0),
          "and no second box is drawn round the plate the wall is wearing");

      window.Close();
    }

    /// <summary>
    /// The wall follows the field while it is being written in: a hand that rests on the last letter is asking a
    /// question, Enter puts the caret onto a result, and the mark at the field's hand takes the words away and
    /// leaves the field standing under the caret - the shelf's three answers to a hand on a keyboard, none of which
    /// this wall had.
    /// </summary>
    [AvaloniaTest]
    public void TheWallAnswersToTheWordsInTheFieldWhileTheyAreBeingTyped()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      TextBox box = window.GetVisualDescendants().OfType<TextBox>().First(field => field.Name == "searchBox");

      wall.OpenSearch();
      WaitUntil("the caret to be handed to the field", delegate { return box.IsFocused; });

      // Letters with the rests a hand makes between them, and no Enter pressed anywhere: the wall answers once the
      // hand has rested, which is what "live" means to a viewer - and answers once, not once per letter.  The words
      // are one letter's dozen names here, because the cut has to be seen to be a cut.
      wall.SearchText = "A.";
      wall.SearchText = "A. Actor 00";
      WaitUntil("the wall to be cut to the words", delegate { return wall.Rows.Count > 0 && wall.Rows.Count < 20; });
      Assert.That(server.LastWall, Does.Contain("search=A.").And.Contain("Actor"),
          "the whole word travelled to the server, spaces and all: " + server.LastWall);
      Assert.That(wall.Rows.OfType<PersonTile>().All(tile => tile.Name.Contains("A. Actor 00")), Is.True,
          "and every name the wall kept is a name the words reach");

      // Enter asks at once and puts the caret onto a result, not merely into the list: a caret that gets no further
      // than the list wears no frame.
      box.Focus();
      SendKey(box, Key.Enter);
      WaitUntil("the caret to stand on a name", delegate
      {
        return names.SelectedIndex == 0 && (names.ContainerFromIndex(0) as Control)?.IsFocused == true;
      });

      // The mark at the field's hand is the shelf's own CloseSearchCommand: it takes the search away whole, so the
      // field goes with its words and the wall stands full again.  (Emptying the box while it stands is clearing,
      // which is what the next case asks of it.)
      Button mark = window.GetVisualDescendants().OfType<Button>().First(button => button.Name == "clearSearchMark");
      Assert.That(mark.Command.CanExecute(null), Is.True, "the mark that takes the search away can be pressed");
      mark.Command.Execute(null);
      WaitUntil("the whole wall to stand again", delegate { return wall.Rows.Count > 30; });
      Assert.That(box.IsEffectivelyVisible, Is.False, "the mark takes the field off the screen with its words");
      Assert.That(wall.ShowsSearch, Is.False, "and the search is gone, so nothing stands in the head band");
      Assert.That(wall.SearchText, Is.Empty, "the box is empty too");

      window.Close();
    }

    /// <summary>
    /// A wall that answers a word leaves the CARET where the hand is standing: in the field.  This is the shelf's
    /// <c>TheWallAnswersUnderATypingHandAndLeavesTheCaretWhereItIs</c> mirrored onto the wall of names, and the wall
    /// failed it: a re-read hands the caret back to the name the wall still holds, the screen answered that answer
    /// by standing the caret on their tile, and one letter typed walked the viewer out of the field they were still
    /// spelling in (viewer note 2026-11-12: "when a letter is typed the focus must not jump into the list").
    /// The shelf never does this, because every one of its caret askers is asked through
    /// <c>NeedsCaretOnTheShelf</c>, which answers "no" while the field owns the caret.
    /// </summary>
    [AvaloniaTest]
    public void TheWallsAnswerUnderATypingHandLeavesTheCaretInTheField()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      TextBox box = window.GetVisualDescendants().OfType<TextBox>().First(field => field.Name == "searchBox");

      // The Find key is the door a hand holding a keyboard walks through.  The word typed next still contains the
      // name the wall stands on, which is the case that moved the caret: the wall hands that name back to the
      // screen after its answer, and the screen used to answer by fetching a tile.
      (names.ContainerFromIndex(0) as Control)?.Focus();
      SendKey(names, Key.F);
      WaitUntil("the caret in the field", delegate { return box.IsFocused; });
      Assert.That(wall.Selected, Is.Not.Null, "the wall the word is cut out of stands on a name");
      string stood_on = wall.Selected!.Name;

      box.Text = "A. Actor 00";
      WaitUntil("the wall to be cut to the word", delegate { return wall.Rows.Count > 0 && wall.Rows.Count < 20; });

      // The rounds a real answer needs: the layout pass that hands the recycled shells out, the asks posted behind
      // it at the settled-layout priority, and the rest a live search keeps.  The jump was never in the answer
      // itself, it was in what the screen did one round after it, so the caret is watched over the whole of that -
      // which is how the shelf's own case measures it.
      DateTimeOffset until = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(700);
      while (DateTimeOffset.UtcNow < until)
        Pump();

      Assert.That(box.IsFocused, Is.True,
          "the wall answered the word without taking the caret out of the field it was typed in");
      Assert.That(window.FocusManager.GetFocusedElement(), Is.SameAs(box),
          "and nothing else on the screen answered it either - not the list, not a tile");
      Assert.That(wall.Selected?.Name, Is.EqualTo(stood_on),
          "the name the column speaks of is still the name the wall was stood on");
      Assert.That(server.LastWall, Does.Contain("search=A."), "and the word did reach the wall");

      // A second word, because a viewer does not type one letter and stop: the claim on the caret has to outlast
      // every answer the field goes on asking for.
      box.Text = "A. Actor 001";
      WaitUntil("the wall to be cut again", delegate
      {
        return wall.Rows.OfType<PersonTile>().Any()
            && wall.Rows.OfType<PersonTile>().All(tile => tile.Name.Contains("A. Actor 001"));
      });
      until = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(400);
      while (DateTimeOffset.UtcNow < until)
        Pump();

      Assert.That(window.FocusManager.GetFocusedElement(), Is.SameAs(box),
          "the hand that is still typing is still holding the field after the second word too");

      // Down is the arrow that leaves the field, and it is the only key that ends the field's claim on the caret.
      SendKey(box, Key.Down);
      WaitUntil("the caret to stand on a name", delegate
      {
        return (names.ContainerFromIndex(names.SelectedIndex) as Control)?.IsKeyboardFocusWithin == true;
      });
      Assert.That(wall.ShowsSearch, Is.True, "walking down onto the matches leaves the search standing");

      window.Close();
    }

    /// <summary>
    /// The screen's switches must not eat the letters of a name.  Every key law on this screen is a TUNNEL handler,
    /// so a branch answered while the caret stands in the field takes the key away from the box before the box ever
    /// sees it, and a letter the viewer typed is a letter they did not get.  That is why the shelf asks
    /// <c>!searchBox.IsFocused</c> at Find, Details, Watched, View and Media alike and returns early from its rail
    /// handler.  The letters at stake are real ones: the details switch is D, and "De Niro" begins with it.
    /// </summary>
    [AvaloniaTest]
    public void TheScreensSwitchesDoNotEatTheLettersOfANameBeingTyped()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      TextBox box = window.GetVisualDescendants().OfType<TextBox>().First(field => field.Name == "searchBox");

      wall.OpenSearch();
      WaitUntil("the caret to be handed to the field", delegate { return box.IsFocused; });
      box.Text = "De";
      box.CaretIndex = 2;
      Pump();

      bool column_stood = wall.ShowsSideDetails;

      KeyEventArgs letter = SendKeyAndGet(box, Key.D);
      Assert.That(letter.Handled, Is.False,
          "the letter of the details switch stays a letter while a hand is typing a name");
      Assert.That(wall.ShowsSideDetails, Is.EqualTo(column_stood),
          "and the column beside the wall did not move on a key that belonged to the field");

      KeyEventArgs find = SendKeyAndGet(box, Key.F);
      Assert.That(find.Handled, Is.False, "the find key does not answer a key the field is wearing");

      // The way out to the rail.  With the caret in the middle of a word, the arrow belongs to the word: the shelf
      // returns early from its rail handler while the box holds the caret, and the box answers the arrow by walking
      // its own caret back - which is what "the key stayed with the field" looks like from outside.
      box.CaretIndex = 2;
      Pump();
      SendKey(box, Key.Left);
      Assert.That(box.CaretIndex, Is.EqualTo(1),
          "the arrow through a word walked the caret inside the word instead of leaving the field");
      Assert.That(wall.MediaMenuVisible, Is.False, "and no rail stood open over a field a hand is typing in");

      // The Menu key and a right press, which reach the screen as one event.  The shelf hears its own only on the
      // shelf's list and guards its info key with the field too, because a key pressed over a word a hand is
      // typing belongs to that word - here that means the box's own right-click plate, and never the rail.
      ContextRequestedEventArgs context = new ContextRequestedEventArgs
      {
        RoutedEvent = InputElement.ContextRequestedEvent,
        Source = box
      };
      box.RaiseEvent(context);
      Pump();
      Assert.That(wall.MediaMenuVisible, Is.False,
          "a menu key pressed over a word did not stand a rail over the hand that pressed it");
      Assert.That(box.IsFocused, Is.True, "and the field is still holding the caret it was asked about");

      // The field's own keys still answer, so this guard is not the field going deaf: the plate nods at the arrow
      // before its first letter, exactly as it does when the caret arrived at the field by another door.
      box.CaretIndex = 0;
      Pump();
      SendKey(box, Key.Left);
      Assert.That(view.LastNodX, Is.LessThan(0.0),
          "the plate's own edge law is still worn while the field holds the caret");

      // The door itself is not gone: the same key one control away - a name wearing the caret - still opens the rail.
      (names.ContainerFromIndex(0) as Control)?.Focus();
      Pump();
      ContextRequestedEventArgs on_the_wall = new ContextRequestedEventArgs
      {
        RoutedEvent = InputElement.ContextRequestedEvent,
        Source = names
      };
      names.RaiseEvent(on_the_wall);
      Pump();
      Assert.That(on_the_wall.Handled, Is.True, "the rail's door is answered as ever");
      Assert.That(wall.MediaMenuVisible, Is.True, "and the rail came up over the wall, as it always did");

      window.Close();
    }

    /// <summary>Raises a key at a control and hands the answer back, so a case can ask whether the screen took the key.</summary>
    private static KeyEventArgs SendKeyAndGet(Control source, Key key)
    {
      KeyEventArgs args = new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = source
      };
      source.RaiseEvent(args);
      Pump();
      return args;
    }

    /// <summary>
    /// The same law through the other door: with the keys on the screen the caret stands AMONG THE KEYS, not in the
    /// field, and the wall still has to answer a pressed letter without touching it - the hand pressing the next
    /// letter is standing there, and a caret set down on a name mid-word also puts OK on that name.  The shelf's
    /// <c>TheWallAnswersAKeyPressedLetterWithoutWalkingTheCaretOntoTheWall</c>, mirrored; the wall's claim on the
    /// caret therefore counts the keys as the field, exactly as <c>MediaBrowserView.TheKeysHoldTheCaret</c> does.
    /// </summary>
    [AvaloniaTest]
    public void AKeyPressedLetterIsAnsweredWithoutWalkingTheCaretOffTheKeys()
    {
      WallServer server = new WallServer(60);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      Button keys = window.GetVisualDescendants().OfType<Button>().First(mark => mark.Name == "keyboardToggle");
      TextBox box = window.GetVisualDescendants().OfType<TextBox>().First(field => field.Name == "searchBox");

      wall.OpenSearch();
      WaitUntil("the caret in the field", delegate { return box.IsFocused; });
      // A word already standing in the field, which is where a viewer who reached for the keys usually comes from.
      // The letters of this wall's names cannot narrow it - every name is "X. Actor 0NN" - so the narrowing letter
      // here is a figure, and the point of the case is not the word but the caret.
      box.Text = "A. Actor 0";
      box.CaretIndex = box.Text.Length;
      WaitUntil("the wall to be cut to the word", delegate
      {
        return wall.Rows.OfType<PersonTile>().Count() > 1 && wall.Rows.OfType<PersonTile>().Count() < 30;
      });

      SendKey(box, Key.Right);
      Assert.That(keys.IsFocused, Is.True, "the arrow off the end of the words steps onto the keyboard button");
      SendKey(keys, Key.Enter);
      WaitUntil("the keys to stand on the screen", delegate
      {
        return (window.FocusManager.GetFocusedElement() as Visual)?.GetVisualAncestors()
            .OfType<VirtualKeyboardView>().Any() == true;
      });

      // One key pressed on the keys, and the wall answers it by itself after the hand rests.
      VirtualKeyboardView thekeys = window.GetVisualDescendants().OfType<VirtualKeyboardView>().First();
      Button figure = thekeys.GetVisualDescendants().OfType<Button>()
          .First(tile => tile.IsEffectivelyVisible && tile.IsEnabled && (tile.Content as string) == "1");
      int names_before = wall.Rows.OfType<PersonTile>().Count();
      figure.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

      WaitUntil("the wall to answer the pressed figure", delegate
      {
        return wall.Rows.OfType<PersonTile>().Count() > 0
            && wall.Rows.OfType<PersonTile>().Count() < names_before;
      });

      // The rounds the answer needs behind it - the layout pass that hands the recycled shells out and every ask
      // posted at the settled-layout priority.  The jump was never in the answer; it was in what the screen did one
      // round later, so the caret is watched over the whole of it.
      DateTimeOffset until = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(700);
      while (DateTimeOffset.UtcNow < until)
        Pump();

      Assert.That(wall.Rows.OfType<PersonTile>().All(tile => tile.Name.Contains("A. Actor 01")), Is.True,
          "the wall is cut to the names the pressed figure reaches");
      object? stood = window.FocusManager.GetFocusedElement();
      Assert.That((stood as Visual)?.GetVisualAncestors().OfType<VirtualKeyboardView>().Any(), Is.True,
          "the caret is still among the keys, where the next letter is pressed: it went to "
          + (stood?.GetType().Name ?? "nothing"));

      window.Close();
    }

    /// <summary>
    /// The wall and a shelf of titles divide ONE room by ONE measure: the same window hands both screens the same
    /// number of points to the words at its right hand, and the wall of names begins where the shelf of titles
    /// begins.  This is the plan's last open question about the grid ("Personen-Raster"), and it is asked of the two
    /// screens standing side by side rather than of a number, because a number copied from one screen into a test
    /// is exactly how two screens drift apart.
    /// </summary>
    [AvaloniaTest]
    public void TheWallAndAShelfDivideOneRoomByOneMeasure()
    {
      WallServer server = new WallServer(60);
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "a laid-out screen needs the application to know what skin it wears");
      FrontendTheme.Apply(app!, FrontendTheme.DEFAULT_NAME, out _);

      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      ShellWindow wall_window = new ShellWindow(new PeopleView(wall, null, null));
      wall_window.Width = 1920;
      wall_window.Height = 1080;
      wall_window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 30; });
      ListBox names = wall_window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });

      MediaBrowserViewModel shelf = new MediaBrowserViewModel(
          new ServerApiClient(SERVER_URL, new HttpClient(server)),
          new HomeSection("Movies", new AspectViewModel(new MediaAspect(Guid.Empty, "MovieItem", false, false,
              null, Array.Empty<AspectAttribute>()))));
      ShellWindow shelf_window = new ShellWindow(new MediaBrowserView(shelf));
      shelf_window.Width = 1920;
      shelf_window.Height = 1080;
      shelf_window.Show();
      WaitUntil("the shelf to fill", delegate { return shelf.Titles.Count == 2; });

      Border wall_words = wall_window.GetVisualDescendants().OfType<Border>().First(b => b.Name == "detailsPanel");
      Border shelf_words = shelf_window.GetVisualDescendants().OfType<Border>().First(b => b.Name == "detailsPanel");
      ListBox titles = shelf_window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "titlesList");
      WaitUntil("the words to stand up in both screens", delegate
      {
        return wall_words.IsEffectivelyVisible && shelf_words.IsEffectivelyVisible;
      });
      // Both screens slide their room into place as they open, so the numbers below are read once that slide is over
      // rather than in its middle - the same reason the render probe puts the transitions away before a plate.
      LetTheRoomComeToRest();
      LetThePlateComeToRest(wall_words, shelf_words, names, titles);

      Assert.That(wall_words.Bounds.Width, Is.EqualTo(shelf_words.Bounds.Width).Within(1.0),
          "the column of words is the same breadth beside a wall of names as beside a wall of titles");
      Assert.That(wall_words.Bounds.X, Is.EqualTo(shelf_words.Bounds.X).Within(1.0),
          "and it stands at the same hand's breadth from the left edge in both screens");
      Assert.That(names.Bounds.X, Is.EqualTo(titles.Bounds.X).Within(1.0),
          "the wall begins where the shelf begins, so a walk from one screen to the other does not step sideways");
      Assert.That(names.Bounds.Width, Is.EqualTo(titles.Bounds.Width).Within(1.0),
          "and the wall has the same room to stand its names in as the shelf has for its covers");

      // With the words turned away the room is the wall's alone - the shelf's own answer when a viewer switches its
      // details off, and the reason the line at the foot takes over the telling then.
      wall.ShowDetails = false;
      shelf.ToggleDetails();
      WaitUntil("both screens to put the words away", delegate
      {
        return !wall_words.IsEffectivelyVisible && !shelf_words.IsEffectivelyVisible;
      });
      LetTheRoomComeToRest();

      Assert.That(wall_words.IsEffectivelyVisible, Is.False, "the wall's words went away with the switch");
      Assert.That(shelf_words.IsEffectivelyVisible, Is.False, "and the shelf's with theirs");
      Assert.That(names.Bounds.Width, Is.EqualTo(titles.Bounds.Width).Within(1.0),
          "both screens then give the whole room to their wall, and give it the same way");

      wall_window.Close();
      shelf_window.Close();
    }

    /// <summary>
    /// The column beside the wall for a person whose record holds no life: the name and the years the row already
    /// carries stay, everything the library never told the screen goes away WHOLE, and no empty heading, no dash
    /// and no word "null" is left standing in its place.  Measured against a server that answers the person's own
    /// record (<c>api/media/persons/{id}</c>), because that is the record the column is made of - and it is the
    /// ordinary case of this household, where the wall of 7543 film actors answers its first name with neither a
    /// life nor a year.
    /// </summary>
    [AvaloniaTest]
    public void AColumnBesideAPersonWithoutALifeSaysNameAndYearsAndNothingElse()
    {
      WallServer server = new WallServer(6);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      ShellWindow window = new ShellWindow(new PeopleView(wall, null, null));
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 3; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      ScrollViewer life_field = window.GetVisualDescendants().OfType<ScrollViewer>()
          .First(field => field.Name == "biographyScroller");

      // The caret on the first name, which is where a wall that opens stands it by itself.
      (names.ContainerFromIndex(0) as Control)?.Focus();
      Pump();
      PersonTile stood_on = wall.Rows.OfType<PersonTile>().First();
      WaitUntil("the column to name the stood-on person", delegate { return wall.DetailsName == stood_on.Name; });

      Assert.That(wall.DetailsYears, Does.Contain("1900"),
          "the years the row carries are on the column too");
      Assert.That(wall.DetailsBiography, Is.Empty, "a record with no biography leaves no words behind");
      Assert.That(wall.ShowsBiography, Is.False, "and the column does not announce that it has nothing to say");
      Assert.That(life_field.IsEffectivelyVisible, Is.False, "so the life's field is not on the screen at all");
      Assert.That(wall.DetailsOrigin, Is.Empty, "a place the library never told is empty rather than guessed");
      Assert.That(wall.ShowsOrigin, Is.False, "and its line is not standing there with nothing on it");

      Border column = window.GetVisualDescendants().OfType<Border>().First(b => b.Name == "detailsPanel");
      Assert.That(column.GetVisualDescendants().OfType<TextBlock>()
              .Any(word => (word.Text ?? "").IndexOf("null", StringComparison.OrdinalIgnoreCase) >= 0),
          Is.False, "and nowhere in the column did the word null reach the screen");

      // And the same column beside a person whose record DOES hold a life: the field comes up, the life stands in
      // it from its first word, and nothing else about the column changed.
      server.LivesHeld = -1;
      PersonTile second = wall.Rows.OfType<PersonTile>().Skip(1).First();
      wall.Selected = second;
      WaitUntil("the life to arrive", delegate { return wall.Selected == second && wall.ShowsBiography; });

      Assert.That(life_field.IsEffectivelyVisible, Is.True, "a life that exists is shown");
      Assert.That(window.GetVisualDescendants().OfType<TextBlock>()
              .Any(word => (word.Text ?? "").Contains("Buenos Aires", StringComparison.Ordinal)),
          Is.True, "and the place the record told is told on the column");

      window.Close();
    }

    /// <summary>
    /// The length of a wall's travel belongs to the shape it wears, not to the moment its scroller was first laid:
    /// the reference glides a grid four hundred points and a list three hundred (<c>MediaStyles.xaml</c>), and the
    /// shelf writes its transition again on every layout change.  Worn once for the life of the scroller, the wall
    /// kept gliding at the length it happened to be born with after a walk from squares into rows.
    /// </summary>
    [AvaloniaTest]
    public void TheWallTravelsAtTheLengthItsShapeIsWornWith()
    {
      WallServer server = new WallServer(300);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 120; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });
      ScrollViewer floor = names.GetVisualDescendants().OfType<ScrollViewer>().First();

      WaitUntil("the squares to wear the grid's glide",
          delegate { return TheWallGlide(floor) == TimeSpan.FromMilliseconds(400); });

      wall.Layout = PeopleViewModel.LAYOUT_LIST;
      WaitUntil("the wall to stand as rows", delegate { return names.Classes.Contains("nereus-shelf-rows"); });
      WaitUntil("the rows to be laid", delegate { return names.ContainerFromIndex(3) != null; });
      WaitUntil("the rows to wear the list's glide",
          delegate { return TheWallGlide(floor) == TimeSpan.FromMilliseconds(300); });

      wall.Layout = PeopleViewModel.LAYOUT_GRID;
      WaitUntil("the wall to stand as squares again", delegate { return names.Classes.Contains("nereus-shelf-tiles"); });
      WaitUntil("the squares to be laid again", delegate { return names.ContainerFromIndex(3) != null; });
      WaitUntil("and the squares to be asked for their glide back",
          delegate { return TheWallGlide(floor) == TimeSpan.FromMilliseconds(400); });

      window.Close();
    }

    /// <summary>The length the wall's scroller is currently worn with, or nothing while it wears no glide.</summary>
    private static TimeSpan? TheWallGlide(ScrollViewer floor)
    {
      if (floor.Transitions == null)
        return null;

      foreach (ITransition transition in floor.Transitions)
      {
        if (transition is VectorTransition glide && glide.Property == ScrollViewer.OffsetProperty)
          return glide.Duration;
      }

      return null;
    }

    /// <summary>
    /// The faces of a wall come the way a shelf's covers come: the drawn tile is offered its picture, nearest the
    /// caret first, and only for the tiles the screen really laid out.  Asked instead by one walk down the whole
    /// wall, the faces arrive one per round trip and the viewer watches them pop into tiles that were standing for
    /// a second already - and a wall of seven thousand names never fills at all.
    /// </summary>
    [AvaloniaTest]
    public void TheFacesOfTheWallArriveWhereTheCaretStandsRatherThanFromItsHead()
    {
      WallServer server = new WallServer(300);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      PeopleView view = new PeopleView(wall, null, null);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      WaitUntil("the wall to fill", delegate { return wall.Rows.Count > 120; });

      ListBox names = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "peopleList");
      WaitUntil("the names to be laid across the wall", delegate { return TheWallColumns(names) > 1; });

      // The caret goes far down the wall - the walk a viewer does in the first minute of looking at a wall of
      // actors.  The names around it are the names the screen has to be able to answer.
      PersonTile far = wall.Rows[200] as PersonTile;
      Assert.That(far, Is.Not.Null, "the wall holds the name the caret is walked to");
      wall.Selected = far;
      WaitUntil("the face of the stood-on name", delegate { return far.Portrait != null; });

      string stood_on = far.Id.ToString("D");
      Assert.That(server.PortraitAsked, Does.Contain(stood_on),
          "a name the viewer stands on is asked for its face, and its face is what filled the tile: "
          + string.Join(" | ", server.PortraitAsked));

      // And the asking stopped there: the far end of a three-hundred-name wall that nobody has walked to is not on
      // the wire, because every face asked for a name out of sight is a place in the queue taken from one in it.
      string out_of_sight = (wall.Rows[wall.Rows.Count - 3] as PersonTile)!.Id.ToString("D");
      Assert.That(server.PortraitAsked, Does.Not.Contain(out_of_sight),
          "the wall asked for the names it can see and not for its whole length");

      // A second pass over the same sighted names asks nothing: the tile is stamped with the holder its face was
      // asked of, which is what keeps a wall that is re-laid - or re-read, which happens on every letter - from
      // asking the server the same hundred questions again.
      int asked_after_the_walk = server.PortraitAsks;
      for (int settling = 0; settling < 40; settling++)
        Pump();
      Assert.That(server.PortraitAsks, Is.EqualTo(asked_after_the_walk),
          "names already asked for are not asked again: " + string.Join(" | ", server.PortraitAsked));

      window.Close();
    }

    /// <summary>
    /// A wall re-reads itself far more often than a viewer notices - the screen reads it once when it opens, and every
    /// letter, word, order and grouping asks it again - while the details column speaks of the name the caret stands
    /// on.  A caret thrown away with the rows was seen to blank that column over a person who was standing on the
    /// screen in front of the viewer, so the wall is asked to keep its names and its caret through a reading.
    /// </summary>
    [Test]
    public async Task AWallReadAgainKeepsTalkingAboutTheNameItStoodOn()
    {
      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      await wall.ReadAsync(true);

      PersonTile stood = wall.Rows.OfType<PersonTile>().First();
      wall.Selected = stood;

      await wall.PickGroupAsync("letter");
      Assert.That(wall.Selected, Is.Not.Null, "a wall stood under its letters still stands on a name");
      Assert.That(wall.Selected!.Id, Is.EqualTo(stood.Id), "the same person it stood on before");
      Assert.That(wall.Selected, Is.Not.SameAs(stood), "on the tile that arrived with this reading, which is the one "
          + "the screen draws");
      Assert.That(wall.DetailsName, Is.EqualTo(stood.Name), "and the column kept speaking of them");

      // The other half: a question that leaves the person out throws the caret away, because a column going on about
      // a person who is not on the wall is a screen lying about what is stood on.
      await wall.PickLetterAsync("C");
      Assert.That(wall.Selected, Is.Null, "the letter left the name out of the wall");
      Assert.That(wall.DetailsName, Is.Empty, "so the column has nothing to say");
    }

    /// <summary>
    /// The room behind a wall of names: whose picture it wears, when it stops wearing one, and what a walk back over
    /// a name it has answered before costs.  The law is the shelf's own (<c>FanartBackdrop</c>), so what is proved
    /// here is only that a wall of people asks it for a person.
    /// </summary>
    [AvaloniaTest]
    public void TheRoomBehindTheWallWearsThePictureOfTheNameTheCaretStandsOn()
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "the room's picture is a skin setting, so this needs a worn skin");
      FrontendTheme.Apply(app!, "nereus", out _);

      WallServer server = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, server);
      try
      {
        Assert.That(wall.FanartShown, Is.True, "Nereus wears a room-picture by default, here as on a shelf");

        // Read the way a screen reads: fired off, and waited for by pumping, so that everything after it - the
        // standing, the walking and the wearing - happens on the thread that draws.
        _ = wall.ReadAsync(true);
        WaitUntil("the wall to stand its names", delegate { return wall.Rows.Count == 9; });

        PersonTile with = wall.Rows.OfType<PersonTile>().First();
        PersonTile without = wall.Rows.OfType<PersonTile>().Skip(1).First();
        server.WithFanArt = with.Id;

        wall.Selected = with;
        WaitUntil("the room to wear the stood name's picture",
            delegate { return wall.BackgroundArtwork != null; });
        Assert.That(server.LastFanart, Does.Contain("/fanart/FanArt"),
            "the room's picture is a name's fanart, never the little face the row wears");
        Assert.That(server.LastFanart, Does.Match(@"width=\d{3,4}"),
            "and it is asked for whole, at the width the screen is painted at, not at a tile's");
        Assert.That(server.FanartAsks, Is.EqualTo(1),
            "asked for once, and only once the caret had stood still: " + string.Join(" | ", server.FanartAsked));

        // Walking onto a name the library has no fanart for takes the room's mood away again, rather than leaving a
        // picture of somebody the caret left standing behind the wall.
        wall.Selected = without;
        WaitUntil("the room to go plain with the name that has none",
            delegate { return wall.BackgroundArtwork == null; });

        // And the walk back costs no second question: the same memory the rows' faces are kept in remembers a picture
        // and its absence alike, so the room is wearing it again before a viewer noticed the walk.  (Two questions
        // had gone out by now - the stood name's picture and the plain name's absence - and a walk back over either
        // adds none.)
        int asked_of_both = server.FanartAsks;
        wall.Selected = with;
        WaitUntil("the remembered picture to be worn again", delegate { return wall.BackgroundArtwork != null; });
        Assert.That(server.FanartAsks, Is.EqualTo(asked_of_both),
            "a name the machine answered before asks nothing again: " + string.Join(" | ", server.FanartAsked));

        // A caret thrown off the wall - the viewer leaving the screen's subject - leaves the plain ground.
        wall.Selected = null;
        Assert.That(wall.BackgroundArtwork, Is.Null, "nobody stood on is a room with no picture");

        // And the column is turned away before the test ends, so that no errand of this wall is still asking a
        // server while the next screen is laid out on the same thread.
        wall.ShowDetails = false;
      }
      finally
      {
        WearThePlainFaceAgain();
      }
    }

    /// <summary>How many names the wall lays across, which only the panel that lays them knows.</summary>
    private static int TheWallColumns(ListBox names)
    {
      return names.GetVisualDescendants().OfType<VirtualizingTilePanel>().FirstOrDefault()?.Columns ?? 1;
    }

    /// <summary>
    /// Pumps until the controls handed in stop changing - in size AND in ground.  A plate that grows into the screen
    /// is a plate whose bounds are a story rather than a fact for a few frames, and its glass is being laid over that
    /// same animation, so a case that asks either question in that window measures the animation instead of the rule:
    /// one such case read #38 of white where the shelf wears #33.  Rest is held for three rounds of pumping, so a
    /// value that only stands for one frame of a slide does not count as an answer.
    /// </summary>
    private static void LetThePlateComeToRest(params Control[] laid)
    {
      DateTimeOffset until = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
      (Rect Frame, Color Glass)[] before = WhereTheyStand(laid);
      int steady = 0;

      while (DateTimeOffset.UtcNow < until)
      {
        Pump();
        (Rect Frame, Color Glass)[] now = WhereTheyStand(laid);
        if (now.SequenceEqual(before))
        {
          steady++;
          if (steady >= 3)
            return;
        }
        else
        {
          steady = 0;
        }

        before = now;
      }
    }

    /// <summary>
    /// Pumps a screen through one animation's length.  The chrome slides and grows on curves of roundly four hundred
    /// milliseconds, so a case that reads numbers inside that time is reading the curve; this is the pause before the
    /// reading, and <see cref="LetThePlateComeToRest"/> is the proof afterwards that nothing is still moving.
    /// </summary>
    private static void LetTheRoomComeToRest()
    {
      DateTimeOffset settle = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(900);
      while (DateTimeOffset.UtcNow < settle)
      {
        Pump();
      }
    }

    /// <summary>Where each control stands and what colour it is wearing at this moment.</summary>
    private static (Rect Frame, Color Glass)[] WhereTheyStand(Control[] laid)
    {
      return laid.Select(control => (control.Bounds, TheGlass(control))).ToArray();
    }

    /// <summary>The ground a control lays under itself, or nothing where it lays none.</summary>
    private static Color TheGlass(Control control)
    {
      return control switch
      {
        Border plate when plate.Background is ISolidColorBrush laid => laid.Color,
        Button key when key.Background is ISolidColorBrush worn => worn.Color,
        _ => Colors.Transparent
      };
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

    /// <summary>
    /// A server that answers <c>api/media/persons</c> the way the product's own controller does, over a wall of
    /// names it makes up letter by letter, and that remembers every question it was asked.
    /// </summary>
    private sealed class WallServer : HttpMessageHandler
    {
      private readonly List<string> _names = new List<string>();
      private readonly Dictionary<string, int> _born = new Dictionary<string, int>();

      internal readonly List<string> Asked = new List<string>();
      internal int WallAsks { get; private set; }
      internal int TitlesAsks { get; private set; }

      /// <summary>The last question about titles, which is what a shelf under a person is asked about.</summary>
      internal string LastTitles { get; private set; } = "";

      /// <summary>
      /// The last question about the wall of names itself.  A face that walks in after the wall was read asks about a
      /// picture too, so a test that wants to know how the wall was asked must not look at just any question.
      /// </summary>
      internal string LastWall { get; private set; } = "";

      /// <summary>
      /// The one name this wall has a room-picture for.  <see cref="Guid.Empty"/> is a household that imported no
      /// fanart for people at all, which is what most of this one looks like.
      /// </summary>
      internal Guid WithFanArt;

      /// <summary>How often a picture of the room was asked for, which a walk back over a name must not raise.</summary>
      internal int FanartAsks { get; private set; }

      /// <summary>The last question about a room-picture, door and width and all.</summary>
      internal string LastFanart { get; private set; } = "";

      /// <summary>Every question about a room-picture, so that a wall asking twice is not left to guesswork.</summary>
      internal readonly List<string> FanartAsked = new List<string>();

      /// <summary>
      /// How many names this household holds a face for, or -1 for a library that answers every name.  Zero is the
      /// ordinary state of this one's film actors, of which not a single name has a portrait filed.
      /// </summary>
      internal int FacesHeld = -1;

      /// <summary>
      /// How many names this household was ever told a life for, or -1 for a library that answers every name.  Zero
      /// is the ordinary state of this one's film actors, whose records hold neither a life nor, more often than
      /// not, a year at all - so a column that cannot survive a name without a biography is broken on the very first
      /// name a viewer stands on.
      /// </summary>
      internal int LivesHeld = 0;

      /// <summary>
      /// How many names this household was ever told a birth year for, or -1 for a library that answers every name.
      /// The films are the other case - two years in 7543 names - and that is the wall a grouping by decade has to
      /// survive, so the tests ask this fake to be as sparse as the library is.
      /// </summary>
      internal int YearsHeld = -1;

      /// <summary>Whether one name's record holds a year at all.</summary>
      private bool HoldsYear(string name)
      {
        return YearsHeld < 0 || _names.Take(YearsHeld).Contains(name);
      }

      /// <summary>
      /// The year a name was born, or the front of an ascending reading for a name the library never gave one - the
      /// place the database actually puts them, measured rather than assumed.
      /// </summary>
      private int YearOf(string name)
      {
        return HoldsYear(name) ? _born[name] : int.MinValue;
      }

      /// <summary>How often a face was asked for, which is what a wall must not spend on names out of sight.</summary>
      internal int PortraitAsks
      {
        get;
        private set;
      }

      /// <summary>Every question about a face, in the order it went out, as the person's id.</summary>
      internal readonly List<string> PortraitAsked = new List<string>();

      /// <param name="count">
      /// How many names this wall holds, spread over the letters A, B and C - three hundred and three of them is a
      /// wall big enough to have a page behind its opening one.
      /// </param>
      internal WallServer(int count)
      {
        string[] letters = { "A", "B", "C" };
        for (int at = 0; at < count; at++)
        {
          string letter = letters[at % letters.Length];
          string name = letter + ". Actor " + (at / letters.Length).ToString("D3", CultureInfo.InvariantCulture);
          _names.Add(name);
          _born[name] = 1900 + at;
        }
        _names.Sort(StringComparer.Ordinal);
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        string query = request.RequestUri.Query.TrimStart('?');
        Asked.Add(query);

        if (path.Contains("/persons/", StringComparison.OrdinalIgnoreCase))
        {
          // GET api/media/persons/{id}: what the library knows about ONE name beyond its row.  How many names were
          // ever told a life is the test's hand on this library's ordinary case - see LivesHeld.
          string asked_id = path.Substring(path.LastIndexOf('/') + 1);
          string? named = null;
          foreach (string name in _names)
          {
            if (string.Equals(IdOf(name), asked_id, StringComparison.OrdinalIgnoreCase))
            {
              named = name;
              break;
            }
          }

          string record;
          if (named == null)
          {
            record = "{\"id\":\"" + asked_id + "\",\"error\":\"no person by that id\"}";
          }
          else
          {
            bool told_a_life = LivesHeld < 0 || _names.Take(LivesHeld).Contains(named);
            record = "{\"id\":\"" + asked_id + "\",\"person\":{\"id\":\"" + asked_id +
                "\",\"name\":\"" + named + "\",\"occupation\":\"ACTOR\"" +
                ",\"bornDate\":" + (HoldsYear(named) ? "\"" + _born[named] + "-01-01T00:00:00\"" : "null") +
                ",\"deathDate\":null" +
                ",\"origin\":" + (told_a_life ? "\"Buenos Aires\"" : "null") +
                ",\"biography\":" + (told_a_life
                    ? "\"She was discovered in a market in Buenos Aires and never left the stage.\""
                    : "null") + "}}";
          }

          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent(record, Encoding.UTF8, "application/json")
          });
        }

        if (path.EndsWith("/fanart/FanArt", StringComparison.OrdinalIgnoreCase))
        {
          // The room's picture.  A real PNG rather than stray bytes, because a backdrop is decoded, blurred and
          // warmed before it is worn, and only bytes that decode can answer a remembered walk-back.
          FanartAsks++;
          LastFanart = path + "?" + query;
          FanartAsked.Add(LastFanart);
          bool wears = WithFanArt != Guid.Empty
              && path.Contains(WithFanArt.ToString("D"), StringComparison.OrdinalIgnoreCase);
          return Task.FromResult(new HttpResponseMessage(wears ? HttpStatusCode.OK : HttpStatusCode.NotFound)
          {
            RequestMessage = request,
            Content = new ByteArrayContent(wears ? PNG_ROOM : Array.Empty<byte>())
          });
        }

        if (path.EndsWith("/fanart/Thumbnail", StringComparison.OrdinalIgnoreCase))
        {
          // A person's face, which the import files as fanart of the kind Thumbnail under the person's own id.
          // Counted and answered by name, because the question a picture test asks is never "did a picture arrive"
          // but "which names did the wall spend its four places on the wire for".
          PortraitAsks++;
          string[] pieces = path.Split('/');
          string asked_for = pieces[pieces.Length - 3];
          PortraitAsked.Add(asked_for);
          bool holds = FacesHeld < 0
              || _names.Take(FacesHeld).Any(name =>
                  string.Equals(IdOf(name), asked_for, StringComparison.OrdinalIgnoreCase));
          return Task.FromResult(new HttpResponseMessage(holds ? HttpStatusCode.OK : HttpStatusCode.NotFound)
          {
            RequestMessage = request,
            Content = new ByteArrayContent(holds ? PNG_FACE : Array.Empty<byte>())
          });
        }

        if (path.EndsWith("/thumbnail", StringComparison.Ordinal)
            || path.EndsWith("/fanart", StringComparison.Ordinal))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        string body;
        if (path.EndsWith("/persons", StringComparison.Ordinal))
        {
          WallAsks++;
          LastWall = query;
          body = People(query);
        }
        else if (path.EndsWith("/groups", StringComparison.Ordinal))
        {
          body = "{\"aspect\":\"MovieItem\",\"by\":\"genre\",\"groups\":[{\"value\":\"Action\",\"count\":2}]}";
        }
        else if (path.EndsWith("/titles", StringComparison.Ordinal))
        {
          TitlesAsks++;
          LastTitles = query;
          body = Titles(query);
        }
        else
        {
          body = "{}";
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }

      /// <summary>A sixty-four by thirty-six PNG: small, and decodable by everything a backdrop runs through.</summary>
      private static readonly byte[] PNG_ROOM = Convert.FromBase64String(
          "iVBORw0KGgoAAAANSUhEUgAAAEAAAAAkCAYAAAA5DDySAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQU"
          + "AAAAJcEhZcwAADsMAAA7DAcdvqGQAAABzSURBVGhD7ZChDYBQEMX+sKzEmMwAvgZx5pL2JTUVT/Rc9/OaORQ2CkBhowAUNgpAYeM3"
          + "wHT820YBKMh0/NtGASjIdPzbRgEoyHT820YBKGwUgMJGAShsFIDCRgEobBSAwkYBKGwUgMJGAShsFIDChj7ABzEumgdmt8OaAAAAAElFTkSuQmCC");

      /// <summary>
      /// A face is a picture like every other picture, so the room's own bytes answer for it here; only the door it
      /// is asked through differs.
      /// </summary>
      private static readonly byte[] PNG_FACE = PNG_ROOM;

      /// <summary>One page of the wall, cut and ordered the way the question asked.</summary>
      private string People(string query)
      {
        Dictionary<string, string> asked = Parse(query);
        string letter = Of(asked, "letter");
        string finding = Of(asked, "search");
        string sort = Of(asked, "sort");
        bool descending = string.Equals(Of(asked, "order"), "desc", StringComparison.OrdinalIgnoreCase);
        int offset = Read(asked, "offset", 0);
        int limit = Read(asked, "limit", 120);

        List<string> standing = new List<string>(_names);
        if (letter.Length > 0)
          standing.RemoveAll(name => !name.StartsWith(letter, StringComparison.OrdinalIgnoreCase));
        if (finding.Length > 0)
          standing.RemoveAll(name => name.IndexOf(finding, StringComparison.OrdinalIgnoreCase) < 0);

        if (string.Equals(sort, "born", StringComparison.OrdinalIgnoreCase))
        {
          // Measured on the living server: a name nobody ever gave a year stands at the FRONT of an ascending
          // reading and at the back of a descending one, which is exactly where a wall plated by decade needs them -
          // together, under one plate, rather than scattered between the decades they do not belong to.
          standing.Sort((one, other) => YearOf(one).CompareTo(YearOf(other)));
        }
        if (descending)
          standing.Reverse();

        int total = standing.Count;
        List<string> page = standing.GetRange(Math.Min(offset, total),
            Math.Max(0, Math.Min(limit, total - offset)));

        StringBuilder rows = new StringBuilder();
        foreach (string name in page)
        {
          if (rows.Length > 0)
            rows.Append(',');
          rows.Append("{\"id\":\"").Append(IdOf(name)).Append("\",\"name\":\"").Append(name)
              .Append("\",\"occupation\":\"ACTOR\",\"bornDate\":")
              .Append(HoldsYear(name) ? "\"" + _born[name] + "-01-01T00:00:00\"" : "null")
              .Append(",\"deathDate\":null}");
        }

        return "{\"on\":\"MovieItem\",\"roles\":[\"actor\"],\"total\":" + total + ",\"offset\":" + offset
            + ",\"limit\":" + limit + ",\"returned\":" + page.Count + ",\"sort\":\"" + sort + "\",\"people\":["
            + rows + "]}";
      }

      /// <summary>Two titles, because the shelf under a person only has to prove it was asked about them.</summary>
      private static string Titles(string query)
      {
        return "{\"total\":2,\"offset\":0,\"limit\":120,\"returned\":2,\"sort\":\"title\",\"titles\":["
            + "{\"id\":\"00000001-0000-0000-0000-000000000000\",\"title\":\"Alien\",\"year\":1979},"
            + "{\"id\":\"00000002-0000-0000-0000-000000000000\",\"title\":\"Brand X\",\"year\":1973}]}";
      }

      /// <summary>The person's id, which the wall carries as its own and the works are filed under.</summary>
      private static string IdOf(string name)
      {
        int letters = 0;
        foreach (char letter in name)
          letters = (letters * 31 + letter) & 0x7FFFFFFF;
        return Guid.Parse(letters.ToString("X8", CultureInfo.InvariantCulture).PadLeft(8, '0')
            + "-0000-0000-0000-000000000000").ToString("D");
      }

      private static Dictionary<string, string> Parse(string query)
      {
        Dictionary<string, string> asked = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string part in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
          int equals = part.IndexOf('=');
          if (equals < 0)
            continue;
          asked[part.Substring(0, equals)] = Uri.UnescapeDataString(part.Substring(equals + 1));
        }
        return asked;
      }

      private static string Of(Dictionary<string, string> asked, string key = "")
      {
        return asked.TryGetValue(key, out string? value) ? value : "";
      }

      /// <summary>
      /// A number the question carried, or the one the server itself falls back to. The client leaves a word out
      /// when it means its beginning - no <c>offset</c> for the first page, no <c>limit</c> for the default one -
      /// and a wall that read those as errors would be a wall that cannot be opened.
      /// </summary>
      private static int Read(Dictionary<string, string> asked, string key, int whenSaidNothing)
      {
        return int.TryParse(Of(asked, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            ? number
            : whenSaidNothing;
      }
    }
  }
}
