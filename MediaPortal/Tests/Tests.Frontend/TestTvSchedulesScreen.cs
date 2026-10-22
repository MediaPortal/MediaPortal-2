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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The screen a viewer opens to see what the television was asked to keep, and to take something away again.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference's schedules screen (<c>TV/SlimTvClient/Skin/Nereus/screens/
  /// SlimTvClient-schedulelist.xaml</c>) was a list with a band of words beside it and a checkbox above the band
  /// deciding whether the viewer was working on single programmes or on whole series. That last detail is the reason
  /// this fixture exists at all: the same press means two different things depending on a switch the viewer can see
  /// but cannot feel, so which question the screen finally asks of the backend is worth asserting, and not only what
  /// it draws.
  /// </para>
  /// <para>
  /// A second reason: every press here ends with the table being read back. A screen that believed its own story
  /// after a delete would be the one place in the house where a recording still stood as planned after the backend
  /// had thrown it away, so the tests count the questions the scripted box was asked, in order.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvSchedulesScreen
  {
    /// <summary>A round hour, so that every time an assertion reads is an offset from something checkable.</summary>
    private const long ANCHOR = 1800000000;

    #region The scripted backend

    /// <summary>
    /// Three plans: one that has not begun, one that is being written right now as part of a renewed series, and
    /// one the backend itself switched off because it clashes with another - which is the row whose complaint the
    /// band at the right has to carry.
    /// </summary>
    private static readonly string PLANS_JSON = "{\"totalCount\":3,\"limit\":2000,\"start\":0,\"dlrEntries\":["
        + "{\"uuid\":\"dvr-1\",\"entryId\":\"11\",\"name\":\"Erste\",\"channel\":\"ch-erste\","
        + "\"title\":\"Frühstück\",\"subtitle\":\"Folge 3\",\"description\":\"Ein Frühstück im Ersten.\","
        + "\"state\":\"scheduled\",\"enabled\":1,\"start\":" + (ANCHOR + 3600) + ",\"stop\":" + (ANCHOR + 5400) + "},"
        + "{\"uuid\":\"dvr-2\",\"entryId\":\"12\",\"name\":\"Dritte\",\"channel\":\"ch-dritte\","
        + "\"title\":\"Spätschicht\",\"state\":\"recording\",\"enabled\":true,\"autorenew\":1,\"autoscheduled\":1,"
        + "\"size\":1073741824,\"url\":\"/srv/spaetschicht.ts\",\"start\":" + (ANCHOR - 600) + ",\"stop\":"
        + (ANCHOR + 1800) + "},"
        + "{\"uuid\":\"dvr-3\",\"entryId\":\"13\",\"name\":\"Zweite\",\"channel\":\"ch-zweite\","
        + "\"title\":\"Ausgefallen\",\"state\":\"conflict\",\"enabled\":0,"
        + "\"disabledText\":\"belegt durch Serienaufnahme\",\"start\":" + (ANCHOR + 7200) + ",\"stop\":"
        + (ANCHOR + 9000) + "}]}";

    /// <summary>Two recordings that are done, one of them with nothing on disk behind it any more.</summary>
    private static readonly string FILES_JSON = "{\"totalCount\":2,\"limit\":2000,\"start\":0,\"dlrEntries\":["
        + "{\"uuid\":\"dvr-9\",\"entryId\":\"19\",\"name\":\"Erste\",\"channel\":\"ch-erste\","
        + "\"title\":\"Vorabend\",\"state\":\"completed\",\"enabled\":1,\"size\":536870912,"
        + "\"url\":\"/srv/vorabend.ts\",\"start\":" + (ANCHOR - 7200) + ",\"stop\":" + (ANCHOR - 5400) + "},"
        + "{\"uuid\":\"dvr-10\",\"entryId\":\"20\",\"name\":\"Dritte\",\"channel\":\"ch-dritte\","
        + "\"title\":\"Mitternacht\",\"state\":\"completed\",\"enabled\":1,\"start\":" + (ANCHOR - 3600)
        + ",\"stop\":" + (ANCHOR - 1800) + "}]}";

    /// <summary>The yes a write is answered with, carrying the entry the backend made.</summary>
    private static readonly string GRANTED_JSON = "{\"success\":true,\"data\":{\"uuid\":\"dvr-1\",\"entryId\":\"11\","
        + "\"name\":\"Erste\",\"title\":\"Frühstück\",\"state\":\"cancelled\",\"enabled\":1,"
        + "\"start\":" + 1800003600 + ",\"stop\":1800005400}}";

    /// <summary>A refusal in the shape the newer boxes write it.</summary>
    private const string REFUSED_JSON = "{\"success\":false,\"errors\":[{\"text\":"
        + "\"Ein identischer Timer existiert bereits\"}]}";

    /// <summary>
    /// A TvHeadend, scripted. It remembers every address and query it was asked, because almost everything here is
    /// about which question the screen asks and in what order rather than about what comes back.
    /// </summary>
    private class ScriptedDvr : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();

      /// <summary>A status for a machine that is not there, so that a fault can be told from a refusal.</summary>
      internal HttpStatusCode? RefuseEverything;

      /// <summary>The answer to give to anything a viewer asks to be done.</summary>
      internal string AnswerToWrite = GRANTED_JSON;

      /// <summary>Forgets what it was asked, so that a test can look at one press on its own.</summary>
      internal void Forget()
      {
        AskedPaths.Clear();
        AskedQueries.Clear();
      }

      /// <summary>The questions about one entry, without the reads: what the viewer asked for.</summary>
      internal List<string> AskedToDo
      {
        get { return Pairs().Select(asked => asked.path).ToList(); }
      }

      /// <summary>The query of the last thing the viewer asked to be done, which is where a switch shows.</summary>
      internal string LastToDoQuery
      {
        get
        {
          List<(string path, string query)> asked = Pairs();
          return asked.Count == 0 ? "" : asked[asked.Count - 1].query;
        }
      }

      /// <summary>Every question about an entry with its query, in the order they were asked.</summary>
      internal List<string> ToDoQueries
      {
        get { return Pairs().Select(asked => asked.query).ToList(); }
      }

      /// <summary>Address and query together, keeping their order, for the two reads above.</summary>
      private List<(string path, string query)> Pairs()
      {
        return AskedPaths
            .Zip(AskedQueries, (path, query) => (path, query))
            .Where(asked => asked.path.Contains("/api/dvr/entry/", StringComparison.Ordinal)
                && !asked.path.Contains("/grid_", StringComparison.Ordinal))
            .ToList();
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        AskedPaths.Add(path);
        AskedQueries.Add(request.RequestUri.Query);

        if (RefuseEverything.HasValue)
          return Answer(request, RefuseEverything.Value, "");

        string body;
        if (path.EndsWith("/api/dvr/entry/grid_upcoming", StringComparison.Ordinal))
          body = PLANS_JSON;
        else if (path.EndsWith("/api/dvr/entry/grid_finished", StringComparison.Ordinal))
          body = FILES_JSON;
        else if (path.EndsWith("/api/dvr/entry/grid_all", StringComparison.Ordinal))
          body = PLANS_JSON;
        else if (path.Contains("/api/dvr/entry/", StringComparison.Ordinal))
          body = AnswerToWrite;
        else
          body = "{\"totalCount\":0,\"dlrEntries\":[]}";

        return Answer(request, HttpStatusCode.OK, body);
      }

      private static Task<HttpResponseMessage> Answer(HttpRequestMessage request, HttpStatusCode code, string body)
      {
        return Task.FromResult(new HttpResponseMessage(code)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private static TvBackendSettings WrittenDown()
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      return settings;
    }

    /// <summary>Builds a screen over the scripted box, with the table it opens on already read.</summary>
    private static TvSchedulesViewModel Opened(ScriptedDvr backend, bool finishedFirst = false)
    {
      TvSchedulesViewModel screen = new TvSchedulesViewModel(finishedFirst, WrittenDown,
          delegate (TvBackendSettings settings)
          {
            return new TvHeadendApiClient(settings, new HttpClient(backend));
          });

      screen.ReloadAsync().GetAwaiter().GetResult();
      return screen;
    }

    #endregion

    #region What the list holds

    [Test]
    public void TheTileForThePlansOpensOnThePlansAndTheTileForTheRecordingsOpensOnTheFiles()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel plans = Opened(backend);
      TvSchedulesViewModel files = Opened(backend, finishedFirst: true);

      Assert.That(plans.Heading, Is.EqualTo("Schedules"));
      Assert.That(files.Heading, Is.EqualTo("Recordings"),
          "the home menu advertises both, and a screen that called itself the same thing as the other would leave "
              + "a viewer wondering which of the two they were standing in");

      Assert.That(plans.Entries.Select(row => row.Heading), Is.EqualTo(new[]
      {
        "Spätschicht",
        "Frühstück - Folge 3",
        "Ausgefallen"
      }), "the plans come from the table of what is still to come, and the earliest beginning stands at the top, "
          + "because a viewer reading a list of plans reads it in the order the plans happen");

      Assert.That(files.Entries.Select(row => row.Heading), Is.EqualTo(new[] { "Vorabend", "Mitternacht" }));
    }

    [Test]
    public void EachRowSaysTheChannelTheNameTheTwoEndsOfItsTimeAndWhetherASeriesStandsBehindIt()
    {
      TvSchedulesViewModel screen = Opened(new ScriptedDvr());
      TvScheduleRow writing = screen.Entries[0];

      Assert.That(writing.Channel, Is.EqualTo("Dritte"));
      Assert.That(writing.TimeRange, Does.Match("\\d.*-.*\\d"), "both ends of the time, as one line");
      Assert.That(writing.SeriesMark, Is.EqualTo("series"), "this one is kept as a series");
      Assert.That(screen.Entries[1].SeriesMark, Is.EqualTo("single"), "and that one is a single programme");
    }

    [Test]
    public void TheStatesAViewerHasToTellApartAreNamedAndTheRestIsSaidAsTheBackendSaidIt()
    {
      TvSchedulesViewModel screen = Opened(new ScriptedDvr());

      Assert.That(screen.Entries[1].State, Is.EqualTo("waiting"));
      Assert.That(screen.Entries[0].State, Is.EqualTo("recording now"),
          "the one thing a viewer has to know before pressing anything");
      Assert.That(screen.Entries[2].State, Is.EqualTo("did not happen"),
          "a clash is a failure, whatever the backend calls it in its own bookkeeping");
    }

    [Test]
    public void TheWordsTheBackendLeftAboutAnEntryAreSaidBesideItAndNowhereElse()
    {
      TvSchedulesViewModel screen = Opened(new ScriptedDvr());
      screen.SelectedRow = screen.Entries[2];

      Assert.That(screen.DetailsProblem, Is.EqualTo("belegt durch Serienaufnahme"),
          "a plan the backend switched off says why in this field and a viewer cannot find it out any other way");

      screen.SelectedRow = screen.Entries[1];
      Assert.That(screen.DetailsProblem, Is.Empty, "a plan that is running happily has nothing to complain about");
    }

    [Test]
    public void AScreenWithNothingWrittenDownSaysSoAndAsksNobody()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = new TvSchedulesViewModel(false, delegate { return new TvBackendSettings(); },
          delegate (TvBackendSettings settings)
          {
            return new TvHeadendApiClient(settings, new HttpClient(backend));
          });

      screen.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedPaths, Is.Empty, "no question is asked of a machine nobody named");
      Assert.That(screen.Status, Does.Contain("No TV backend"));
    }

    [Test]
    public void ABoxThatWillNotAnswerAtAllIsSaidAsOneKindOfFaultAndLeavesTheListAlone()
    {
      ScriptedDvr backend = new ScriptedDvr { RefuseEverything = HttpStatusCode.ServiceUnavailable };
      TvSchedulesViewModel screen = Opened(backend);

      Assert.That(screen.Status, Does.Contain("would not answer"));
      Assert.That(screen.Entries, Is.Empty);
    }

    #endregion

    #region What the presses ask

    [Test]
    public void TurningTheTableOverAsksTheOtherTableAndNothingElse()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      backend.Forget();

      screen.SelectedTable = screen.Tables[1];
      screen.LastWork.GetAwaiter().GetResult();

      Assert.That(backend.AskedPaths, Is.EqualTo(new[] { "/api/dvr/entry/grid_finished" }),
          "reading the recordings reads the recordings and does not read the plans a second time");
      Assert.That(screen.Heading, Is.EqualTo("Recordings"));
    }

    [Test]
    public void DeletingASingleProgrammeAsksOnceAndReadsTheTableBack()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = screen.Entries[1];
      backend.Forget();

      screen.DeleteAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedToDo, Is.EqualTo(new[] { "/api/dvr/entry/delete" }),
          "one entry, one question about it");
      Assert.That(backend.LastToDoQuery, Does.Contain("uuid=dvr-1"));
      Assert.That(backend.AskedPaths, Does.Contain("/api/dvr/entry/grid_upcoming"),
          "and the list is read back, so that the row the viewer threw away is not still standing there");
      Assert.That(screen.Status, Does.Contain("Deleted"));
    }

    [Test]
    public void WithTheSeriesSwitchOnADeleteLetsGoOfTheSeriesFirstAndOnlyThenTheEntry()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SeriesMode = true;
      screen.SelectedRow = screen.Entries[0];

      Assert.That(screen.DeleteWord, Is.EqualTo("Delete series schedule"),
          "the button says which of the two questions it is, because the switch cannot be felt");
      backend.Forget();

      screen.DeleteAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedToDo, Is.EqualTo(new[] { "/api/dvr/entry/autorenew", "/api/dvr/entry/delete" }),
          "a backend still renewing an event would answer the delete by putting the entry back");
      Assert.That(backend.ToDoQueries[0], Does.Contain("active=0").And.Contain("all=1"),
          "the whole series, not the one entry");
      Assert.That(screen.Status, Does.Contain("Deleted, with the series behind"));
    }

    [Test]
    public void WithTheSeriesSwitchOffADeleteOfASeriesEntryLeavesTheSeriesStanding()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = screen.Entries[0];
      backend.Forget();

      screen.DeleteAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedToDo, Is.EqualTo(new[] { "/api/dvr/entry/delete" }),
          "the reference's own 'Delete single program', which was the same distinction");
    }

    [Test]
    public void StoppingIsOfferedOnlyWhileTheBackendIsWriting()
    {
      TvSchedulesViewModel screen = Opened(new ScriptedDvr());

      screen.SelectedRow = screen.Entries[1];
      Assert.That(screen.CanStop, Is.False, "a plan that has not begun cannot be stopped");

      screen.SelectedRow = screen.Entries[0];
      Assert.That(screen.CanStop, Is.True, "one that is being written can");
    }

    [Test]
    public void StoppingEndsTheWritingAndLeavesTheEntryStanding()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = screen.Entries[0];
      backend.Forget();

      screen.StopAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedToDo, Is.EqualTo(new[] { "/api/dvr/entry/cancel" }),
          "cancel, not delete - the reference's 'Stop current recording', which left the entry to be read");
      Assert.That(screen.Status, Does.Contain("Stopped"));
    }

    [Test]
    public void RenewingFlipsTheBackendTheOtherWayFromWhatTheEntrySaid()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = screen.Entries[1];
      backend.Forget();

      screen.RenewSeriesAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedToDo, Is.EqualTo(new[] { "/api/dvr/entry/autorenew" }));
      Assert.That(backend.LastToDoQuery, Does.Contain("active=1"),
          "the entry was a single programme, so the press makes it a series");
      Assert.That(screen.Status, Does.Contain("Started keeping the series of"));

      screen.SelectedRow = screen.Entries[0];
      backend.Forget();
      screen.RenewSeriesAsync().GetAwaiter().GetResult();

      Assert.That(backend.LastToDoQuery, Does.Contain("active=0"),
          "and the same press on a series lets go of it instead");
    }

    [Test]
    public void ARefusalIsSaidInTheBackendOwnWordsAndTheListIsStillReadBack()
    {
      ScriptedDvr backend = new ScriptedDvr { AnswerToWrite = REFUSED_JSON };
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = screen.Entries[1];

      screen.DeleteAsync().GetAwaiter().GetResult();

      Assert.That(screen.Status, Does.Contain("Ein identischer Timer existiert bereits"),
          "the box said why in the viewer's own language, which is more than any sentence written here could say");
      Assert.That(backend.AskedPaths, Does.Contain("/api/dvr/entry/grid_upcoming"),
          "the list is read back even after a no, because a refusal may still have changed another entry");
    }

    [Test]
    public void AQuestionAskedWithoutAnythingStoodOnIsAnsweredWithoutAskingTheBackend()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = null;
      backend.Forget();

      screen.DeleteAsync().GetAwaiter().GetResult();
      screen.StopAsync().GetAwaiter().GetResult();
      screen.RenewSeriesAsync().GetAwaiter().GetResult();

      Assert.That(backend.AskedToDo, Is.Empty, "nothing is stood on, so nothing is pointed at");
      Assert.That(screen.Status, Is.EqualTo("Nothing is picked."));
    }

    [Test]
    public void TheRowAViewerStoodOnIsStillStoodOnAfterTheTableIsReadAgain()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = Opened(backend);
      screen.SelectedRow = screen.Entries[2];

      screen.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(screen.SelectedRow, Is.Not.Null);
      Assert.That(screen.SelectedRow!.Recording.Uuid, Is.EqualTo("dvr-3"),
          "a screen that jumped back to the top every time it read the table could not be read while a recording "
              + "ended in front of the viewer");
    }

    #endregion

    #region What the screen draws

    /// <summary>
    /// The model can hold the right rows and the screen still draw something else, because the two agree only
    /// through the bindings in the markup. These tests look at what came out.
    /// </summary>
    private static Window ShowOnScreen(TvSchedulesViewModel screen)
    {
      Window window = new Window
      {
        Content = new TvSchedulesView(screen),
        Width = 1280,
        Height = 720
      };
      window.Show();
      Dispatcher.UIThread.RunJobs();
      return window;
    }

    private static List<string> DrawnWords(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>()
          .Select(text => text.Text ?? "")
          .Where(words => words.Length > 0)
          .ToList();
    }

    [Test]
    [AvaloniaTest]
    public void TheScreenDrawsOneRowPerEntryAndTalksAboutTheOneThatIsStoodOn()
    {
      TvSchedulesViewModel screen = Opened(new ScriptedDvr());
      Window window = ShowOnScreen(screen);

      ListBox list = window.GetVisualDescendants().OfType<ListBox>().First();
      List<ListBoxItem> rows = list.GetVisualDescendants().OfType<ListBoxItem>()
          .Where(item => item.Content is TvScheduleRow)
          .ToList();

      Assert.That(rows.Count, Is.EqualTo(screen.Entries.Count), "one row on the screen per entry in the model");

      List<string> words = DrawnWords(window);
      Assert.That(words, Does.Contain("Schedules"));
      Assert.That(words, Has.Some.Contains("Spätschicht"));
      Assert.That(words, Has.Some.Contains("recording now"));
      Assert.That(words, Does.Contain(screen.DetailsHeading),
          "and the band at the right says the same thing the model says about the row that is stood on");
    }

    [Test]
    [AvaloniaTest]
    public void TheScreenWithoutABackendDrawsItsSentenceRatherThanAnEmptyListAndSilence()
    {
      ScriptedDvr backend = new ScriptedDvr();
      TvSchedulesViewModel screen = new TvSchedulesViewModel(false, delegate { return new TvBackendSettings(); },
          delegate (TvBackendSettings settings)
          {
            return new TvHeadendApiClient(settings, new HttpClient(backend));
          });

      screen.ReloadAsync().GetAwaiter().GetResult();
      Window window = ShowOnScreen(screen);

      Assert.That(DrawnWords(window), Has.Some.Contains("No TV backend"),
          "the reason an empty screen is empty belongs on the screen");
    }

    #endregion
  }
}
