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

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// The household's users, as the server's <c>api/users</c> answers them. One entry per user the store
  /// keeps - the password itself never travels to this side, only the word of whether there is one.
  /// </summary>
  public sealed class UserInfo
  {
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = "";

    /// <summary><c>"User"</c> or <c>"Client"</c> - a client profile belongs to a machine, not to a person.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Whether this user must say something to sign in.</summary>
    public bool HasPassword { get; set; }

    /// <summary>Whether this is the user the current session is signed in as.</summary>
    public bool IsYou { get; set; }

    /// <summary>What a list row shows: the name, and beside it only what sets the entry apart.</summary>
    public string Label
    {
      get
      {
        string line = Name;
        if (IsYou)
          line += "  (you)";
        if (Kind == "Client")
          line += "  (machine)";
        else if (!HasPassword)
          line += "  (no PIN)";
        return line;
      }
    }
  }

  /// <summary>The answer of <c>GET api/users</c>: the whole list, or the reason there is none.</summary>
  public sealed class UsersAnswer
  {
    public UserInfo[]? Users { get; set; }
    public string? Error { get; set; }
  }

  /// <summary>The answer of the write endpoints: the user they touched, or the refusal.</summary>
  public sealed class UserAnswer
  {
    public UserInfo? User { get; set; }
    public string? Error { get; set; }
  }

  /// <summary>What <c>POST api/users</c> asks for.</summary>
  public sealed class CreateUserWish
  {
    public string Name { get; set; } = "";

    /// <summary>Plain, as the caller spells it; the server hashes it on the way into its store.</summary>
    public string? Password { get; set; }
  }

  /// <summary>What <c>PUT api/users/{id}</c> asks for. Null fields stay as they are.</summary>
  public sealed class UpdateUserWish
  {
    public string? Name { get; set; }

    /// <summary>Null keeps the current password; the empty string removes it.</summary>
    public string? Password { get; set; }
  }

  /// <summary>
  /// What the front end asks of a server that keeps users. ServerApiClient answers it; a test wears any
  /// other answer, so the settings screen can be walked through without a server standing behind it.
  /// </summary>
  public interface IUsersApi
  {
    System.Threading.Tasks.Task<UsersAnswer> GetUsersAsync(
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<UserAnswer> CreateUserAsync(CreateUserWish wish,
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
        System.Threading.CancellationToken cancellation = default);

    System.Threading.Tasks.Task<UserAnswer> DeleteUserAsync(Guid profileId,
        System.Threading.CancellationToken cancellation = default);
  }
}
