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
using System.Globalization;
using System.Net;

namespace MediaPortal.Frontend.Discovery
{
  /// <summary>
  /// One SSDP message a device sent us - the answer to our search, or an announcement it sent on its own.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The strings here are the ones MediaPortal 2 itself uses: <see cref="MulticastAddress"/> and
  /// <see cref="SsdpPort"/> are the SSDP well-known group and port, and <see cref="ServerSearchTarget"/> is the
  /// URN of the device type the server publishes (the server's own <c>UPnPTypesAndIds</c> calls it
  /// <c>...:device:MP2-Server</c> version 1). They are written out again instead of referenced because the front
  /// end deliberately does not reference the server's assemblies - it has to run on platforms where those
  /// assemblies do not exist.
  /// </para>
  /// <para>
  /// Nothing in this class touches a socket. Parsing is a pure function of the received bytes, which is what
  /// makes the meaning of a reply testable without a network.
  /// </para>
  /// </remarks>
  public sealed class SsdpAnnouncement
  {
    /// <summary>The UDP port SSDP uses, fixed by the protocol.</summary>
    public const int SsdpPort = 1900;

    /// <summary>The IPv4 SSDP discovery group.</summary>
    public static readonly IPAddress MulticastAddress = IPAddress.Parse("239.255.255.250");

    /// <summary>Search target for the MediaPortal 2 server device, so only servers of that kind answer.</summary>
    public const string ServerSearchTarget = "urn:schemas-team-mediaportal-com:device:MP2-Server:1";

    /// <summary>Search target that makes every UPnP device on the network answer.</summary>
    public const string AllSearchTarget = "ssdp:all";

    /// <summary>Prefix of the URL under which the server publishes its UPnP descriptions.</summary>
    public const string UpnpPathPrefix = "/MediaPortal/UPnPServer";

    private SsdpAnnouncement(Uri location, string? searchTarget, string? server, IPEndPoint? from)
    {
      Location = location;
      SearchTarget = searchTarget;
      Server = server;
      From = from;
    }

    /// <summary>Where this device's description document lives. Its host and port name the machine.</summary>
    public Uri Location { get; }

    /// <summary>The device type the sender is announcing, from the <c>ST</c> header.</summary>
    public string? SearchTarget { get; }

    /// <summary>The sender's software, from the <c>SERVER</c> header. Informational; nothing depends on it.</summary>
    public string? Server { get; }

    /// <summary>The UDP endpoint the message arrived from, or <c>null</c> when unknown.</summary>
    public IPEndPoint? From { get; }

    /// <summary>
    /// Whether this announcement came from something that presents itself as a MediaPortal 2 server.
    /// </summary>
    /// <remarks>
    /// Either half is enough. The device type is the correct test, and the description path is the one
    /// MediaPortal 2's own discovery gate checks - keeping both means a server that answers a broadcast
    /// <c>ssdp:all</c> search is still recognised when its <c>ST</c> header names a parent device instead.
    /// </remarks>
    public bool IsMediaPortalServer =>
        (SearchTarget != null && SearchTarget.IndexOf("MP2-Server", StringComparison.OrdinalIgnoreCase) >= 0) ||
        Location.AbsolutePath.StartsWith(UpnpPathPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Builds the search request. SSDP wants a plain-text request with the discovery extension declared, and the
    /// <c>MX</c> value tells devices the maximum number of seconds they may wait before answering, which is how a
    /// crowded network avoids replying all at once.
    /// </summary>
    public static string BuildSearchRequest(string searchTarget, int mxSeconds)
    {
      if (string.IsNullOrWhiteSpace(searchTarget)) throw new ArgumentException("Need a search target.", nameof(searchTarget));
      if (mxSeconds < 1) mxSeconds = 1;
      return string.Join("\r\n", new[]
      {
          "M-SEARCH * HTTP/1.1",
          "HOST: " + MulticastAddress + ":" + SsdpPort.ToString(CultureInfo.InvariantCulture),
          "MAN: \"ssdp:discover\"",
          "MX: " + mxSeconds.ToString(CultureInfo.InvariantCulture),
          "ST: " + searchTarget,
          string.Empty,
          string.Empty
      });
    }

    /// <summary>
    /// Turns received bytes into an announcement, or returns <c>null</c> when the message is not one.
    /// </summary>
    /// <remarks>
    /// The two shapes a UPnP device sends are the search answer (<c>HTTP/1.1 200 OK</c>) and the alive
    /// announcement (<c>NOTIFY * HTTP/1.1</c>); both carry the same headers we need. Anything else - a search
    /// request from another control point, a malformed packet - is not an announcement, and returning
    /// <c>null</c> for it is how a listener keeps its silence on a busy network.
    /// </remarks>
    public static SsdpAnnouncement? TryParse(string message, IPEndPoint? from)
    {
      if (string.IsNullOrEmpty(message)) return null;
      Dictionary<string, string> headers = ParseHeaders(message, out string startLine);
      if (!IsAnnouncement(startLine)) return null;
      if (!headers.TryGetValue("location", out string? location) ||
          !Uri.TryCreate(location, UriKind.Absolute, out Uri? locationUri) ||
          !locationUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
          string.IsNullOrEmpty(locationUri.Host))
      {
        return null;
      }
      headers.TryGetValue("st", out string? searchTarget);
      headers.TryGetValue("server", out string? server);
      return new SsdpAnnouncement(locationUri, Clean(searchTarget), Clean(server), from);
    }

    /// <summary>
    /// Whether the first line of a message is one device telling us about itself.
    /// </summary>
    private static bool IsAnnouncement(string startLine)
    {
      return startLine.StartsWith("HTTP/1.1 200", StringComparison.OrdinalIgnoreCase) ||
             startLine.StartsWith("NOTIFY ", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> ParseHeaders(string message, out string startLine)
    {
      var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      string[] lines = message.Replace("\r\n", "\n").Split('\n');
      startLine = lines.Length > 0 ? lines[0].Trim() : string.Empty;
      for (int i = 1; i < lines.Length; i++)
      {
        string line = lines[i];
        int colon = line.IndexOf(':');
        if (colon <= 0) continue;
        string name = line.Substring(0, colon).Trim();
        string value = line.Substring(colon + 1).Trim();
        if (name.Length > 0) headers[name] = value;
      }
      return headers;
    }

    private static string? Clean(string? value)
    {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public override string ToString()
    {
      return (SearchTarget ?? "ssdp") + " -> " + Location;
    }
  }
}
