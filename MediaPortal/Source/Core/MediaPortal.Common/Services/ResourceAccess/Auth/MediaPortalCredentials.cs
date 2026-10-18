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
using System.Security.Claims;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Common.Services.ResourceAccess.Auth
{
  /// <summary>
  /// Checks a username and password against MediaPortal 2's own profiles and says what identity a token should
  /// carry, if any.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the code that used to sit inside <see cref="ResourceServer"/>'s Owin branch, where it was reachable
  /// only through <c>OAuthAuthorizationServerProvider</c> - a type from <c>Microsoft.Owin.Security.OAuth</c>, which
  /// is a .NET Framework library and has no .NET Core or .NET 10 successor. Moving the decision here is what makes
  /// the password grant portable: the HTTP shape around it can change per host, the answer to "may this person in"
  /// cannot.
  /// </para>
  /// <para>
  /// It is deliberately not an ASP.NET type. The unit tests call it directly, with a profile store of their own, so
  /// the rules below are covered without starting a listener.
  /// </para>
  /// </remarks>
  public static class MediaPortalCredentials
  {
    /// <summary>
    /// The <c>error</c> value of the OAuth 2.0 RFC 6749 §5.2 answer. Kept identical to what the Owin provider
    /// produced, because clients sort their own behaviour out from this string - "invalid_grant" means "ask the
    /// human for the password again", anything else means "the request was wrong".
    /// </summary>
    public const string ERROR_INVALID_GRANT = "invalid_grant";

    /// <summary>
    /// The <c>error</c> value for a <c>grant_type</c> other than <c>password</c>.
    /// </summary>
    public const string ERROR_UNSUPPORTED_GRANT_TYPE = "unsupported_grant_type";

    /// <summary>
    /// Result of a credential check: either an identity to put in a token, or the two fields of an OAuth error
    /// answer.
    /// </summary>
    public sealed class Result
    {
      private Result(bool granted, ClaimsIdentity identity, string error, string description)
      {
        Granted = granted;
        Identity = identity;
        Error = error;
        Description = description;
      }

      public bool Granted { get; }
      public ClaimsIdentity Identity { get; }
      public string Error { get; }
      public string Description { get; }

      public static Result Grant(ClaimsIdentity identity)
      {
        return new Result(true, identity, null, null);
      }

      public static Result Rejected(string error, string description)
      {
        return new Result(false, null, error, description);
      }
    }

    /// <summary>
    /// Turns the password as it arrives on the wire into the password to compare.
    /// </summary>
    /// <remarks>
    /// The client base64-encodes it, which the old server also expected. That is transport packaging and not
    /// protection: over plain HTTP a base64 password is as readable as the password itself, so this is only safe
    /// behind TLS or on a LAN the user has decided to trust, which is the same position MP2 has always been in.
    /// A value that is not valid base64 is passed through unchanged, because a profile whose password really is a
    /// string with a <c>/</c> or a <c>+</c> in it must keep working.
    /// </remarks>
    public static string DecodePassword(string encoded)
    {
      if (string.IsNullOrEmpty(encoded))
        return string.Empty;
      try
      {
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
      }
      catch (FormatException)
      {
        return encoded;
      }
    }

    /// <summary>
    /// The password grant: look the profile up by name, verify its password, log the profile in, and describe the
    /// caller as the claims MP2 has always used.
    /// </summary>
    /// <param name="userName">Profile name, matched case sensitively by the profile store.</param>
    /// <param name="password">The value from the request, still base64-encoded.</param>
    /// <param name="profileStore">
    /// Resolved from <paramref name="profileStore"/> when given, else from the running server's
    /// <see cref="IUserProfileDataManagement"/>, else from the client's <see cref="IUserManagement"/> - the same
    /// three-step search the old code did, in the same order, so a server and a client answer the same question the
    /// same way.
    /// </param>
    public static async Task<Result> GrantResourceOwnerCredentialsAsync(string userName, string password,
        IUserProfileDataManagement profileStore = null)
    {
      IUserProfileDataManagement userManagement = profileStore;
      if (userManagement == null)
      {
        userManagement = ServiceRegistration.Get<IUserProfileDataManagement>(false);
        if (userManagement == null)
        {
          // A client process has the profile store behind its own user management. Tried second, exactly as before.
          IUserManagement clientUserManagement = ServiceRegistration.Get<IUserManagement>(false);
          if (clientUserManagement != null)
            userManagement = clientUserManagement.UserProfileDataManagement;
        }
      }

      if (userManagement == null)
        return Result.Rejected(ERROR_INVALID_GRANT, "User management not available.");

      AsyncResult<UserProfile> user = await userManagement.GetProfileByNameAsync(userName).ConfigureAwait(false);
      if (user == null || !user.Success)
        return Result.Rejected(ERROR_INVALID_GRANT, "The user name or password is incorrect.");

      if (!UserProfile.VerifyPassword(DecodePassword(password), user.Result.Password))
        return Result.Rejected(ERROR_INVALID_GRANT, "The user name or password is incorrect.");

      // The rule for who is an administrator, unchanged: a profile that restricts nothing at all, is a real user
      // profile rather than a TV or guest one, and does not use restriction groups.
      bool isAdmin = !user.Result.RestrictShares && !user.Result.RestrictAges
          && user.Result.ProfileType == UserProfileType.UserProfile
          && !user.Result.EnableRestrictionGroups;

      ClaimsIdentity identity = new ClaimsIdentity(ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE);
      identity.AddClaim(new Claim(ClaimTypes.Name, userName));
      identity.AddClaim(new Claim(ClaimTypes.Sid, user.Result.ProfileId.ToString()));
      identity.AddClaim(new Claim(ROLE_CLAIM, isAdmin ? "Admin" : "User"));

      // Logging in is what makes the profile's shares and restrictions apply to this connection; it is the same
      // call the old provider made before validating.
      await userManagement.LoginProfileAsync(user.Result.ProfileId).ConfigureAwait(false);

      return Result.Grant(identity);
    }

    private const string ROLE_CLAIM = MediaPortalAccessToken.ROLE_CLAIM;
  }
}
