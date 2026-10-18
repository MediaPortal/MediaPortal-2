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

namespace MediaPortal.Frontend.Discovery
{
  /// <summary>
  /// A server the front end found on its own and then talked to.
  /// </summary>
  /// <remarks>
  /// A name from the network is only a claim, so this record is not produced by the discovery packets alone:
  /// <see cref="ServerLocator"/> asks the machine over its HTTP API first, and what the server itself answered is
  /// what the list shows. That is also why an entry here can be saved straight away - the address was not typed
  /// by a person who might have mistyped it, it came from the server's own announcement and was then used
  /// successfully.
  /// </remarks>
  public sealed class DetectedServer : IEquatable<DetectedServer>
  {
    public DetectedServer(string name, string version, Uri baseUrl, Uri location, string? operatingSystem = null)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A detected server needs a name.", nameof(name));
      if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));
      Name = name;
      Version = version ?? string.Empty;
      BaseUrl = baseUrl;
      Location = location;
      OperatingSystem = operatingSystem;
    }

    /// <summary>What the server called itself.</summary>
    public string Name { get; }

    /// <summary>The server's version string, as it reported it.</summary>
    public string Version { get; }

    /// <summary>The address to save and talk to, prefix included.</summary>
    public Uri BaseUrl { get; }

    /// <summary>The UPnP description URL this server announced itself with. Kept for the log.</summary>
    public Uri Location { get; }

    /// <summary>The operating system the server said it runs, when it said. Shown so that two servers on one
    /// network can be told apart.</summary>
    public string? OperatingSystem { get; }

    /// <summary>The machine and port, without the path. The line a user recognises as "that computer".</summary>
    public string AddressLine => BaseUrl.GetLeftPart(UriPartial.Authority);

    /// <summary>The two lines the list shows.</summary>
    public string DetailLine
    {
      get
      {
        string version = Version.Length > 0 ? Version : "version unknown";
        return AddressLine + "  -  " + version + (OperatingSystem == null ? string.Empty : "  -  " + OperatingSystem);
      }
    }

    /// <summary>
    /// What makes two entries the same server: the address to be talked to, apart from a trailing slash and the
    /// case of the host. Names are not part of it - a server keeps its address when the user renames it.
    /// </summary>
    private string Key => BaseUrl.GetLeftPart(UriPartial.Authority).ToLowerInvariant() +
                          BaseUrl.AbsolutePath.TrimEnd('/').ToLowerInvariant();

    public bool Equals(DetectedServer? other)
    {
      return other != null && Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
      return Equals(obj as DetectedServer);
    }

    public override int GetHashCode()
    {
      return Key.GetHashCode();
    }

    public override string ToString()
    {
      return Name + " <" + BaseUrl + ">";
    }
  }
}
