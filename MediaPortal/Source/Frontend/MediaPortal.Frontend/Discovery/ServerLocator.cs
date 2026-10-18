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
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.Discovery
{
  /// <summary>
  /// Turns the machines that answered a discovery search into the servers the front end can actually use.
  /// </summary>
  /// <remarks>
  /// <para>
  /// What a device announces is the URL of its UPnP description document (<c>http://host:55555/MediaPortal/UPnPServer_x/description.xml</c>),
  /// which is not where the media API lives. The machine and port are the useful part: the front end's own API
  /// sits on the same listener under <see cref="ApiPathBase"/>, which is how MediaPortal 2 deploys, so the
  /// candidate address is built from the announcement's host and port plus that prefix.
  /// </para>
  /// <para>
  /// The candidate is then asked, with the same two requests <c>--check</c> makes. A server that does not answer
  /// them is not listed, even if it answered the search: the list is a list of things the user can pick and that
  /// will work, not a list of packets that arrived. Because the server answers, its name and version come from
  /// the server rather than from the announcement, and a server that was configured with a different path prefix
  /// corrects us itself - the health reply names the base it was reached under, so the address stored afterwards
  /// is one that has just been used successfully.
  /// </para>
  /// </remarks>
  public sealed class ServerLocator
  {
    /// <summary>Where the API of a MediaPortal 2 server sits on its HTTP listener.</summary>
    public const string ApiPathBase = "/MediaPortal/Server";

    private readonly TimeSpan _probeTimeout;

    public ServerLocator(TimeSpan? probeTimeout = null)
    {
      _probeTimeout = probeTimeout ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Builds the address to ask, from the location a device announced.
    /// </summary>
    public static Uri BuildApiBaseUrl(Uri location, string? pathBase = null)
    {
      if (location == null) throw new ArgumentNullException(nameof(location));
      string path = string.IsNullOrWhiteSpace(pathBase) ? ApiPathBase : pathBase.Trim();
      if (!path.StartsWith("/", StringComparison.Ordinal)) path = "/" + path;
      var builder = new UriBuilder(location.Scheme, location.Host) { Path = path.TrimEnd('/') };
      if (!location.IsDefaultPort) builder.Port = location.Port;
      // The trailing slash is not decoration: HttpClient resolves a relative URI against the *directory* of its
      // base address, so without it "api/serverinfo" would land one level too high.
      return new Uri(builder.Uri.ToString().TrimEnd('/') + "/");
    }

    /// <summary>
    /// The address to use after a server reported the prefix it is reached under, or <c>null</c> when what it
    /// reported is where we already are.
    /// </summary>
    /// <remarks>
    /// The health endpoint names the path base it answered on, so a server deployed under a prefix other than
    /// <see cref="ApiPathBase"/> can correct our guess instead of failing the search. Asking again there is what
    /// makes the address that gets saved one that has just been used, rather than one that merely looked right.
    /// </remarks>
    public static Uri? CorrectedBaseUrl(Uri triedBaseUrl, string? reportedPathBase)
    {
      if (triedBaseUrl == null) throw new ArgumentNullException(nameof(triedBaseUrl));
      if (string.IsNullOrWhiteSpace(reportedPathBase)) return null;
      string reported = reportedPathBase.Trim();
      if (!reported.StartsWith("/", StringComparison.Ordinal)) reported = "/" + reported;
      if (string.Equals(reported.TrimEnd('/'), triedBaseUrl.AbsolutePath.TrimEnd('/'),
                        StringComparison.OrdinalIgnoreCase))
      {
        return null;
      }

      var builder = new UriBuilder(triedBaseUrl.Scheme, triedBaseUrl.Host) { Path = reported.TrimEnd('/') };
      if (!triedBaseUrl.IsDefaultPort) builder.Port = triedBaseUrl.Port;
      return new Uri(builder.Uri.ToString().TrimEnd('/') + "/");
    }

    /// <summary>
    /// Asks every machine in <paramref name="announcementList"/> whether a MediaPortal 2 server lives there, and
    /// returns the ones that said yes, ordered by the name they gave themselves.
    /// </summary>
    public async Task<IReadOnlyList<DetectedServer>> LocateAsync(IEnumerable<SsdpAnnouncement> announcementList,
                                                                CancellationToken cancellation)
    {
      if (announcementList == null) throw new ArgumentNullException(nameof(announcementList));
      List<Task<DetectedServer?>> probes = announcementList
          .Where(a => a.IsMediaPortalServer)
          .Select(a => ProbeAsync(a.Location, cancellation))
          .ToList();
      if (probes.Count == 0) return Array.Empty<DetectedServer>();

      DetectedServer?[] results = await Task.WhenAll(probes).ConfigureAwait(false);
      return results
          .Where(r => r != null)
          .GroupBy(r => r!)
          .Select(g => g.First())
          .OrderBy(r => r!.Name, StringComparer.CurrentCultureIgnoreCase)
          .ThenBy(r => r!.AddressLine, StringComparer.OrdinalIgnoreCase)
          .ToList()!;
    }

    /// <summary>
    /// Asks one machine. Returns <c>null</c> when nothing there answers as a MediaPortal 2 server, which is the
    /// common case on a network: a TV, a printer and a NAS answer SSDP too, and every one of them is a
    /// non-server rather than an error.
    /// </summary>
    public async Task<DetectedServer?> ProbeAsync(Uri location, CancellationToken cancellation)
    {
      if (location == null) throw new ArgumentNullException(nameof(location));
      try
      {
        Uri baseUrl = BuildApiBaseUrl(location);
        (HealthInfo health, ServerInfo info) = await AskAsync(baseUrl, cancellation).ConfigureAwait(false);

        Uri? corrected = CorrectedBaseUrl(baseUrl, health.PathBase);
        if (corrected != null)
        {
          // The server told us where it actually is. Ask again there, so that what gets saved is an address that
          // has just worked rather than one assembled from a guess.
          baseUrl = corrected;
          (health, info) = await AskAsync(baseUrl, cancellation).ConfigureAwait(false);
        }

        string name = FirstNonEmpty(info.Name, health.Name, location.Host);
        return new DetectedServer(name, FirstNonEmpty(info.Version, health.Version), baseUrl, location,
            string.IsNullOrWhiteSpace(info.OperatingSystem) ? null : info.OperatingSystem);
      }
      catch (Exception ex) when (IsReasonToSkip(ex))
      {
        cancellation.ThrowIfCancellationRequested();
        return null;
      }
    }

    /// <summary>
    /// The two requests that decide whether something is a MediaPortal 2 server.
    /// </summary>
    private async Task<(HealthInfo, ServerInfo)> AskAsync(Uri baseUrl, CancellationToken cancellation)
    {
      using (var client = new ServerApiClient(baseUrl, null, _probeTimeout))
      {
        HealthInfo health = await client.GetHealthAsync(cancellation).ConfigureAwait(false);
        ServerInfo info = await client.GetServerInfoAsync(cancellation).ConfigureAwait(false);
        return (health, info);
      }
    }

    private static bool IsReasonToSkip(Exception ex)
    {
      // Anything that means "this machine or this port is not a MediaPortal 2 API" - including a socket error on
      // a host that announced itself and then vanished, and a reply that is JSON-shaped but not our shape.
      return ex is ServerApiException or HttpRequestException or SocketException or JsonException
          or OperationCanceledException or TimeoutException or UriFormatException or InvalidOperationException;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
      foreach (string? value in values)
      {
        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
      }
      return "MediaPortal server";
    }
  }
}
