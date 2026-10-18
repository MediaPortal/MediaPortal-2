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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The short trip from "the viewer stopped the film" to "the library knows", and what the front end says about
  /// how it went.
  /// </summary>
  /// <remarks>
  /// The playback screen is already tested for knowing when a viewing ended, and the client is tested for the HTTP
  /// itself. What lives in between - a report is started without waiting for the server, and whatever comes back is
  /// said out loud rather than dropped - is the part a viewer notices either as a tick on a poster or as a tick that
  /// quietly never arrives, and it is the part that sits in an application object no test can open. It is here so
  /// that "the application said nothing at all" is a failure a build catches rather than a story from a sofa.
  /// </remarks>
  [TestFixture]
  public class TestPlaybackReporter
  {
    private static readonly Guid TITLE_ID = new Guid("88888888-8888-8888-8888-888888888888");
    private static readonly Uri SERVER = new Uri("http://127.0.0.1:1/MediaPortal/Server/");

    private FakePlayer _player = null!;
    private NowPlayingViewModel _screen = null!;
    private readonly Queue<Action> _queued = new Queue<Action>();
    private readonly List<string> _said = new List<string>();

    /// <summary>
    /// What the library said about each title a report was taken for, in the order the reports came back. The
    /// screens are handed the server's own answer rather than the id alone, so that a row can say what the library
    /// says instead of working out for itself what a percentage means.
    /// </summary>
    private readonly List<MediaPlayback> _answers = new List<MediaPlayback>();

    [SetUp]
    public void SetUp()
    {
      // The fixture is one instance reused for every test, so what a test heard is cleared here rather than
      // trusted to arrive empty: a sentence left over from the previous viewing would be read as this one.
      _said.Clear();
      _answers.Clear();
      _queued.Clear();

      _player = new FakePlayer();
      _screen = new NowPlayingViewModel(_player, _queued.Enqueue);
      Pump();
    }

    [TearDown]
    public void TearDown()
    {
      _screen.Dispose();
    }

    private void Pump()
    {
      while (_queued.Count > 0)
        _queued.Dequeue()();
    }

    private MediaLocation SomeFile(string name)
    {
      return MediaLocation.FromFile(Path.Combine(Path.GetTempPath(), "mp2-report", name));
    }

    /// <summary>Watches half of a film and stops it, which is the report every test here is about.</summary>
    private void WatchHalfAFilmAndStop()
    {
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(30_000, 60_000);
      Pump();

      _screen.Stop();
      Pump();
    }

    /// <summary>
    /// A server that answers the report with what the test names, and writes down the question it was asked.
    /// </summary>
    private class PlaybackServer : HttpMessageHandler
    {
      private readonly HttpStatusCode _code;
      private readonly string _body;

      internal PlaybackServer(HttpStatusCode code, string body)
      {
        _code = code;
        _body = body;
      }

      internal string? Method { get; private set; }
      internal string? Path { get; private set; }
      internal string? Body { get; private set; }
      internal int TimesAsked { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        Method = request.Method.Method;
        Path = request.RequestUri!.AbsolutePath;
        Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellation);
        TimesAsked++;

        return new HttpResponseMessage(_code)
        {
          RequestMessage = request,
          Content = new StringContent(_body, Encoding.UTF8, "application/json")
        };
      }
    }

    private static readonly string ANSWER =
        "{\"id\":\"" + TITLE_ID + "\",\"title\":\"Alien Earth\",\"watched\":false,\"playCount\":3,"
        + "\"lastPlayed\":\"2026-02-14T22:05:00\",\"resume\":50}";

    private PlaybackReporter ReportTo(Func<ServerApiClient?> clients)
    {
      return new PlaybackReporter(_screen, clients, _said.Add,
          delegate (Guid mediaItemId, MediaPlayback after)
          {
            _answers.Add(after);
          });
    }

    [Test]
    public async Task AViewingIsSentToTheServerAndWhatItSaysComesBackToTheViewer()
    {
      PlaybackServer server = new PlaybackServer(HttpStatusCode.OK, ANSWER);
      using (var client = new ServerApiClient(SERVER, new HttpClient(server)))
      using (PlaybackReporter reporter = ReportTo(delegate { return client; }))
      {
        WatchHalfAFilmAndStop();

        Assert.That(reporter.LastReport, Is.Not.Null, "the report was started");
        await reporter.LastReport!;

        Assert.That(server.Method, Is.EqualTo("POST"));
        Assert.That(server.Path, Does.EndWith("/api/media/items/" + TITLE_ID + "/playback"));
        Assert.That(server.Body, Is.EqualTo("{\"percentage\":50}"),
            "the number is the content of the request, not a way of choosing which title");
        Assert.That(server.TimesAsked, Is.EqualTo(1));

        Assert.That(_said, Has.Count.EqualTo(1), "one viewing, one sentence about it");
        Assert.That(_said[0], Does.Contain("50%"));
        Assert.That(_said[0], Does.Contain("played 3 time(s)"),
            "what the library says now, which is the only count that matters");
      }
    }

    [Test]
    public async Task AScreenShowingTheTitleIsToldWhatTheLibraryNowSays()
    {
      PlaybackServer server = new PlaybackServer(HttpStatusCode.OK, ANSWER);
      using (var client = new ServerApiClient(SERVER, new HttpClient(server)))
      using (PlaybackReporter reporter = ReportTo(delegate { return client; }))
      {
        WatchHalfAFilmAndStop();
        await reporter.LastReport!;

        Assert.That(_answers, Has.Count.EqualTo(1),
            "the row on screen was moved by this report, so the screen hearing about it is the point");
        MediaPlayback said = _answers[0];
        Assert.That(said.Id, Is.EqualTo(TITLE_ID));
        Assert.That(said.PlayCount, Is.EqualTo(3), "the library's count, not the one the screen inferred");
        Assert.That(said.Watched, Is.False, "stopped halfway, and the server is the one that says so");
        Assert.That(said.Resume, Is.EqualTo(50),
            "the position to come back to travels with the answer, which is why a row can be rewritten from it "
            + "instead of asking the server what it has just been told");
        Assert.That(said.LastPlayed, Is.EqualTo(new DateTime(2026, 2, 14, 22, 5, 0)));
      }
    }

    [Test]
    public async Task AServerThatRefusesTheReportIsAnsweredWithItsOwnWords()
    {
      PlaybackServer server = new PlaybackServer(HttpStatusCode.Forbidden,
          "{\"error\":\"this server asks a client to sign in before it accepts a viewing\"}");
      using (var client = new ServerApiClient(SERVER, new HttpClient(server)))
      using (PlaybackReporter reporter = ReportTo(delegate { return client; }))
      {
        WatchHalfAFilmAndStop();
        await reporter.LastReport!;

        Assert.That(_said, Has.Count.EqualTo(1));
        Assert.That(_said[0], Does.Contain("did not take the report"));
        Assert.That(_said[0], Does.Contain("sign in"),
            "the server's sentence, because a report lost in a status code teaches nobody anything");
        Assert.That(_answers, Is.Empty, "nothing changed on the server, so no screen has a reason to hear about it");
      }
    }

    [Test]
    public void AViewingWithNoServerToHearItIsSaidOutLoudRatherThanLost()
    {
      using (PlaybackReporter reporter = ReportTo(delegate { return null; }))
      {
        WatchHalfAFilmAndStop();

        Assert.That(reporter.LastReport, Is.Null, "there was nothing to start");
        Assert.That(_said, Has.Count.EqualTo(1));
        Assert.That(_said[0], Does.Contain("no server configured"));
        Assert.That(_answers, Is.Empty);
      }
    }

    [Test]
    public async Task TheServerAskedIsTheOneStandingThereWhenTheViewingEnded()
    {
      // The front end can be moved to another server while a film plays, and a report belongs to the server the
      // title was picked from - which is the one standing there at the moment the report is made, not the one that
      // existed when the player was built.
      PlaybackServer first = new PlaybackServer(HttpStatusCode.OK, ANSWER);
      PlaybackServer second = new PlaybackServer(HttpStatusCode.OK, ANSWER);
      using (var clientA = new ServerApiClient(SERVER, new HttpClient(first)))
      using (var clientB = new ServerApiClient(SERVER, new HttpClient(second)))
      {
        ServerApiClient? current = clientA;
        using (PlaybackReporter reporter = ReportTo(delegate { return current; }))
        {
          WatchHalfAFilmAndStop();
          await reporter.LastReport!;

          current = clientB;
          WatchHalfAFilmAndStop();
          await reporter.LastReport!;
        }
      }

      Assert.That(first.TimesAsked, Is.EqualTo(1));
      Assert.That(second.TimesAsked, Is.EqualTo(1), "and the report after the move went to the new server");
    }

    [Test]
    public void AReporterThatHasGoneStopsCarryingReports()
    {
      PlaybackServer server = new PlaybackServer(HttpStatusCode.OK, ANSWER);
      using (var client = new ServerApiClient(SERVER, new HttpClient(server)))
      {
        PlaybackReporter reporter = ReportTo(delegate { return client; });
        reporter.Dispose();

        WatchHalfAFilmAndStop();

        Assert.That(server.TimesAsked, Is.EqualTo(0), "a closed application does not phone a server on its way out");
        Assert.That(_said, Is.Empty);
      }
    }
  }
}
