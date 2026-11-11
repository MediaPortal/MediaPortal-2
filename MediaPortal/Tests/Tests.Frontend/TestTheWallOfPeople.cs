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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Avalonia;
using Avalonia.Headless.NUnit;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

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

      Assert.That(server.Asked[server.Asked.Count - 1], Does.Contain("letter=B"));
      Assert.That(wall.IsNarrowed, Is.True);
      foreach (object row in wall.Rows)
      {
        PersonTile tile = (PersonTile) row;
        Assert.That(tile.Name, Does.StartWith("B"), "the wall holds the names the server gave it for its letter");
      }

      await wall.ClearLetterAsync();
      Assert.That(server.Asked[server.Asked.Count - 1], Does.Not.Contain("letter="),
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

      string last = server.Asked[server.Asked.Count - 1];
      Assert.That(last, Does.Contain("search="), "the word is carried to the server, which is the one that knows "
          + "every name it holds");
      Assert.That(wall.Rows.Count, Is.EqualTo(3), "one name of each letter carries the looked-for word");

      await wall.ClearSearchAsync();
      Assert.That(server.Asked[server.Asked.Count - 1], Does.Not.Contain("search="));
      Assert.That(wall.Rows.Count, Is.EqualTo(9));
    }

    [Test]
    public async Task TheOrderAndTheEndAreAskedForAndRemembered()
    {
      WallServer first = new WallServer(9);
      PeopleViewModel wall = AWall(PeopleShelf.FilmActors, first);
      await wall.ReadAsync(true);

      await wall.PickSortAsync("born");
      string last = first.Asked[first.Asked.Count - 1];
      Assert.That(last, Does.Contain("sort=born"));

      await wall.PickSortAsync("born");
      last = first.Asked[first.Asked.Count - 1];
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

        if (path.EndsWith("/thumbnail", StringComparison.Ordinal)
            || path.EndsWith("/fanart", StringComparison.Ordinal))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        string body;
        if (path.EndsWith("/persons", StringComparison.Ordinal))
        {
          WallAsks++;
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
