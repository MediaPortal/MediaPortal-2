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
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The question a programme in the guide can be asked: which answers stand on the list for which kind of
  /// programme, what each of them asks of the backend, and what a viewer is told when the backend would not do it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The list is the whole of this dialog, and it is the one part of the guide that could not be written out of what
  /// the guide already knew. An EPG event says a recording is attached to a programme but not whether that plan
  /// renews as a series, and the reference's list turns on exactly that (<c>SeriesScheduled</c>): behind a renewing
  /// entry the choices are "Delete single program" and "Delete full series schedule" and a plain "Delete schedule"
  /// never appears, because that leaves the series to write the entry straight back. So one entry is read before the
  /// list is written, and these tests measure both halves - what a given entry produces, and what a press on a given
  /// line sends back.
  /// </para>
  /// <para>
  /// The order is asserted as well as the contents, because the reference's order was not arbitrary
  /// (<c>SlimTvGuideModelBase.ShowProgramActions</c> and <c>SlimTvModelBase.AddRecordingOptions</c>): what a viewer
  /// most likely wants stands where their thumb already is, and the line that throws away a whole series stands last.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvProgramActions
  {
    #region The scripted backend

    /// <summary>One bouquet, two channels, so that a row can be found by name and a channel handed over.</summary>
    private const string BOUQUETS_JSON = "{\"total\":1,\"entries\":["
        + "{\"uuid\":\"all\",\"name\":\"All\",\"services\":[\"s1\",\"s2\"]}]}";

    private const string CHANNELS_JSON = "{\"total\":2,\"entries\":["
        + "{\"number\":3,\"name\":\"Das Erste\",\"epgchannelid\":\"epg-ard\",\"streamurl\":\"//stream/channelid/3\","
        + "\"tags\":[\"all\"],\"services\":[\"s1\"]},"
        + "{\"number\":12,\"name\":\"ZDF\",\"epgchannelid\":\"epg-zdf\",\"streamurl\":\"//stream/channelid/12\","
        + "\"tags\":[\"all\"],\"services\":[\"s2\"]}]}";

    private const string SERVICES_JSON = "{\"total\":2,\"entries\":["
        + "{\"uuid\":\"s1\",\"type\":1},{\"uuid\":\"s2\",\"type\":1}]}";

    /// <summary>
    /// Four programmes anchored to the running clock the way the guide's own tests anchor theirs, one for each
    /// situation the dialog's list turns on: a bulletin that is running and nobody planned, a concert that is
    /// running and has a plan attached (what kind of plan is the scripted entry's business), a film that has not
    /// begun with a plan of its own, and something later tonight nobody planned at all.
    /// </summary>
    private static string EventsFor()
    {
      long anchor = Anchor();
      return "{\"total\":4,\"entries\":["
          + "{\"event_id\":7001,\"start\":" + (anchor - 660) + ",\"stop\":" + (anchor + 2940)
          + ",\"title\":\"Tagesschau\",\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\"},"
          + "{\"event_id\":7004,\"start\":" + (anchor - 300) + ",\"stop\":" + (anchor + 3300)
          + ",\"title\":\"Kaminkonzert\",\"channel\":\"epg-zdf\",\"channelname\":\"ZDF\","
          + "\"dvrUuid\":\"dvr-1\",\"dvrState\":\"recording\"},"
          + "{\"event_id\":7002,\"start\":" + (anchor + 2940) + ",\"stop\":" + (anchor + 8340)
          + ",\"title\":\"Tatort\",\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\","
          + "\"dvrUuid\":\"dvr-2\",\"dvrState\":\"scheduled\"},"
          + "{\"event_id\":7005,\"start\":" + (anchor + 8340) + ",\"stop\":" + (anchor + 11940)
          + ",\"title\":\"Nachtspendung\",\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\"}]}";
    }

    /// <summary>An entry that is planned once and has not begun.</summary>
    private static string EntrySingle(string uuid, long anchor)
    {
      return EntryOf(uuid, "scheduled", 0, anchor);
    }

    /// <summary>An entry the box is writing at the moment.</summary>
    private static string EntryRecording(string uuid, long anchor)
    {
      return EntryOf(uuid, "recording", 0, anchor);
    }

    /// <summary>An entry that renews itself every time the broadcaster lists the programme again.</summary>
    private static string EntrySeries(string uuid, long anchor)
    {
      return EntryOf(uuid, "scheduled", 1, anchor);
    }

    private static string EntryOf(string uuid, string state, int autorenew, long anchor)
    {
      return "{\"uuid\":\"" + uuid + "\",\"entryId\":11,\"channel\":\"epg-zdf\",\"name\":\"ZDF\","
          + "\"title\":\"Kaminkonzert\",\"start\":" + (anchor - 300) + ",\"stop\":" + (anchor + 3300)
          + ",\"state\":\"" + state + "\",\"enabled\":1,\"autoscheduled\":0,\"duplicate\":0,"
          + "\"prio\":0,\"retention\":0,\"size\":0,\"dir\":\"/mnt/tv\",\"autorenew\":" + autorenew + "}";
    }

    private const string GRANTED_JSON = "{\"success\":true,\"data\":{\"uuid\":\"dvr-1\",\"entryId\":11}}";

    private const string REFUSED_JSON = "{\"success\":false,\"errors\":[{\"text\":\"Kein freier Receiver\"}]}";

    private static long Anchor()
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      return now - now % (TvGuideViewModel.MINUTES_PER_COLUMN * 60L);
    }

    private class ScriptedActions : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();

      /// <summary>What the one entry is, and so whether it is a series or being written.</summary>
      internal string EntryBody = EntrySingle("dvr-1", Anchor());

      /// <summary>What a write is answered with.</summary>
      internal string AnswerToWrite = GRANTED_JSON;

      /// <summary>Whether the question about the entry is answered at all.</summary>
      internal bool EntryIsUnreadable;

      /// <summary>Everything the backend was told to do, in the order it was told.</summary>
      internal List<string> Wrote
      {
        get
        {
          return AskedPaths.Where(p => p.Contains("entry/", StringComparison.Ordinal)
              && !p.EndsWith("entry/get", StringComparison.Ordinal)).ToList();
        }
      }

      internal string LastToDoPath
      {
        get { return Wrote.Last(); }
      }

      internal string LastToDoQuery
      {
        get
        {
          return AskedQueries.Last(q => q.Contains("uuid=", StringComparison.Ordinal)
              || q.Contains("event_id=", StringComparison.Ordinal));
        }
      }

      /// <summary>Forgets the questions asked so far, so that a later one can be picked out on its own.</summary>
      internal void Forget()
      {
        AskedPaths.Clear();
        AskedQueries.Clear();
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        AskedPaths.Add(path);
        AskedQueries.Add(request.RequestUri.Query);

        if (EntryIsUnreadable && path.EndsWith("/api/dvr/entry/get", StringComparison.Ordinal))
          return Answer(request, HttpStatusCode.NotFound, "");

        string body = path.EndsWith("/api/channeltag/list", StringComparison.Ordinal) ? BOUQUETS_JSON
            : path.EndsWith("/api/channel/grid", StringComparison.Ordinal) ? CHANNELS_JSON
            : path.EndsWith("/api/service/grid", StringComparison.Ordinal) ? SERVICES_JSON
            : path.EndsWith("/api/epg/events", StringComparison.Ordinal) ? EventsFor()
            : path.EndsWith("/api/dvr/entry/get", StringComparison.Ordinal) ? EntryBody
            : GRANTED_JSON;

        if (path.Contains("entry/cancel", StringComparison.Ordinal)
            || path.Contains("entry/delete", StringComparison.Ordinal)
            || path.Contains("entry/autorenew", StringComparison.Ordinal)
            || path.Contains("entry/create", StringComparison.Ordinal))
          body = AnswerToWrite;

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

    /// <summary>
    /// Opens a guide over the scripted backend and waits until the grid is really there, because a dialog about a
    /// programme can only be put up over a grid that has programmes on it.
    /// </summary>
    private static TvGuideViewModel Opened(ScriptedActions backend, TvBackendSettings? with = null)
    {
      TvBackendSettings settings = with ?? new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;

      TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television, delegate { return settings; },
          delegate (TvBackendSettings s) { return new TvHeadendApiClient(s, new HttpClient(backend)); });

      guide.ReloadAsync().GetAwaiter().GetResult();
      backend.Forget();
      return guide;
    }

    private static TvGuideBlock Block(TvGuideViewModel guide, string title)
    {
      TvGuideBlock? block = guide.Rows.SelectMany(r => r.Blocks).FirstOrDefault(b => b.Title == title);
      Assert.That(block, Is.Not.Null, "no '" + title + "' on the grid; the grid has "
          + string.Join(", ", guide.Rows.SelectMany(r => r.Blocks).Select(b => b.Title)));
      return block!;
    }

    /// <summary>
    /// A programme of one's own making, for the two situations a fixture anchored to now cannot put on a grid: a
    /// programme that is already over, which stands to the left of where the window begins, and a programme whose
    /// backend was never written down, which has no grid to be on.
    /// </summary>
    private static TvGuideBlock StandingAt(string title, DateTimeOffset start, DateTimeOffset stop,
        string? scheduleId, string? state)
    {
      return new TvGuideBlock(new TvProgram(7003, "epg-zdf", "ZDF", start, stop, title, null, null, null,
          0, Array.Empty<string>(), null, null, null, null, null, null, scheduleId, state),
          0, 1, false, false);
    }

    /// <summary>Something that finished a minute ago, which is what walking back ten minutes reveals.</summary>
    private static TvGuideBlock FinishedTonight(string title)
    {
      return StandingAt(title, DateTimeOffset.Now.AddMinutes(-50), DateTimeOffset.Now.AddMinutes(-1), null, null);
    }

    /// <summary>
    /// Puts the question up for a block and waits for the answers, which means waiting through the question about
    /// the entry that a planned programme makes the guide ask first.
    /// </summary>
    private static List<TvProgramAction> Asked(TvGuideViewModel guide, TvGuideBlock block,
        DateTimeOffset? moment = null)
    {
      bool asked = false;
      guide.ActionsRequested += delegate { asked = true; };
      guide.OpenProgramActionsAsync(block, moment).GetAwaiter().GetResult();

      Assert.That(asked, Is.True, "the answers were worked out but nobody was told to put the dialog up");
      Assert.That(guide.ActionsOpen, Is.True);
      Assert.That(guide.ActionsAbout, Is.SameAs(block));
      return guide.ProgramActions.ToList();
    }

    private static List<TvProgramActionKind> KindsOf(IEnumerable<TvProgramAction> actions)
    {
      return actions.Select(a => a.Kind).ToList();
    }

    /// <summary>
    /// Gives an answer and waits for the questions it started, including the reading of the programmes afterwards -
    /// which is the part a viewer waits in front of, so it is the part a test may not leave half-done.
    /// </summary>
    private static bool Run(TvGuideViewModel guide, TvProgramAction action)
    {
      return guide.RunProgramActionAsync(action).GetAwaiter().GetResult();
    }

    #endregion

    #region What stands on the list

    [Test]
    public void AProgrammeThatIsOverSaysSoAndStillOffersItsSeries()
    {
      TvGuideViewModel guide = Opened(new ScriptedActions());

      // The reference's own comment here was "if program is over already, there is nothing to do", and it went on
      // adding its recording options under that sentence anyway: what a series is about is the airing that is still
      // to come, not the one that just ended.
      List<TvProgramAction> actions = Asked(guide, FinishedTonight("Gestern"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.SaidOnly,
        TvProgramActionKind.RecordSeries
      }));
      Assert.That(actions[0].Word, Is.EqualTo("Program already over"));
      Assert.That(actions[0].IsCommand, Is.False, "a line that is only a sentence must not answer to a press");
    }

    [Test]
    public void AProgrammeThatIsRunningAndNobodyPlannedIsWatchedAndAskedForAndNothingIsRead()
    {
      ScriptedActions backend = new ScriptedActions();
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tagesschau"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.Watch,
        TvProgramActionKind.RecordOnce,
        TvProgramActionKind.RecordSeries
      }), "the reference put the channel first, and a programme that is running can still be joined from now on");

      Assert.That(actions[1].Word, Is.EqualTo("Record program 'Tagesschau'"));
      Assert.That(backend.AskedPaths, Is.Empty,
          "a programme with no plan attached has no entry to read - the reference's own status question answered "
          + "\"None\" without asking either");
    }

    [Test]
    public void AProgrammeThatIsRunningAndBeingWrittenIsWatchedAndTheWritingIsStopped()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntryRecording("dvr-1", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Kaminkonzert"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.Watch,
        TvProgramActionKind.StopRecording,
        TvProgramActionKind.RecordSeries
      }), "a writing always comes before a series: what is happening now outranks what might happen again");

      Assert.That(actions[1].Word, Is.EqualTo("Stop current recording 'Kaminkonzert'"),
          "the reference's own wording, with the programme's name in it - a household with three boxes needs to "
          + "know which recording is about to end");
    }

    [Test]
    public void AProgrammeAheadThatIsPlannedOnceIsDeletedOrMadeIntoASeries()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntrySingle("dvr-2", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tatort"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.DeleteSingle,
        TvProgramActionKind.RecordSeries
      }));
      Assert.That(actions[0].Word, Is.EqualTo("Delete schedule"),
          "the plan is what is being let go of, so that is the word on the line");
      Assert.That(actions[1].Word, Is.EqualTo("Record series"));
    }

    [Test]
    public void AProgrammeAheadThatNobodyPlannedIsAskedForOnceAndAsASeries()
    {
      TvGuideViewModel guide = Opened(new ScriptedActions());

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Nachtspendung"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.RecordOnce,
        TvProgramActionKind.RecordSeries
      }), "this is the ordinary case of the whole dialog - somebody stood on something they have not recorded yet");

      Assert.That(actions[0].Word, Is.EqualTo("Schedule recording"));
    }

    [Test]
    public void AProgrammeBehindASeriesIsOfferedItsOwnEntryAndTheWholeSeriesAndNeverAPlainDelete()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntrySeries("dvr-2", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tatort"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.DeleteSingle,
        TvProgramActionKind.DeleteSeries
      }), "behind a renewing entry the reference never offered a plain \"Delete schedule\", because the series "
          + "writes the entry straight back");

      Assert.That(actions[0].Word, Is.EqualTo("Delete single program"));
      Assert.That(actions[1].Word, Is.EqualTo("Delete full series schedule"));
    }

    [Test]
    public void AProgrammeBehindASeriesThatIsBeingWrittenIsStoppedAndItsSeriesLetGo()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntryOf("dvr-1", "recording", 1, Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Kaminkonzert"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[]
      {
        TvProgramActionKind.Watch,
        TvProgramActionKind.StopRecording,
        TvProgramActionKind.DeleteSeries
      }), "the series takes the plain \"Record series\" line away - behind a renewing entry there is nothing left "
          + "to ask for, only things to let go of");
    }

    [Test]
    public void TheEntryBehindAPlannedProgrammeIsReadByTheUuidTheProgrammeCarries()
    {
      ScriptedActions backend = new ScriptedActions();
      TvGuideViewModel guide = Opened(backend);

      Asked(guide, Block(guide, "Tatort"));

      Assert.That(backend.AskedPaths, Does.Contain("/api/dvr/entry/get"),
          "whether a plan renews as a series is the one thing an EPG event does not say, and the whole list "
          + "turns on it");
      Assert.That(backend.AskedQueries.First(q => q.Contains("uuid=", StringComparison.Ordinal)),
          Does.Contain("uuid=dvr-2"), "the uuid the programme came with, not a whole table read looking for it");
    }

    [Test]
    public void ABackendThatWillNotSayWhatTheEntryIsLeavesTheListAsShortAsTheReferenceLeftIt()
    {
      ScriptedActions backend = new ScriptedActions { EntryIsUnreadable = true };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Kaminkonzert"));

      Assert.That(KindsOf(actions), Is.EqualTo(new[] { TvProgramActionKind.Watch }),
          "no answer about the entry means no recording line at all: the reference added nothing to its list when "
          + "its own status question failed, because offering \"Delete schedule\" on a guess throws away a "
          + "household's plan");
    }

    [Test]
    public void AProgrammeWhoseBackendWasNeverNamedIsOnlyWatchedAndNothingIsAsked()
    {
      ScriptedActions backend = new ScriptedActions();

      TvGuideViewModel blind = new TvGuideViewModel(TvMediaType.Television,
          delegate { return new TvBackendSettings(); },
          delegate (TvBackendSettings settings)
          {
            return new TvHeadendApiClient(settings, new HttpClient(backend));
          });

      TvGuideBlock block = StandingAt("Ohne Backend", DateTimeOffset.Now.AddMinutes(-11),
          DateTimeOffset.Now.AddMinutes(49), "dvr-1", "recording");

      bool asked = false;
      blind.ActionsRequested += delegate { asked = true; };
      blind.OpenProgramActionsAsync(block).GetAwaiter().GetResult();

      Assert.That(asked, Is.True, "the dialog still goes up - the viewer pressed, and silence is not an answer");
      Assert.That(KindsOf(blind.ProgramActions), Is.EqualTo(new[] { TvProgramActionKind.Watch }),
          "what can be answered without a backend is still answered, and only that - which is the reference's own "
          + "branch for a handler that has no schedule control at all");
      Assert.That(backend.AskedPaths, Is.Empty, "and a box that was never written down is not asked about");
    }

    #endregion

    #region What a press asks

    [Test]
    public void StoppingTheWritingAsksTheBackendToStopItAndNotToDeleteIt()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntryRecording("dvr-1", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Kaminkonzert"));
      backend.Forget();

      Run(guide, actions[1]);

      Assert.That(backend.LastToDoPath, Does.EndWith("/api/dvr/entry/cancel"),
          "a viewer who stops a recording normally still wants the part that was already watched");
      Assert.That(backend.LastToDoQuery, Does.Contain("uuid=dvr-1"));
    }

    [Test]
    public void DeletingOneEntryOfASeriesLetsTheSeriesGoFirstAndThenTheEntry()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntrySeries("dvr-2", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tatort"));
      Run(guide, actions[1]);

      Assert.That(backend.Wrote, Is.EqualTo(new[] { "/api/dvr/entry/autorenew", "/api/dvr/entry/delete" }),
          "a backend that is still renewing the event answers a delete by putting the entry back, so the series "
          + "has to be let go of first");
      Assert.That(backend.AskedQueries.First(q => q.Contains("active=", StringComparison.Ordinal)),
          Does.Contain("active=0").And.Contain("all=1").And.Contain("uuid=dvr-2"));
    }

    [Test]
    public void RecordingASeriesOfSomethingNobodyPlannedYetAsksForItsEventAndRenewsIt()
    {
      ScriptedActions backend = new ScriptedActions();
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Nachtspendung"));
      backend.Forget();

      Run(guide, actions[1]);

      Assert.That(backend.LastToDoPath, Does.EndWith("/api/dvr/entry/create"),
          "with no entry to switch, a series has to be made from the programme's own event");
      Assert.That(backend.LastToDoQuery, Does.Contain("event_id=7005").And.Contain("autorenew=1"));
    }

    [Test]
    public void RecordingASeriesOfSomethingAlreadyPlannedTurnsTheRenewalOnForEveryEpisode()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntrySingle("dvr-2", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tatort"));
      Run(guide, actions[1]);

      Assert.That(backend.Wrote, Is.EqualTo(new[] { "/api/dvr/entry/autorenew" }));
      Assert.That(backend.LastToDoQuery, Does.Contain("active=1").And.Contain("all=1").And.Contain("uuid=dvr-2"));
    }

    [Test]
    public void SchedulingOneProgrammeAsksForItsEventOnceAndNotAsASeries()
    {
      ScriptedActions backend = new ScriptedActions();
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Nachtspendung"));
      backend.Forget();

      Run(guide, actions[0]);

      Assert.That(backend.LastToDoPath, Does.EndWith("/api/dvr/entry/create"));
      Assert.That(backend.LastToDoQuery, Does.Contain("event_id=7005").And.Contain("autorenew=0"));
    }

    [Test]
    public void AnAnswerTheBackendRefusesSaysWhatTheBackendSaidInsteadOfClaimingItWasDone()
    {
      ScriptedActions backend = new ScriptedActions
      {
        EntryBody = EntrySingle("dvr-2", Anchor()),
        AnswerToWrite = REFUSED_JSON
      };
      TvGuideViewModel guide = Opened(backend);

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tatort"));

      bool done = Run(guide, actions[1]);

      Assert.That(done, Is.False);
      Assert.That(guide.Status, Does.Contain("Kein freier Receiver"),
          "the backend's own reason, because \"it did not work\" is what sends a viewer to reboot a box that is "
          + "perfectly well");
      Assert.That(guide.ActionsOpen, Is.False, "and the dialog goes away either way - a viewer cannot answer twice");
    }

    [Test]
    public void AnAnswerClosesTheDialogAndReadsTheProgrammesBack()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntrySingle("dvr-2", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      int before = backend.AskedPaths.Count(p => p.EndsWith("/api/epg/events", StringComparison.Ordinal));
      int closed = 0;
      guide.ActionsClosed += delegate { closed++; };

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Tatort"));
      Run(guide, actions[0]);

      Assert.That(closed, Is.EqualTo(1));
      Assert.That(guide.ActionsOpen, Is.False);
      Assert.That(backend.AskedPaths.Count(p => p.EndsWith("/api/epg/events", StringComparison.Ordinal)),
          Is.GreaterThan(before),
          "a series touches more blocks than the one that was stood on, which is why the grid is read again");
    }

    [Test]
    public void WatchingFromTheDialogHandsTheChannelOverAndClosesTheQuestion()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntryRecording("dvr-1", Anchor()) };
      TvGuideViewModel guide = Opened(backend);

      List<TvChannel> handed = new List<TvChannel>();
      guide.WatchRequested += handed.Add;

      List<TvProgramAction> actions = Asked(guide, Block(guide, "Kaminkonzert"));
      Run(guide, actions[0]);

      Assert.That(handed.Select(c => c.Name).ToList(), Is.EqualTo(new[] { "ZDF" }));
      Assert.That(guide.ActionsOpen, Is.False);
      Assert.That(backend.Wrote, Is.Empty, "getting to the channel writes nothing to anybody's recording table");
    }

    [Test]
    public void ClosingTheQuestionWithoutAnsweringItAsksNothingAndIsSaidToTheGuide()
    {
      ScriptedActions backend = new ScriptedActions();
      TvGuideViewModel guide = Opened(backend);

      bool closed = false;
      guide.ActionsClosed += delegate { closed = true; };

      Asked(guide, Block(guide, "Kaminkonzert"));
      backend.Forget();
      guide.CloseProgramActions();

      Assert.That(closed, Is.True);
      Assert.That(guide.ActionsOpen, Is.False);
      Assert.That(backend.AskedPaths, Is.Empty, "a question nobody answered writes nothing");
    }

    #endregion

    #region How the guide gets to the dialog

    [Test]
    public void TheFirstPressOnAProgrammeOnlyStandsOnIt()
    {
      TvGuideViewModel guide = Opened(new ScriptedActions());
      TvGuideBlock film = Block(guide, "Tatort");

      bool opened = false;
      guide.ActionsRequested += delegate { opened = true; };

      guide.Press(film);

      Assert.That(guide.SelectedBlock, Is.SameAs(film));
      Assert.That(opened, Is.False, "one press selects; the reference's dialog came on the second press");
    }

    [Test]
    public void PressingASecondTimeOnAProgrammeThatIsRunningGoesToItWhileTheViewerWantsThat()
    {
      TvGuideViewModel guide = Opened(new ScriptedActions());
      List<TvChannel> handed = new List<TvChannel>();
      guide.WatchRequested += handed.Add;

      // Stood on first, whatever the guide had already stood on by itself when the grid arrived.
      TvGuideBlock running = Block(guide, "Tagesschau");
      guide.Press(running);
      handed.Clear();

      guide.Press(running);

      Assert.That(handed.Select(c => c.Name).ToList(), Is.EqualTo(new[] { "Das Erste" }),
          "ZapFromGuide was on unless somebody turned it off, and on the sofa pressing the thing that is on now "
          + "is how a person gets to it");
      Assert.That(guide.ActionsOpen, Is.False);
    }

    [Test]
    public void PressingASecondTimeOnAProgrammeThatIsRunningAsksOnceTheViewerWouldRatherBeAsked()
    {
      TvBackendSettings switched = new TvBackendSettings();
      switched.ZapFromGuide = false;
      TvGuideViewModel guide = Opened(new ScriptedActions(), switched);

      List<TvChannel> handed = new List<TvChannel>();
      guide.WatchRequested += handed.Add;

      TvGuideBlock running = Block(guide, "Tagesschau");
      guide.Press(running);
      handed.Clear();

      guide.Press(running);

      Assert.That(handed, Is.Empty);
      Assert.That(guide.ActionsOpen, Is.True,
          "with the switch off, the second press is the reference's OK on a programme, which asked rather than "
          + "guessed");
      Assert.That(KindsOf(guide.ProgramActions), Does.Contain(TvProgramActionKind.Watch),
          "and getting to the channel is still the first line, it is only asked for now");
    }

    [Test]
    public void PressingASecondTimeOnAProgrammeThatIsNotRunningAsks()
    {
      TvGuideViewModel guide = Opened(new ScriptedActions());
      TvGuideBlock film = Block(guide, "Tatort");

      guide.Press(film);
      guide.Press(film);

      Assert.That(guide.ActionsOpen, Is.True);
      Assert.That(guide.ActionsTitle, Is.EqualTo("Choose program action..."),
          "the reference's own title for the dialog, [SlimTvClient.ChooseProgramAction]");
      Assert.That(guide.ActionsWords, Does.Contain("Tatort").And.Contain("Das Erste"),
          "the dialog says which of the ninety blocks it is about, or a viewer has to look for the one they "
          + "pressed in the grid behind it");
    }

    #endregion

    #region The dialog on the screen

    private static Window PutUp(TvGuideViewModel guide)
    {
      Window window = new Window
      {
        Content = new TvProgramActionsView(guide, null),
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
          .Where(text => text.Length > 0)
          .ToList();
    }

    [Test]
    [AvaloniaTest]
    public void TheDialogDrawsOneLinePerAnswerAndNamesTheProgrammeItIsAbout()
    {
      ScriptedActions backend = new ScriptedActions { EntryBody = EntryRecording("dvr-1", Anchor()) };
      TvGuideViewModel guide = Opened(backend);
      Asked(guide, Block(guide, "Kaminkonzert"));

      Window window = PutUp(guide);

      ListBox list = window.GetVisualDescendants().OfType<ListBox>().First();
      List<ListBoxItem> rows = list.GetVisualDescendants().OfType<ListBoxItem>()
          .Where(item => item.Content is TvProgramAction)
          .ToList();

      Assert.That(rows.Count, Is.EqualTo(guide.ProgramActions.Count),
          "one line on the screen per answer the model worked out");

      List<string> words = DrawnWords(window);
      Assert.That(words, Has.Some.Contains("Stop current recording"), "the answers are written out, not numbered");
      Assert.That(words, Has.Some.Contains("Kaminkonzert"),
          "and the line about which programme this is all about is on the dialog itself, not only in the grid "
          + "behind it");

      Assert.That(rows.All(row => row.IsEnabled), Is.True, "every answer that was worked out can be stood on");
    }

    [Test]
    [AvaloniaTest]
    public void TheSentenceAboutAFinishedProgrammeIsDrawnButCannotBeStoodOn()
    {
      TvGuideViewModel guide = Opened(new ScriptedActions());
      Asked(guide, FinishedTonight("Gestern"));

      Window window = PutUp(guide);
      Assert.That(DrawnWords(window), Does.Contain("Program already over"));

      ListBox list = window.GetVisualDescendants().OfType<ListBox>().First();
      List<TextBlock> lines = list.GetVisualDescendants().OfType<TextBlock>()
          .Where(text => !string.IsNullOrEmpty(text.Text))
          .ToList();

      TextBlock sentence = lines.First(text => text.Text == "Program already over");
      Assert.That(sentence.IsEnabled, Is.False, "it is a sentence about the programme, not a thing the dialog can do");

      TextBlock series = lines.First(text => text.Text == "Record series");
      Assert.That(series.IsEnabled, Is.True, "and the series is still offered, as the reference offered it");
    }

    #endregion
  }
}
