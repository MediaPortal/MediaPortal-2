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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Messaging;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Frontend
{
  /// <summary>
  /// The front end's ear for the server: the reader of the event stream, the frames it makes of the bytes, and
  /// what the home screen does when something arrives.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The other fixtures about the event stream are about the server (TestMediaEventsOverHttp). This one points the
  /// real client at the real controller and asks the questions that belong to this side of the wire: whether a
  /// stream that ends is opened again, whether a stream that is refused is left alone, and whether a screen that
  /// hears about a change actually goes and looks - which is the entire reason the endpoint exists.
  /// </para>
  /// <para>
  /// The waits are shortened rather than waited out. The client's patience and the screen's debounce are written
  /// for a person in front of a television and are exposed as exactly that, so a test can say "a tenth of a
  /// second" without changing what it measures.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMediaEventStream
  {
    private readonly List<IDisposable> _toDispose = new List<IDisposable>();

    private TimeSpan _oldRetry;
    private TimeSpan _oldCeiling;
    private TimeSpan _oldDebounce;
    private TimeSpan _oldFlush;
    private TimeSpan _oldHeartbeat;

    [SetUp]
    public void MakeTheWaitsShort()
    {
      _oldRetry = ServerApiClient.EventRetryTime;
      _oldCeiling = ServerApiClient.EventRetryCeiling;
      _oldDebounce = HomeViewModel.EVENT_REFRESH_DEBOUNCE;
      _oldFlush = MediaEventsController.FLUSH_TIME;
      _oldHeartbeat = MediaEventsController.HEARTBEAT_TIME;

      ServerApiClient.EventRetryTime = TimeSpan.FromMilliseconds(100);
      ServerApiClient.EventRetryCeiling = TimeSpan.FromMilliseconds(250);
      HomeViewModel.EVENT_REFRESH_DEBOUNCE = TimeSpan.FromMilliseconds(150);

      // Longer than the server's default, not shorter: several tests here send a burst of messages and then insist
      // the screen asked once, which is only a statement about coalescing if the burst was one burst on the wire.
      MediaEventsController.FLUSH_TIME = TimeSpan.FromSeconds(1);

      // This one is not about patience but about noticing. The server finds out that a client went away when it
      // next writes to it, and with the production heartbeat of fifteen seconds a test that closes its screen and
      // then shuts the server down waits on a request nobody is answering any more. Measured: the shutdown sat
      // there until the fixture learned to write sooner.
      MediaEventsController.HEARTBEAT_TIME = TimeSpan.FromMilliseconds(200);
    }

    [TearDown]
    public void PutTheWaitsBack()
    {
      ServerApiClient.EventRetryTime = _oldRetry;
      ServerApiClient.EventRetryCeiling = _oldCeiling;
      HomeViewModel.EVENT_REFRESH_DEBOUNCE = _oldDebounce;
      MediaEventsController.FLUSH_TIME = _oldFlush;
      MediaEventsController.HEARTBEAT_TIME = _oldHeartbeat;

      InProcessApi.RemoveMediaLibrary();
      ServiceRegistration.Remove<IMessageBroker>();

      foreach (IDisposable disposable in _toDispose)
        disposable.Dispose();
      _toDispose.Clear();
    }

    #region The client, against a server that is really answering

    [Test]
    public async Task TheScreenHearsWhatTheServerSays()
    {
      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());
      ServerApiClient client = ListeningClient();

      List<MediaEvent> heard = new List<MediaEvent>();
      List<MediaEventConnection> states = new List<MediaEventConnection>();
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _toDispose.Add(cancellation);

      Task watching = client.WatchMediaEventsAsync(
          delegate (MediaEvent mediaEvent)
          {
            lock (heard)
              heard.Add(mediaEvent);
            return Task.CompletedTask;
          },
          delegate (MediaEventConnection connection)
          {
            lock (states)
              states.Add(connection);
          },
          cancellation.Token);

      // Only after the hello, because the hello is what says the server has subscribed to its own broker; before
      // it, a published message is a message shouted at a server that was not yet listening.
      await Until(delegate { lock (heard) return heard.Count > 0; }, "the server to say hello");

      Guid film = Guid.NewGuid();
      MediaLibraryMessaging.SendMediaItemsAddedOrUpdatedMessage(film);
      await Until(delegate { lock (heard) return heard.Exists(e => e.Kind == MediaEvent.KIND_LIBRARY); },
          "the library change to arrive");

      MediaEvent library;
      lock (heard)
        library = heard.Find(e => e.Kind == MediaEvent.KIND_LIBRARY);

      Assert.That(library.Change, Is.EqualTo("media-items-changed"));
      Assert.That(library.Items, Is.EquivalentTo(new[] { film }),
          "the ids have to survive the trip as ids, or a screen cannot update one row and has to read the shelf again");
      Assert.That(library.IsKnown, Is.True);

      Assert.That(states, Has.Some.Property("IsConnected").EqualTo(true),
          "a screen that says \"listening\" has to have heard the server say it is listening");

      cancellation.Cancel();
      await watching;

      // Ending on a cancel is the ordinary ending, and it must be an ending rather than a complaint.
      Assert.That(watching.IsCompleted, Is.True);
    }

    [Test]
    public async Task AStreamThatEndsIsOpenedAgain()
    {
      // A server that closes the stream is the interesting case, because that is what a server restart, a proxy
      // with a timeout, and a laptop whose address changed all look like from here. Nothing on the wire tells the
      // client which it was, so what matters is that it comes back by itself.
      int opens = 0;
      int port = InProcessApi.GetFreePort();
      IDisposable host = SharedHttpPortHost.Start(
          new[] { string.Format("http://127.0.0.1:{0}/MediaPortal/Server", port) },
          services => { },
          app => app.Run(async context =>
          {
            Interlocked.Increment(ref opens);
            context.Response.ContentType = "text/event-stream";
            await context.Response.WriteAsync("event: hello\ndata: {\"kind\":\"hello\",\"protocol\":1}\n\n");
            // and the handler ends, which ends the response, which is the stream ending.
          }));
      _toDispose.Add(host);

      List<MediaEventConnection> states = new List<MediaEventConnection>();
      Uri baseUrl = new Uri(string.Format("http://127.0.0.1:{0}/MediaPortal/Server/", port));

      using (var client = new ServerApiClient(baseUrl))
      {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        _toDispose.Add(cancellation);

        Task watching = client.WatchMediaEventsAsync(
            delegate (MediaEvent mediaEvent) { return Task.CompletedTask; },
            delegate (MediaEventConnection connection)
            {
              lock (states)
                states.Add(connection);
            },
            cancellation.Token);

        await Until(delegate { return Volatile.Read(ref opens) >= 3; },
            "the client to ask for the stream again by itself");

        cancellation.Cancel();
        await watching;
      }

      lock (states)
      {
        Assert.That(states, Has.Some.Property("IsConnected").EqualTo(false),
            "the screen has to be told the stream went away, or it keeps showing what it last heard as if it were now");
        Assert.That(states.FindAll(s => !s.IsConnected)[0].RetryAfter, Is.GreaterThan(TimeSpan.Zero),
            "and it is worth saying when the next attempt is, because \"lost\" without a \"soon\" reads as \"gone\"");
      }
    }

    [Test]
    public async Task AServerThatRefusesTheStreamIsReportedAndNotRetried()
    {
      // The other half of the same rule: a stream that ends is worth opening again, a stream that is refused is a
      // fact about this client, and retrying a fact produces a screen that says nothing forever.
      //
      // The order matters. InProcessApi.Start() configures an open server of its own, so the settings have to be
      // locked after it came up - switched before, that call quietly switched them back, the stream was accepted,
      // and what was meant to prove a refusal proved only that a healthy stream never ends.
      InProcessApi api = InProcessApi.Start();
      _toDispose.Add(api);
      InProcessApi.RegisterSettings(false);

      ServerApiException? refusal = null;
      try
      {
        using (var client = new ServerApiClient(api.BaseUrl))
        using (var patience = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
          // Waited on, not fired and forgotten: the point is that this call comes back by itself. The deadline is
          // not part of the expectation but a witness - if the refusal ever stops arriving, the test says so in ten
          // seconds instead of holding the fixture open until the runner gives up on the whole run.
          try
          {
            await client.WatchMediaEventsAsync(
                delegate (MediaEvent mediaEvent) { return Task.CompletedTask; },
                null,
                patience.Token);
          }
          catch (ServerApiException e)
          {
            refusal = e;
          }

          Assert.That(patience.IsCancellationRequested, Is.False,
              "a refused stream is an answer, and an answer does not have to be timed out to be noticed");
        }
      }
      finally
      {
        InProcessApi.RegisterSettings(true);
      }

      Assert.That(refusal, Is.Not.Null,
          "a refusal has to arrive as the same kind of answer every other endpoint gives, or the screen has to "
          + "learn a second way of being told no");
      Assert.That(refusal.StatusCode, Is.EqualTo((int) HttpStatusCode.Forbidden),
          "and the reason has to survive, because \"the server said no\" and \"this server needs a password\" are "
          + "different things to say to a person standing at a television");
    }

    [Test]
    public void WaitingGrowsButNeverWithoutLimit()
    {
      Assert.That(ServerApiClient.EventRetryDelay(0), Is.EqualTo(ServerApiClient.EventRetryTime),
          "a server that restarted a moment ago is back within the first wait, so the first wait is short");
      Assert.That(ServerApiClient.EventRetryDelay(1), Is.GreaterThan(ServerApiClient.EventRetryDelay(0)),
          "a server still missing after that is not coming back in the time it took before");
      Assert.That(ServerApiClient.EventRetryDelay(40), Is.EqualTo(ServerApiClient.EventRetryCeiling),
          "a front end on a television that cannot reach its server may be alone for hours; asking every "
          + "half-minute is enough for anybody");
    }

    #endregion

    #region The screen's side

    [Test]
    public async Task WhenTheServerSaysTheLibraryChangedTheScreenLooksAgain()
    {
      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());
      List<IEnumerable<Guid>> counted = ReadCounted();

      InProcessApi.RegisterSettings(true);
      using (InProcessApi api = InProcessApi.Start())
      using (var client = new ServerApiClient(api.BaseUrl))
      {
        HomeViewModel home = new HomeViewModel(client);

        await home.RefreshAsync();
        int afterTheFirstRead = counted.Count;
        Assert.That(afterTheFirstRead, Is.EqualTo(2), "two aspects, counted once each");

        // No marshaller: this screen has no thread of its own, so what arrives is applied where it arrived. The
        // running front end hands the home screen the dispatcher, which is what its own wiring does.
        home.WatchServerEvents();
        await Until(delegate { return home.ServerActivity == "Listening to the server"; },
            "the screen to hear that it is connected");

        MediaLibraryMessaging.SendMediaItemsDeletedMessage();

        await Until(delegate { return counted.Count > afterTheFirstRead; },
            "the screen to ask the server again after being told the library changed");

        Assert.That(home.ServerActivity, Is.EqualTo("Listening to the server"),
            "and the line about what the server is doing stays truthful through a refresh");

        home.StopWatchingEvents();
        Assert.That(home.IsWatchingEvents, Is.False);
      }
    }

    [Test]
    public async Task ABurstOfNewsIsOneLookAndNotSeveral()
    {
      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());
      List<IEnumerable<Guid>> counted = ReadCounted();

      InProcessApi.RegisterSettings(true);
      using (InProcessApi api = InProcessApi.Start())
      using (var client = new ServerApiClient(api.BaseUrl))
      {
        HomeViewModel home = new HomeViewModel(client);
        await home.RefreshAsync();
        int first = counted.Count;

        home.WatchServerEvents();
        await Until(delegate { return home.ServerActivity == "Listening to the server"; },
            "the screen to connect");

        // An import finishing: the library says items changed, then that watch states changed, then the importer
        // says it is done. A screen that read the server after each one would ask three times about one event.
        MediaLibraryMessaging.SendMediaItemsDeletedMessage();
        MediaLibraryMessaging.SendMediaItemUserDataAddedOrUpdateMessage(Guid.NewGuid());
        SendImport(ImporterWorkerMessaging.MessageType.ImportCompleted, "D:/films");

        await Until(delegate { return counted.Count > first; }, "the screen to react to the news");

        // And to prove it stopped there: another second and a half of silence, which at this server's flush
        // interval is several windows, and nothing new may arrive in them.
        await Task.Delay(1500);

        Assert.That(counted.Count, Is.EqualTo(first + 2),
            "three pieces of news about one import are one refresh - which is two counts here, one per aspect - "
            + "but the server was asked " + counted.Count / 2 + " times");

        home.StopWatchingEvents();
      }
    }

    /// <summary>
    /// A media library that records every count it is asked for, which is how these tests count what the screen
    /// asked without the screen knowing it is being counted.
    /// </summary>
    private static List<IEnumerable<Guid>> ReadCounted()
    {
      List<IEnumerable<Guid>> counted;
      Mock<IMediaLibrary> library = InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 2, out counted);
      InProcessApi.RegisterMediaLibrary(library.Object);
      return counted;
    }

    private ServerApiClient ListeningClient()
    {
      ReadCounted();

      InProcessApi.RegisterSettings(true);
      InProcessApi api = InProcessApi.Start();
      _toDispose.Add(api);

      var client = new ServerApiClient(api.BaseUrl);
      _toDispose.Add(client);
      return client;
    }

    private static void SendImport(ImporterWorkerMessaging.MessageType type, string path)
    {
      SystemMessage message = new SystemMessage(type);
      message.MessageData[ImporterWorkerMessaging.RESOURCE_PATH] =
          ResourcePath.BuildBaseProviderPath(Guid.NewGuid(), "/" + path);
      ServiceRegistration.Get<IMessageBroker>().Send(ImporterWorkerMessaging.CHANNEL, message);
    }

    #endregion

    #region What the frames mean before anybody acts on them

    [Test]
    public void ALinesBodyIsJoinedAndItsOneSpaceIsNotPartOfIt()
    {
      ServerEventFramer framer = new ServerEventFramer();

      Assert.That(framer.Feed("event: library"), Is.Null);
      Assert.That(framer.Feed("data: {\"a\":"), Is.Null);
      Assert.That(framer.Feed("data: 1}"), Is.Null);
      Tuple<string?, string> frame = framer.Feed(string.Empty);

      Assert.That(frame, Is.Not.Null, "a blank line is what ends a frame");
      Assert.That(frame!.Item1, Is.EqualTo("library"));
      Assert.That(frame.Item2, Is.EqualTo("{\"a\":\n1}"),
          "the data lines of one frame are joined with a newline, which is what lets a server send a body over "
          + "several lines; a client that ran them together without one would be reading a different document");
      Assert.That(framer.HasContent, Is.False, "and the framer starts the next frame clean");

      ServerEventFramer spacing = new ServerEventFramer();
      spacing.Feed("data:  x");
      Assert.That(spacing.Feed(string.Empty)!.Item2, Is.EqualTo(" x"),
          "exactly one space after the colon belongs to the protocol, so a second one is part of the value");
    }

    [Test]
    public void ACommentIsNotAFrameAndEndsNothing()
    {
      ServerEventFramer framer = new ServerEventFramer();

      Assert.That(framer.Feed(": keepalive"), Is.Null);
      Assert.That(framer.Feed(string.Empty), Is.Null,
          "the blank line after a keepalive must not be mistaken for the end of a frame, or every keepalive "
          + "would look like an empty event");

      framer.Feed("data: {\"kind\":\"gap\",\"dropped\":2}");
      Tuple<string?, string>? frame = framer.Feed(string.Empty);
      Assert.That(frame, Is.Not.Null);
      Assert.That(MediaEvent.Parse(frame!.Item2).Dropped, Is.EqualTo(2));
    }

    [Test]
    public void AFrameFromANewerServerIsIgnoredAndNotFatal()
    {
      MediaEvent mediaEvent = MediaEvent.Parse("{\"kind\":\"timer-fired\",\"channel\":\"tv\"}");

      Assert.That(mediaEvent.IsKnown, Is.False);
      Assert.That(mediaEvent.ShouldRefresh, Is.False,
          "a frame this client has never seen means this client keeps showing what it has, not that it clears it");
      Assert.That(mediaEvent.ToString(), Does.Contain("not understood"),
          "and it says what happened rather than pretending to be nothing");
    }

    [Test]
    public void OneUnreadableIdCostsOneIdAndNotTheFrame()
    {
      MediaEvent mediaEvent = MediaEvent.Parse(
          "{\"kind\":\"library\",\"change\":\"media-items-changed\",\"items\":[\"not-a-guid\",\"" +
          new Guid("11111111-1111-1111-1111-111111111111").ToString("N") + "\"]}");

      Assert.That(mediaEvent.Items, Has.Count.EqualTo(1),
          "the server's ids are a courtesy; losing the whole frame over one this build cannot read would throw "
          + "away the news that the library changed");
      Assert.That(mediaEvent.ShouldRefresh, Is.True);
    }

    [Test]
    public void OnlyTheNewsThatCanStaleTheScreenAsksForARefresh()
    {
      Assert.That(Parse("library", "change", "media-items-deleted").ShouldRefresh, Is.True);
      Assert.That(MediaEvent.Parse("{\"kind\":\"gap\",\"dropped\":1}").ShouldRefresh, Is.True);
      Assert.That(Parse("import", "state", "completed").ShouldRefresh, Is.True);
      Assert.That(Parse("import", "state", "started").ShouldRefresh, Is.False,
          "an import that has started has not put anything on the shelf yet");
      Assert.That(Parse("import", "state", "status").ShouldRefresh, Is.False,
          "and one that is on its 400th directory is not a reason to read the library 400 times");
      Assert.That(MediaEvent.Parse("{\"kind\":\"hello\",\"protocol\":1}").ShouldRefresh, Is.False);
    }

    [Test]
    public void ProgressNumbersAreReadAsNumbersAndNotAsText()
    {
      MediaEvent mediaEvent = MediaEvent.Parse(
          "{\"kind\":\"import-progress\",\"jobs\":[{\"path\":\"{A}:///%2FD:/films\",\"identified\":200,\"completed\":20}]}");

      Assert.That(mediaEvent.Jobs, Has.Count.EqualTo(1));
      Assert.That(mediaEvent.Jobs[0].Completed, Is.EqualTo(20));
      Assert.That(mediaEvent.Jobs[0].Fraction, Is.EqualTo(0.1d).Within(0.001d),
          "the bar a viewer watches is this ratio, and a client showing the raw counters would show a library "
          + "going backwards as the importer's estimate of the total grows");

      Assert.That(new MediaImportJob(null, 0, 5).Fraction, Is.Null,
          "and a server that has not counted anything yet is not a bar at zero, it is no bar at all");
    }

    [Test]
    public void ABodyThatIsNotAFrameSaysSoRatherThanArrivingAsNothing()
    {
      Assert.That(() => MediaEvent.Parse("<html>a proxy's error page</html>"), Throws.InstanceOf<FormatException>());
      Assert.That(() => MediaEvent.Parse(string.Empty), Throws.InstanceOf<FormatException>());
      Assert.That(() => MediaEvent.Parse("{}"), Throws.InstanceOf<FormatException>(),
          "a frame with no kind is not a frame; guessing from the event line alone is how a screen ends up "
          + "refreshing on something that meant nothing");
    }

    private static MediaEvent Parse(string kind, string field, string value)
    {
      return MediaEvent.Parse("{\"kind\":\"" + kind + "\",\"" + field + "\":\"" + value + "\"}");
    }

    #endregion

    #region Waiting for the other end

    private static async Task Until(Func<bool> condition, string whatItWasWaitingFor)
    {
      DateTime deadline = DateTime.UtcNow.AddSeconds(20);
      while (!condition() && DateTime.UtcNow < deadline)
        await Task.Delay(25);

      Assert.That(condition(), Is.True, "timed out waiting for {0}", whatItWasWaitingFor);
    }

    #endregion
  }
}