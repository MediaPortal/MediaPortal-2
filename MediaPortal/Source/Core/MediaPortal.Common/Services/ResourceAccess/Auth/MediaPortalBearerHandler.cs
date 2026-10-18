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

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaPortal.Common.Services.ResourceAccess.Auth
{
  /// <summary>
  /// Turns <c>Authorization: Bearer &lt;token&gt;</c> into an authenticated <c>HttpContext.User</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the piece whose absence made <see cref="MediaPortalAuthorizeAttribute"/> unusable on .NET: the filter
  /// asks whether <c>HttpContext.User.Identity</c> is authenticated with
  /// <see cref="ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE"/>, and with no handler nothing ever set it, so the
  /// answer was "no" for every request including a well-identified one. On .NET Framework that job was done by
  /// <c>UseOAuthBearerAuthentication</c>, which belongs to the Owin stack.
  /// </para>
  /// <para>
  /// The identity it produces carries exactly the claims that are inside the token and nothing more - it does not
  /// look the profile up again, so a request cannot turn authentication into a database round trip.
  /// </para>
  /// </remarks>
  public class MediaPortalBearerHandler : AuthenticationHandler<AuthenticationSchemeOptions>
  {
    /// <summary>
    /// The scheme name. Same string as the authentication type, because there is one of each and two names for the
    /// same thing is how a scheme ends up registered under one name and challenged under another.
    /// </summary>
    public const string SCHEME = ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE;

    private const string BEARER_PREFIX = "Bearer ";

    private readonly IMediaPortalAccessTokenProtector _protector;

    public MediaPortalBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, IMediaPortalAccessTokenProtector protector)
        : base(options, logger, encoder)
    {
      _protector = protector;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      string header = Request.Headers["Authorization"];
      if (string.IsNullOrEmpty(header))
        return AuthenticateResult.NoResult();   // anonymous request; the endpoint decides what that means

      if (!header.StartsWith(BEARER_PREFIX, System.StringComparison.OrdinalIgnoreCase))
        return AuthenticateResult.NoResult();   // another scheme's header (Basic, Digest, ...), not ours to judge

      string token = header.Substring(BEARER_PREFIX.Length).Trim();
      if (token.Length == 0)
        return AuthenticateResult.NoResult();

      ClaimsIdentity identity;
      string error;
      try
      {
        if (!_protector.TryValidate(token, out identity, out error))
        {
          // Deliberately without the token: this reaches the server log, and a rejected token is still somebody's
          // credential.
          Logger.LogInformation("MediaPortal bearer token rejected ({Reason}) from {RemoteIp}", error,
              Context.Connection.RemoteIpAddress);
          return AuthenticateResult.Fail("invalid token");
        }
      }
      catch (System.Exception ex)
      {
        // A failure inside validation is not "anonymous" - reporting it is the only way a broken key shows up as
        // something other than a wave of 403s.
        Logger.LogError(ex, "MediaPortal bearer token could not be validated");
        return AuthenticateResult.Fail("token validation failed");
      }

      return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SCHEME));
    }
  }
}
