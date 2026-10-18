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
using System.Net;
using System.Net.Http;
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
  public sealed class ServerApiClient : IDisposable
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

    private async Task<T> GetJsonAsync<T>(string relativeUrl, CancellationToken cancellation)
    {
      Uri url = new Uri(BaseUrl, relativeUrl);
      try
      {
        using (HttpResponseMessage response = await _http.GetAsync(url, HttpCompletionOption.ResponseContentRead,
            cancellation).ConfigureAwait(false))
        {
          string body = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
          if (!response.IsSuccessStatusCode)
            throw new ServerApiException(url, response.StatusCode, Summarize(body));

          T? parsed = JsonSerializer.Deserialize<T>(body, JSON_OPTIONS);
          if (parsed == null)
            throw new ServerApiException(url,
                string.Format("The server answered {0} at {1} with a body that was not JSON.",
                    (int) response.StatusCode, url));
          return parsed;
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
        // HttpClient reports a timeout as a cancelled task, and a timeout is a different complaint from the
        // user pressing cancel - only the first one is worth showing as a connection problem.
        if (cancellation.IsCancellationRequested)
          throw;
        throw new ServerApiException(url,
            string.Format("{0} did not answer within {1}.", url, _http.Timeout), ex);
      }
      catch (JsonException ex)
      {
        throw new ServerApiException(url,
            string.Format("The answer from {0} could not be read: {1}", url, ex.Message), ex);
      }
    }

    private static string Summarize(string body)
    {
      string text = body.Replace('\r', ' ').Replace('\n', ' ').Trim();
      return text.Length <= 300 ? text : text.Substring(0, 300) + "…";
    }
  }
}
