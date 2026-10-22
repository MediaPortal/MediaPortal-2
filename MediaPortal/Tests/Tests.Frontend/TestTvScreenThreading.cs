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
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// What a television screen does with an answer that arrives on a thread which is not the one the screen lives
  /// on, which is what every answer from a machine in another room is.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nothing in this file is about what a backend says; the answers are as ordinary as they come. All of it is
  /// about where the answer is applied. A backend on the network is asked a question, and the moment the answer
  /// comes back the code waiting for it continues on a thread of the .NET thread pool. Avalonia keeps the two
  /// threads apart - the rows a guide is bound to belong to the thread that built the guide, and touching them
  /// from anywhere else throws - so a screen that applies its answer where it arrived either dies or says its own
  /// undoing. Pressing Live TV closed the front end exactly this way, and the programme guide drew an evening
  /// collapsed against its headers and said "the calling thread cannot access this object" where the number of
  /// programmes should have stood.
  /// </para>
  /// <para>
  /// Every test here therefore uses a backend that answers <see cref="ELSEWHERE_DELAY_MS"/> later, from a thread
  /// of its own. A scripted backend that answers while the question is still being asked continues on the thread
  /// that asked, which is the thread the screen lives on - so the whole suite written before this bug looked at
  /// the one case that could not go wrong and proved nothing about the other one.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvScreenThreading
  {
    /// <summary>
    /// How long the backend holds an answer back. Long enough that the asking thread has definitely moved on, and
    /// short enough that the suite does not notice.
    /// </summary>
    private const int ELSEWHERE_DELAY_MS = 40;

    #region A backend that answers from elsewhere

    private const string BOUQUETS_JSON = "{\"total\":2,\"entries\":["
        + "{\"uuid\":\"alle\",\"name\":\"Alle Kanäle\",\"services\":[\"s1\",\"s2\"]},"
        + "{\"uuid\":\"radio\",\"name\":\"Radio\",\"services\":[\"s3\"]}]}";

    private const string CHANNELS_JSON = "{\"total\":3,\"entries\":["
        + "{\"number\":1,\"name\":\"Erstes\",\"epgchannelid\":\"epg-a\",\"streamurl\":\"//stream/channelid/1\","
        + "\"tags\":[\"alle\"],\"services\":[\"s1\"]},"
        + "{\"number\":2,\"name\":\"Zweites\",\"epgchannelid\":\"epg-b\",\"streamurl\":\"//stream/channelid/2\","
        + "\"tags\":[\"alle\"],\"services\":[\"s2\"]},"
        + "{\"number\":9,\"name\":\"Welle\",\"epgchannelid\":\"epg-c\",\"streamurl\":\"//stream/channelid/9\","
        + "\"tags\":[\"radio\"],\"services\":[\"s3\"]}]}";

    private const string SERVICES_JSON = "{\"total\":3,\"entries\":["
        + "{\"uuid\":\"s1\",\"type\":1},{\"uuid\":\"s2\",\"type\":1},{\"uuid\":\"s3\",\"type\":2}]}";

    /// <summary>An evening running through now on both television channels, so that there is an evening to draw.</summary>
    private static string EventsJson()
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      long anchor = now - now % (TvGuideViewModel.MINUTES_PER_COLUMN * 60L);

      return "{\"total\":4,\"entries\":["
          + "{\"event_id\":1,\"start\":" + (anchor - 600) + ",\"stop\":" + (anchor + 3000)
          + ",\"title\":\"Laufendes\",\"channel\":\"epg-a\",\"channelname\":\"Erstes\",\"contentLabel\":\"Movie\"},"
          + "{\"event_id\":2,\"start\":" + (anchor + 3000) + ",\"stop\":" + (anchor + 6600)
          + ",\"title\":\"Danach\",\"channel\":\"epg-a\",\"channelname\":\"Erstes\",\"contentLabel\":\"News\"},"
          + "{\"event_id\":3,\"start\":" + (anchor - 600) + ",\"stop\":" + (anchor + 4800)
          + ",\"title\":\"Und noch was\",\"channel\":\"epg-b\",\"channelname\":\"Zweites\","
          + "\"contentLabel\":\"Documentary\"},"
          + "{\"event_id\":4,\"start\":" + (anchor + 4800) + ",\"stop\":" + (anchor + 8400)
          + ",\"title\":\"Später\",\"channel\":\"epg-b\",\"channelname\":\"Zweites\"}]}";
    }

    private static string UpcomingJson()
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

      return "{\"totalCount\":2,\"limit\":2000,\"start\":0,\"dlrEntries\":["
          + "{\"uuid\":\"dvr-1\",\"entryId\":1,\"channel\":\"epg-a\",\"name\":\"Erstes\",\"title\":\"G plant\","
          + "\"start\":" + (now + 7200) + ",\"stop\":" + (now + 10800)
          + ",\"state\":\"Scheduled\",\"enabled\":true,\"dir\":\"/mnt/tv\"},"
          + "{\"uuid\":\"dvr-2\",\"entryId\":2,\"channel\":\"epg-b\",\"name\":\"Zweites\",\"title\":\"Läuft\","
          + "\"start\":" + (now - 600) + ",\"stop\":" + (now + 3000)
          + ",\"state\":\"Recording\",\"enabled\":true,\"dir\":\"/mnt/tv\"}]}";
    }

    /// <summary>
    /// A backend that answers every question a television screen asks, and answers it late and from somewhere
    /// else: the task it hands back is completed by a timer, so whoever was waiting for it wakes up on a thread
    /// of the thread pool instead of on the one that asked.
    /// </summary>
    private class Elsewhere : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        lock (AskedPaths)
        {
          AskedPaths.Add(path);
        }

        string body = path.EndsWith("/api/channeltag/list", StringComparison.Ordinal) ? BOUQUETS_JSON
            : path.EndsWith("/api/channel/grid", StringComparison.Ordinal) ? CHANNELS_JSON
            : path.EndsWith("/api/service/grid", StringComparison.Ordinal) ? SERVICES_JSON
            : path.EndsWith("/api/epg/events", StringComparison.Ordinal) ? EventsJson()
            : path.Contains("grid_upcoming", StringComparison.Ordinal) ? UpcomingJson()
            : "{\"total\":0,\"entries\":[]}";

        // The pause is what a machine in another room does to the thread that asked of it. A task completed here
        // would run its waiters on this thread, so the wait is slept rather than skipped.
        await Task.Delay(ELSEWHERE_DELAY_MS, cancellation).ConfigureAwait(false);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
      }
    }

    private static TvBackendSettings WrittenDown()
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      return settings;
    }

    private static TvHeadendApiClient Through(Elsewhere backend, TvBackendSettings settings)
    {
      return new TvHeadendApiClient(settings, new HttpClient(backend));
    }

    /// <summary>The guide a viewer is looking at: its own thread handed over, the way the application hands it over.</summary>
    private static TvGuideViewModel GuideOnScreen(Elsewhere backend, Action<Action>? marshal = null)
    {
      return new TvGuideViewModel(TvMediaType.Television, WrittenDown,
          delegate (TvBackendSettings s) { return Through(backend, s); },
          marshal ?? ScreenThread.Run);
    }

    private static TvTunerViewModel TunerOnScreen(Elsewhere backend)
    {
      return new TvTunerViewModel(TvMediaType.Television, WrittenDown,
          delegate (TvBackendSettings s) { return Through(backend, s); }, ScreenThread.Run);
    }

    private static TvSchedulesViewModel SchedulesOnScreen(Elsewhere backend)
    {
      return new TvSchedulesViewModel(false, WrittenDown,
          delegate (TvBackendSettings s) { return Through(backend, s); }, ScreenThread.Run);
    }

    private static Window ShowOnScreen(Control screen)
    {
      Window window = new Window
      {
        Content = screen,
        Width = 1280,
        Height = 720
      };
      window.Show();
      Pump();
      return window;
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(ELSEWHERE_DELAY_MS * 3);
      Dispatcher.UIThread.RunJobs();
    }

    #endregion

    #region The three screens that read a backend when they open

    [Test]
    [AvaloniaTest]
    public void AnEveningArrivingFromAnotherThreadIsDrawnRatherThanStruckThrough()
    {
      Elsewhere backend = new Elsewhere();
      TvGuideViewModel guide = GuideOnScreen(backend);
      Window window = ShowOnScreen(new TvGuideView(guide));

      guide.ReloadAsync().GetAwaiter().GetResult();
      Pump();

      Assert.That(guide.Status, Does.Not.Contain("went wrong"),
          "the guide's own line about itself is not a place to report a threading mistake: " + guide.Status);
      Assert.That(guide.Rows, Is.Not.Empty, "the evening arrived, so the grid has rows");

      List<Border> drawn = window.GetVisualDescendants().OfType<Border>().Where(b => b.Tag is TvGuideBlock)
          .ToList();
      Assert.That(drawn, Is.Not.Empty, "and the boxes are on the screen");
      Assert.That(drawn.Count, Is.EqualTo(guide.Rows.Sum(r => r.Blocks.Count)),
          "one box per programme, neither fewer because a rebuild died halfway nor more because an old one stayed");
      Assert.That(drawn.Select(b => Grid.GetColumn(b)).Distinct().Count(), Is.GreaterThan(1),
          "and they stand in more than one column - an evening all pressed against the headers is what the "
          + "half-finished rebuild of a guide that died of a threading mistake left looking like");
      Assert.That(drawn.Select(b => Grid.GetColumn(b)).Distinct().Count(), Is.GreaterThan(1),
          "and they stand in more than one column - an evening all pressed against the headers is what the "
          + "half-finished rebuild of a guide that died of a threading mistake left looking like");
      foreach (TvGuideRow row in guide.Rows)
      {
        foreach (TvGuideBlock block in row.Blocks)
        {
          Border box = drawn.First(b => ReferenceEquals(b.Tag, block));
          Assert.That(Grid.GetColumn(box), Is.EqualTo(block.Column), block.Title + " reached its column");
          Assert.That(Grid.GetColumnSpan(box), Is.EqualTo(block.ColumnSpan),
              block.Title + " spans the columns its own length covers");
        }
      }
    }

    [Test]
    [AvaloniaTest]
    public void AChannelListArrivingFromAnotherThreadDoesNotTakeTheFrontEndWithIt()
    {
      Elsewhere backend = new Elsewhere();
      TvTunerViewModel tuner = TunerOnScreen(backend);
      // The screen has to be standing there while the answer arrives, because it is the screen's own list that
      // the answer is put into.
      _ = ShowOnScreen(new TvWatchView(tuner));

      // Pressing Live TV got this far and no further: the channels were read, and putting them where the screen
      // could see them threw on a thread that owns no controls. Nothing between here and the screen catches that
      // - a tuner's reload is fired and forgotten - so the exception ended the process.
      Assert.That(() => tuner.ReloadAsync().GetAwaiter().GetResult(), Throws.Nothing);
      Pump();

      Assert.That(tuner.Channels.Select(c => c.Name).ToList(), Is.EqualTo(new[] { "Erstes", "Zweites" }),
          "the television bouquet, on the screen's thread, without taking the front end with it");
      Assert.That(tuner.Status, Does.Contain("2 channels in"), "and the screen says what it has");

      Assert.That(tuner.WatchRows.Select(r => r.Channel.Name).ToList(), Is.EqualTo(new[] { "Erstes", "Zweites" }),
          "and the list over the picture is filled from the same answer, on the same thread");
    }

    [Test]
    [AvaloniaTest]
    public void ATableOfTimersArrivingFromAnotherThreadSaysHowManyItHas()
    {
      Elsewhere backend = new Elsewhere();
      TvSchedulesViewModel schedules = SchedulesOnScreen(backend);
      ShowOnScreen(new TvSchedulesView(schedules));

      schedules.ReloadAsync().GetAwaiter().GetResult();
      Pump();

      Assert.That(schedules.Entries.Count, Is.EqualTo(2), "both plans are on the screen");
      Assert.That(schedules.Status, Does.Not.Contain("went wrong"),
          "and the screen is counting its rows rather than reporting itself: " + schedules.Status);
    }

    #endregion

    #region The way onto the screen's thread is the only way an answer gets applied

    /// <summary>
    /// A guide handed a way onto the screen's thread that does not use it yet: nothing may have happened to the
    /// grid in the meantime. This is what proves the model is not merely lucky about which thread it wakes up on.
    /// </summary>
    [Test]
    public void AGuideKeepsItsHandsOffTheGridUntilTheWayOntoTheScreenThreadIsWalked()
    {
      Elsewhere backend = new Elsewhere();
      List<Action> waiting = new List<Action>();
      TvGuideViewModel guide = GuideOnScreen(backend, delegate (Action work) { lock (waiting) { waiting.Add(work); } });

      guide.ReloadAsync().GetAwaiter().GetResult();

      lock (waiting)
      {
        Assert.That(waiting, Is.Not.Empty, "the answer came and was handed over rather than applied");
      }
      Assert.That(guide.Rows, Is.Empty, "and until that handover happens the grid is untouched");

      Action[] toDo;
      lock (waiting)
      {
        toDo = waiting.ToArray();
      }
      foreach (Action work in toDo)
        work();

      Assert.That(guide.Rows, Is.Not.Empty, "and walking the way is what fills it");
      Assert.That(guide.Rows.Sum(r => r.Blocks.Count), Is.GreaterThan(0), "with the evening in it");
    }

    /// <summary>
    /// A model built without a way onto anybody's thread - which is every test of a model on its own - still
    /// applies its answer where it arrives, so that nobody has to pump a dispatcher to look at a model.
    /// </summary>
    [Test]
    public void AModelOnItsOwnStillAppliesItsAnswerWhereItArrives()
    {
      Elsewhere backend = new Elsewhere();
      TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television, WrittenDown,
          delegate (TvBackendSettings s) { return Through(backend, s); });

      guide.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(guide.Rows, Is.Not.Empty);
      Assert.That(guide.Status, Does.Not.Contain("went wrong"));
    }

    [Test]
    public void TheSettingsSentenceAboutABackendComesThroughTheWayOntoTheScreenThread()
    {
      Elsewhere backend = new Elsewhere();
      List<Action> waiting = new List<Action>();
      TvSettingsViewModel screen = new TvSettingsViewModel(
          delegate (TvBackendSettings s) { return Through(backend, s); },
          delegate (Action work) { lock (waiting) { waiting.Add(work); } });
      screen.Show(WrittenDown(), null);
      string saidBefore = screen.Status;

      screen.CheckAsync().GetAwaiter().GetResult();

      lock (waiting)
      {
        Assert.That(waiting, Is.Not.Empty, "what the backend said is the screen's, so it was handed over");
      }
      Assert.That(screen.Status, Is.EqualTo(saidBefore),
          "and the screen has not been spoken to from the thread the answer arrived on");

      Action[] toDo;
      lock (waiting)
      {
        toDo = waiting.ToArray();
      }
      foreach (Action work in toDo)
        work();

      Assert.That(screen.Status, Does.Contain("answered"), "and walked, it says what the backend answered");
    }

    #endregion
  }
}
