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
using System.Text.Json;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// What the server said on its event stream: one decoded frame.
  /// </summary>
  /// <remarks>
  /// <para>
  /// One type for every frame, with the fields the frames do not share left empty, rather than a class per frame.
  /// The reason is forward compatibility: a front end built today will be pointed at servers built later, and a
  /// frame carrying a kind this file has never heard of has to arrive as something a caller can ignore instead of
  /// a deserialization failure at the door. <see cref="Kind"/> therefore stays a string and
  /// <see cref="IsKnown"/> says whether this build knows what to do with it.
  /// </para>
  /// <para>
  /// The property names match the server's <c>MediaEventsController</c> frame declarations one for one. That is a
  /// deliberate duplication of a wire format, on the same terms as the rest of this API: the two ends are versioned
  /// together here, and a client that reflected the server's types would drag the server's assembly onto the
  /// television.
  /// </para>
  /// </remarks>
  public sealed class MediaEvent
  {
    /// <summary><c>hello</c> - the connection works and this is the protocol the server speaks.</summary>
    public const string KIND_HELLO = "hello";

    /// <summary><c>library</c> - the titles the server holds are not what they were.</summary>
    public const string KIND_LIBRARY = "library";

    /// <summary><c>import</c> - an import was scheduled, started, is running, finished or was canceled.</summary>
    public const string KIND_IMPORT = "import";

    /// <summary><c>import-progress</c> - how far the running imports have got.</summary>
    public const string KIND_PROGRESS = "import-progress";

    /// <summary><c>gap</c> - the server had more to say than this connection could be given.</summary>
    public const string KIND_GAP = "gap";

    private MediaEvent(string kind, string? change, string? state, string? path, string? jobType,
        List<Guid> items, bool itemsTruncated, List<MediaImportJob> jobs, int dropped, int protocol,
        string? serverTime)
    {
      Kind = kind;
      Change = change;
      State = state;
      Path = path;
      JobType = jobType;
      Items = items;
      ItemsTruncated = itemsTruncated;
      Jobs = jobs;
      Dropped = dropped;
      Protocol = protocol;
      ServerTime = serverTime;
    }

    /// <summary>One of the <c>KIND_</c> constants, or whatever a newer server sent.</summary>
    public string Kind { get; }

    /// <summary>Whether this build knows what the frame means. Unknown kinds still arrive, they are only not acted on.</summary>
    public bool IsKnown
    {
      get
      {
        return Kind == KIND_HELLO || Kind == KIND_LIBRARY || Kind == KIND_IMPORT || Kind == KIND_PROGRESS ||
            Kind == KIND_GAP;
      }
    }

    /// <summary>For a library frame: <c>media-items-changed</c>, <c>media-items-deleted</c> or <c>user-data-changed</c>.</summary>
    public string? Change { get; }

    /// <summary>For an import frame: <c>started</c>, <c>completed</c> and so on.</summary>
    public string? State { get; }

    /// <summary>The share an import is about, in the serialized form every other path in this API uses.</summary>
    public string? Path { get; }

    /// <summary><c>import</c> or <c>refresh</c>, when the server said which.</summary>
    public string? JobType { get; }

    /// <summary>Ids a library frame names. Empty when the server did not name any.</summary>
    public IReadOnlyList<Guid> Items { get; }

    /// <summary>True when more items changed than are named, so the list is a sample rather than the answer.</summary>
    public bool ItemsTruncated { get; }

    /// <summary>One entry per running import.</summary>
    public IReadOnlyList<MediaImportJob> Jobs { get; }

    /// <summary>For a gap frame: how many changes were left out.</summary>
    public int Dropped { get; }

    /// <summary>For a hello frame: the protocol revision the server is speaking.</summary>
    public int Protocol { get; }

    /// <summary>For a hello frame: the server's clock, as it wrote it.</summary>
    public string? ServerTime { get; }

    /// <summary>Whether this frame means the titles on screen may be stale and should be read again.</summary>
    /// <remarks>
    /// The three ways that happens in one place, so that a screen does not have to remember them: the library
    /// changed, an import finished, or the server ran out of room and said so. A client that refreshed on every
    /// frame would ask the server for its whole library every time the importer walked into a new directory.
    /// </remarks>
    public bool ShouldRefresh
    {
      get
      {
        if (Kind == KIND_LIBRARY)
          return true;
        if (Kind == KIND_GAP)
          return true;
        return Kind == KIND_IMPORT && (State == "completed" || State == "canceled");
      }
    }

    /// <summary>
    /// Turns the <c>data:</c> of one frame into what it means.
    /// </summary>
    /// <param name="data">The JSON the server sent.</param>
    /// <param name="eventName">
    /// The <c>event:</c> line, used only when the body carries no <c>kind</c> - which the server always does, but
    /// the protocol line is the one a proxy could in principle rewrite, and taking the body when both are there
    /// means one answer rather than two.
    /// </param>
    /// <exception cref="FormatException">The body is not the JSON a frame is made of.</exception>
    public static MediaEvent Parse(string data, string? eventName = null)
    {
      if (string.IsNullOrWhiteSpace(data))
        throw new FormatException("An event frame arrived with no body.");

      try
      {
        using (JsonDocument document = JsonDocument.Parse(data))
        {
          JsonElement root = document.RootElement;
          string kind = ReadString(root, "kind") ?? eventName;
          if (string.IsNullOrEmpty(kind))
            throw new FormatException("An event frame arrived without saying what it is: " + data);

          List<Guid> items = new List<Guid>();
          JsonElement itemsField;
          if (root.TryGetProperty("items", out itemsField) && itemsField.ValueKind == JsonValueKind.Array)
          {
            foreach (JsonElement entry in itemsField.EnumerateArray())
            {
              // Read rather than parsed on purpose: an id this build cannot read is a reason to ignore one entry,
              // never a reason to lose the frame and whatever else it carried.
              Guid id;
              if (entry.ValueKind == JsonValueKind.String &&
                  Guid.TryParse(entry.GetString(), out id))
                items.Add(id);
            }
          }

          List<MediaImportJob> jobs = new List<MediaImportJob>();
          JsonElement jobsField;
          if (root.TryGetProperty("jobs", out jobsField) && jobsField.ValueKind == JsonValueKind.Array)
          {
            foreach (JsonElement entry in jobsField.EnumerateArray())
              jobs.Add(MediaImportJob.Read(entry));
          }

          return new MediaEvent(kind!, ReadString(root, "change"), ReadString(root, "state"),
              ReadString(root, "path"), ReadString(root, "jobType"), items,
              ReadBool(root, "itemsTruncated"), jobs, ReadInt(root, "dropped"), ReadInt(root, "protocol"),
              ReadString(root, "serverTime"));
        }
      }
      catch (JsonException ex)
      {
        throw new FormatException("An event frame arrived that was not JSON: " + data, ex);
      }
    }

    /// <summary>A one-line account of the frame, for a status line and for a test that failed.</summary>
    public override string ToString()
    {
      switch (Kind)
      {
        case KIND_HELLO:
          return "hello (protocol " + Protocol + ")";
        case KIND_LIBRARY:
          return "library: " + Change + (Items.Count > 0 ? ", " + Items.Count + " item(s)" : string.Empty);
        case KIND_IMPORT:
          return "import: " + State + (Path == null ? string.Empty : " in " + Path);
        case KIND_PROGRESS:
          return "progress: " + string.Join(", ", Jobs);
        case KIND_GAP:
          return "gap: " + Dropped + " frame(s) missed";
        default:
          return Kind + " (not understood by this client)";
      }
    }

    private static string? ReadString(JsonElement root, string name)
    {
      JsonElement value;
      return root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.String
          ? value.GetString()
          : null;
    }

    private static int ReadInt(JsonElement root, string name)
    {
      JsonElement value;
      int parsed;
      return root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Number &&
          value.TryGetInt32(out parsed)
          ? parsed
          : 0;
    }

    private static bool ReadBool(JsonElement root, string name)
    {
      JsonElement value;
      return root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.True;
    }
  }

  /// <summary>One import the server is running, as far as its own counter goes.</summary>
  /// <param name="Path">The share being imported, serialized the way every path in this API is.</param>
  /// <param name="Identified">Files found so far.</param>
  /// <param name="Completed">Files filed so far, which is the number a progress bar shows.</param>
  public sealed record MediaImportJob(string? Path, int Identified, int Completed)
  {
    internal static MediaImportJob Read(JsonElement entry)
    {
      JsonElement value;
      int identified = entry.TryGetProperty("identified", out value) && value.ValueKind == JsonValueKind.Number
          ? value.GetInt32()
          : 0;
      int completed = entry.TryGetProperty("completed", out value) && value.ValueKind == JsonValueKind.Number
          ? value.GetInt32()
          : 0;
      string? path = entry.TryGetProperty("path", out value) && value.ValueKind == JsonValueKind.String
          ? value.GetString()
          : null;

      return new MediaImportJob(path, identified, completed);
    }

    /// <summary>How far this import has got, from nothing to everything; negative when the server does not know.</summary>
    public double? Fraction
    {
      get
      {
        if (Identified <= 0 || Completed < 0)
          return null;
        return Math.Min(1d, (double)Completed / Identified);
      }
    }

    public override string ToString()
    {
      return Completed + " of " + Identified + (Path == null ? string.Empty : " in " + Path);
    }
  }

  /// <summary>
  /// What the watch on the server's event stream is doing right now, which a status line has to be able to tell
  /// apart from the frames: "connected, nothing happening" and "not connected, pretending" look identical from the
  /// frames alone.
  /// </summary>
  public sealed class MediaEventConnection
  {
    private MediaEventConnection(bool connected, TimeSpan retryAfter)
    {
      IsConnected = connected;
      RetryAfter = retryAfter;
    }

    /// <summary>The stream is open and the server has said hello.</summary>
    public static MediaEventConnection Connected { get; } = new MediaEventConnection(true, TimeSpan.Zero);

    /// <summary>The stream broke and the client will ask for it again after <paramref name="after"/>.</summary>
    public static MediaEventConnection Reconnecting(TimeSpan after)
    {
      return new MediaEventConnection(false, after);
    }

    /// <summary>Whether the client is currently hearing the server.</summary>
    public bool IsConnected { get; }

    /// <summary>How long the client will wait before the next attempt; zero while connected.</summary>
    public TimeSpan RetryAfter { get; }

    public override string ToString()
    {
      return IsConnected
          ? "connected"
          : "reconnecting in " + (int)Math.Round(RetryAfter.TotalSeconds) + "s";
    }
  }

  /// <summary>
  /// Assembles the lines of a server-sent event stream into frames.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Split out from the reading of the socket for the same reason the server's own listener is split out from its
  /// writing: this is the part with rules in it, and the rules are worth testing without a wire. The rules are
  /// SSE's: a blank line ends a frame, <c>data:</c> lines of one frame are joined, a line starting with a colon is
  /// a comment and carries nothing, and a field's value loses exactly one space after the colon - which is why
  /// <c>data:{"a":1}</c> and <c>data: {"a":1}</c> are the same frame and <c>data:  x</c> is <c>" x"</c>.
  /// </para>
  /// <para>
  /// What is deliberately not implemented: the <c>retry:</c> field (this client decides its own patience, see
  /// <see cref="ServerApiClient.EventRetryTime"/>), the <c>lastEventId:</c> reconnection behaviour (the server
  /// keeps no history to resume from, so a resuming client would be told a lie), and any framing of our own.
  /// </para>
  /// </remarks>
  internal sealed class ServerEventFramer
  {
    private readonly List<string> _data = new List<string>();

    /// <summary>What the frame was called by the <c>event:</c> line, or null when it said nothing.</summary>
    public string? Event { get; private set; }

    /// <summary>The body, with the <c>data:</c> lines joined as the protocol says.</summary>
    public string Data
    {
      get { return string.Join("\n", _data.ToArray()); }
    }

    /// <summary>Whether anything has been collected, which tells a blank line meaning "frame over" from one meaning "nothing yet".</summary>
    public bool HasContent
    {
      get { return _data.Count > 0 || Event != null; }
    }

    /// <summary>
    /// Feeds one line.
    /// </summary>
    /// <param name="line">A line without its terminator.</param>
    /// <returns>The finished frame as <c>(event, data)</c>, or null when the frame is not over yet.</returns>
    public Tuple<string?, string>? Feed(string line)
    {
      if (line.Length == 0)
      {
        if (!HasContent)
          return null;

        Tuple<string?, string> frame = new Tuple<string?, string>(Event, Data);
        Clear();
        return frame;
      }

      if (line[0] == ':')
        // A comment. The spec lets it be a keepalive or a BOM guard; either way it belongs to no frame.
        return null;

      string field;
      string value;
      int colon = line.IndexOf(':');
      if (colon < 0)
      {
        field = line;
        value = string.Empty;
      }
      else
      {
        field = line.Substring(0, colon);
        value = line.Substring(colon + 1);
        if (value.Length > 0 && value[0] == ' ')
          value = value.Substring(1);
      }

      if (field == "event")
        Event = value;
      else if (field == "data")
        _data.Add(value);

      // "id" and "retry" are read by no here, and that is the decision documented above rather than an omission.
      return null;
    }

    private void Clear()
    {
      _data.Clear();
      Event = null;
    }
  }
}
