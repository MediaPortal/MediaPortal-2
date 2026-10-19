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

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// One credential the server offers to the network's shares, as <c>api/media/network-credentials</c>
  /// answers it. The password never crosses to this side; <see cref="HasPassword"/> is all a screen gets to
  /// say about it.
  /// </summary>
  public sealed class NetworkCredentialInfo
  {
    public string Path { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Domain { get; set; } = "";
    public bool HasPassword { get; set; }

    /// <summary>What a list row shows: the path, its user, and whether a password rides along.</summary>
    public string Label
    {
      get
      {
        string line = Path + "  -  " + UserName;
        if (Domain.Length > 0)
          line += "\\" + Domain;
        return line + (HasPassword ? "  (password set)" : "  (no password)");
      }
    }
  }

  /// <summary>The answer of <c>GET api/media/network-credentials</c>: the entries and the provider's root.</summary>
  public sealed class NetworkCredentialsAnswer
  {
    public NetworkCredentialInfo[]? Credentials { get; set; }
    public string? RootPath { get; set; }
    public string? Error { get; set; }
  }

  /// <summary>
  /// What <c>PUT api/media/network-credentials</c> asks for. An empty <see cref="Path"/> stores at the
  /// provider's root - the reference's one and only slot. A null <see cref="Password"/> keeps an existing
  /// entry's password; a set one replaces it.
  /// </summary>
  public sealed class NetworkCredentialWish
  {
    public string Path { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? Password { get; set; }
    public string Domain { get; set; } = "";
  }

  /// <summary>
  /// What the front end asks of a server that can hold network credentials. ServerApiClient answers it; a
  /// test wears any other answer, so the screen can be walked through without a server behind it.
  /// </summary>
  public interface INetworkCredentialsApi
  {
    System.Threading.Tasks.Task<NetworkCredentialsAnswer> GetNetworkCredentialsAsync(
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<NetworkCredentialsAnswer> StoreNetworkCredentialAsync(NetworkCredentialWish wish,
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<NetworkCredentialsAnswer> RemoveNetworkCredentialAsync(string path,
        System.Threading.CancellationToken cancellation = default);
  }
}
