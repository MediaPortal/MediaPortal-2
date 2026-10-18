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
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Common.Services.ResourceAccess;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// Asks the server who it thinks the caller is: the first endpoint behind
  /// <see cref="MediaPortalAuthorizeAttribute"/>, and the proof that the token endpoint and the filter agree.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Until now every endpoint of this API answered anybody. That was recorded as a debt in
  /// <see cref="ServerInfoController"/>, and the reason for it was that no component could turn a request into a
  /// named identity on .NET. With <c>POST Token</c> in place the filter can be used, and this is where it is used
  /// first: it is the smallest possible endpoint that a wrong answer about authentication can show up on. Whether a
  /// token is accepted, whether a tampered one is not, and whether the name in the answer is the name that was
  /// logged in are all measurable from outside the process, over HTTP, on both hosts.
  /// </para>
  /// <para>
  /// It reports the caller's own identity and nobody else's, so it asks for a token without exposing anything a
  /// token is not already the key to. That is also why it can be the first protected endpoint while the existing
  /// ones stay open: taking <c>api/library/aspects</c> behind the filter in the same change would have broken every
  /// client that has no token yet - including the gates that measure this server - and that migration is a decision
  /// about defaults, not about capability.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/[controller]")]
  public class SessionController : ControllerBase
  {
    /// <summary>
    /// The answer of <c>GET api/session</c>.
    /// </summary>
    public class SessionResult
    {
      /// <summary>
      /// The profile name the token was issued for.
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      /// The profile's id, as the claim the server put in the token.
      /// </summary>
      public Guid ProfileId { get; set; }

      /// <summary>
      /// <c>Admin</c> or <c>User</c>, from the same rule the .NET Framework server applied.
      /// </summary>
      public string Role { get; set; }

      /// <summary>
      /// The authentication type the identity arrived with. Reported because the filter compares this exact string,
      /// so a client or a future handler that gets it wrong can see that it did.
      /// </summary>
      public string AuthenticationType { get; set; }

      public string ServerUtc { get; set; }
    }

    /// <summary>
    /// Who the caller is, according to the bearer token it presented.
    /// </summary>
    [HttpGet]
    [MediaPortalAuthorize]
    public SessionResult Get()
    {
      ClaimsPrincipal caller = HttpContext.User;
      Guid profileId;
      string sid = caller.FindFirstValue(ClaimTypes.Sid);
      if (sid == null || !Guid.TryParse(sid, out profileId))
        profileId = Guid.Empty;

      return new SessionResult
      {
        Name = caller.Identity != null ? caller.Identity.Name : null,
        ProfileId = profileId,
        Role = caller.FindFirstValue(MediaPortal.Common.Services.ResourceAccess.Auth.MediaPortalAccessToken.ROLE_CLAIM),
        AuthenticationType = caller.Identity != null ? caller.Identity.AuthenticationType : null,
        ServerUtc = DateTimeOffset.UtcNow.ToString("o")
      };
    }
  }
}
