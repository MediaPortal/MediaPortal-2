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
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The household's users, over the same HTTP API the library is read from - the endpoints behind the
  /// reference's "Settings -> Users" window.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The user store itself is not this controller's invention: it is MP2's own
  /// <see cref="IUserProfileDataManagement"/>, the same store the token endpoint asks when somebody signs in,
  /// the same store that decides whose shares and age restrictions apply to a connection. What the reference
  /// configured through a window on the server's desktop - who exists, what they must say to get in, which
  /// ones are gone - is what these endpoints carry, because a headless server has no desktop and the
  /// household reaches it from a television.
  /// </para>
  /// <para>
  /// <b>Passwords are hashed on the way in, exactly as the store expects them.</b> The store compares a login
  /// by hashing what the signer says and matching it against what is stored, so a password written down as
  /// plain text would never match again; a null password in an update means "leave it as it is", while an
  /// empty one is the answer itself - a user who needs no password, which the token endpoint already
  /// treats as a valid profile. The stored hash is never sent out; a screen only learns whether one exists.
  /// </para>
  /// <para>
  /// <b>The caller's own account is protected from their own screen.</b> Deleting the signed-in user would end
  /// the session that asked for it mid-gesture, so it is refused in words; whoever really wants to remove
  /// themselves signs in as somebody else first, which is also the reference's rule of thumb.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/users")]
  [MediaPortalAuthorize]
  public class UsersController : ControllerBase
  {
    /// <summary>One user as the store keeps them, minus everything a screen has no business seeing.</summary>
    public class UserInfo
    {
      public Guid ProfileId { get; set; }
      public string Name { get; set; }

      /// <summary><c>"User"</c> or <c>"Client"</c> - a client profile belongs to a machine, not to a person.</summary>
      public string Kind { get; set; }

      /// <summary>Whether this user must say something to sign in. The thing itself is never sent.</summary>
      public bool HasPassword { get; set; }

      /// <summary>Whether this is the user the calling request is signed in as.</summary>
      public bool IsYou { get; set; }
    }

    /// <summary>The answer of <c>GET api/users</c>.</summary>
    public class UserListResult
    {
      public List<UserInfo> Users { get; set; }
      public string Error { get; set; }
    }

    /// <summary>The answer of the write endpoints: the user they created or changed, or the refusal.</summary>
    public class UserResult
    {
      public UserInfo User { get; set; }
      public string Error { get; set; }
    }

    /// <summary>What a caller asks for when it adds a user.</summary>
    public class CreateUserRequest
    {
      /// <summary>The name the user signs in with. Required; a name the store already has is answered honestly.</summary>
      public string Name { get; set; }

      /// <summary>The password, plain, hashed here. Null or empty means the user signs in without one.</summary>
      public string Password { get; set; }
    }

    /// <summary>What a caller asks for when it changes a user. Both fields optional; what is null stays.</summary>
    public class UpdateUserRequest
    {
      public string Name { get; set; }

      /// <summary>New password, plain, hashed here. Null keeps the current one; empty removes the password.</summary>
      public string Password { get; set; }
    }

    /// <summary>The reason a write refused, in a body a screen can show.</summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    private static IUserProfileDataManagement Store
    {
      get { return ServiceRegistration.Get<IUserProfileDataManagement>(false); }
    }

    /// <summary>
    /// Every user the store keeps, in the store's own order.
    /// </summary>
    [HttpGet]
    public async Task<UserListResult> GetUsers()
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return new UserListResult { Error = "This server has no user store." };

      Guid callerId = CallerProfileId();
      var users = new List<UserInfo>();
      foreach (UserProfile profile in await store.GetProfilesAsync().ConfigureAwait(false))
        users.Add(Describe(profile, callerId));

      return new UserListResult { Users = users };
    }

    /// <summary>
    /// Adds a user. The name must be new; the store's own opinion about a name it already has is passed
    /// through rather than pretended away.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResult>> CreateUser([FromBody] CreateUserRequest request)
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return StatusCode(503, new ErrorResponse { Error = "This server has no user store." });

      string name = (request?.Name ?? "").Trim();
      if (name.Length == 0)
        return BadRequest(new ErrorResponse { Error = "A user needs a name." });

      AsyncResult<UserProfile> existing = await store.GetProfileByNameAsync(name).ConfigureAwait(false);
      if (existing != null && existing.Success)
        return Conflict(new ErrorResponse { Error = "There is already a user called '" + name + "'." });

      string password = string.IsNullOrEmpty(request.Password) ? "" : UserProfile.HashPassword(request.Password);
      Guid profileId = await store.CreateProfileAsync(name, UserProfileType.UserProfile, password)
          .ConfigureAwait(false);

      AsyncResult<UserProfile> created = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      if (created == null || !created.Success)
        return StatusCode(500, new ErrorResponse { Error = "The store did not take the new user." });

      return new UserResult { User = Describe(created.Result, CallerProfileId()) };
    }

    /// <summary>
    /// Renames a user, gives them a new password, or both - the same two answers the reference's user editor
    /// held open on one row.
    /// </summary>
    [HttpPut("{profileId}")]
    public async Task<ActionResult<UserResult>> UpdateUser(Guid profileId, [FromBody] UpdateUserRequest request)
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return StatusCode(503, new ErrorResponse { Error = "This server has no user store." });

      AsyncResult<UserProfile> found = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      if (found == null || !found.Success)
        return NotFound(new ErrorResponse { Error = "There is no user with this id." });

      UserProfile current = found.Result;
      string name = string.IsNullOrWhiteSpace(request?.Name) ? current.Name : request.Name.Trim();
      if (name.Length == 0)
        return BadRequest(new ErrorResponse { Error = "A user needs a name." });

      if (!string.Equals(name, current.Name, StringComparison.Ordinal))
      {
        AsyncResult<UserProfile> taken = await store.GetProfileByNameAsync(name).ConfigureAwait(false);
        if (taken != null && taken.Success)
          return Conflict(new ErrorResponse { Error = "There is already a user called '" + name + "'." });
      }

      string password = request?.Password == null
          ? current.Password
          : request.Password.Length == 0 ? "" : UserProfile.HashPassword(request.Password);

      if (!await store.UpdateProfileAsync(profileId, name, current.ProfileType, password).ConfigureAwait(false))
        return StatusCode(500, new ErrorResponse { Error = "The store did not accept the change." });

      AsyncResult<UserProfile> updated = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      UserProfile after = updated != null && updated.Success ? updated.Result : current;
      return new UserResult { User = Describe(after, CallerProfileId()) };
    }

    /// <summary>
    /// Removes a user. The signed-in user cannot remove themselves from their own screen; the data question
    /// behind it - what happens to a deleted user's watched marks - is the store's, which is the same answer
    /// the reference's own user window gave.
    /// </summary>
    [HttpDelete("{profileId}")]
    public async Task<ActionResult<UserResult>> DeleteUser(Guid profileId)
    {
      IUserProfileDataManagement store = Store;
      if (store == null)
        return StatusCode(503, new ErrorResponse { Error = "This server has no user store." });

      if (profileId == CallerProfileId())
        return BadRequest(new ErrorResponse
        {
          Error = "You cannot delete the user you are signed in as. Sign in as somebody else first."
        });

      AsyncResult<UserProfile> found = await store.GetProfileAsync(profileId).ConfigureAwait(false);
      if (found == null || !found.Success)
        return NotFound(new ErrorResponse { Error = "There is no user with this id." });

      if (!await store.DeleteProfileAsync(profileId).ConfigureAwait(false))
        return StatusCode(500, new ErrorResponse { Error = "The store did not remove the user." });

      return new UserResult { User = Describe(found.Result, CallerProfileId()) };
    }

    /// <summary>The profile id the bearer token was issued for, or empty when the call carries no name.</summary>
    private Guid CallerProfileId()
    {
      string sid = HttpContext.User?.FindFirstValue(ClaimTypes.Sid);
      return sid != null && Guid.TryParse(sid, out Guid profileId) ? profileId : Guid.Empty;
    }

    private static UserInfo Describe(UserProfile profile, Guid callerId)
    {
      return new UserInfo
      {
        ProfileId = profile.ProfileId,
        Name = profile.Name,
        Kind = profile.ProfileType == UserProfileType.ClientProfile ? "Client" : "User",
        HasPassword = !string.IsNullOrEmpty(profile.Password),
        IsYou = profile.ProfileId == callerId
      };
    }
  }
}
