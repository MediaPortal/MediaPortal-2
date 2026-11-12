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
      await wall.PickSortAsync("born");
      await wall.PickGroupAsync("letter");

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
        string sort = Of(asked);
        bool descending = string.Equals(Of(asked, "order"), "desc", StringComparison.OrdinalIgnoreCase);
        int offset = Read(asked, "offset", 0);
        int limit = Read(asked, "limit", 120);

        List<string> standing = new List<string>(_names);
        if (letter.Length > 0)
          standing.RemoveAll(name => !name.StartsWith(letter, StringComparison.OrdinalIgnoreCase));
        if (finding.Length > 0)
          standing.RemoveAll(name => name.IndexOf(finding, StringComparison.OrdinalIgnoreCase) < 0);

        if (string.Equals(sort, "born", StringComparison.OrdinalIgnoreCase))
          standing.Sort((one, other) => _born[one].CompareTo(_born[other]));
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
              .Append("\",\"occupation\":\"ACTOR\",\"bornDate\":\"").Append(_born[name])
              .Append("-01-01T00:00:00\",\"deathDate\":null}");
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
