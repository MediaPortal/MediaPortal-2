#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// What the server says without being asked: the library changed, an import is running and this far in it is.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists.</b> MP2's server already announces everything a front end needs to know, in its own
  /// process: <see cref="MediaLibraryMessaging"/> says items were added, updated or deleted and
  /// <see cref="ImporterWorkerMessaging"/> says an import was scheduled, started, is this far along, and finished.
  /// The old client heard those because it <i>was</i> the process, or was sitting next to it. Over HTTP there was
  /// nothing between the message broker and a client, so a front end learned about a new film by being told to
  /// look again - which in practice means the user does it by pressing a button, and means the first-run moment
  /// ("I have just pointed the server at my films, now what?") is a screen that says nothing while the server
  /// files thousands of files.
  /// </para>
  /// <para>
  /// <b>Server-sent events, not a WebSocket.</b> The direction is one way: the server has something to say and the
  /// client's answer is to ask a normal HTTP question afterwards. Keeping it an ordinary HTTP GET means it runs
  /// through the same pipeline as every other endpoint - the same port, the same
  /// <see cref="MediaPortalAuthorizeAttribute"/>, the same listener on Windows and Linux, no second protocol for
  /// the portable host to grow - and unlike a media URL, an event stream is opened by our own client code, which
  /// can put a bearer token in a header. A WebSocket would buy nothing for that and cost a subprotocol, a
  /// middleware in the core assembly (where the pipeline lives but where the media library may not be referenced),
  /// and a keep-alive protocol of our own invention.
  /// </para>
  /// <para>
  /// <b>What a stream may not become.</b> The broker delivers messages on the thread that produced them, and a
  /// slow or dead client must not be able to stop an import. So the receiving end never writes: it only records
  /// what it was told, in a bounded space, and a separate loop writes when it can. Two properties follow and are
  /// the reason the state below is a dictionary of <i>latest</i> frames rather than a queue: a client that is not
  /// reading costs a fixed amount of memory, and an import that files five thousand films does not put five
  /// thousand frames on the wire, because there is no front end on earth that will render them one by one. What a
  /// client gets instead is the newest state plus, if it did miss something, an explicit <c>gap</c> telling it to
  /// refetch - a stream that silently drops is a stream whose consumers show stale data with confidence.
  /// </para>
  /// <para>
  /// <b>What is not here.</b> No history and no replay: connect, and the state is whatever the library endpoints
  /// say, which is why every frame carries no more information than "go and look". Other clients' playback
  /// (<c>ClientManagerMessaging</c>), TV timer state (no TV backend in this tree) and share errors are not
  /// bridged; each is a mapping of one more channel and none of them is the reason this endpoint exists.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class MediaEventsController : ControllerBase
  {
    /// <summary>The number a client sends nothing about and can compare against its own expectation.</summary>
    public const int PROTOCOL_VERSION = 1;

    /// <summary>
    /// How long a recorded change waits before it is written. Half a second is imperceptible to a person watching
    /// a screen and long enough that an importer's burst of messages becomes one frame.
    /// </summary>
    /// <remarks>
    /// Writable, like the cache times in <c>ResourceAccessModule</c>, so that a test measuring what a client
    /// receives does not have to spend ten seconds on a clock.
    /// </remarks>
    public static TimeSpan FLUSH_TIME = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// How often a comment line is written when nothing happened, so that an idle connection is known to be alive
    /// and a proxy that drops silent streams does not drop this one.
    /// </summary>
    public static TimeSpan HEARTBEAT_TIME = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How many distinct frames may be waiting at once. Beyond it, frames are counted as missed rather than
    /// growing the server's memory for a client that is not reading.
    /// </summary>
    public const int PENDING_LIMIT = 64;

    /// <summary>
    /// How many media item ids one library frame names. A client refreshes a whole shelf on any library frame, so
    /// the ids are a courtesy for one that wants to update a single row; they are not needed, which is what makes
    /// capping them cheap.
    /// </summary>
    public const int ITEMS_PER_FRAME = 50;

    /// <summary>
    /// The frame every connection gets first, before anything has happened.
    /// </summary>
    public const string KIND_HELLO = "hello";

    /// <summary>Media items were added, updated or deleted; a client that shows titles should ask again.</summary>
    public const string KIND_LIBRARY = "library";

    /// <summary>An import was scheduled, started, is running, finished or was canceled.</summary>
    public const string KIND_IMPORT = "import";

    /// <summary>How far the running imports have got, as the server last measured it.</summary>
    public const string KIND_PROGRESS = "import-progress";

    /// <summary>
    /// The server had more to say than this connection could be given. Everything before it was delivered; the
    /// client should assume it missed changes and refetch.
    /// </summary>
    public const string KIND_GAP = "gap";

    /// <summary>
    /// How the frames are written: no indentation, and only the properties declared below.
    /// </summary>
    /// <remarks>
    /// Explicit names on every property rather than a naming policy, because this is the contract a client is
    /// built against and it should be readable in one place without knowing what <c>JsonSerializerOptions</c>
    /// would have done to it.
    /// </remarks>
    internal static readonly JsonSerializerOptions FRAME_OPTIONS = new JsonSerializerOptions
    {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Keeps the connection open and forwards the server's own messages as frames.
    /// </summary>
    /// <remarks>
    /// The action returns <see cref="Task"/> rather than an <see cref="IActionResult"/> on purpose: by the time it
    /// returns, the response has been fully written (or the client has gone). MVC's job ends where the streaming
    /// begins.
    /// </remarks>
    /// <param name="cancellationToken">Aborted when the client disconnects or the server shuts down.</param>
    [HttpGet("events")]
    public async Task Events(CancellationToken cancellationToken)
    {
      HttpResponse response = Response;
      response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
      // "no-transform" for the intermediaries that would otherwise wait to buffer a response they think they may
      // optimize; "X-Accel-Buffering" is the same request phrased for nginx, which is what a server behind one
      // would otherwise sit behind.
      response.Headers["Cache-Control"] = "no-cache, no-store, no-transform";
      response.Headers["X-Accel-Buffering"] = "no";
      response.StatusCode = StatusCodes.Status200OK;

      BrokerListener listener = new BrokerListener();
      IMessageBroker broker = ServiceRegistration.Get<IMessageBroker>();
      bool onLibrary = false;
      bool onImporter = false;
      long sequence = 0;
      DateTime lastWrite = DateTime.UtcNow;

      try
      {
        broker.RegisterMessageReceiver(MediaLibraryMessaging.CHANNEL, listener);
        onLibrary = true;
        broker.RegisterMessageReceiver(ImporterWorkerMessaging.CHANNEL, listener);
        onImporter = true;

        ILogger log = ServiceRegistration.Get<ILogger>(false);
        log?.Debug("MediaEventsController: client started listening to the server's events");

        await Write(response, Render(++sequence, KIND_HELLO, BrokerListener.Hello()), cancellationToken)
            .ConfigureAwait(false);
        lastWrite = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
          try
          {
            await Task.Delay(FLUSH_TIME, cancellationToken).ConfigureAwait(false);
          }
          catch (OperationCanceledException)
          {
            break;
          }

          List<BrokerListener.Pending> frames = listener.Drain();
          if (frames.Count == 0 && DateTime.UtcNow - lastWrite < HEARTBEAT_TIME)
            continue;

          if (frames.Count == 0)
          {
            // A comment line. It carries nothing; it says the other end is still there, which is the only way
            // either side finds out that it is not.
            await Write(response, ": keepalive\n\n", cancellationToken).ConfigureAwait(false);
          }
          else
          {
            foreach (BrokerListener.Pending frame in frames)
              await Write(response, Render(++sequence, frame.Kind, frame.Body), cancellationToken)
                  .ConfigureAwait(false);
          }
          lastWrite = DateTime.UtcNow;
        }
      }
      catch (OperationCanceledException)
      {
        // The viewer closed the window, switched servers or the server is stopping. The commonest way this action
        // ends, and the least interesting.
      }
      catch (Exception ex)
      {
        // A client that dies mid-write surfaces as an IOException from the body, and there is nobody left to
        // tell. Anything else a listener or the pipeline threw is worth a line in the server's log.
        ILogger log = ServiceRegistration.Get<ILogger>(false);
        log?.Debug("MediaEventsController: the event stream ended", ex);
      }
      finally
      {
        // Unregistering is the whole of the cleanup, and it has to happen on every way out: a receiver left on a
        // channel is a receiver the broker keeps handing messages to, holding the request's state alive for as
        // long as the server runs.
        if (onLibrary)
          broker.UnregisterMessageReceiver(MediaLibraryMessaging.CHANNEL, listener);
        if (onImporter)
          broker.UnregisterMessageReceiver(ImporterWorkerMessaging.CHANNEL, listener);
      }
    }

    /// <summary>
    /// Writes and flushes one frame. The flush is the point of the method: without it the frame sits in Kestrel's
    /// buffer and the client waits for something that has already been produced.
    /// </summary>
    private static async Task Write(HttpResponse response, string text, CancellationToken cancellationToken)
    {
      await response.WriteAsync(text, cancellationToken).ConfigureAwait(false);
      await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// One server-sent event: an id so a client can tell repeats from new ones, the kind for anyone filtering at
    /// the protocol level, and the JSON body.
    /// </summary>
    /// <remarks>
    /// The kind is handed in rather than read back out of the body. The first version of this looked for a
    /// <c>{"kind":"..."}</c> prefix, and its own tests caught it producing <c>event: message</c> for every frame,
    /// because where the serializer puts the <c>kind</c> property of an inherited member is its own business. A
    /// frame is always stored together with the constant that names it, so saying it again costs nothing and cannot
    /// be wrong.
    /// </remarks>
    internal static string Render(long sequence, string kind, string body)
    {
      return "id: " + sequence.ToString(CultureInfo.InvariantCulture) + "\nevent: " + kind +
          "\ndata: " + body + "\n\n";
    }

    /// <summary>
    /// Listens on the server's own channels and remembers what it was told, without ever blocking the sender.
    /// </summary>
    /// <remarks>
    /// <see cref="Receive"/> runs on whichever thread the broker was on - the importer's thread for import
    /// messages, the thread that finished a database write for library messages - so it takes a lock, writes a
    /// few fields and returns. Everything slow happens in <see cref="Drain"/>, on the request's own thread.
    /// </remarks>
    internal sealed class BrokerListener : IMessageReceiver
    {
      /// <summary>The frame written before anything has happened, so a client knows it is really connected.</summary>
      public static string Hello()
      {
        return JsonSerializer.Serialize(new HelloFrame
        {
          Protocol = PROTOCOL_VERSION,
          ServerTime = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        }, FRAME_OPTIONS);
      }

      /// <summary>Frames waiting to be written, keyed so that the newest of a kind replaces the previous one.</summary>
      private readonly Dictionary<string, Pending> _latest = new Dictionary<string, Pending>();

      /// <summary>The order the waiting frames were first recorded in, so a burst reads as a sequence.</summary>
      private readonly List<string> _order = new List<string>();

      /// <summary>The ids named so far by each waiting library frame, and whether they ran out of room.</summary>
      private readonly Dictionary<string, List<string>> _libraryIds = new Dictionary<string, List<string>>();

      private int _dropped;
      private int _received;

      /// <summary>How many messages the broker delivered, including the ones that were coalesced away.</summary>
      public int Received
      {
        get { return _received; }
      }

      /// <summary>How many changes could not be kept because the client was not reading.</summary>
      public int Dropped
      {
        get { return _dropped; }
      }

      /// <inheritdoc/>
      public void Receive(SystemMessage message)
      {
        if (message == null)
          return;

        lock (_latest)
        {
          _received++;
          if (string.Equals(message.ChannelName, MediaLibraryMessaging.CHANNEL, StringComparison.Ordinal))
            OnLibrary(message);
          else if (string.Equals(message.ChannelName, ImporterWorkerMessaging.CHANNEL, StringComparison.Ordinal))
            OnImport(message);
        }
      }

      /// <summary>
      /// What to write next: everything recorded since the last call, in the order it started, with a gap frame
      /// first if anything had to be left out.
      /// </summary>
      public List<Pending> Drain()
      {
        lock (_latest)
        {
          List<Pending> frames = new List<Pending>(_order.Count + 1);
          if (_dropped > 0)
          {
            // Said before the frames that follow, because the one thing a client must not do is guess whether what
            // it received is everything.
            frames.Add(new Pending(KIND_GAP,
                JsonSerializer.Serialize(new GapFrame { Dropped = _dropped }, FRAME_OPTIONS)));
            _dropped = 0;
          }

          foreach (string key in _order)
            frames.Add(_latest[key]);

          _latest.Clear();
          _order.Clear();
          _libraryIds.Clear();
          return frames;
        }
      }

      private void OnLibrary(SystemMessage message)
      {
        MediaLibraryMessaging.MessageType type = (MediaLibraryMessaging.MessageType)message.MessageType;
        string change;
        List<string> ids = null;

        switch (type)
        {
          case MediaLibraryMessaging.MessageType.MediaItemsAddedOrUpdated:
            change = "media-items-changed";
            ids = ItemIds(message);
            break;
          case MediaLibraryMessaging.MessageType.MediaItemsDeleted:
            // The message carries no ids on purpose - a delete is followed by the client asking again anyway.
            change = "media-items-deleted";
            break;
          case MediaLibraryMessaging.MessageType.MediaItemsUserDataAddedOrUpdated:
            change = "user-data-changed";
            ids = ItemIds(message);
            break;
          default:
            return;
        }

        string key = KIND_LIBRARY + ":" + change;
        List<string> kept;
        if (!_libraryIds.TryGetValue(key, out kept))
        {
          kept = new List<string>();
          _libraryIds[key] = kept;
        }

        bool truncated = false;
        if (ids != null)
        {
          if (ids.Count > ITEMS_PER_FRAME)
            truncated = true;
          foreach (string id in ids)
          {
            if (kept.Count >= ITEMS_PER_FRAME)
              break;
            if (!kept.Contains(id))
              kept.Add(id);
          }
        }

        Store(key, KIND_LIBRARY, new LibraryFrame
        {
          Change = change,
          Items = kept.Count > 0 ? kept : null,
          ItemsTruncated = truncated ? true : (bool?)null
        });
      }

      private void OnImport(SystemMessage message)
      {
        ImporterWorkerMessaging.MessageType type = (ImporterWorkerMessaging.MessageType)message.MessageType;

        if (type == ImporterWorkerMessaging.MessageType.ImportProgress)
        {
          // Latest wins, always: progress is a number a viewer watches climb, not a list of moments.
          Store(KIND_PROGRESS, KIND_PROGRESS, new ProgressFrame { Jobs = Jobs(message) });
          return;
        }

        string state;
        switch (type)
        {
          case ImporterWorkerMessaging.MessageType.ImportScheduled:
            state = "scheduled";
            break;
          case ImporterWorkerMessaging.MessageType.ImportScheduleCanceled:
            state = "canceled";
            break;
          case ImporterWorkerMessaging.MessageType.ImportStarted:
            state = "started";
            break;
          case ImporterWorkerMessaging.MessageType.ImportStatus:
            state = "status";
            break;
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            state = "completed";
            break;
          case ImporterWorkerMessaging.MessageType.RefreshLocalShares:
            state = "refresh-local-shares";
            break;
          default:
            return;
        }

        string path = PathOf(message, ImporterWorkerMessaging.RESOURCE_PATH);
        string jobType = null;
        object rawJobType;
        if (message.MessageData != null &&
            message.MessageData.TryGetValue(ImporterWorkerMessaging.IMPORT_JOB_TYPE, out rawJobType) &&
            rawJobType != null)
          jobType = rawJobType.ToString().ToLowerInvariant();

        // Keyed by state, and by path too except for the states the importer repeats for every directory it walks.
        // Those two say "here is where I got to", and a client handed two hundred of them one after another cannot
        // render them any better than the newest one; the states that name a beginning or an end keep their path,
        // because an import finishing on music is a different sentence from one finishing on films.
        Store(state == "status" || state == "refresh-local-shares"
            ? KIND_IMPORT + ":" + state
            : KIND_IMPORT + ":" + state + ":" + (path ?? ""),
            KIND_IMPORT,
            new ImportFrame { State = state, Path = path, JobType = jobType });
      }

      /// <summary>
      /// Remembers one frame, replacing whatever the same key held and counting a miss when there is no room.
      /// </summary>
      private void Store(string key, string kind, object frame)
      {
        Pending pending = new Pending(kind, JsonSerializer.Serialize(frame, FRAME_OPTIONS));
        if (_latest.ContainsKey(key))
          _latest[key] = pending;
        else if (_latest.Count >= PENDING_LIMIT)
          _dropped++;
        else
        {
          _latest[key] = pending;
          _order.Add(key);
        }
      }

      private static List<string> ItemIds(SystemMessage message)
      {
        object raw;
        if (message.MessageData == null ||
            !message.MessageData.TryGetValue(MediaLibraryMessaging.PARAM, out raw) || raw == null)
          return null;

        IEnumerable<Guid> guids = raw as IEnumerable<Guid>;
        if (guids == null)
          return null;
        return guids.Select(id => id.ToString("N")).ToList();
      }

      private static string PathOf(SystemMessage message, string parameter)
      {
        object raw;
        if (message.MessageData == null || !message.MessageData.TryGetValue(parameter, out raw))
          return null;
        ResourcePath path = raw as ResourcePath;
        // The serialized form, never the object: this assembly's own rule is that server types do not reach the
        // wire, and a client that wants to show "D:\Films" shows what the server's own library says about the path.
        return path == null ? null : path.Serialize();
      }

      private static List<JobProgress> Jobs(SystemMessage message)
      {
        object raw;
        if (message.MessageData == null ||
            !message.MessageData.TryGetValue(ImporterWorkerMessaging.IMPORT_PROGRESS, out raw))
          return null;

        IDictionary<ImportJobInformation, Tuple<int, int>> progress =
            raw as IDictionary<ImportJobInformation, Tuple<int, int>>;
        if (progress == null)
          return null;

        List<JobProgress> jobs = new List<JobProgress>(progress.Count);
        foreach (KeyValuePair<ImportJobInformation, Tuple<int, int>> entry in progress)
        {
          jobs.Add(new JobProgress
          {
            Path = entry.Key.BasePath == null ? null : entry.Key.BasePath.Serialize(),
            Identified = entry.Value != null ? entry.Value.Item1 : 0,
            Completed = entry.Value != null ? entry.Value.Item2 : 0
          });
        }
        return jobs;
      }

      /// <summary>
      /// A frame that has been recorded and not yet written: its kind and its JSON body, kept together because the
      /// protocol line naming the kind must not have to be guessed from the body.
      /// </summary>
      internal sealed class Pending
      {
        internal Pending(string kind, string body)
        {
          Kind = kind;
          Body = body;
        }

        internal string Kind { get; }
        internal string Body { get; }
      }
    }

    #region The frames

    /// <summary>
    /// The base of every frame: which of them this is, so a client written against a later server can ignore what
    /// it does not know.
    /// </summary>
    public abstract class FrameBase
    {
      /// <summary>One of the <c>KIND_</c> constants.</summary>
      [JsonPropertyName("kind")]
      public string Kind { get; set; }
    }

    /// <summary>Says the connection works and which protocol the server is speaking.</summary>
    public sealed class HelloFrame : FrameBase
    {
      /// <summary>Initializes the frame with its kind.</summary>
      public HelloFrame() { Kind = KIND_HELLO; }

      /// <summary>The protocol revision, <see cref="PROTOCOL_VERSION"/>.</summary>
      [JsonPropertyName("protocol")]
      public int Protocol { get; set; }

      /// <summary>Server clock, ISO 8601, so a client can tell "nothing happened" from "the clock disagrees".</summary>
      [JsonPropertyName("serverTime")]
      public string ServerTime { get; set; }
    }

    /// <summary>The titles the server holds are not what they were.</summary>
    public sealed class LibraryFrame : FrameBase
    {
      /// <summary>Initializes the frame with its kind.</summary>
      public LibraryFrame() { Kind = KIND_LIBRARY; }

      /// <summary><c>media-items-changed</c>, <c>media-items-deleted</c> or <c>user-data-changed</c>.</summary>
      [JsonPropertyName("change")]
      public string Change { get; set; }

      /// <summary>Up to <see cref="ITEMS_PER_FRAME"/> ids, as lowercase <c>N</c>-format GUIDs. May be absent.</summary>
      [JsonPropertyName("items")]
      public List<string> Items { get; set; }

      /// <summary>True when more items changed than are named.</summary>
      [JsonPropertyName("itemsTruncated")]
      public bool? ItemsTruncated { get; set; }
    }

    /// <summary>An import on the server changed state.</summary>
    public sealed class ImportFrame : FrameBase
    {
      /// <summary>Initializes the frame with its kind.</summary>
      public ImportFrame() { Kind = KIND_IMPORT; }

      /// <summary><c>scheduled</c>, <c>started</c>, <c>status</c>, <c>completed</c>, <c>canceled</c> or <c>refresh-local-shares</c>.</summary>
      [JsonPropertyName("state")]
      public string State { get; set; }

      /// <summary>The root the import is about, serialized the way every other path in this API is.</summary>
      [JsonPropertyName("path")]
      public string Path { get; set; }

      /// <summary><c>import</c> or <c>refresh</c>, when the message said which.</summary>
      [JsonPropertyName("jobType")]
      public string JobType { get; set; }
    }

    /// <summary>How far the imports running right now have got.</summary>
    public sealed class ProgressFrame : FrameBase
    {
      /// <summary>Initializes the frame with its kind.</summary>
      public ProgressFrame() { Kind = KIND_PROGRESS; }

      /// <summary>One entry per running job, as the importer last reported it.</summary>
      [JsonPropertyName("jobs")]
      public List<JobProgress> Jobs { get; set; }
    }

    /// <summary>One running import, as far as the importer's counter goes.</summary>
    public sealed class JobProgress
    {
      /// <summary>The share's root.</summary>
      [JsonPropertyName("path")]
      public string Path { get; set; }

      /// <summary>Files found so far.</summary>
      [JsonPropertyName("identified")]
      public int Identified { get; set; }

      /// <summary>Files filed so far, which is the number a progress bar shows.</summary>
      [JsonPropertyName("completed")]
      public int Completed { get; set; }
    }

    /// <summary>
    /// The server had more to say than this connection could be given.
    /// </summary>
    /// <remarks>
    /// Everything a client was told before this frame was delivered; what it did not get is counted here. A client
    /// that sees one should stop believing anything it derived from earlier frames and ask the library again,
    /// which is what MP2's own clients did without this endpoint anyway.
    /// </remarks>
    public sealed class GapFrame : FrameBase
    {
      /// <summary>Initializes the frame with its kind.</summary>
      public GapFrame() { Kind = KIND_GAP; }

      /// <summary>How many changes were left out.</summary>
      [JsonPropertyName("dropped")]
      public int Dropped { get; set; }
    }

    #endregion
  }
}
