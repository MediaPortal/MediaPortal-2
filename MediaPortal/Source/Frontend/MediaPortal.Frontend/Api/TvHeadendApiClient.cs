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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// A question to the TV backend that did not produce an answer to read.
  /// </summary>
  /// <remarks>
  /// The same three-way difference <see cref="ServerApiException"/> insists on, for the same reason: "nobody was
  /// there", "somebody answered and said no" and "somebody answered something that was not the answer" are three
  /// different things for a viewer to be told, and the first is fixed by typing an address while the second is
  /// fixed by typing a password.
  /// </remarks>
  public class TvBackendException : Exception
  {
    public TvBackendException(Uri url, string message, Exception? inner = null)
        : base(message, inner)
    {
      Url = url;
    }

    public TvBackendException(Uri url, HttpStatusCode statusCode, string body)
        : base(Says(url, statusCode, body))
    {
      Url = url;
      StatusCode = (int) statusCode;
    }

    /// <summary>The address that was asked, so the sentence can name it.</summary>
    public Uri Url { get; }

    /// <summary>The HTTP status, or null when the question never got an answer.</summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Whether the refusal was about who is asking rather than about what was asked, which is the one answer a
    /// screen can turn into "then fill in the password on the television settings".
    /// </summary>
    public bool NeedsLogin
    {
      get { return StatusCode == 401 || StatusCode == 403; }
    }

    /// <summary>
    /// The sentence a viewer reads. A TV backend that is not a TV backend - a wrong port landing on the router's
    /// own web page, say - answers <c>404</c> to everything, so that answer is translated into what it means
    /// rather than into a number.
    /// </summary>
    private static string Says(Uri url, HttpStatusCode statusCode, string body)
    {
      if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
        return string.Format("{0} asked who was asking, and the name and password on the television settings "
            + "were not accepted", url.Host);

      if (statusCode == HttpStatusCode.NotFound)
        return string.Format("{0} answered {1} ({2}) at {3}, which is what a machine that is not a TV backend "
            + "does to every question", url.Host, (int) statusCode, statusCode, url);

      if (string.IsNullOrEmpty(body))
        return string.Format("{0} answered {1} ({2}) at {3}", url.Host, (int) statusCode, statusCode, url);

      return string.Format("{0} answered {1} ({2}) at {3}: {4}", url.Host, (int) statusCode, statusCode, url,
          Shorten(body));
    }

    private static string Shorten(string body)
    {
      string oneLine = body.Replace("\r", " ").Replace("\n", " ").Trim();
      return oneLine.Length <= 180 ? oneLine : oneLine.Substring(0, 180) + "...";
    }
  }

  /// <summary>
  /// The TV backend asked over HTTP: TVHeadend's own JSON interface, which is the only television this front end
  /// can be pointed at.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference reached a TV backend through one of two providers - a UPnP proxy to MediaPortal's own TV
  /// service, or a WCF call to MPExtended - and both of those are gone from this tree and from the world.
  /// TVHeadend is what a multiplatform media centre has instead: one daemon on any machine in the house,
  /// speaking plain HTTP, holding the cards, the programme data and the recordings, and offering a JSON
  /// interface that needs no client library and no code generation.
  /// </para>
  /// <para>
  /// What is asked, in the backend's own words: the channel list is <c>api/channel/grid</c> with the bouquets at
  /// <c>api/channeltag/list</c>, the programme data is <c>api/epg/events</c>, and the names of the programme
  /// kinds come from <c>api/epg/content_type/list</c>. The recordings are <c>api/dvr/entry</c> and its tables
  /// (<c>grid_upcoming</c>, <c>grid_finished</c>, <c>grid_all</c>) with <c>create</c>, <c>delete</c>,
  /// <c>cancel</c> and the <c>autorenew</c> sibling for the questions a viewer asks about one of them.
  /// </para>
  /// <para>
  /// Two things about that interface are worth knowing while reading the methods. Times are seconds since 1970
  /// in UTC and are turned into offsets the moment they are read, because subtracting two of them is the only way
  /// a living room in Lisbon and a living room in Helsinki share one guide layout. And a table answers at most
  /// <c>limit</c> rows and says how many there really were in <c>total</c>, so every question here pages rather
  /// than assuming its window is small - a fortnight of a forty-channel bouquet is not. The recording tables page
  /// the same way but call their rows <c>dlrEntries</c> and their count <c>totalCount</c>, which is a typo the
  /// backend has carried for years and every client has to carry with it.
  /// </para>
  /// <para>
  /// Fields are read by name and forgiven when absent. The backend answers a great many fields per programme,
  /// and which of them arrive depends on its version, on the language asked for, and on whether the broadcaster
  /// told anything at all; a screen that broke because a sender left the episode number out is not a screen.
  /// </para>
  /// </remarks>
  public sealed class TvHeadendApiClient : IDisposable
  {
    /// <summary>
    /// How long one question gets. A guide over the local network answers in a fraction of a second, so anything
    /// nearing this is a machine that is not there rather than a window that is too big.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    /// <summary>Rows asked for per page. A full guide window needs several of these and says so by paging.</summary>
    private const int PAGE_SIZE = 2000;

    /// <summary>
    /// How many pages one question may take. Beyond this the answer is not "a lot of programmes" but "a window
    /// nobody meant to ask for", and the screens are better off with what they have than with hanging.
    /// </summary>
    private const int MAX_PAGES = 40;

    /// <summary>A channel number of zero is what a backend writes when it has not been given one.</summary>
    private const int NO_NUMBER = 0;

    /// <summary>The DVB service-type bit that says "this is a thing you listen to".</summary>
    private const int SERVICE_IS_RADIO = 0x02;

    /// <summary>The same bit for a thing you watch.</summary>
    private const int SERVICE_IS_TELEVISION = 0x01;

    private readonly TvBackendSettings _settings;
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public TvHeadendApiClient(TvBackendSettings settings, HttpClient? httpClient = null)
    {
      _settings = settings ?? throw new ArgumentNullException(nameof(settings));
      _ownsHttpClient = httpClient == null;
      _http = httpClient ?? new HttpClient();
      if (_ownsHttpClient)
        _http.Timeout = DefaultTimeout;
    }

    public void Dispose()
    {
      if (_ownsHttpClient)
        _http.Dispose();
    }

    /// <summary>The address every question is asked at, or null when nobody named a backend.</summary>
    public Uri? BaseUrl
    {
      get { return _settings.BaseUrl; }
    }

    /// <summary>
    /// Whether there is anything to ask. Screens ask this before the first question so that a box without a
    /// backend says "no TV backend named" instead of complaining about a machine it never reached.
    /// </summary>
    public bool IsConfigured
    {
      get { return _settings.IsConfigured && _settings.BaseUrl != null; }
    }

    /// <summary>What the screens say when <see cref="IsConfigured"/> was false.</summary>
    public string NotConfiguredSays
    {
      get
      {
        return "No TV backend named yet - say where the television comes from on the television settings.";
      }
    }

    /// <summary>
    /// The bouquets the backend offers, in the order it offers them. A bouquet with nothing in it is left in the
    /// answer rather than dropped: the count is what a screen shows, and an empty one is a fact about the backend
    /// that somebody may want to know about.
    /// </summary>
    public async Task<IReadOnlyList<TvChannelGroup>> GetChannelGroupsAsync(CancellationToken cancellation = default)
    {
      return await GetTableAsync("api/channeltag/list", "", delegate (JsonElement entry)
      {
        string id = Text(entry, "uuid") ?? Text(entry, "id") ?? "";
        string name = Text(entry, "name") ?? "";
        if (id.Length == 0 || name.Length == 0)
          return null;

        // The tag carries its channels as a list of numbers, which is all a tag is; the count is read from that
        // list rather than guessed at, and a tag with none is reported as empty rather than left out.
        int count = 0;
        if (entry.TryGetProperty("services", out JsonElement services)
            && services.ValueKind == JsonValueKind.Array)
          count = services.GetArrayLength();

        return new TvChannelGroup(id, name, count);
      }, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Every channel the backend has, sorted by the number a viewer would say.
    /// </summary>
    /// <remarks>
    /// Two questions are asked for this answer, and both are wanted. The channel grid says which channels there
    /// are, which bouquets they belong to and where to point the player; the service grid says which of them are
    /// radio, because a channel is only ever called radio through the services behind it. The second question
    /// goes out with a fallback: a backend that will not answer it, or answers it without type bits, gets the
    /// channels called "radio" by their name or by their bouquet, which is how the reference's own provider told
    /// the two apart when its backend said nothing either.
    /// </remarks>
    public async Task<IReadOnlyList<TvChannel>> GetChannelsAsync(CancellationToken cancellation = default)
    {
      HashSet<string>? radioServices = await TryLoadRadioServicesAsync(cancellation).ConfigureAwait(false);
      HashSet<string>? radioGroups = radioServices == null
          ? await TryLoadRadioGroupsAsync(cancellation).ConfigureAwait(false)
          : null;

      List<TvChannel> channels = await GetTableAsync("api/channel/grid", "", delegate (JsonElement entry)
      {
        int number = Whole(entry, "number") ?? NO_NUMBER;
        string name = Text(entry, "name") ?? "";
        if (name.Length == 0 && number == NO_NUMBER)
          return null;

        List<string> groups = ReadIdList(entry, "tags");
        List<string> services = ReadIdList(entry, "services");

        // The id programmes are filed under is the EPG channel, not the channel: the two are separate tables in
        // the backend and a guide question has to be asked of the first. A channel without one can be watched
        // but not looked up, which is said by leaving the id empty rather than by pretending.
        string epgId = Text(entry, "epgchannelid") ?? Text(entry, "epg_channel_id") ?? "";

        bool isRadio = radioServices != null
            ? services.Count > 0 && services.All(radioServices.Contains)
            : (radioGroups != null && groups.Any(radioGroups.Contains))
                || name.Contains("radio", StringComparison.OrdinalIgnoreCase);

        return new TvChannel(number, name, epgId, ResolveStreamUrl(Text(entry, "streamurl"), number), groups,
            isRadio ? TvMediaType.Radio : TvMediaType.Television);
      }, cancellation).ConfigureAwait(false);

      channels.Sort(delegate (TvChannel a, TvChannel b)
      {
        int byNumber = a.Number.CompareTo(b.Number);
        return byNumber != 0 ? byNumber : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
      });
      return channels;
    }

    /// <summary>
    /// The address the player is pointed at for one channel, or null when there is nothing to point at.
    /// </summary>
    /// <remarks>
    /// The backend's own <c>streamurl</c> is preferred because it knows which streaming profile the channel
    /// wants. It is written as <c>//stream/channelid/5</c> - two slashes and no host, which a browser resolves
    /// against the page it came from - and combining that with the backend's address the ordinary way would make
    /// "stream" the host name, so the two-slash form is turned into a path here by hand. When the field is
    /// missing entirely, the documented public address is built from the channel number, which is what people
    /// type into a browser when they set one of these up.
    /// </remarks>
    internal Uri? ResolveStreamUrl(string? streamUrl, int channelNumber)
    {
      Uri? baseAddress = BaseUrl;
      if (baseAddress == null)
        return null;

      string said = (streamUrl ?? "").Trim();
      if (said.Length > 0)
      {
        try
        {
          if (said.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
              || said.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return new Uri(said);

          if (said.StartsWith("//", StringComparison.Ordinal))
            said = "/" + said.Substring(2);
          else if (!said.StartsWith("/", StringComparison.Ordinal))
            said = "/" + said;

          return new Uri(baseAddress, said);
        }
        catch (UriFormatException)
        {
          // Fall through to the address built from the number: a half-written stream URL is still a channel.
        }
      }

      if (channelNumber <= NO_NUMBER)
        return null;

      try
      {
        return new Uri(baseAddress, string.Format(CultureInfo.InvariantCulture,
            "/stream/channelid/{0}", channelNumber));
      }
      catch (UriFormatException)
      {
        return null;
      }
    }

    /// <summary>
    /// Which service ids a backend calls radio, or null when it would not say. The type bits are the DVB ones:
    /// a service is radio when its type carries the radio bit and not the television bit.
    /// </summary>
    private async Task<HashSet<string>?> TryLoadRadioServicesAsync(CancellationToken cancellation)
    {
      try
      {
        List<string> ids = await GetTableAsync("api/service/grid", "", delegate (JsonElement entry)
        {
          string? id = Text(entry, "uuid") ?? Text(entry, "id");
          int? type = Whole(entry, "type");
          if (string.IsNullOrWhiteSpace(id) || !type.HasValue)
            return null;
          bool radio = (type.Value & SERVICE_IS_RADIO) != 0 && (type.Value & SERVICE_IS_TELEVISION) == 0;
          return radio ? id.Trim() : null;
        }, cancellation).ConfigureAwait(false);

        return ids.Count > 0 ? new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase) : null;
      }
      catch (TvBackendException)
      {
        // A backend that has no service grid, or will not let this name read it, still has channels to show.
        return null;
      }
    }

    /// <summary>The bouquets whose name says radio, for a backend that will not talk about services.</summary>
    private async Task<HashSet<string>?> TryLoadRadioGroupsAsync(CancellationToken cancellation)
    {
      try
      {
        IReadOnlyList<TvChannelGroup> groups = await GetChannelGroupsAsync(cancellation).ConfigureAwait(false);
        List<string> named = groups
            .Where(g => g.Name.Contains("radio", StringComparison.OrdinalIgnoreCase))
            .Select(g => g.Id)
            .ToList();
        return named.Count > 0 ? new HashSet<string>(named, StringComparer.OrdinalIgnoreCase) : null;
      }
      catch (TvBackendException)
      {
        return null;
      }
    }

    /// <summary>
    /// Every programme one channel showed, shows or will show between two moments.
    /// </summary>
    /// <remarks>
    /// The window is asked for as an overlap, not as a containment: a programme that started an hour before the
    /// window and ends inside it is very much on the screen when the screen opens, and leaving it out would move
    /// every bar in the guide by an hour. The reference's guide asked the same thing of its backend
    /// (<c>GetProgramsAsync(channel, from, to)</c>) and got the same answer.
    /// </remarks>
    public async Task<IReadOnlyList<TvProgram>> GetProgramsAsync(TvChannel channel, DateTimeOffset from,
        DateTimeOffset to, CancellationToken cancellation = default)
    {
      if (channel == null)
        throw new ArgumentNullException(nameof(channel));
      if (to <= from)
        throw new ArgumentException("A guide window ending before it begins holds no programmes, and the backend "
            + "would answer everything it has rather than nothing.", nameof(to));

      // A channel whose EPG id is known is asked for by it, because that is the table the programmes are filed
      // under. A channel without one - which a backend does produce for channels nobody linked to a broadcaster -
      // is asked for by the name its programmes carry, which is the same question one step less precise.
      List<string> clauses = new List<string>();
      if (!string.IsNullOrWhiteSpace(channel.Id))
        clauses.Add("channel=" + Uri.EscapeDataString(channel.Id));
      else
        clauses.Add(WhereField("channelName", channel.Name));

      clauses.Add(Window(from, to));
      List<TvProgram> programs = await AskForProgramsAsync(string.Join("&", clauses), cancellation)
          .ConfigureAwait(false);

      return Sort(programs);
    }

    /// <summary>Every programme every channel of one bouquet showed in a window, which is a full guide.</summary>
    public async Task<IReadOnlyList<TvProgram>> GetProgramsAsync(TvChannelGroup group, DateTimeOffset from,
        DateTimeOffset to, CancellationToken cancellation = default)
    {
      if (group == null)
        throw new ArgumentNullException(nameof(group));
      if (to <= from)
        throw new ArgumentException("A guide window ending before it begins holds no programmes, and the backend "
            + "would answer everything it has rather than nothing.", nameof(to));

      string query = "channelTag=" + Uri.EscapeDataString(group.Id) + "&" + Window(from, to);
      return Sort(await AskForProgramsAsync(query, cancellation).ConfigureAwait(false));
    }

    /// <summary>
    /// Programmes whose title says something, in a window. The backend's own word for this question is
    /// <c>title</c>, and <c>fulltext</c> is asked for with it because without it the backend matches a title
    /// beginning with the words rather than containing them, which is not what anybody means by searching.
    /// </summary>
    public async Task<IReadOnlyList<TvProgram>> FindProgramsAsync(string title, DateTimeOffset from,
        DateTimeOffset to, CancellationToken cancellation = default)
    {
      string said = (title ?? "").Trim();
      if (said.Length == 0)
        return Array.Empty<TvProgram>();
      if (to <= from)
        throw new ArgumentException("A search over a window ending before it begins has nothing to search.",
            nameof(to));

      string query = "title=" + Uri.EscapeDataString(said) + "&fulltext=1&" + Window(from, to);
      return Sort(await AskForProgramsAsync(query, cancellation).ConfigureAwait(false));
    }

    /// <summary>
    /// What is running now on one channel and what follows it, which is what an on-screen strip and every
    /// channel list next to a picture is made of.
    /// </summary>
    /// <remarks>
    /// Asked as one window twelve hours either way rather than as two questions, because the backend answers a
    /// window in one go and the reference's <c>GetNowNextProgramAsync</c> was one question too. A programme that
    /// began more than twelve hours ago is a broadcast nobody should be shown as "now" anyway.
    /// </remarks>
    public async Task<TvNowAndNext> GetNowAndNextAsync(TvChannel channel, DateTimeOffset moment,
        CancellationToken cancellation = default)
    {
      IReadOnlyList<TvProgram> around = await GetProgramsAsync(channel, moment.AddHours(-12),
          moment.AddHours(12), cancellation).ConfigureAwait(false);

      TvProgram? now = around.LastOrDefault(p => p.IsOnAt(moment));
      TvProgram? next = around.FirstOrDefault(p => p.Start > moment);
      return new TvNowAndNext(now, next);
    }

    /// <summary>
    /// The recordings one of the backend's recording tables knows about, oldest first when the table itself has no
    /// opinion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the question the reference's <c>GetSchedulesAsync</c> asked of its TV server and its media library
    /// asked of the recordings folder, in one question instead of two: a TVHeadend keeps planned recordings and
    /// finished ones in the same table and says which is which by the entry's state, so a screen that wants the
    /// upcoming ones and a screen that wants the ones on disk are asking the same thing of different tables.
    /// </para>
    /// <para>
    /// Nothing is filtered here on purpose. The tables already are the filter, and an entry that "should" have been
    /// left out - a recording of a channel nobody watches, one that failed, one somebody else on the network
    /// planned - is a fact about the household's box that its screens are better off showing than hiding.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<TvRecording>> GetRecordingsAsync(TvRecordingTable table,
        CancellationToken cancellation = default)
    {
      List<TvRecording> entries = await GetTableAsync("api/dvr/entry/" + TablePath(table), "", "dlrEntries",
          "totalCount", ReadRecording, cancellation).ConfigureAwait(false);

      entries.Sort(delegate (TvRecording a, TvRecording b)
      {
        int byStart = a.Start.CompareTo(b.Start);
        return byStart != 0 ? byStart
            : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
      });
      return entries;
    }

    /// <summary>
    /// Ask for a programme to be recorded.
    /// </summary>
    /// <remarks>
    /// The event's own id is the whole question: a TVHeadend knows the channel, the title and both ends of the
    /// window from its EPG, and re-sending what the caller read out of that same EPG would only give the box two
    /// answers to choose between. The reference asked the equivalent single question
    /// (<c>CreateScheduleAsync(program, recordingType)</c>).
    ///
    /// <c>asSeries</c> is the reference's <c>SeriesScheduled</c> and is asked here as the backend's own
    /// <c>autorenew</c>: keep making a timer for this event every time the broadcaster lists it again. The
    /// reference offered eight kinds of series (daily, weekly, on this channel, ...); this backend offers one
    /// switch, and duplicate detection is deliberately left off - a household that asked for a series wants the
    /// repeats too unless it said otherwise somewhere else.
    /// </remarks>
    public Task<TvBackendAnswer> ScheduleRecordingAsync(TvProgram program, bool asSeries,
        CancellationToken cancellation = default)
    {
      if (program == null)
        throw new ArgumentNullException(nameof(program));

      string query = "event_id=" + program.Id.ToString(CultureInfo.InvariantCulture)
          + "&enabled=1"
          + "&autorenew=" + (asSeries ? "1" : "0")
          + "&dupdetect=0"
          // Let the backend write the programme's own words into the recording's comment rather than leaving the
          // entry with nothing but a title, which is what the reference's schedules carried into their band.
          + "&autocomm=1";

      return AskForAnswerAsync("api/dvr/entry/create?" + query, cancellation);
    }

    /// <summary>
    /// Call a recording off. For one still to come this takes the timer away; for one in progress the backend
    /// stops writing and keeps what it has.
    /// </summary>
    public Task<TvBackendAnswer> RemoveRecordingAsync(TvRecording recording, CancellationToken cancellation = default)
    {
      return AskForAnswerAsync("api/dvr/entry/delete?" + EntryWhose(recording), cancellation);
    }

    /// <summary>
    /// Stop a recording that is happening without throwing the entry away, which is the reference's
    /// "Stop current recording '{title}'" - its own wording asks for the recording to end, not for the plan to
    /// vanish, and a viewer who stops a recording usually still wants the part that was already watched.
    /// </summary>
    public Task<TvBackendAnswer> StopRecordingAsync(TvRecording recording, CancellationToken cancellation = default)
    {
      return AskForAnswerAsync("api/dvr/entry/cancel?" + EntryWhose(recording), cancellation);
    }

    /// <summary>
    /// Turn the series behaviour of an entry on or off, which is the reference's "Record series" and its
    /// counterpart asked of an entry that already exists. <c>all</c> is sent because the backend's own screen
    /// does the same: switching a series on is meant to cover every episode the box already knows about, not
    /// only the one that happened to be selected.
    /// </summary>
    public Task<TvBackendAnswer> SetSeriesRenewalAsync(TvRecording recording, bool renew,
        CancellationToken cancellation = default)
    {
      return AskForAnswerAsync("api/dvr/entry/autorenew?" + EntryWhose(recording)
          + "&active=" + (renew ? "1" : "0") + "&all=1", cancellation);
    }

    /// <summary>
    /// One entry by its uuid, for the screens that know a programme rather than a table.
    /// </summary>
    /// <remarks>
    /// A programme in the EPG says that something is planned for it and what the backend calls the state, but it
    /// does not say whether the plan is a single programme or a series that keeps renewing - and the dialog the
    /// guide puts up asks exactly that question ("Delete single program" or "Delete full series schedule"), so one
    /// cheap question about the entry is asked here rather than reading a whole table to find one row. The answer's
    /// shape is read three ways because the endpoint has been seen handing out the entry as the whole answer, under
    /// <c>data</c>, and as the single row of a table-shaped answer.
    /// </remarks>
    /// <returns>The entry, or nothing when the backend no longer has it - which is what a plan somebody deleted
    /// on another screen looks like, and a guide that still has the mark on the programme.</returns>
    public async Task<TvRecording?> GetRecordingAsync(string uuid, CancellationToken cancellation = default)
    {
      using (JsonDocument answer = await GetJsonAsync(
          "api/dvr/entry/get?uuid=" + Uri.EscapeDataString(uuid ?? ""), cancellation).ConfigureAwait(false))
      {
        JsonElement row = answer.RootElement;
        if (row.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
          row = data;
        else if (row.TryGetProperty("dlrEntries", out JsonElement rows) && rows.ValueKind == JsonValueKind.Array
            && rows.GetArrayLength() > 0)
          row = rows[0];

        return ReadRecording(row);
      }
    }

    /// <summary>
    /// Call a recording off by its uuid, for the screens that only ever saw the uuid on a programme.
    /// </summary>
    public Task<TvBackendAnswer> RemoveRecordingAsync(string uuid, CancellationToken cancellation = default)
    {
      return AskForAnswerAsync("api/dvr/entry/delete?" + ByUuid(uuid), cancellation);
    }

    /// <summary>End the writing of one entry by its uuid and leave the entry standing.</summary>
    public Task<TvBackendAnswer> StopRecordingAsync(string uuid, CancellationToken cancellation = default)
    {
      return AskForAnswerAsync("api/dvr/entry/cancel?" + ByUuid(uuid), cancellation);
    }

    /// <summary>Turn the keeping of a series on or off by uuid, for every episode the backend knows about.</summary>
    public Task<TvBackendAnswer> SetSeriesRenewalAsync(string uuid, bool renew,
        CancellationToken cancellation = default)
    {
      return AskForAnswerAsync("api/dvr/entry/autorenew?" + ByUuid(uuid)
          + "&active=" + (renew ? "1" : "0") + "&all=1", cancellation);
    }

    /// <summary>Point at an entry by uuid alone, which is all a programme in the EPG ever says about it.</summary>
    private static string ByUuid(string uuid)
    {
      if (string.IsNullOrWhiteSpace(uuid))
        throw new ArgumentException("A recording with no uuid cannot be pointed at - the backend answers questions "
            + "about entries by that id and by nothing else.", nameof(uuid));

      return "uuid=" + Uri.EscapeDataString(uuid);
    }

    /// <summary>The backend's address for one of its recording tables.</summary>
    private static string TablePath(TvRecordingTable table)
    {
      switch (table)
      {
        case TvRecordingTable.Upcoming:
          return "grid_upcoming";
        case TvRecordingTable.Finished:
          return "grid_finished";
        default:
          return "grid_all";
      }
    }

    /// <summary>
    /// How an entry is pointed at. The uuid is what every version of the backend takes; the row number is sent
    /// with it because the update endpoint of some versions reads that one and ignores the other, and an address
    /// carrying both is answered by all of them.
    /// </summary>
    private static string EntryWhose(TvRecording recording)
    {
      if (recording == null)
        throw new ArgumentNullException(nameof(recording));
      if (string.IsNullOrWhiteSpace(recording.Uuid))
        throw new ArgumentException("A recording with no uuid cannot be pointed at - the backend answers questions "
            + "about entries by that id and by nothing else.", nameof(recording));

      string said = "uuid=" + Uri.EscapeDataString(recording.Uuid);
      return string.IsNullOrWhiteSpace(recording.EntryId) ? said
          : said + "&entryId=" + Uri.EscapeDataString(recording.EntryId!);
    }

    /// <summary>
    /// Ask something to be done and read the backend's yes or no.
    /// </summary>
    /// <remarks>
    /// A refusal arrives as an ordinary answer rather than as an exception, because a viewer who asks to record
    /// something that is already being recorded has not met a fault - they have met an answer, and the screen has
    /// to say it in its own status line and leave the guide standing behind it. What cannot be answered at all
    /// (a box that is down, a name that is not allowed) still arrives as <see cref="TvBackendException"/>, so a
    /// screen has exactly two things to think about and not three.
    /// </remarks>
    private async Task<TvBackendAnswer> AskForAnswerAsync(string pathAndQuery, CancellationToken cancellation)
    {
      using (JsonDocument answer = await GetJsonAsync(pathAndQuery, cancellation).ConfigureAwait(false))
      {
        JsonElement root = answer.RootElement;

        bool allowed = true;
        if (root.TryGetProperty("success", out JsonElement success))
          allowed = success.ValueKind != JsonValueKind.False;

        if (!allowed)
          return TvBackendAnswer.Refused(RefusalsIn(root));

        TvRecording? made = null;
        if (root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
          made = ReadRecording(data);

        return TvBackendAnswer.Made(made);
      }
    }

    /// <summary>
    /// The words a refusal came with. The backend writes them as a list under <c>errors</c>, sometimes as plain
    /// sentences and sometimes as objects whose <c>text</c> holds the sentence, and a screen that showed the first
    /// shape where the second one arrived would show a blank complaint.
    /// </summary>
    private static string RefusalsIn(JsonElement root)
    {
      List<string> said = new List<string>();
      if (root.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement error in errors.EnumerateArray())
        {
          string? words = error.ValueKind == JsonValueKind.String
              ? error.GetString()
              : (error.TryGetProperty("text", out JsonElement text) ? text.GetString() : null);
          if (!string.IsNullOrWhiteSpace(words))
            said.Add(words!.Trim());
        }
      }

      return said.Count == 0 ? "the TV backend said no, and did not say why" : string.Join("; ", said);
    }

    /// <summary>
    /// One row of a recording table. Every field is read loosely because the tables have changed shape between
    /// backend versions - a title that moved from <c>name</c> to <c>title</c>, a size that is a number on one
    /// version and a formatted string on another - and a screen that lost an entry over one of those would be
    /// worse than a screen that showed the entry without that detail.
    /// </summary>
    private static TvRecording? ReadRecording(JsonElement entry)
    {
      string uuid = Text(entry, "uuid") ?? "";
      if (uuid.Length == 0)
        return null;

      // The channel's name lives in "name", its uuid in "channel" - the same naming the channel table uses, and
      // the same naming that makes a screen mix them up if it is not paying attention.
      string channelName = Text(entry, "name") ?? "";
      string? channelId = Text(entry, "channel") ?? Text(entry, "channelUuid");
      string title = Text(entry, "title") ?? channelName;

      // Both ends of the window: the table gives seconds since 1970 in "start" and "stop", and an entry with
      // neither is a row that cannot be placed on a screen, so the second one is asked for as a date/time pair
      // before the row is given up on.
      DateTimeOffset begins;
      DateTimeOffset ends;
      if (!TryInstant(entry, "start", out begins))
      {
        if (!TryRecordingDay(entry, "date", "timevalue", out begins))
          return null;
      }
      if (!TryInstant(entry, "stop", out ends))
        ends = begins;
      if (ends < begins)
        ends = begins;

      return new TvRecording(uuid,
          Text(entry, "entryId") ?? Whole(entry, "entryId")?.ToString(CultureInfo.InvariantCulture),
          channelId, channelName, begins, ends, title,
          Text(entry, "subtitle"), Text(entry, "description") ?? Text(entry, "comment"),
          Text(entry, "state") ?? "",
          Flagged(entry, "enabled"),
          // "autoscheduled" is the backend saying it made this entry itself - because a series renewed it - rather
          // than a person asking for it, which is what a screen marks as "automatic".
          Flagged(entry, "autoscheduled"),
          // A duplicate entry is the backend's own bookkeeping: the recording happens elsewhere, and this row only
          // marks that it does. "passive" is the same statement in some backend versions' words.
          Flagged(entry, "duplicate") || Flagged(entry, "passive"),
          Flagged(entry, "autorenew"),
          Whole(entry, "prio") ?? 0,
          Whole(entry, "retention") ?? 0,
          Wide(entry, "size") ?? 0L,
          Text(entry, "dir") ?? Text(entry, "directory"),
          Text(entry, "url") ?? Text(entry, "path"),
          Text(entry, "disabledText") ?? Text(entry, "updateError"));
    }

    /// <summary>
    /// A recording's beginning from the two halves some backends send instead of an instant: a day counted in
    /// seconds since the epoch at midnight, and a time of day counted in seconds since that midnight.
    /// </summary>
    private static bool TryRecordingDay(JsonElement entry, string dayField, string timeField,
        out DateTimeOffset when)
    {
      when = DateTimeOffset.MinValue;
      long? day = Wide(entry, dayField);
      if (!day.HasValue)
        return false;

      long? secondsIntoDay = Wide(entry, timeField) ?? Wide(entry, "time");
      when = DateTimeOffset.FromUnixTimeSeconds(day.Value)
          + TimeSpan.FromSeconds(secondsIntoDay ?? 0);
      return true;
    }

    /// <summary>
    /// Whether a field says yes. The backend answers these as real JSON booleans in some places and as 0 and 1 in
    /// others, and reading only the first shape quietly turns a yes into a no.
    /// </summary>
    private static bool Flagged(JsonElement entry, string name)
    {
      if (!entry.TryGetProperty(name, out JsonElement value))
        return false;

      switch (value.ValueKind)
      {
        case JsonValueKind.True:
          return true;
        case JsonValueKind.False:
          return false;
        default:
          return (Whole(entry, name) ?? 0) != 0;
      }
    }

    /// <summary>
    /// What the backend calls its programme kinds, so a screen names them the way its own web interface does
    /// instead of inventing a list. The codes are the backend's own; where they are DVB's four-bit genre codes,
    /// the names behind them are written here too because a guide that colours a programme still needs a word
    /// for it when the backend's answer held only the number.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, string>> GetProgramKindNamesAsync(
        CancellationToken cancellation = default)
    {
      Dictionary<int, string> names = new Dictionary<int, string>();
      try
      {
        await GetTableAsync("api/epg/content_type/list", "", delegate (JsonElement entry)
        {
          int? code = Whole(entry, "id");
          string? name = Text(entry, "val") ?? Text(entry, "name");
          if (!code.HasValue || string.IsNullOrWhiteSpace(name))
            return null;
          names[code.Value] = name.Trim();
          return "";
        }, cancellation).ConfigureAwait(false);
      }
      catch (TvBackendException)
      {
        // A backend without the endpoint leaves the guide with the names its programmes carried, which is enough.
      }
      return names;
    }

    /// <summary>
    /// The names behind the DVB four-bit genre codes (EN 300 468's user-content nibble), which is what a
    /// programme's <c>genre</c> list holds when the broadcaster said anything at all.
    /// </summary>
    internal static string? DvbGenreName(int code)
    {
      switch (code & 0x0F)
      {
        case 0x01: return "News";
        case 0x02: return "Show";
        case 0x03: return "Entertainment";
        case 0x04: return "Information";
        case 0x05: return "Sports";
        case 0x06: return "Drama";
        case 0x07: return "Music";
        case 0x08: return "Arts";
        case 0x09: return "Education";
        case 0x0A: return "Education";
        case 0x0B: return "Social";
        case 0x0C: return "Science";
        case 0x0D: return "Performing";
        case 0x0E: return "Religion";
        case 0x0F: return "Children";
        default: return null;
      }
    }

    /// <summary>Asks one table of programmes and reads every row of every page it answers with.</summary>
    private async Task<List<TvProgram>> AskForProgramsAsync(string query, CancellationToken cancellation)
    {
      return await GetTableAsync("api/epg/events", query, ReadProgram, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// One programme, out of whatever the backend happened to answer. A row without a beginning is not a
    /// programme - there is nothing to put it next to - and is dropped rather than drawn at midnight.
    /// </summary>
    private static TvProgram? ReadProgram(JsonElement entry)
    {
      if (!TryInstant(entry, "start", out DateTimeOffset start))
        return null;

      DateTimeOffset stop;
      if (TryInstant(entry, "stop", out DateTimeOffset endsAt))
      {
        stop = endsAt;
      }
      else
      {
        // Only older answers leave the end out and give a length instead, and that length is counted in seconds.
        // It is used because a bar with no end would be drawn across the whole guide.
        int? seconds = Whole(entry, "duration");
        stop = seconds.HasValue && seconds.Value > 0 ? start.AddSeconds(seconds.Value) : start;
      }

      List<string> genres = new List<string>();
      foreach (string name in new[] { "contentLabel", "content_label", "genreLabel", "genre_label" })
      {
        string? said = Text(entry, name);
        if (!string.IsNullOrWhiteSpace(said) && !genres.Contains(said.Trim()))
          genres.Add(said.Trim());
      }

      if (entry.TryGetProperty("genre", out JsonElement genreList) && genreList.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement code in genreList.EnumerateArray())
        {
          int? number = NumberIn(code);
          string? named = number.HasValue ? DvbGenreName(number.Value) : null;
          if (named != null && !genres.Contains(named))
            genres.Add(named);
        }
      }

      TvEpisode? episode = null;
      int? season = Whole(entry, "seasonNumber");
      int? part = Whole(entry, "episodeNumber");
      int? piece = Whole(entry, "partNumber");
      string? onScreen = Text(entry, "episodeOnscreen");
      if (season.HasValue || part.HasValue || piece.HasValue || !string.IsNullOrWhiteSpace(onScreen))
        episode = new TvEpisode(season, part, piece, onScreen);

      return new TvProgram(Whole(entry, "event_id") ?? 0L,
          Text(entry, "channel") ?? Text(entry, "channelUuid"),
          Text(entry, "channelname") ?? Text(entry, "channelName"),
          start, stop,
          Text(entry, "title") ?? Text(entry, "name") ?? "",
          Text(entry, "subtitle"), Text(entry, "summary"), Text(entry, "description"),
          Whole(entry, "contentType") ?? Whole(entry, "content_type") ?? 0,
          genres, episode,
          Whole(entry, "ageRating"), Whole(entry, "starRating"), Text(entry, "image"),
          Whole(entry, "nextEventId"), Text(entry, "registration_ref"),
          Text(entry, "dvrUuid"), Text(entry, "dvrState"));
    }

    private static IReadOnlyList<TvProgram> Sort(List<TvProgram> programs)
    {
      // Sorted here rather than asked for: a table's sort keys are the backend's own spelling, and a guide that
      // handed the wrong word would come back unordered without ever saying so.
      programs.Sort(delegate (TvProgram a, TvProgram b)
      {
        int byStart = a.Start.CompareTo(b.Start);
        return byStart != 0 ? byStart : string.Compare(a.ChannelName ?? "", b.ChannelName ?? "",
            StringComparison.OrdinalIgnoreCase);
      });
      return programs;
    }

    /// <summary>The two clauses that mean "anything overlapping this window".</summary>
    private static string Window(DateTimeOffset from, DateTimeOffset to)
    {
      return "filter=" + Uri.EscapeDataString("["
          + Clause("start", "numeric", "lt", to.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)) + ","
          + Clause("stop", "numeric", "gt", from.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
          + "]");
    }

    /// <summary>A clause saying a text field has to read exactly this.</summary>
    private static string WhereField(string field, string value)
    {
      return "filter=" + Uri.EscapeDataString("[" + Clause(field, "string", "eq", Quote(value)) + "]");
    }

    private static string Clause(string field, string type, string comparison, string value)
    {
      return "{\"field\":\"" + field + "\",\"type\":\"" + type + "\",\"comparison\":\"" + comparison
          + "\",\"value\":" + value + "}";
    }

    /// <summary>
    /// A word as the backend's JSON wants it. The one thing to be careful about is the quotation mark, because a
    /// channel called <c>Tonight's "Best" Of</c> would otherwise end the clause and take the rest of the query
    /// with it.
    /// </summary>
    private static string Quote(string value)
    {
      return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    /// <summary>
    /// Asks one of the backend's tables, page by page, until the rows run out or the answer says there are no
    /// more. The reader runs while the answer is still alive, because the rows of a parsed JSON document stop
    /// being readable the moment the document is put down.
    /// </summary>
    private async Task<List<T>> GetTableAsync<T>(string path, string query, Func<JsonElement, T?> read,
        CancellationToken cancellation) where T : class
    {
      return await GetTableAsync(path, query, "entries", "total", read, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// The same, for the tables that do not call their rows "entries". One backend, two spellings: the channel,
    /// service and programme tables answer with <c>entries</c> and <c>total</c>, while the recording tables answer
    /// with <c>dlrEntries</c> - a transposition nobody has fixed since it was written - and <c>totalCount</c>.
    /// A reader that only knew the first words would report a backend full of recordings as having none, quietly.
    /// </summary>
    private async Task<List<T>> GetTableAsync<T>(string path, string query, string rowsKey, string totalKey,
        Func<JsonElement, T?> read, CancellationToken cancellation) where T : class
    {
      List<T> all = new List<T>();

      for (int page = 0; page < MAX_PAGES; page++)
      {
        string url = path + (query.Length == 0 ? "?" : "?" + query + "&")
            + "start=" + (page * PAGE_SIZE).ToString(CultureInfo.InvariantCulture)
            + "&limit=" + PAGE_SIZE.ToString(CultureInfo.InvariantCulture);

        using (JsonDocument answer = await GetJsonAsync(url, cancellation).ConfigureAwait(false))
        {
          JsonElement root = answer.RootElement;
          int rows = 0;

          if (root.TryGetProperty(rowsKey, out JsonElement entries)
              && entries.ValueKind == JsonValueKind.Array)
          {
            foreach (JsonElement entry in entries.EnumerateArray())
            {
              rows++;
              T? value = read(entry);
              if (value != null)
                all.Add(value);
            }
          }

          int total = Whole(root, totalKey) ?? rows;
          if (rows == 0 || (page + 1) * PAGE_SIZE >= total)
            break;
        }
      }

      return all;
    }

    /// <summary>
    /// Asks one address and hands back the JSON it answered with. Everything that can go wrong on the way is
    /// turned into <see cref="TvBackendException"/>, because the screens are the ones that have to say what
    /// happened and they should only ever have to catch one thing.
    /// </summary>
    private async Task<JsonDocument> GetJsonAsync(string pathAndQuery, CancellationToken cancellation)
    {
      Uri? baseAddress = BaseUrl;
      if (baseAddress == null)
        throw new InvalidOperationException(NotConfiguredSays);

      Uri url = new Uri(baseAddress, pathAndQuery);

      HttpResponseMessage response;
      try
      {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
        {
          AddLogin(request);
          response = await _http.SendAsync(request, cancellation).ConfigureAwait(false);
        }
      }
      catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
      {
        throw new TvBackendException(url, string.Format(
            "nobody answered at {0} within {1} seconds - is the TV backend running, and is that the port it "
            + "listens on?", url.Host, (int) _http.Timeout.TotalSeconds), ex);
      }
      catch (HttpRequestException ex)
      {
        throw new TvBackendException(url, string.Format("could not reach {0} ({1})", url.Host, ex.Message), ex);
      }
      catch (UriFormatException ex)
      {
        throw new TvBackendException(url, string.Format(
            "the address on the television settings could not be used to ask anything: {0}",
            _settings.Describe()), ex);
      }

      using (response)
      {
        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
          throw new TvBackendException(url, response.StatusCode, body);

        if (string.IsNullOrWhiteSpace(body))
          return JsonDocument.Parse("{\"entries\":[]}");

        try
        {
          return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
          throw new TvBackendException(url, string.Format(
              "{0} answered something that was not the answer a TV backend gives. {1} is probably a different "
              + "machine, or TVHeadend is not answering on that port", url.Host, _settings.Describe()), ex);
        }
      }
    }

    /// <summary>
    /// Puts the household's name and password on the question. On the message rather than on the client, for the
    /// reason the server's token is: a credential belongs to the machine being asked, and a client outlives both
    /// the address and the answer.
    /// </summary>
    private void AddLogin(HttpRequestMessage request)
    {
      if (string.IsNullOrWhiteSpace(_settings.Username))
        return;

      string pair = _settings.Username + ":" + (_settings.Password ?? "");
      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
          Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pair)));
    }

    private static string? Text(JsonElement entry, string name)
    {
      if (!entry.TryGetProperty(name, out JsonElement value))
        return null;

      switch (value.ValueKind)
      {
        case JsonValueKind.String:
          return value.GetString();
        case JsonValueKind.Number:
          return value.GetRawText();
        default:
          return null;
      }
    }

    /// <summary>
    /// A list of ids, read tolerantly. A backend answers these as an array of strings, as an array of numbers,
    /// and - for a tag holding exactly one thing, in one older version people still run - as the single value on
    /// its own. All three mean the same list to a screen.
    /// </summary>
    private static List<string> ReadIdList(JsonElement entry, string name)
    {
      List<string> ids = new List<string>();
      if (!entry.TryGetProperty(name, out JsonElement value))
        return ids;

      if (value.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement item in value.EnumerateArray())
        {
          string? said = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
          if (!string.IsNullOrWhiteSpace(said))
            ids.Add(said.Trim());
        }
        return ids;
      }

      string? alone = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
      if (!string.IsNullOrWhiteSpace(alone))
        ids.Add(alone!.Trim());
      return ids;
    }

    private static int? Whole(JsonElement entry, string name)
    {
      if (!entry.TryGetProperty(name, out JsonElement value))
        return null;
      return NumberIn(value);
    }

    /// <summary>
    /// A whole number written either way round: the backend answers most numbers as numbers and some as text,
    /// and its ids are text that means a hexadecimal number.
    /// </summary>
    private static int? NumberIn(JsonElement value)
    {
      if (value.ValueKind == JsonValueKind.Number)
      {
        if (value.TryGetInt32(out int direct))
          return direct;
        if (value.TryGetInt64(out long wide))
          return wide >= int.MinValue && wide <= int.MaxValue ? (int) wide : (int?) null;
        return null;
      }

      if (value.ValueKind != JsonValueKind.String)
        return null;

      string said = (value.GetString() ?? "").Trim();
      if (said.Length == 0)
        return null;

      if (said.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
          && int.TryParse(said.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex))
        return hex;

      if (int.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out int plain))
        return plain;

      return null;
    }

    /// <summary>The same question one number wider, because seconds since 1970 do not fit in 2038.</summary>
    private static long? Wide(JsonElement entry, string name)
    {
      if (!entry.TryGetProperty(name, out JsonElement value))
        return null;

      if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long direct))
        return direct;

      string? said = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
      if (said != null && long.TryParse(said.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
              out long read))
        return read;

      return null;
    }

    private static bool TryInstant(JsonElement entry, string name, out DateTimeOffset when)
    {
      long? seconds = Wide(entry, name);
      if (!seconds.HasValue)
      {
        when = DateTimeOffset.MinValue;
        return false;
      }

      when = DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
      return true;
    }
  }
}
