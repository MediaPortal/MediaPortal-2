#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Messaging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Server.Api;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// The server's event stream, read by a real client over a real socket while the server's own message broker is
  /// doing what the importer and the media library make it do.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What this is the proof of.</b> MP2's server has been announcing library changes and import progress all
  /// along, to anybody listening inside its own process; a client across the network had nothing to listen to, so
  /// a front end found out about a finished import by the user pressing a button. This fixture connects the way a
  /// front end connects, publishes the messages the way <c>ImporterWorker</c> and <c>MediaLibrary</c> publish
  /// them, and asserts what arrives - including the two things that are easy to get wrong and impossible to see
  /// afterwards: a slow client must not be able to hold up the server, and a client that missed something must be
  /// told so rather than left to display what it last heard.
  /// </para>
  /// <para>
  /// <b>Real broker, manufactured messages.</b> The broker is MP2's own <see cref="MessageBroker"/>, registered
  /// where the server looks for it, and the messages carry what the messaging classes put on them. The importer is
  /// not run: it walks a directory tree, needs shares and providers, and its messages are the thing under test -
  /// handing it a real library would test the importer, which other tests already do, and would make this one
  /// depend on files that have to exist wherever the tests happen to run.
  /// </para>
  /// <para>
  /// <b>The broker is wrapped, not replaced.</b> <see cref="WatchingBroker"/> forwards to the real broker and
  /// counts registrations, which is the only way to know that the server has started listening before publishing
  /// the first message (without that the test is a race) and the only way to see that it stopped when the client
  /// went away - a receiver left behind is a leak that outlives the connection, and nothing in the endpoint's
  /// answer would show it.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMediaEventsOverHttp
  {
    private const string PREFIX = "/MediaPortal/Server";

    private readonly List<IDisposable> _listeners = new List<IDisposable>();

    private string _baseUrl;
    private StringWriter _serverLog;
    private WatchingBroker _broker;

    [SetUp]
    public void ShortenTheWaits()
    {
      // The production waits are right for a person watching a screen and wrong for a test that has to sit
      // through them; the numbers themselves are not what these tests measure.
      MediaEventsController.FLUSH_TIME = TimeSpan.FromMilliseconds(50);
      MediaEventsController.HEARTBEAT_TIME = TimeSpan.FromMilliseconds(200);
    }

    [TearDown]
    public void StopEverythingAndPutTheNumbersBack()
    {
      MediaEventsController.FLUSH_TIME = TimeSpan.FromMilliseconds(500);
      MediaEventsController.HEARTBEAT_TIME = TimeSpan.FromSeconds(15);

      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();

      ServiceRegistration.Remove<IMessageBroker>();
      ServiceRegistration.Remove<IMediaLibrary>();
      ServiceRegistration.Remove<ISystemResolver>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
      ServiceRegistration.Remove<ILogger>();
    }

    #region Starting the server

    /// <summary>
    /// Starts the resource server's pipeline, with the real message broker behind the one the tests hand out.
    /// </summary>
    /// <param name="authorize">
    /// Whether the server wants to know its callers, which is its default and the case that decides whether an
    /// open-ended stream is a hole in it.
    /// </param>
    private void StartServer(bool authorize = false)
    {
      _serverLog = new StringWriter();
      ServiceRegistration.Set<ILogger>(new DefaultLogger(_serverLog, LogLevel.All, false, true));

      _broker = new WatchingBroker(new MessageBroker());
      ServiceRegistration.Set<IMessageBroker>(_broker);

      ServiceRegistration.Set<IMediaLibrary>(new Mock<IMediaLibrary>().Object);

      // The resource access module, which owns the API pipeline this endpoint is mounted in, works out the
      // server's own base path from the kind of system it is running on. Without this every request fails before
      // reaching a controller, and the failure looks like a routing problem.
      Mock<ISystemResolver> systemResolver = new Mock<ISystemResolver>();
      systemResolver.Setup(sr => sr.SystemType).Returns(SystemType.Server);
      ServiceRegistration.Set<ISystemResolver>(systemResolver.Object);

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load<ServerSettings>()).Returns(() => new ServerSettings
      {
        WebAutorizationEnabled = authorize
      });
      ServiceRegistration.Set<ISettingsManager>(settingsManager.Object);

      // The pipeline's authorization middleware asks for these two; the bearer handler only reaches the profile
      // store once somebody names themselves, which nobody below does.
      ServiceRegistration.Set<IThreadPool>(new Mock<IThreadPool>().Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(new Mock<IUserProfileDataManagement>().Object);

      ResourceServer resourceServer = new ResourceServer();
      resourceServer.AddHttpControllerAssembly(typeof(MediaController));

      int port = FreePort();
      _listeners.Add(SharedHttpPortHost.Start(new[] { "http://127.0.0.1:" + port + PREFIX },
          resourceServer.ConfigureApiServices, resourceServer.ConfigureApiPipeline));
      _baseUrl = "http://127.0.0.1:" + port + PREFIX;
    }

    static int FreePort()
    {
      TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    #endregion

    #region Tests

    [Test]
    public async Task TheFirstFrameSaysTheConnectionWorks()
    {
      StartServer();

      using (Subscription watch = await Listen())
      {
        Frame hello = await watch.Next();

        Assert.That(hello.Kind, Is.EqualTo(MediaEventsController.KIND_HELLO),
            "the first thing on a new stream should identify the protocol, not wait for something to happen");
        Assert.That(hello.Json.GetProperty("protocol").GetInt32(),
            Is.EqualTo(MediaEventsController.PROTOCOL_VERSION));
        Assert.That(hello.Json.GetProperty("serverTime").GetString(), Is.Not.Empty,
            "a client telling \"nothing happened\" apart from \"the server is on another clock\" needs the server's time");
      }
    }

    [Test]
    public async Task AChangedLibraryReachesAWaitingClient()
    {
      StartServer();
      Guid film = Guid.NewGuid();

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        // The message the media library itself sends, through the class the library calls.
        MediaLibraryMessaging.SendMediaItemsAddedOrUpdatedMessage(film);

        Frame library = await watch.Next(MediaEventsController.KIND_LIBRARY);
        Assert.That(library.Json.GetProperty("change").GetString(), Is.EqualTo("media-items-changed"));
        Assert.That(library.Json.GetProperty("items")[0].GetString(), Is.EqualTo(film.ToString("N")),
            "the ids a front end could use to update one row without refetching the shelf");
      }
    }

    [Test]
    public async Task ADeletedLibrarySaysSoWithoutNamingItems()
    {
      StartServer();

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        // MP2's delete message names nobody - the importer may have removed hundreds of items and the client is
        // going to ask again regardless.
        MediaLibraryMessaging.SendMediaItemsDeletedMessage();

        Frame library = await watch.Next(MediaEventsController.KIND_LIBRARY);
        Assert.That(library.Json.GetProperty("change").GetString(), Is.EqualTo("media-items-deleted"));
        Assert.That(library.Json.TryGetProperty("items", out JsonElement _), Is.False,
            "inventing an empty list where the server said nothing would look like a specific answer");
      }
    }

    [Test]
    public async Task TwoHundredDirectoriesArriveAsTheNewestOne()
    {
      StartServer();
      // The burst has to fit into one flush window or this measures the clock rather than the coalescing; see the
      // test about the missed frames for the same reason.
      MediaEventsController.FLUSH_TIME = TimeSpan.FromSeconds(1);

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        // What the importer actually does: one status message per directory it walks, each naming a different
        // directory. A front end cannot render two hundred of these one after another and neither can a wire
        // enjoy them, so what it gets is the one it would want: where the importer has got to.
        for (int i = 0; i < 200; i++)
          Send(ImporterWorkerMessaging.MessageType.ImportStatus, "D:/films/disc" + i);

        List<Frame> frames = await watch.UntilIdle();
        List<Frame> import = frames.FindAll(frame => frame.Kind == MediaEventsController.KIND_IMPORT);

        Assert.That(import, Has.Count.EqualTo(1),
            "two hundred status messages are one sentence to a viewer; got {0} frames: {1}",
            import.Count, Describe(frames));
        Assert.That(import[0].Json.GetProperty("path").GetString(), Does.Contain("disc199"),
            "the surviving frame has to be the newest one, or the screen sits naming a directory finished long ago");
      }
    }

    [Test]
    public async Task ProgressArrivesAsTheNumberItReached()
    {
      StartServer();
      MediaEventsController.FLUSH_TIME = TimeSpan.FromSeconds(1);

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        for (int completed = 0; completed < 200; completed++)
          SendProgress(1000, completed);

        List<Frame> frames = await watch.UntilIdle();
        List<Frame> progress = frames.FindAll(frame => frame.Kind == MediaEventsController.KIND_PROGRESS);

        Assert.That(progress, Has.Count.EqualTo(1),
            "a progress bar wants where things are, not every step; got {0} frame(s): {1}",
            progress.Count, Describe(frames));

        JsonElement job = progress[0].Json.GetProperty("jobs")[0];
        Assert.That(job.GetProperty("completed").GetInt32(), Is.EqualTo(199),
            "the number a client shows has to be the newest one, or the bar runs backwards");
        Assert.That(job.GetProperty("identified").GetInt32(), Is.EqualTo(1000));
      }
    }

    [Test]
    public async Task AnImportsOwnNewsIsNotEatenByAnotherShare()
    {
      StartServer();

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        Send(ImporterWorkerMessaging.MessageType.ImportStarted, "D:/films");
        Send(ImporterWorkerMessaging.MessageType.ImportStarted, "D:/music");
        Send(ImporterWorkerMessaging.MessageType.ImportCompleted, "D:/films");

        List<Frame> frames = await watch.UntilIdle();
        List<Frame> import = frames.FindAll(frame => frame.Kind == MediaEventsController.KIND_IMPORT);

        Assert.That(import, Has.Count.EqualTo(3),
            "started-on-films, started-on-music and finished-films are three different sentences: " + Describe(frames));

        List<string> states = import.ConvertAll(frame => frame.Json.GetProperty("state").GetString());
        Assert.That(states, Does.Contain("started").And.Contain("completed"),
            "the states a client needs in order to know an import is over: " + string.Join(", ", states));
      }
    }

    [Test]
    public async Task TooLittleRoomSaysSoInsteadOfKeepingQuiet()
    {
      StartServer();
      // Longer than the loop below takes, so that the burst is certainly one burst: with the 50 ms flush of the
      // other tests the server would empty its box halfway through and this would measure nothing.
      MediaEventsController.FLUSH_TIME = TimeSpan.FromSeconds(1);

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        // One frame per share, which is what a server does when a household's whole set of shares is scheduled at
        // once - imports that begin and end are news a client must be able to tell apart, so they are the frames
        // this server does not merge, and the ones that can run out of room.
        for (int i = 0; i < MediaEventsController.PENDING_LIMIT + 40; i++)
          Send(ImporterWorkerMessaging.MessageType.ImportStarted, "D:/shares/" + i);

        Frame gap = await watch.Next(MediaEventsController.KIND_GAP);

        Assert.That(gap.Json.GetProperty("dropped").GetInt32(), Is.GreaterThan(0),
            "and it is worth saying how much was missed, because the client's choice is between refetching and guessing");

        List<Frame> rest = await watch.UntilIdle();
        Assert.That(rest.FindAll(frame => frame.Kind == MediaEventsController.KIND_GAP), Has.Count.EqualTo(0),
            "one miss is said once; what arrived afterwards was " + Describe(rest));
      }
    }

    [Test]
    public async Task AnIdleConnectionIsKeptAlive()
    {
      StartServer();

      using (Subscription watch = await Listen())
      {
        await watch.Next();

        // Nothing happens, and the stream still has to say something: this is how a client, and any proxy between,
        // finds out that the other end is there.
        Frame keepalive = await watch.NextComment();

        Assert.That(keepalive.IsComment, Is.True,
            "an idle stream sends a comment line; the alternative is a client waiting forever on a dead socket");
      }
    }

    [Test]
    public async Task TheServerStopsListeningWhenTheClientGoes()
    {
      StartServer();

      Subscription watch = await Listen();
      await watch.Next();
      Assert.That(_broker.Registrations, Is.EqualTo(2),
          "the library channel and the importer channel, and no more than that per client");
      Assert.That(_broker.Unregistrations, Is.EqualTo(0));

      watch.Dispose();

      // The unregistration happens when the aborted request unwinds, which is a moment after the socket closes.
      DateTime deadline = DateTime.UtcNow.AddSeconds(10);
      while (_broker.Unregistrations < 2 && DateTime.UtcNow < deadline)
        await Task.Delay(50);

      Assert.That(_broker.Unregistrations, Is.EqualTo(2),
          "a receiver left on the broker keeps the request's state alive for the rest of the server's life, and "
          + "nothing about the connection would show it");

      // And the server goes on serving, with nothing listening.
      MediaLibraryMessaging.SendMediaItemsDeletedMessage();
      Assert.That(_broker.Sent, Is.EqualTo(1), "publishing with nobody listening is normal, not an error");
    }

    [Test]
    public async Task AServerThatWantsToKnowItsCallersRefusesTheStream()
    {
      StartServer(authorize: true);

      using (HttpClient client = new HttpClient())
      {
        using (HttpResponseMessage response = await client.GetAsync(_baseUrl + "/api/media/events"))
        {
          Assert.That((int)response.StatusCode, Is.EqualTo(403),
              "an open-ended connection into the server's own events is the same kind of door as a file server "
              + "that answers anybody, and the API already knows how to say no");
        }
      }
    }

    #endregion

    #region Publishing, the way the server does

    /// <summary>
    /// Puts an import message on the broker with the parameter <c>ImporterWorker</c> puts on it.
    /// </summary>
    private static void Send(ImporterWorkerMessaging.MessageType type, string path)
    {
      SystemMessage message = new SystemMessage(type);
      if (path != null)
        message.MessageData[ImporterWorkerMessaging.RESOURCE_PATH] =
            ResourcePath.BuildBaseProviderPath(Guid.NewGuid(), "/" + path);
      ServiceRegistration.Get<IMessageBroker>().Send(ImporterWorkerMessaging.CHANNEL, message);
    }

    /// <summary>
    /// Puts a progress message on the broker: one job, at the given numbers.
    /// </summary>
    private static void SendProgress(int identified, int completed)
    {
      ImportJobInformation job = new ImportJobInformation(ImportJobType.Import,
          ResourcePath.BuildBaseProviderPath(Guid.NewGuid(), "/D:/films"), new List<Guid>(), true);

      SystemMessage message = new SystemMessage(ImporterWorkerMessaging.MessageType.ImportProgress);
      message.MessageData[ImporterWorkerMessaging.IMPORT_PROGRESS] =
          new Dictionary<ImportJobInformation, Tuple<int, int>> { { job, Tuple.Create(identified, completed) } };
      ServiceRegistration.Get<IMessageBroker>().Send(ImporterWorkerMessaging.CHANNEL, message);
    }

    #endregion

    #region The other end of the socket

    private async Task<Subscription> Listen()
    {
      Subscription subscription = new Subscription(_baseUrl + "/api/media/events", _broker,
          delegate { return _serverLog.ToString(); });
      await subscription.Open();
      return subscription;
    }

    /// <summary>
    /// One frame off the wire: what the server called it, what it said, and whether it was the keepalive line.
    /// </summary>
    private sealed class Frame
    {
      internal Frame(string kind, JsonElement json, bool isComment, string text)
      {
        Kind = kind;
        Json = json;
        IsComment = isComment;
        Text = text;
      }

      internal string Kind { get; }
      internal JsonElement Json { get; }
      internal bool IsComment { get; }
      internal string Text { get; }

      public override string ToString()
      {
        return Text;
      }
    }

    /// <summary>
    /// An open event stream, read one frame at a time, with the timeout the tests need because a stream that never
    /// delivers is exactly the failure they are looking for.
    /// </summary>
    private sealed class Subscription : IDisposable
    {
      private readonly string _url;
      private readonly WatchingBroker _broker;
      private readonly Func<string> _serverLog;
      private readonly HttpClient _client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

      // The frame being assembled, kept between reads because a frame can span two calls.
      private string _kind;
      private StringBuilder _data = new StringBuilder();
      private bool _sawData;
      private bool _eof;

      private CancellationTokenSource _cancellation;
      private Stream _stream;
      private StreamReader _reader;

      /// <summary>A line read that is still in flight. See <see cref="ReadLine"/>.</summary>
      private Task<string> _pendingRead;

      internal Subscription(string url, WatchingBroker broker, Func<string> serverLog)
      {
        _url = url;
        _broker = broker;
        _serverLog = serverLog;
      }

      internal async Task Open()
      {
        _cancellation = new CancellationTokenSource();
        HttpResponseMessage response = await _client.GetAsync(_url,
            HttpCompletionOption.ResponseHeadersRead, _cancellation.Token);

        if (!response.IsSuccessStatusCode)
          throw new InvalidOperationException("the server answered " + (int)response.StatusCode +
              " to the event stream. It said:\n" + _serverLog());

        Assert.That(response.Content.Headers.ContentType.MediaType, Is.EqualTo("text/event-stream"),
            "a client that is told it is getting a stream stops buffering, and a proxy does too");

        _stream = await response.Content.ReadAsStreamAsync();
        _reader = new StreamReader(_stream, Encoding.UTF8);
      }

      /// <summary>
      /// The next data frame. When a kind is asked for, the frames before it are skipped, and the server is first
      /// waited into its subscription: without that the test publishes into a server that has not registered yet.
      /// </summary>
      /// <remarks>
      /// The deadline covers the whole wait, not one read. A live stream produces something every few hundred
      /// milliseconds even when it has nothing to say, so a timeout measured per read is a test that never ends.
      /// </remarks>
      internal async Task<Frame> Next(string expectedKind = null, int timeoutMs = 15000)
      {
        if (expectedKind != null)
        {
          DateTime registered = DateTime.UtcNow.AddSeconds(15);
          while (_broker.Registrations < 2 && DateTime.UtcNow < registered)
            await Task.Delay(20);
        }

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        List<Frame> passed = new List<Frame>();

        while (true)
        {
          int budget = Remaining(deadline);
          if (budget <= 0)
            Assert.Fail("no \"{0}\" frame within {1} ms. The stream said: {2}. The server said:\n{3}",
                expectedKind ?? "any", timeoutMs, Describe(passed), _serverLog());

          Frame frame = await ReadFrame(budget, true);
          if (frame == null)
            continue;
          if (frame.IsComment)
          {
            passed.Add(frame);
            continue;
          }

          if (expectedKind == null || frame.Kind == expectedKind)
            return frame;

          passed.Add(frame);
        }
      }

      /// <summary>The next keepalive line, ignoring anything else that comes.</summary>
      internal async Task<Frame> NextComment(int timeoutMs = 15000)
      {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (true)
        {
          int budget = Remaining(deadline);
          if (budget <= 0)
            Assert.Fail("the stream stayed silent for {0} ms, so nothing on it says the other end is alive. "
                + "The server said:\n{1}", timeoutMs, _serverLog());

          Frame frame = await ReadFrame(budget, true);
          if (frame != null && frame.IsComment)
            return frame;
        }
      }

      /// <summary>
      /// Everything the server has to say right now: read until the stream goes quiet, then stop. For the frames
      /// that must have collapsed into each other, where waiting for one named frame would pass without proving.
      /// </summary>
      /// <remarks>
      /// "Quiet" means quiet of <i>data</i>: a keepalive line is not something happening, and the server sends one
      /// every couple of hundred milliseconds in these tests, so counting it as activity would keep this reading
      /// until the heat death of the connection. Waiting for the very first frame is a separate budget, because
      /// until a flush interval has passed, an empty read means "not yet", not "nothing more".
      /// </remarks>
      internal async Task<List<Frame>> UntilIdle(int quietReads = 2, int firstWaitMs = 4000,
          int quietReadMs = 300)
      {
        List<Frame> frames = new List<Frame>();
        DateTime firstDeadline = DateTime.UtcNow.AddMilliseconds(firstWaitMs);
        int silence = 0;

        while (silence < quietReads)
        {
          bool waitingForTheFirst = frames.Count == 0;
          int budget = waitingForTheFirst
              ? (int)Math.Max(0, firstDeadline.Subtract(DateTime.UtcNow).TotalMilliseconds)
              : quietReadMs;
          if (budget <= 0)
            return frames;

          Frame frame = await ReadFrame(budget, true);
          if (frame == null || frame.IsComment)
          {
            if (!waitingForTheFirst)
              silence++;
            continue;
          }

          silence = 0;
          frames.Add(frame);
        }
        return frames;
      }

      private static int Remaining(DateTime deadline)
      {
        return (int)Math.Max(0, deadline.Subtract(DateTime.UtcNow).TotalMilliseconds);
      }

      /// <remarks>
      /// <para>
      /// A timeout here may not be a cancelled read. <c>HttpClient</c> reacts to the cancellation of a read on a
      /// response body by aborting the connection, so a per-read <c>CancellationToken</c> - the obvious way to
      /// write this, and the way this fixture first wrote it - silently ends the stream at the first quiet moment:
      /// every read after it reports a disposed <c>NetworkStream</c>. Measured, after it had looked like a server
      /// that stopped answering.
      /// </para>
      /// <para>
      /// So the read is started once and kept. Timing out means "nothing has arrived yet, ask me again", and the
      /// unfinished read is still there for the next caller to wait on; nothing on the wire is interrupted.
      /// </para>
      /// </remarks>
      private async Task<Frame> ReadFrame(int timeoutMs, bool nullOnTimeout = false)
      {
        while (true)
        {
          string line = await ReadLine(timeoutMs);
          if (line == null)
          {
            if (_eof && !_sawData)
            {
              if (nullOnTimeout)
                return null;
              Assert.Fail("the server closed the event stream. It said:\n{0}", _serverLog());
              return null;
            }

            if (nullOnTimeout)
              return null;
            Assert.Fail("nothing arrived on the event stream within {0} ms. The server said:\n{1}",
                timeoutMs, _serverLog());
            return null;
          }

          if (line.Length == 0)
          {
            if (!_sawData)
              // A frame with nothing but the blank line after it: the tail of a keepalive.
              return new Frame(null, default, true, ":");

            string assembled = _data.ToString();
            string named = _kind;
            _data = new StringBuilder();
            _kind = null;
            _sawData = false;
            return Parse(named, assembled);
          }

          if (line.StartsWith(":", StringComparison.Ordinal))
            return new Frame(null, default, true, line);
          if (line.StartsWith("event:", StringComparison.Ordinal))
            _kind = line.Substring(6).Trim();
          else if (line.StartsWith("data:", StringComparison.Ordinal))
          {
            _data.Append(line.Substring(5).Trim());
            _sawData = true;
          }
        }
      }

      /// <summary>
      /// The next line, or null when <paramref name="timeoutMs"/> passed without one. The read outlives the call.
      /// </summary>
      private async Task<string> ReadLine(int timeoutMs)
      {
        if (_pendingRead == null)
          _pendingRead = _reader.ReadLineAsync();

        Task finished = await Task.WhenAny(_pendingRead, Task.Delay(timeoutMs));
        if (!ReferenceEquals(finished, _pendingRead))
          return null;

        Task<string> read = _pendingRead;
        _pendingRead = null;
        string value = await read;
        if (value == null)
          // The other end is gone. Remembered, because a read that answers "nothing" immediately would otherwise
          // look like a quiet moment for as long as the caller is willing to wait for one.
          _eof = true;
        return value;
      }

      private static Frame Parse(string kind, string data)
      {
        using (JsonDocument document = JsonDocument.Parse(data))
        {
          JsonElement root = document.RootElement.Clone();
          string announced = root.TryGetProperty("kind", out JsonElement kindField)
              ? kindField.GetString()
              : null;

          if (kind != null && announced != null && kind != announced)
            Assert.Fail("a frame says \"{0}\" in the protocol and \"{1}\" in its body - a client that filters on "
                + "one and reads the other gets nothing", kind, announced);

          return new Frame(announced ?? kind, root, false, data);
        }
      }

      public void Dispose()
      {
        // A read still in flight faults the moment the socket goes away; that is the point of closing it, and an
        // exception nobody observes afterwards is noise a test run cannot act on.
        _pendingRead?.ContinueWith(task =>
        {
          AggregateException ignored = task.Exception;
        }, TaskContinuationOptions.OnlyOnFaulted);

        try
        {
          _cancellation?.Cancel();
          _reader?.Dispose();
          _stream?.Dispose();
        }
        catch (Exception)
        {
          // Closing is best effort; what the test measures is what the server does next.
        }
        finally
        {
          _client.Dispose();
        }
      }
    }

    private static string Describe(List<Frame> frames)
    {
      return frames.Count + " frame(s): " + string.Join(", ", frames.ConvertAll(f => f.Kind ?? "comment").ToArray());
    }

    #endregion

    #region The broker the tests watch

    /// <summary>
    /// Forwards to MP2's real broker and counts what goes through it, because the two things a test of a
    /// subscription cannot see from the outside are whether the subscription was there yet and whether it was
    /// taken away again.
    /// </summary>
    private sealed class WatchingBroker : IMessageBroker
    {
      private readonly IMessageBroker _inner;

      internal WatchingBroker(IMessageBroker inner)
      {
        _inner = inner;
      }

      internal int Registrations { get; private set; }
      internal int Unregistrations { get; private set; }
      internal int Sent { get; private set; }

      public void RegisterMessageReceiver(string channel, IMessageReceiver receiver)
      {
        Registrations++;
        _inner.RegisterMessageReceiver(channel, receiver);
      }

      public void UnregisterMessageReceiver(string channel, IMessageReceiver receiver)
      {
        Unregistrations++;
        _inner.UnregisterMessageReceiver(channel, receiver);
      }

      public void Send(string channelName, SystemMessage msg)
      {
        Sent++;
        _inner.Send(channelName, msg);
      }
    }

    #endregion
  }
}
