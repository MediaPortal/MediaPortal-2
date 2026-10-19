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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// Reads the server's HTTP API. Everything the front end knows about the media library arrives through here.
  /// </summary>
  /// <remarks>
  /// <para>
  /// One class, four GETs, no caching and no retries: deciding what to do when the server is not there belongs
  /// to whoever asked, and a client that retries on its own turns a dead server into a screen that thinks for
  /// ten seconds before saying so.
  /// </para>
  /// <para>
  /// The base URL keeps MP2's prefix (<c>http://host:55555/MediaPortal/Server</c>) rather than being a bare
  /// origin, because that prefix is how the server is actually deployed - the same listener also answers below
  /// <c>/MediaPortal/UPnPServer_&#42;</c>, and the health endpoint exists partly to prove the prefix arrives
  /// intact on both HTTP hosts. <see cref="BaseUrl"/> therefore always ends with a slash: <c>HttpClient</c>
  /// resolves a relative URI against the *directory* of its base address, so a base without the slash would
  /// silently turn <c>api/serverinfo</c> into <c>/MediaPortal/api/serverinfo</c>.
  /// </para>
  /// </remarks>
  public sealed class ServerApiClient : IDisposable, IServerSettingsApi
  {
    /// <summary>
    /// Where the front end looks for a server when nothing else was said. The port is MP2's own and the prefix
    /// is the one a fresh install serves.
    /// </summary>
    public const string DefaultUrl = "http://127.0.0.1:55555/MediaPortal/Server";

    /// <summary>
    /// Environment variable that overrides the default, so a packaged client can be pointed at a server without
    /// being rebuilt or given an argument.
    /// </summary>
    public const string UrlEnvironmentVariable = "MP2_SERVER";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>
    /// How long one request waits before this client calls it "no answer".
    /// </summary>
    /// <remarks>
    /// <see cref="HttpClient"/>'s own default is 100 seconds, which is a sensible guess for a download and a bad
    /// one for a screen: somebody standing in front of a television should be told within a few seconds that the
    /// server is not answering, not after a minute and a half. A caller who wants a different patience passes it
    /// to the constructor.
    /// </remarks>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private string? _accessToken;

    public ServerApiClient(Uri baseUrl, HttpClient? httpClient = null, TimeSpan? timeout = null)
    {
      if (baseUrl == null)
        throw new ArgumentNullException(nameof(baseUrl));

      string text = baseUrl.ToString();
      BaseUrl = text.EndsWith("/", StringComparison.Ordinal) ? baseUrl : new Uri(text + "/");
      _ownsHttpClient = httpClient == null;
      _http = httpClient ?? new HttpClient();
      _http.BaseAddress = BaseUrl;
      if (timeout.HasValue)
        _http.Timeout = timeout.Value;
      else if (_ownsHttpClient)
        _http.Timeout = DefaultTimeout;
    }

    /// <summary>
    /// The URL every request of this client is relative to, always with a trailing slash.
    /// </summary>
    public Uri BaseUrl { get; }

    /// <summary>
    /// How long one request of this client waits before it is called no answer. Readable so that the patience a
    /// screen is built with is something a test can state instead of something a reader has to trust.
    /// </summary>
    public TimeSpan Timeout { get { return _http.Timeout; } }

    /// <summary>
    /// The bearer token this client sends along, or <c>null</c> while it is asking as a stranger.
    /// </summary>
    /// <remarks>
    /// Set by <see cref="SignInAsync"/> and sent as <c>Authorization: Bearer</c> on every following request. A
    /// server that has MP2's default configuration answers a stranger at the library endpoints with 403 - that
    /// is the product's own rule, gate-tested in <c>run-server-check.ps1</c> - so this field is the difference
    /// between a media centre and a screen that says "not for you".
    /// </remarks>
    public string? AccessToken
    {
      get { return _accessToken; }
      set { _accessToken = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }
    }

    /// <summary>
    /// Asks the server who this is, and keeps the token it hands back for every later request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MP2's own OAuth endpoint at <c>POST Token</c>, password grant, form encoded - the endpoint the 2.3 remote
    /// clients use, with the error strings it has always produced. Nothing here invents a second way in.
    /// </para>
    /// <para>
    /// Over plain HTTP, which is what MP2's resource server speaks on a home network. A password across a network
    /// the viewer does not control is a bad idea and this code does not pretend otherwise; the server's own
    /// "Web authorization" setting is the switch for a deployment that must not identify its callers at all.
    /// </para>
    /// </remarks>
    /// <returns>The name the server says it signed in, which is what the screen repeats back.</returns>
    public async Task<string> SignInAsync(string userName, string password, CancellationToken cancellation)
    {
      if (string.IsNullOrWhiteSpace(userName))
        throw new ArgumentException("Nobody is named.", nameof(userName));

      Uri url = new Uri(BaseUrl, "Token");
      FormUrlEncodedContent form = new FormUrlEncodedContent(new[]
      {
          new KeyValuePair<string, string>("grant_type", "password"),
          new KeyValuePair<string, string>("username", userName),
          new KeyValuePair<string, string>("password", EncodePassword(password))
      });

      try
      {
        using (HttpResponseMessage response = await _http.PostAsync(url, form, cancellation).ConfigureAwait(false))
        {
          string body = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
          TokenAnswer? answer = TryRead(body);

          if (!response.IsSuccessStatusCode || answer == null || string.IsNullOrEmpty(answer.access_token))
          {
            string said = answer != null && !string.IsNullOrEmpty(answer.error_description)
                ? answer.error_description
                : Summarize(body);
            if (string.IsNullOrEmpty(said))
              said = "the server answered " + (int) response.StatusCode;
            throw new ServerApiException(url, response.StatusCode, said);
          }

          _accessToken = answer.access_token;
          return userName;
        }
      }
      catch (ServerApiException)
      {
        throw;
      }
      catch (HttpRequestException ex)
      {
        throw new ServerApiException(url, string.Format("No usable answer from {0}: {1}", url, ex.Message), ex);
      }
      catch (TaskCanceledException ex)
      {
        if (cancellation.IsCancellationRequested)
          throw;
        throw new ServerApiException(url, string.Format("{0} did not answer within {1}.", url, _http.Timeout), ex);
      }
    }

    /// <summary>
    /// Forgets who this client signed in as. The next request is a stranger's again.
    /// </summary>
    public void SignOut()
    {
      _accessToken = null;
    }

    /// <summary>
    /// How MP2's token endpoint wants the password on the wire.
    /// </summary>
    /// <remarks>
    /// Not this client's invention: <c>ResourceServer.GrantResourceOwnerCredentials</c> calls
    /// <c>Convert.FromBase64String</c> on whatever arrives in the <c>password</c> field and decodes the result as
    /// UTF-8. A plain-text password therefore is not merely rejected - it is a format error on the server. This
    /// is encoding, not protection: over MP2's plain HTTP the password is readable by anybody on the network,
    /// which is why the server's "Web authorization" switch exists.
    /// </remarks>
    private static string EncodePassword(string? password)
    {
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(password ?? string.Empty));
    }

    private static TokenAnswer? TryRead(string body)
    {
      try
      {
        return JsonSerializer.Deserialize<TokenAnswer>(body, JSON_OPTIONS);
      }
      catch (JsonException)
      {
        return null;
      }
    }

    /// <summary>
    /// The body of <c>POST Token</c>, in both directions. The server spells its fields the way OAuth does and
    /// the property names say so; <paramref name="userName"/> is what the server chose to answer with, which is
    /// not always what was typed.
    /// </summary>
    private sealed class TokenAnswer
    {
      public string? access_token { get; set; }
      public string? token_type { get; set; }
      public long expires_in { get; set; }
      public string? error { get; set; }
      public string? error_description { get; set; }
    }

    /// <summary>
    /// Works out which server to talk to: an explicit argument wins over <c>MP2_SERVER</c>, which wins over
    /// <see cref="DefaultUrl"/>.
    /// </summary>
    public static Uri ResolveBaseUrl(string? fromCommandLine)
    {
      return ResolveBaseUrl(fromCommandLine, null);
    }

    /// <summary>
    /// The same, with the address the viewer last saved on the screen taking part.
    /// </summary>
    /// <remarks>
    /// The order is argument, environment, saved address, built-in default. A saved address loses to the first two
    /// on purpose: whoever starts the front end over ssh, or in a container, knows which server they meant, and that
    /// must not be silently overridden by whatever was typed into the box months ago. It wins over the default,
    /// because the default is a guess and the saved address was a decision.
    /// </remarks>
    public static Uri ResolveBaseUrl(string? fromCommandLine, string? fromSettings)
    {
      return ResolveBaseUrl(fromCommandLine, Environment.GetEnvironmentVariable(UrlEnvironmentVariable),
                            fromSettings);
    }

    /// <summary>
    /// The rule itself, with every input in the signature: the environment is not a constant, it is one of the
    /// four places the address can come from.
    /// </summary>
    /// <remarks>
    /// This overload exists because the container image sets <c>MP2_SERVER</c> for its own reasons, and a test
    /// that asked the two-argument version "where would a start with no argument go?" got the image's answer
    /// instead of the rule's. Saying the environment out loud is also the only way to test that it really does
    /// outrank a saved address - setting it process-wide to find out would disturb every other test in the run.
    /// </remarks>
    public static Uri ResolveBaseUrl(string? fromCommandLine, string? fromEnvironment, string? fromSettings)
    {
      string chosen = FirstNonEmpty(fromCommandLine, fromEnvironment, fromSettings, DefaultUrl);
      if (!Uri.TryCreate(chosen, UriKind.Absolute, out Uri? parsed))
        throw new ArgumentException(string.Format(
            "'{0}' is not an absolute URL; expected something like {1}", chosen, DefaultUrl), nameof(fromCommandLine));
      return parsed;
    }

    /// <summary>
    /// <c>GET health</c>. The one endpoint that does not need the server's services, so it is what a client uses
    /// to find out whether anything is listening and under which prefix it answered.
    /// </summary>
    public async Task<HealthInfo> GetHealthAsync(CancellationToken cancellation)
    {
      return await GetJsonAsync<HealthInfo>("health", cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/serverinfo</c>.
    /// </summary>
    public async Task<ServerInfo> GetServerInfoAsync(CancellationToken cancellation)
    {
      return await GetJsonAsync<ServerInfo>("api/serverinfo", cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/library/aspects</c> - the schema of the media database.
    /// </summary>
    public async Task<LibraryAspects> GetLibraryAspectsAsync(CancellationToken cancellation)
    {
      return await GetJsonAsync<LibraryAspects>("api/library/aspects", cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/library/count</c>, for one aspect by name or for every media item when <paramref name="aspect"/>
    /// is <c>null</c>.
    /// </summary>
    public async Task<MediaCount> GetMediaCountAsync(string? aspect, CancellationToken cancellation)
    {
      string url = aspect == null
          ? "api/library/count"
          : "api/library/count?aspect=" + Uri.EscapeDataString(aspect);
      return await GetJsonAsync<MediaCount>(url, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/media/titles</c> - the titles carrying one aspect, one page at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the first endpoint of this client that can answer <c>403</c>. Titles are a household's data, and
    /// MP2's rule is that a caller is identified unless the server was configured not to care; where it does
    /// care, an anonymous request is refused. That reaches the caller as a <see cref="ServerApiException"/> with
    /// <see cref="ServerApiException.StatusCode"/> 403, and the screen turns it into "this server wants you to
    /// sign in". It is not retried here: an answer that was a decision is not a hiccup.
    /// </para>
    /// <para>
    /// <paramref name="limit"/> and <paramref name="sort"/> are sent only when they say something, so that the
    /// URL a viewer sees in a log is the request they meant rather than the whole parameter list with defaults
    /// spelled out.
    /// </para>
    /// </remarks>
    /// <param name="seen">
    /// <c>unwatched</c> or <c>watched</c>, or null for both. Lower-cased on the way out because the server's own
    /// answer quotes those two words and a log line should read like the answer.
    /// </param>
    /// <param name="inside">
    /// The id of a title that holds others - an album, a series, a season - to list the titles inside it, or null
    /// for the whole shelf. The server answers a parent that cannot hold this kind of title with a refusal, so a
    /// screen that got here by pressing an album is asking a question the server agrees is worth answering.
    /// </param>
    /// <param name="by">
    /// A category of <see cref="GetGroupsAsync"/> - <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c>,
    /// <c>writers</c> - to keep only the titles carrying <paramref name="value"/> under it, the second half of
    /// browsing: the groups said which genres there are, this asks which films are in one of them. Sent together
    /// or not at all, because the server refuses a category whose value went missing, and rightly.
    /// </param>
    public async Task<MediaTitles> GetTitlesAsync(string aspect, string? sort = null, bool descending = false,
        int limit = 0, int offset = 0, string? search = null, string? seen = null, Guid? inside = null,
        string? by = null, string? value = null,
        CancellationToken cancellation = default)
    {
      if (string.IsNullOrWhiteSpace(aspect))
        throw new ArgumentException("Titles of which aspect?", nameof(aspect));

      string url = "api/media/titles?aspect=" + Uri.EscapeDataString(aspect.Trim());
      if (!string.IsNullOrEmpty(sort))
        url += "&sort=" + Uri.EscapeDataString(sort);
      if (descending)
        url += "&order=desc";
      if (limit > 0)
        url += "&limit=" + limit.ToString(CultureInfo.InvariantCulture);
      if (offset > 0)
      {
        url += "&offset=" + offset.ToString(CultureInfo.InvariantCulture);
      }
      if (!string.IsNullOrWhiteSpace(search))
      {
        // Escaped, because a viewer searching for "2 + 2" or for "A_B" would otherwise be sending a URL whose
        // characters mean something else than the words they typed.
        url += "&search=" + Uri.EscapeDataString(search.Trim());
      }
      if (!string.IsNullOrWhiteSpace(seen))
      {
        url += "&seen=" + Uri.EscapeDataString(seen.Trim().ToLowerInvariant());
      }
      if (inside.HasValue && inside.Value != Guid.Empty)
      {
        // The id in its hyphenated form, which is the one the server printed in the list this id was picked from
        // and needs no escaping.
        url += "&inside=" + inside.Value.ToString("D");
      }
      if (!string.IsNullOrWhiteSpace(by) && !string.IsNullOrWhiteSpace(value))
      {
        // Both or neither, as above; a genre with a space in its name is the reason for escaping here too.
        url += "&by=" + Uri.EscapeDataString(by.Trim())
            + "&value=" + Uri.EscapeDataString(value.Trim());
      }

      return await GetJsonAsync<MediaTitles>(url, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/media/groups</c> - the values one category takes across one shelf, and how many titles carry
    /// each, counted by the database rather than by whoever is asking.
    /// </summary>
    /// <param name="aspect">The shelf to count, by its server name.</param>
    /// <param name="by">The category: <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c> or <c>writers</c>.</param>
    /// <remarks>
    /// The answer is already in the server's own order - biggest category first, then by name - and the client
    /// keeps it, because that order is a statement about the library and a screen that re-sorted it alphabetically
    /// would quietly unpick the one piece of judgement the counting involved.
    /// </remarks>
    public async Task<MediaGroups> GetGroupsAsync(string aspect, string by, CancellationToken cancellation = default)
    {
      if (string.IsNullOrWhiteSpace(aspect))
        throw new ArgumentException("Groups of which aspect?", nameof(aspect));
      if (string.IsNullOrWhiteSpace(by))
        throw new ArgumentException("Grouped by what?", nameof(by));

      string url = "api/media/groups?aspect=" + Uri.EscapeDataString(aspect.Trim())
          + "&by=" + Uri.EscapeDataString(by.Trim());
      return await GetJsonAsync<MediaGroups>(url, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// What the server knows about one title, by the id a title list gave.
    /// </summary>
    /// <param name="mediaItemId">The id from <see cref="MediaTitle.Id"/>.</param>
    /// <remarks>
    /// A 404 is an exception like any other answer this client does not understand, and the message carries the
    /// server's own words: "this server has no title with the id ..." is what a details screen should say, because
    /// it is what happened - usually that the library was rebuilt between listing a title and asking about it.
    /// </remarks>
    public async Task<MediaItemDetails> GetItemAsync(Guid mediaItemId, CancellationToken cancellation = default)
    {
      return await GetJsonAsync<MediaItemDetails>(
          "api/media/items/" + mediaItemId.ToString("D"), cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// The picture of a title, as the server's database holds it, or null when it has none.
    /// </summary>
    /// <param name="mediaItemId">The id from <see cref="MediaTitle.Id"/>.</param>
    /// <returns>The bytes of the picture - PNG, JPEG, whatever the importer stored - or null.</returns>
    /// <remarks>
    /// <para>
    /// <b>A missing picture is not a failure.</b> A 404 here means either that the title is gone or that nobody
    /// ever gave it a poster, and both leave a screen drawing the same placeholder; turning that into an exception
    /// would mean every shelf of titles without art threw an exception per row. A server that is not there at all
    /// still throws, because that is worth telling the viewer about - it is the difference between "this film has
    /// no poster" and "the server went away".
    /// </para>
    /// <para>
    /// Nothing here decodes or resizes the bytes. The server stores the picture at MP2's own thumbnail size (at
    /// most 256 pixels across, which is what the old skin engine drew), so a front end that scaled it again would
    /// only blur it.
    /// </para>
    /// </remarks>
    public async Task<byte[]?> GetThumbnailAsync(Guid mediaItemId, CancellationToken cancellation = default)
    {
      Uri url = new Uri(BaseUrl, "api/media/items/" + mediaItemId.ToString("D") + "/thumbnail");
      RawAnswer answer = await GetRawAsync(url, cancellation).ConfigureAwait(false);

      if (answer.Status == HttpStatusCode.NotFound)
        return null;
      if (!answer.Success)
        throw new ServerApiException(url, answer.Status, Summarize(answer.Text()));
      return answer.Body;
    }

    /// <summary>
    /// The background fanart the skin may paint behind an item list: a poster or backdrop from the server's
    /// own fanart cache, or null when the library has no picture for that title.
    /// </summary>
    /// <param name="mediaItemId">The id from <see cref="MediaTitle.Id"/>.</param>
    /// <param name="type">
    /// The kind of picture asked for - <c>"Banner"</c>, <c>"FanArt"</c>, <c>"Thumbnail"</c> and the other kinds
    /// the server names - or null for the server's own choice of poster-or-backdrop. The kinds matter because a
    /// banner tile that drew a poster would stretch it, which is the lie the typed endpoint exists to prevent.
    /// </param>
    public async Task<byte[]?> GetFanArtAsync(Guid mediaItemId, string? type = null,
        CancellationToken cancellation = default)
    {
      Uri url = new Uri(BaseUrl, "api/media/items/" + mediaItemId.ToString("D") + "/fanart"
          + (string.IsNullOrEmpty(type) ? "" : "/" + Uri.EscapeDataString(type.Trim())));
      RawAnswer answer = await GetRawAsync(url, cancellation).ConfigureAwait(false);
      if (answer.Status == HttpStatusCode.NotFound)
        return null;
      if (!answer.Success)
        throw new ServerApiException(url, answer.Status, Summarize(answer.Text()));
      return answer.Body;
    }

    /// <summary>
    /// The address a player is given to watch one title from this server.
    /// </summary>
    /// <param name="mediaItemId">The id from <see cref="MediaTitle.Id"/>.</param>
    /// <remarks>
    /// <para>
    /// This builds an address and asks for nothing, which is why it is not a <c>Task</c> like everything else in
    /// this class: the bytes are fetched by the player, over the course of a film, with seeks of its own. A client
    /// that fetched them here would be a web server in the middle of its own living room.
    /// </para>
    /// <para>
    /// <b>What it does not solve is the bearer token.</b> The player opens this address with its own HTTP stack,
    /// which has never heard of the token this client signed in with, so on a server with web authorization on,
    /// the answer to a stream is the same refusal every other endpoint gives an anonymous caller. That is a real
    /// limit of handing a player an address, not of this address; the front end says so rather than pretending,
    /// and the plain answer - a short-lived grant in the URL, or the token handed to the player as a header - is
    /// a decision about the API's authorization, not something to smuggle into a playback screen.
    /// </para>
    /// </remarks>
    public Uri GetStreamUri(Guid mediaItemId)
    {
      return new Uri(BaseUrl, "api/media/items/" + mediaItemId.ToString("D") + "/stream");
    }

    /// <summary>
    /// Tells the server how far a viewer got into a title, so that the library's "watched" figures say something
    /// about the viewer rather than about the file's import date.
    /// </summary>
    /// <param name="mediaItemId">The id from <see cref="MediaTitle.Id"/>.</param>
    /// <param name="percentage">0 to 100; 100 says the title was watched to the end.</param>
    /// <returns>What the library believes now - the count and the date, as it holds them.</returns>
    /// <remarks>
    /// <para>
    /// JSON rather than a query parameter, because the number is the content of the request rather than a way of
    /// choosing which title: <c>?percentage=100</c> on a POST reads like a GET that was told to lie.
    /// </para>
    /// <para>
    /// Nothing here retries. A report that failed is a report that did not happen, and a client that kept trying
    /// against a server that said "no such title" would be arguing with an answer that is not going to change -
    /// while the viewer has already gone to bed. Whoever called it decides whether to say anything.
    /// </para>
    /// </remarks>
    public async Task<MediaPlayback> ReportPlaybackAsync(Guid mediaItemId, int percentage,
        CancellationToken cancellation = default)
    {
      if (percentage < 0 || percentage > 100)
      {
        throw new ArgumentOutOfRangeException(nameof(percentage), percentage,
            "How far a viewer watched is a number from 0 to 100.");
      }

      return await SendJsonAsync<MediaPlayback>(HttpMethod.Post,
          "api/media/items/" + mediaItemId.ToString("D") + "/playback",
          new Dictionary<string, object> { ["percentage"] = percentage }, cancellation)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/media/shares</c> - the folders the library is built from, and the media categories a new one
    /// can be filed under.
    /// </summary>
    public async Task<SharesAnswer> GetSharesAsync(CancellationToken cancellation = default)
    {
      return await SendJsonAsync<SharesAnswer>(HttpMethod.Get, "api/media/shares", null, cancellation)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>POST api/media/shares</c> - point the server at a folder.
    /// </summary>
    /// <param name="path">
    /// The folder, spelled the way the <b>server's</b> filesystem spells it. This client cannot check that: the
    /// front end is often not on the same machine as the server, and a path that means nothing here may be the
    /// right one there. What the server insists on comes back as a refusal with its reason attached.
    /// </param>
    /// <param name="categories">
    /// The kinds of media the folder holds, as <see cref="SharesAnswer.MediaCategories"/> reported them, or null
    /// for "whatever you can find". Naming them is worth doing: a folder filed under a category no extractor
    /// reads is imported and stays empty.
    /// </param>
    /// <param name="name">What to call the share, or null for the folder's own name.</param>
    /// <param name="useShareWatcher">
    /// Whether the server should notice changes by itself, or null to leave the server's default.
    /// </param>
    /// <remarks>
    /// Three answers here are decisions rather than failures and arrive as <see cref="ServerApiException"/> with
    /// the server's sentence in <see cref="ServerApiException.Message"/>: <c>400</c> when the path is not one the
    /// server can use or a category is unknown (the refusal lists the ones it knows), and <c>409</c> when the
    /// folder is already shared - which names the share that owns it, so a screen can point at the existing row
    /// instead of adding a second one.
    /// </remarks>
    public async Task<ShareInfo> AddShareAsync(string path, IEnumerable<string>? categories = null,
        string? name = null, bool? useShareWatcher = null, CancellationToken cancellation = default)
    {
      if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Which folder?", nameof(path));

      List<string>? chosen = categories == null
          ? null
          : new List<string>(categories.Where(category => !string.IsNullOrWhiteSpace(category))
              .Select(category => category.Trim()));
      if (chosen != null && chosen.Count == 0)
        chosen = null;

      return await SendJsonAsync<ShareInfo>(HttpMethod.Post, "api/media/shares",
          new ShareRequest(path.Trim(), name, chosen, useShareWatcher), cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>DELETE api/media/shares/{id}</c> - stop watching a folder and drop the titles that came out of it.
    /// </summary>
    /// <remarks>
    /// This is the one call of this client that answers <c>204</c> with no body, because there is nothing left to
    /// describe. A <c>404</c> means the share is already gone - which for a screen that was about to delete it is
    /// the state it wanted, though it is still worth saying so rather than pretending the button did something.
    /// </remarks>
    public async Task RemoveShareAsync(Guid mediaShareId, CancellationToken cancellation = default)
    {
      await SendAsync(HttpMethod.Delete, "api/media/shares/" + mediaShareId.ToString("D"), cancellation)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>POST api/media/shares/{id}/import</c> - walk that folder again, now.
    /// </summary>
    /// <returns>The queue the job was added to.</returns>
    /// <remarks>
    /// Answered with <c>202</c> and the queue rather than with the result, because importing a folder tree takes
    /// as long as it takes; <see cref="GetImportStatusAsync"/> is how a screen finds out afterwards. Refused with
    /// <c>409</c> for a share on another system or one without categories - both would queue a job that cannot
    /// import anything.
    /// </remarks>
    public async Task<ImportStatus> ImportShareAsync(Guid mediaShareId, CancellationToken cancellation = default)
    {
      return await SendJsonAsync<ImportStatus>(HttpMethod.Post,
          "api/media/shares/" + mediaShareId.ToString("D") + "/import", null, cancellation)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/media/import</c> - how much the importer still has to do.
    /// </summary>
    public async Task<ImportStatus> GetImportStatusAsync(CancellationToken cancellation = default)
    {
      return await SendJsonAsync<ImportStatus>(HttpMethod.Get, "api/media/import", null, cancellation)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET api/media/settings</c> - how the server is configured: login requirement,
    /// listening address, and the importer's own schedule.
    /// </summary>
    /// <remarks>
    /// These are the <b>server's</b> settings, stored on the machine the server runs on.
    /// Refused with <c>401</c> when the server demands logins and this client has none -
    /// which is the one setting here a screen can use to explain a server that has gone
    /// quiet since it was configured.
    /// </remarks>
    public async Task<ServerSettingsAnswer> GetServerSettingsAsync(CancellationToken cancellation = default)
    {
      return await SendJsonAsync<ServerSettingsAnswer>(HttpMethod.Get, "api/media/settings", null, cancellation)
          .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>PUT api/media/settings</c> - change how the server behaves, and have it store
    /// the change.
    /// </summary>
    /// <param name="update">
    /// The complete new state, read from <see cref="GetServerSettingsAsync"/> and edited.
    /// The server stores what arrives, so fields not meant to change must come back as
    /// they were.
    /// </param>
    /// <returns>
    /// What the server now has, including <see cref="ServerSettingsAnswer.RestartNeededFor"/>:
    /// the fields it has stored but will only act on after a restart. Saying "saved" for
    /// a port number that stays the old one until the server comes back would be a lie.
    /// </returns>
    /// <remarks>
    /// Refused with <c>400</c> and the server's reason for a port outside 1-65535 or an
    /// import interval outside 0-24 hours.
    /// </remarks>
    public async Task<ServerSettingsAnswer> PutServerSettingsAsync(ServerSettingsUpdate update,
        CancellationToken cancellation = default)
    {
      return await SendJsonAsync<ServerSettingsAnswer>(HttpMethod.Put, "api/media/settings", update, cancellation)
          .ConfigureAwait(false);
    }

    #region The folders behind the library

    /// <summary>
    /// Where a folder's contents are asked for.
    /// </summary>
    public const string BrowseUrl = "api/media/browse";

    /// <summary>
    /// The path the server serves the bytes of any resource at - a picture, a file nobody filed as anything.
    /// </summary>
    public const string ResourceUrl = "GetResource";

    /// <summary>
    /// What one folder of the server's files holds: its subfolders and the pictures inside it.
    /// </summary>
    /// <param name="resourcePath">A serialized resource path, as <see cref="GetSharesAsync"/> reports them.</param>
    /// <remarks>
    /// The path goes in a query string, URL-encoded, because a serialized resource path contains ':' and '/' and
    /// sometimes a space, and a path segment would have to be escaped anyway - <c>?</c> says plainly that the whole
    /// rest of the address is one argument rather than a place in a hierarchy the server would try to route.
    /// </remarks>
    public async Task<FolderListing> GetFolderAsync(string resourcePath, CancellationToken cancellation = default)
    {
      if (string.IsNullOrEmpty(resourcePath))
        throw new ArgumentException("Say which folder to look in.", nameof(resourcePath));

      return await SendJsonAsync<FolderListing>(HttpMethod.Get,
          BrowseUrl + "?path=" + Uri.EscapeDataString(resourcePath), null, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// The address of a resource's bytes on this server.
    /// </summary>
    /// <param name="resourcePath">The serialized resource path.</param>
    /// <remarks>
    /// This is the address the server's own file server was built for and which UPnP renderers are handed; the
    /// front end uses it for the same purpose, to fetch a picture. Unlike <see cref="GetStreamUri"/> it is not a
    /// player's address - the bytes arrive here through <see cref="GetResourceBytesAsync"/>, with this client's
    /// token on the request.
    /// </remarks>
    public Uri GetResourceUri(string resourcePath)
    {
      if (string.IsNullOrEmpty(resourcePath))
        throw new ArgumentException("Say which resource is wanted.", nameof(resourcePath));

      return new Uri(BaseUrl, ResourceUrl + "?ResourcePath=" + Uri.EscapeDataString(resourcePath));
    }

    /// <summary>
    /// The bytes of one resource, or <c>null</c> when the server will not or cannot give them.
    /// </summary>
    /// <param name="resourcePath">The serialized resource path.</param>
    /// <remarks>
    /// <para>
    /// A refusal is an answer and not a fault here. The file server says <c>403</c> for anything outside a share
    /// and <c>404</c> - through its own error path - for a file that is gone, and to a picture screen both mean
    /// the same thing: this one is not here to look at. Every other failure keeps its <see cref="ServerApiException"/>
    /// so that a server that is down is not reported as an empty folder.
    /// </para>
    /// <para>
    /// There is no smaller version of this: the server has no way to resize an image, so a tile's thumbnail is the
    /// whole photograph. That is why the picture screen asks for a bounded number of them at a time and says
    /// which ones it did not fetch.
    /// </para>
    /// </remarks>
    public async Task<byte[]?> GetResourceBytesAsync(string resourcePath, CancellationToken cancellation = default)
    {
      Uri url = GetResourceUri(resourcePath);
      RawAnswer answer = await GetRawAsync(url, cancellation).ConfigureAwait(false);

      if (answer.Status == HttpStatusCode.NotFound || answer.Status == HttpStatusCode.Forbidden)
        return null;
      if (!answer.Success)
        throw new ServerApiException(url, answer.Status, Summarize(answer.Text()));
      return answer.Body;
    }

    #endregion

    #region The event stream

    /// <summary>
    /// Where the server says what happened without being asked.
    /// </summary>
    public const string EventsUrl = "api/media/events";

    /// <summary>
    /// How long the first wait after a lost stream is, and the ceiling it doubles up to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One second, then two, four, eight, sixteen, and no further: a server that is restarting comes back within
    /// the first three steps, and a server that is switched off tomorrow should cost this client one request a
    /// half-minute rather than one a second until the power returns. Writable for the same reason the server's own
    /// waiting times are: a test that measures reconnection should not spend half a minute on clocks.
    /// </para>
    /// <para>
    /// The server also offers a <c>retry:</c> field for telling a client this. It is ignored on purpose - the
    /// client is the one that knows whether a television is sitting in front of nothing.
    /// </para>
    /// </remarks>
    public static TimeSpan EventRetryTime = TimeSpan.FromSeconds(1);

    /// <summary>The longest the client will ever wait before asking for the stream again.</summary>
    public static TimeSpan EventRetryCeiling = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long to wait after the <paramref name="attempt"/>'th failure, in the ladder described on
    /// <see cref="EventRetryTime"/>.
    /// </summary>
    public static TimeSpan EventRetryDelay(int attempt)
    {
      if (attempt <= 0)
        return EventRetryTime;

      double seconds = EventRetryTime.TotalSeconds * Math.Pow(2, Math.Min(attempt, 10));
      TimeSpan grown = TimeSpan.FromSeconds(seconds);
      return grown > EventRetryCeiling ? EventRetryCeiling : grown;
    }

    /// <summary>
    /// Keeps the server's event stream open, hands every frame to <paramref name="onEvent"/>, and reconnects when
    /// the stream breaks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why the task does not end.</b> The stream is how the front end learns that the library changed while
    /// nobody pressed anything, and one dropped connection should not be the end of that - the server restarting
    /// for a plugin update is an ordinary Tuesday. So the loop reconnects, with the patience of
    /// <see cref="EventRetryDelay"/>, and this task only finishes when it is cancelled or when continuing would be
    /// pretending.
    /// </para>
    /// <para>
    /// <b>The two things that do stop it.</b> A server that refuses the stream outright - 401 or 403 - is not a
    /// connection problem but a configuration one, and reconnecting forever behind a locked door would turn a
    /// message a person could act on into silence; that comes back as the <see cref="ServerApiException"/> every
    /// other endpoint uses. So does a frame this client cannot read, which means the two ends disagree about the
    /// protocol and no retry will make them agree.
    /// </para>
    /// <para>
    /// <b>Its own connection.</b> A stream is not a request: the 15 seconds this client waits for an answer is
    /// right for a shelf of titles and would kill a healthy stream the first time the library had nothing to say.
    /// The connection is therefore opened here, without a timeout, and cancellation is the only thing that ends it.
    /// </para>
    /// </remarks>
    /// <param name="onEvent">
    /// Called with every frame, on this task's thread, in the order they arrived. The screen is the caller's
    /// thread to get back to.
    /// </param>
    /// <param name="onConnection">
    /// Told when the stream is established and when it was lost and a reconnection is being attempted, so that a
    /// status line can say "watching the server" instead of lying about it.
    /// </param>
    /// <param name="cancellation">Ending the watch. The connection is closed on the way out.</param>
    public async Task WatchMediaEventsAsync(Func<MediaEvent, Task> onEvent,
        Action<MediaEventConnection>? onConnection = null, CancellationToken cancellation = default)
    {
      if (onEvent == null)
        throw new ArgumentNullException(nameof(onEvent));

      Uri url = new Uri(BaseUrl, EventsUrl);
      int failures = 0;

      while (!cancellation.IsCancellationRequested)
      {
        bool connected = false;
        try
        {
          connected = await ReadEventsAsync(url, onEvent, onConnection, cancellation).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
          return;
        }

        if (connected)
          // The stream was healthy and ended, which is what a server does when it shuts down. Start counting the
          // waits again from the top, because this is a new interruption rather than the old one persisting.
          failures = 0;

        TimeSpan wait = EventRetryDelay(failures);
        failures++;
        onConnection?.Invoke(MediaEventConnection.Reconnecting(wait));

        try
        {
          await Task.Delay(wait, cancellation).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          return;
        }
      }
    }

    /// <summary>
    /// One connection to the stream: open it, deliver frames until it ends, and say whether it ever worked.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the server accepted the request and at least the protocol was spoken, which is what makes
    /// the next wait short instead of long; <c>false</c> when it never got that far.
    /// </returns>
    /// <exception cref="ServerApiException">The server said this client may not watch.</exception>
    private async Task<bool> ReadEventsAsync(Uri url, Func<MediaEvent, Task> onEvent,
        Action<MediaEventConnection>? onConnection, CancellationToken cancellation)
    {
      using (HttpClient stream = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan })
      using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
      {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        AddToken(request);

        HttpResponseMessage response;
        try
        {
          response = await stream.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation)
              .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
          // No server, no route, a reset mid-handshake. The same complaint a person gets from the connect screen,
          // except that here it is worth trying again.
          return false;
        }
        catch (TaskCanceledException) when (!cancellation.IsCancellationRequested)
        {
          // Cannot happen with an infinite timeout, and saying so in code costs one line and spares somebody a
          // mystery when the timeout is ever made finite again.
          return false;
        }

        using (response)
        {
          if (response.StatusCode == HttpStatusCode.Unauthorized ||
              response.StatusCode == HttpStatusCode.Forbidden)
            throw new ServerApiException(url, response.StatusCode,
                response.ReasonPhrase ?? "The server would not let this client listen.");

          if (!response.IsSuccessStatusCode)
            return false;

          bool spoke = false;
          ServerEventFramer framer = new ServerEventFramer();

          using (Stream body = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false))
          using (StreamReader reader = new StreamReader(body, Encoding.UTF8))
          {
            while (!cancellation.IsCancellationRequested)
            {
              string? line = await reader.ReadLineAsync(cancellation).ConfigureAwait(false);
              if (line == null)
                // The server closed it. Its shutdown, or a proxy giving up; either way the loop above decides
                // whether to ask again.
                return spoke;

              Tuple<string?, string>? frame = framer.Feed(line);
              if (frame == null)
                continue;

              if (frame.Item2.Length == 0)
                // A frame with no body is a keepalive; it proved the wire works, which is all it does.
                continue;

              MediaEvent mediaEvent = MediaEvent.Parse(frame.Item2, frame.Item1);
              if (mediaEvent.Kind == MediaEvent.KIND_HELLO)
              {
                spoke = true;
                onConnection?.Invoke(MediaEventConnection.Connected);
              }

              await onEvent(mediaEvent).ConfigureAwait(false);
            }
          }

          return spoke;
        }
      }
    }

    #endregion

    /// <summary>
    /// The body of <see cref="AddShareAsync"/>.
    /// </summary>
    private sealed record ShareRequest(string Path, string? Name, List<string>? MediaCategories,
        bool? UseShareWatcher);

    public void Dispose()
    {
      if (_ownsHttpClient)
        _http.Dispose();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
      foreach (string? value in values)
        if (!string.IsNullOrWhiteSpace(value))
          return value.Trim();
      return DefaultUrl;
    }

    /// <summary>
    /// One request with a JSON answer, whatever the verb.
    /// </summary>
    /// <param name="method">The verb the endpoint answers.</param>
    /// <param name="relativeUrl">Address below the server's base.</param>
    /// <param name="body">The object to send as JSON, or null to send no body at all.</param>
    /// <remarks>
    /// The same rules as <see cref="GetJsonAsync"/>, and deliberately one code path rather than a second one
    /// written per verb: what a refusal means ("409, and here is why") has to be reported in the same words
    /// whether it came back from a GET or from a DELETE, or a screen ends up translating some of its answers
    /// and guessing at the rest.
    /// </remarks>
    private async Task<T> SendJsonAsync<T>(HttpMethod method, string relativeUrl, object? body,
        CancellationToken cancellation)
    {
      Uri url = new Uri(BaseUrl, relativeUrl);
      string? text = await SendAsync(method, url, SerializeBody(body), cancellation).ConfigureAwait(false);

      if (text == null)
        throw new ServerApiException(url,
            string.Format("The server answered {0} at {1} with nothing, where an answer was expected.",
                (int) HttpStatusCode.NoContent, url));

      T? parsed;
      try
      {
        parsed = JsonSerializer.Deserialize<T>(text, JSON_OPTIONS);
      }
      catch (JsonException ex)
      {
        throw new ServerApiException(url,
            string.Format("The answer from {0} could not be read: {1}", url, ex.Message), ex);
      }
      if (parsed == null)
        throw new ServerApiException(url,
            string.Format("The server answered {0} at {1} with a body that was not JSON.",
                (int) HttpStatusCode.OK, url));
      return parsed;
    }

    /// <summary>
    /// One request that answers with a status and nothing else - a deletion, which has no reason to describe
    /// what is no longer there.
    /// </summary>
    private async Task SendAsync(HttpMethod method, string relativeUrl, CancellationToken cancellation)
    {
      Uri url = new Uri(BaseUrl, relativeUrl);
      await SendAsync(method, url, null, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// The body of a request as JSON, or null when the caller had nothing to say.
    /// </summary>
    private static string? SerializeBody(object? body)
    {
      return body == null ? null : JsonSerializer.Serialize(body, JSON_OPTIONS);
    }

    /// <summary>
    /// One request out, the server's answer as text, and the failure turned into the one exception type.
    /// </summary>
    /// <returns>The body, or null when the server answered without one.</returns>
    private async Task<string?> SendAsync(HttpMethod method, Uri url, string? json,
        CancellationToken cancellation)
    {
      using (HttpRequestMessage request = new HttpRequestMessage(method, url))
      {
        if (json != null)
          request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        AddToken(request);
        try
        {
          using (HttpResponseMessage response = await _http.SendAsync(request,
              HttpCompletionOption.ResponseContentRead, cancellation).ConfigureAwait(false))
          {
            byte[] raw = await response.Content.ReadAsByteArrayAsync(cancellation).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
              throw new ServerApiException(url, response.StatusCode, Summarize(Encoding.UTF8.GetString(raw)));

            return raw.Length == 0 ? null : Encoding.UTF8.GetString(raw);
          }
        }
        catch (ServerApiException)
        {
          throw;
        }
        catch (HttpRequestException ex)
        {
          throw new ServerApiException(url,
              string.Format("No usable answer from {0}: {1}", url, ex.Message), ex);
        }
        catch (TaskCanceledException ex)
        {
          if (cancellation.IsCancellationRequested)
            throw;
          throw new ServerApiException(url,
              string.Format("{0} did not answer within {1}.", url, _http.Timeout), ex);
        }
      }
    }

    /// <summary>
    /// Puts the bearer token on one request, if this client has one.
    /// </summary>
    /// <remarks>
    /// On the message rather than in <see cref="HttpClient.DefaultRequestHeaders"/>, because a token belongs to
    /// one call: a screen that signs out while a read is in flight must not have its new anonymity overwritten by
    /// whatever that read set up, and a client that signs in twice must not send two authorizations.
    /// </remarks>
    private void AddToken(HttpRequestMessage request)
    {
      string? token = _accessToken;
      if (!string.IsNullOrEmpty(token))
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<T> GetJsonAsync<T>(string relativeUrl, CancellationToken cancellation)
    {
      Uri url = new Uri(BaseUrl, relativeUrl);
      RawAnswer answer = await GetRawAsync(url, cancellation).ConfigureAwait(false);
      string body = answer.Text();

      if (!answer.Success)
        throw new ServerApiException(url, answer.Status, Summarize(body));

      T? parsed;
      try
      {
        parsed = JsonSerializer.Deserialize<T>(body, JSON_OPTIONS);
      }
      catch (JsonException ex)
      {
        throw new ServerApiException(url,
            string.Format("The answer from {0} could not be read: {1}", url, ex.Message), ex);
      }
      if (parsed == null)
        throw new ServerApiException(url,
            string.Format("The server answered {0} at {1} with a body that was not JSON.",
                (int) answer.Status, url));
      return parsed;
    }

    /// <summary>
    /// One GET, answered with the bytes the server sent and nothing made of them.
    /// </summary>
    /// <remarks>
    /// Everything that can go wrong on the way - no route, a timeout, a server that closed the connection - is
    /// turned into <see cref="ServerApiException"/> here, so that every caller of it reports a connection problem
    /// in the same words whether it wanted JSON or a picture. What the server said about a title it does not have
    /// is a different matter: a status that is not a success comes back here to be judged by the caller, because
    /// for a title it is an error worth showing while for a picture it is the ordinary case.
    /// </remarks>
    private async Task<RawAnswer> GetRawAsync(Uri url, CancellationToken cancellation)
    {
      try
      {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
        {
          AddToken(request);
          using (HttpResponseMessage response = await _http.SendAsync(request,
              HttpCompletionOption.ResponseContentRead, cancellation).ConfigureAwait(false))
          {
            byte[] body = await response.Content.ReadAsByteArrayAsync(cancellation).ConfigureAwait(false);
            return new RawAnswer(response.StatusCode, response.IsSuccessStatusCode, body);
          }
        }
      }
      catch (HttpRequestException ex)
      {
        throw new ServerApiException(url,
            string.Format("No usable answer from {0}: {1}", url, ex.Message), ex);
      }
      catch (TaskCanceledException ex)
      {
        // HttpClient reports a timeout as a cancelled task, and a timeout is a different complaint from the
        // user pressing cancel - only the first one is worth showing as a connection problem.
        if (cancellation.IsCancellationRequested)
          throw;
        throw new ServerApiException(url,
            string.Format("{0} did not answer within {1}.", url, _http.Timeout), ex);
      }
    }

    /// <summary>
    /// What a GET brought back, before anybody decided what it means.
    /// </summary>
    private sealed class RawAnswer
    {
      public RawAnswer(HttpStatusCode status, bool success, byte[] body)
      {
        Status = status;
        Success = success;
        Body = body;
      }

      public HttpStatusCode Status { get; }

      public bool Success { get; }

      /// <summary>The body, as it arrived.</summary>
      public byte[] Body { get; }

      /// <summary>The body read as the text a server writes when it has something to say about a failure.</summary>
      public string Text()
      {
        return Encoding.UTF8.GetString(Body);
      }
    }

    private static string Summarize(string body)
    {
      string text = body.Replace('\r', ' ').Replace('\n', ' ').Trim();
      return text.Length <= 300 ? text : text.Substring(0, 300) + "…";
    }
  }
}
