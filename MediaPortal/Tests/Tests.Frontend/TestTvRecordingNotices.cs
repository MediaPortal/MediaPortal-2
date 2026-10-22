/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
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
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The recording notices: what the front end says at the corner of the screen when the TV machine starts writing
  /// something, and what it refuses to say.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference watched the TV server's pushed state and diffed the set of entries that were recording
  /// (<c>SlimTvClient/Notifications/SlimTvNotificationService.cs</c>), and the tests below diff the same way against
  /// the same two sentences - "Recording started" and "Recording ended", the reference's own strings
  /// (<c>Language/strings_en.xml:172-173</c>) - with its two switches and its timeout honoured. What is not the
  /// same is how a change is found out about at all: TVHeadend pushes nothing at this interface, so the watcher asks
  /// every half minute, and a test therefore drives the looks by hand rather than waiting half a minute for each.
  /// </para>
  /// <para>
  /// Half of these tests are about silence, which is the part a viewer only notices when it is wrong. A front end
  /// that announced every recording that had started before it was switched on would open by lying about when things
  /// happened; one that filed a router's blink as "eleven recordings ended" would frighten a household about
  /// recordings that are sitting on the disk quite happily.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvRecordingNotices
  {
    /// <summary>One round hour, so that every entry in a table is anchored to something checkable.</summary>
    private const long ANCHOR = 1800000000;

    #region The TV machine, and the clock a test drives

    /// <summary>
    /// A recording timer the machine was asked about, written the way a TVHeadend writes it in its upcoming table.
    /// </summary>
    private sealed class Entry
    {
      internal Entry(string uuid, string title, string channel, bool recording)
      {
        Uuid = uuid;
        Title = title;
        Channel = channel;
        Recording = recording;
      }

      internal readonly string Uuid;
      internal readonly string Title;
      internal readonly string Channel;
      internal readonly bool Recording;

      internal string Json
      {
        get
        {
          return "{\"uuid\":\"" + Uuid + "\",\"name\":\"" + Channel + "\",\"title\":\"" + Title
              + "\",\"start\":" + ANCHOR.ToString(CultureInfo.InvariantCulture) + ",\"stop\":"
              + (ANCHOR + 3600).ToString(CultureInfo.InvariantCulture)
              + ",\"state\":\"" + (Recording ? "Recording" : "Scheduled") + "\",\"enabled\":true}";
        }
      }
    }

    /// <summary>
    /// A TVHeadend whose upcoming table a test rewrites between looks, standing for the thirty seconds in which a
    /// programme began or an entry left the table because the machine had finished with it.
    /// </summary>
    private class ScriptedDvr : HttpMessageHandler
    {
      internal readonly List<Entry> Upcoming = new List<Entry>();

      /// <summary>How many times the table was read, which is how often this watched.</summary>
      internal int Looks;

      /// <summary>A status to answer everything with, standing for a machine that is not there.</summary>
      internal HttpStatusCode? RefuseWith;

      /// <summary>Set when the questions are to fail on the way rather than be answered badly.</summary>
      internal bool Nowhere;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;

        if (Nowhere)
          throw new HttpRequestException("no route to the television");

        if (RefuseWith.HasValue)
          return Task.FromResult(new HttpResponseMessage(RefuseWith.Value)
          {
            RequestMessage = request,
            Content = new StringContent("")
          });

        if (path.EndsWith("/api/dvr/entry/grid_upcoming", StringComparison.Ordinal))
        {
          Looks++;
          StringBuilder body = new StringBuilder("{\"totalCount\":")
              .Append(Upcoming.Count.ToString(CultureInfo.InvariantCulture))
              .Append(",\"dlrEntries\":[");

          for (int i = 0; i < Upcoming.Count; i++)
          {
            if (i > 0)
              body.Append(',');
            body.Append(Upcoming[i].Json);
          }

          body.Append("]}");

          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
          });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent("{\"total\":0,\"entries\":[]}", Encoding.UTF8, "application/json")
        });
      }
    }

    /// <summary>
    /// The waiting, taken out of the watcher and held here. A test of a sentence about a recording should not take
    /// the half minute between looks, and a test of that sentence going away again should not take the five seconds
    /// the viewer asked for; so the watcher hands its next look and its next clearing to this, and a test fires them
    /// when it wants to.
    /// </summary>
    private sealed class Clock
    {
      internal readonly List<KeyValuePair<Action, TimeSpan>> Pending =
          new List<KeyValuePair<Action, TimeSpan>>();

      internal void Wake(Action action, TimeSpan after)
      {
        Pending.Add(new KeyValuePair<Action, TimeSpan>(action, after));
      }

      /// <summary>Fires the wait for the next look, and only that one.</summary>
      internal void LookAgain()
      {
        Fire(TimeSpan.FromSeconds(TvRecordingWatcher.LOOK_SECONDS));
      }

      /// <summary>Fires whatever was scheduled for the given length of waiting: a sentence's lifetime.</summary>
      internal void Fire(TimeSpan after)
      {
        for (int i = 0; i < Pending.Count; i++)
        {
          if (Pending[i].Value != after)
            continue;

          Action action = Pending[i].Key;
          Pending.RemoveAt(i);
          action();
          return;
        }

        Assert.Fail("nothing was scheduled for " + after.TotalSeconds + " seconds; the clock holds "
            + Pending.Count + " waits");
      }

      internal int WaitingFor(TimeSpan after)
      {
        return Pending.Count(pair => pair.Value == after);
      }
    }

    private static TvBackendSettings WrittenDown(bool started = true, bool ended = true, double seconds = 5)
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      settings.ShowRecordingStartedNotifications = started;
      settings.ShowRecordingEndedNotifications = ended;
      settings.RecordingNotificationSeconds = seconds;
      return settings;
    }

    /// <summary>
    /// A watcher over a scripted machine, with its waiting in a test's hands. The settings are handed over as a
    /// function, so that a test can change the answers between looks the way the settings screen does.
    /// </summary>
    private static TvRecordingWatcher Watching(ScriptedDvr backend, TvBackendSettings settings, Clock clock)
    {
      return new TvRecordingWatcher(delegate { return settings; },
          delegate (TvBackendSettings s) { return new TvHeadendApiClient(s, new HttpClient(backend)); },
          null,
          clock.Wake);
    }

    /// <summary>
    /// Waits for an answer that was asked for by a look. The question is asked of a machine across a network and so
    /// is answered on a thread of its own, and a test that looked at the notice before the answer came back would
    /// only ever prove that the test was quicker than the network. It does not ask a question of its own, because
    /// then a test would be counting its own questions rather than the watcher's.
    /// </summary>
    private static void WaitUntil(TvRecordingWatcher watcher, Func<bool> until)
    {
      for (int attempt = 0; attempt < 200; attempt++)
      {
        if (until())
          return;
        Thread.Sleep(10);
      }

      Assert.Fail("the watcher did not get there: it last said '" + watcher.Notice + "' and is recording="
          + watcher.IsRecording);
    }

    private static void Table(ScriptedDvr backend, params Entry[] entries)
    {
      backend.Upcoming.Clear();
      foreach (Entry entry in entries)
        backend.Upcoming.Add(entry);
    }

    #endregion

    #region The first look, which says nothing

    [Test]
    public void TheFirstLookRemembersWithoutSayingAnything()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true),
          new Entry("dvr-2", "Tatort", "ZDF", false));
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.NoticeChanged += sentence => said.Add(sentence);
        watcher.Start();
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Assert.That(said.Count, Is.EqualTo(0),
            "a recording that began before this front end existed is not news about now");
        Assert.That(watcher.IsRecording, Is.True,
          "but the mark is lit, because that one is true at the moment it is drawn rather than about a moment");
      }
    }

    [Test]
    public void ABoxWithNoTelevisionWrittenDownClaimsNoRecording()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true));
      Clock clock = new Clock();

      using (TvRecordingWatcher watcher = Watching(backend, new TvBackendSettings(), clock))
      {
        watcher.Start();
        WaitUntil(watcher, delegate { return true; });

        Assert.That(backend.Looks, Is.EqualTo(0), "nothing was asked, because there was nobody to ask");
        Assert.That(watcher.IsRecording, Is.False);
        Assert.That(watcher.Notice, Is.Empty);
      }
    }

    #endregion

    #region What is said, and what is not

    [Test]
    public void ARecordingThatStartsIsAnnounced()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend);
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.NoticeChanged += sentence => said.Add(sentence);
        watcher.Start();
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return said.Count > 0; });

        Assert.That(said.Count, Is.EqualTo(1));
        Assert.That(said[0], Is.EqualTo("Recording started: Tagesschau on Das Erste"),
            "the reference's own sentence, with what and where beside it");
      }
    }

    [Test]
    public void ARecordingThatEndsIsAnnounced()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tatort", "ZDF", true));
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend);
        clock.LookAgain();
        WaitUntil(watcher, delegate { return said.Count > 0; });

        Assert.That(said.Count, Is.EqualTo(1));
        Assert.That(said[0], Is.EqualTo("Recording ended: Tatort on ZDF"));
      }
    }

    /// <summary>
    /// A timer that was merely waiting is not a recording that began, and one that moved out of the table because it
    /// never started is not one that ended. The difference is what a viewer would say happened, and the state in the
    /// row is what says it here.
    /// </summary>
    [Test]
    public void ATimerThatWasOnlyWaitingIsNotNews()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tatort", "ZDF", false));
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend, new Entry("dvr-1", "Tatort", "ZDF", false),
            new Entry("dvr-2", "Kaminkonzert", "Radio", false));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return backend.Looks > 1; });

        Assert.That(said.Count, Is.EqualTo(0));
        Assert.That(watcher.IsRecording, Is.False);
      }
    }

    [Test]
    public void BothSwitchesOffSayNothingAtAll()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend);
      Clock clock = new Clock();
      List<string?> said = new List<string?>();
      TvBackendSettings settings = WrittenDown(started: false, ended: false);

      using (TvRecordingWatcher watcher = Watching(backend, settings, clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return watcher.IsRecording; });

        Assert.That(said.Count, Is.EqualTo(0), "the viewer asked not to be told");
        Assert.That(watcher.IsRecording, Is.True, "and the mark in the header still says what is happening");

        // Switched back on, and the recording that started in the quiet is not announced late - the watching went on
        // underneath, so the watcher is not holding an afternoon of news for the moment the switches came back.
        said.Clear();
        settings.ShowRecordingStartedNotifications = true;
        Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true),
            new Entry("dvr-2", "Tatort", "ZDF", true));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return said.Count > 0; });

        Assert.That(said.Count, Is.EqualTo(1), "only the one that began after the switch came back");
        Assert.That(said[0], Does.Contain("Tatort"));
      }
    }

    [Test]
    public void ASecondIsTooShortToSayAnything()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend);
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(seconds: 0), clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return watcher.IsRecording; });

        Assert.That(said.Count, Is.EqualTo(0),
            "under a second of standing time the reference said nothing either, and nothing here does");
      }
    }

    /// <summary>
    /// Only one of the two halves of the question is worth asking: a household that switched off the notice for the
    /// endings kept the ones for the beginnings.
    /// </summary>
    [Test]
    public void AnEndingSwitchedOffIsNotSaidWhileABeginningStillIs()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tatort", "ZDF", true));
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(ended: false), clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend);
        clock.LookAgain();
        WaitUntil(watcher, delegate { return !watcher.IsRecording; });
        Assert.That(said.Count, Is.EqualTo(0));

        Table(backend, new Entry("dvr-2", "Tagesschau", "Das Erste", true));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return said.Count > 0; });
        Assert.That(said[0], Does.Contain("Recording started"));
      }
    }

    #endregion

    #region When a sentence goes away, and when a machine stops talking

    [Test]
    public void TheSentenceGoesAwayAgainAfterTheTimeTheViewerAskedFor()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend);
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(seconds: 7), clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return backend.Looks > 0; });

        Table(backend, new Entry("dvr-1", "Tagesschau", "Das Erste", true));
        clock.LookAgain();
        WaitUntil(watcher, delegate { return watcher.Notice.Length > 0; });

        Assert.That(clock.WaitingFor(TimeSpan.FromSeconds(7)), Is.EqualTo(1),
            "a sentence about a recording borrows the corner of the screen for the seconds it was promised");

        clock.Fire(TimeSpan.FromSeconds(7));

        Assert.That(said.Last(), Is.Null, "and then the corner is cleared again");
        Assert.That(watcher.Notice, Is.Empty);
      }
    }

    /// <summary>
    /// A machine that will not answer is not a set of recordings that ended. Without this, a household whose router
    /// blinked would be told that every recording they had asked for had ended, about recordings that are sitting on
    /// the disk quite happily.
    /// </summary>
    [Test]
    public void AMachineThatStopsAnsweringIsNotANewsOfEndedRecordings()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tatort", "ZDF", true));
      Clock clock = new Clock();
      List<string?> said = new List<string?>();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.Start();
        watcher.NoticeChanged += sentence => said.Add(sentence);
        WaitUntil(watcher, delegate { return watcher.IsRecording; });

        backend.Nowhere = true;
        clock.LookAgain();
        WaitUntil(watcher, delegate { return true; });

        Assert.That(said.Count, Is.EqualTo(0));
        Assert.That(watcher.IsRecording, Is.True,
            "the last thing that was truly known is still what is on the screen");

        // And when it answers again, the same entry is still the one that is recording: no late notice for it.
        backend.Nowhere = false;
        clock.LookAgain();
        WaitUntil(watcher, delegate { return backend.Looks > 1; });

        Assert.That(said.Count, Is.EqualTo(0));
      }
    }

    /// <summary>
    /// A five hundred from a TVHeadend is an answer, and an unfriendly one; it is treated exactly like silence, for
    /// the same reason.
    /// </summary>
    [Test]
    public void AMachineThatRefusesIsTreatedLikeOneThatIsSilent()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Table(backend, new Entry("dvr-1", "Tatort", "ZDF", true));
      Clock clock = new Clock();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.Start();
        WaitUntil(watcher, delegate { return watcher.IsRecording; });

        backend.RefuseWith = HttpStatusCode.InternalServerError;
        clock.LookAgain();
        WaitUntil(watcher, delegate { return true; });

        Assert.That(watcher.IsRecording, Is.True);
        Assert.That(watcher.Notice, Is.Empty);
      }
    }

    [Test]
    public void WatchingIsAskingEveryHalfMinuteAndNoMoreOften()
    {
      ScriptedDvr backend = new ScriptedDvr();
      Clock clock = new Clock();

      using (TvRecordingWatcher watcher = Watching(backend, WrittenDown(), clock))
      {
        watcher.Start();
        WaitUntil(watcher, delegate { return backend.Looks > 0; });
        Assert.That(clock.Pending.Count, Is.EqualTo(1), "the first look asked for the next one, and only that");

        clock.LookAgain();
        WaitUntil(watcher, delegate { return backend.Looks > 1; });
        Assert.That(backend.Looks, Is.EqualTo(2));
        Assert.That(clock.Pending.Count, Is.EqualTo(1));

        // Stopped, and the waits it already handed over are not turned into questions any more.
        watcher.Stop();
        clock.LookAgain();
        Thread.Sleep(30);
        Assert.That(backend.Looks, Is.EqualTo(2));
      }
    }

    #endregion

    #region The corner of the screen it is said in

    /// <summary>
    /// The chrome is a dumb surface: it holds the sentence it was handed and says whether it holds one, and it owns
    /// no timers of its own - a sentence's lifetime belongs to whoever raised it, so that two screens showing the
    /// same corner cannot each be counting down a different idea of when it goes away.
    /// </summary>
    [Test]
    public void TheHeaderHoldsWhateverSentenceItWasHanded()
    {
      ChromeInfo chrome = new ChromeInfo();

      Assert.That(chrome.HasRecordingNotice, Is.False);
      chrome.ReportRecordingNotice("Recording started: Tatort on ZDF");
      Assert.That(chrome.HasRecordingNotice, Is.True);
      Assert.That(chrome.RecordingNotice, Is.EqualTo("Recording started: Tatort on ZDF"));

      chrome.ReportRecordingNotice(null);
      Assert.That(chrome.HasRecordingNotice, Is.False);
    }

    [Test]
    public void TheHeaderCarriesTheRecordingMarkSeparatelyFromTheSentence()
    {
      ChromeInfo chrome = new ChromeInfo();

      chrome.ReportRecording(true);
      Assert.That(chrome.IsRecording, Is.True);
      Assert.That(chrome.HasRecordingNotice, Is.False,
          "a recording running is a fact about the moment, and outlives the sentence about it beginning");

      chrome.ReportRecording(false);
      Assert.That(chrome.IsRecording, Is.False);
    }

    [Test]
    public void AWatcherToldToSpeakOnAnotherThreadSpeaksThere()
    {
      List<Action> queued = new List<Action>();
      ChromeInfo chrome = new ChromeInfo(marshal: action => queued.Add(action));

      chrome.ReportRecordingNotice("Recording ended: Tatort on ZDF");
      chrome.ReportRecording(true);

      Assert.That(chrome.HasRecordingNotice, Is.False, "nothing has been drawn yet, because nothing has run yet");
      Assert.That(queued.Count, Is.EqualTo(2));

      foreach (Action action in queued)
        action();

      Assert.That(chrome.HasRecordingNotice, Is.True);
      Assert.That(chrome.IsRecording, Is.True);
    }

    #endregion
  }
}
