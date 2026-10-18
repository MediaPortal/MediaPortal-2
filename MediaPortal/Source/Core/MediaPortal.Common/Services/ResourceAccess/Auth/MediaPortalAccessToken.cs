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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediaPortal.Common.Services.ResourceAccess.Auth
{
  /// <summary>
  /// The access token MediaPortal 2's HTTP API issues and accepts.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <c>&lt;base64url(payload)&gt;.&lt;base64url(hmac-sha256)&gt;</c>, where the payload is a small JSON object
  /// carrying the claims and the expiry. Self-contained on purpose: the server keeps no session table, so a token
  /// is validated with the signing key alone - which is what makes it work identically on a machine that has
  /// restarted since, and on a second listener on the same database.
  /// </para>
  /// <para>
  /// Deliberately not the old format. On .NET Framework the token was an Owin ticket protected with
  /// <c>MachineKey</c>, i.e. readable only by the machine that wrote it and only through ASP.NET's machine-key
  /// configuration, which is Windows/IIS machinery. It could not be ported; this can.
  /// </para>
  /// <para>
  /// The signature covers the payload including <c>exp</c>, so an attacker who can see a token cannot extend it,
  /// change its subject, or make it non-expiring; a token whose signature does not match is rejected before its
  /// contents are looked at. <see cref="CryptographicOperations.FixedTimeEquals"/> is used because a byte-by-byte
  /// early exit would let someone binary-search a signature over the network.
  /// </para>
  /// </remarks>
  public static class MediaPortalAccessToken
  {
    /// <summary>
    /// How long an issued token stays valid. Long enough that a television does not have to log in again after a
    /// nightly restart, short enough that a token copied off one device is not a permanent key to the household.
    /// </summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(12);

    /// <summary>
    /// The claim types carried. They are the ones the old Owin provider put in its identity
    /// (<c>ClaimTypes.Name</c>, <c>ClaimTypes.Sid</c>, <c>ClaimTypes.Role</c>), so code that reads the validated
    /// user does not have to know which issued it.
    /// </summary>
    public const string NAME_CLAIM = ClaimTypes.Name;
    public const string PROFILE_ID_CLAIM = ClaimTypes.Sid;
    public const string ROLE_CLAIM = ClaimTypes.Role;

    /// <summary>
    /// Creates a token for <paramref name="identity"/>. Only the three claims above travel; anything else on the
    /// identity is dropped rather than echoed to the client.
    /// </summary>
    public static string Create(byte[] signingKey, ClaimsIdentity identity, TimeSpan lifetime)
    {
      if (signingKey == null || signingKey.Length == 0)
        throw new ArgumentException("A token cannot be signed without a key.", nameof(signingKey));
      if (identity == null)
        throw new ArgumentNullException(nameof(identity));

      DateTimeOffset expires = DateTimeOffset.UtcNow.Add(lifetime);
      Dictionary<string, string> payload = new Dictionary<string, string>
      {
        { "sub", identity.FindFirst(NAME_CLAIM)?.Value ?? string.Empty },
        { "sid", identity.FindFirst(PROFILE_ID_CLAIM)?.Value ?? string.Empty },
        { "role", identity.FindFirst(ROLE_CLAIM)?.Value ?? string.Empty },
        { "iss", ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE },
        // Seconds, UTC, no fractions: an expiry that depends on a local clock's precision is an expiry with wiggle
        // room in it.
        { "exp", expires.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture) }
      };

      byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
      byte[] signature = Sign(signingKey, body);
      return Encode(body) + "." + Encode(signature);
    }

    /// <summary>
    /// Checks a token and, when it holds, returns the identity it describes.
    /// </summary>
    /// <param name="token">The raw value from <c>Authorization: Bearer …</c>, without the scheme prefix.</param>
    /// <param name="now">
    /// Injectable so that the expiry test is a test and not a sleep. Defaults to the wall clock.
    /// </param>
    /// <param name="identity">
    /// An authenticated identity whose <see cref="ClaimsIdentity.AuthenticationType"/> is
    /// <see cref="ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE"/>, which is exactly what
    /// <see cref="MediaPortalAuthorizeAttribute"/> looks for.
    /// </param>
    /// <param name="error">
    /// A short, fixed reason. Never the token and never a claim value - this text ends up in a log and in a
    /// response body, and a rejected token is somebody else's credential.
    /// </param>
    public static bool TryValidate(byte[] signingKey, string token, out ClaimsIdentity identity, out string error,
        DateTimeOffset? now = null)
    {
      identity = null;
      error = null;

      if (signingKey == null || signingKey.Length == 0)
      {
        error = "the server has no signing key";
        return false;
      }
      if (string.IsNullOrEmpty(token))
      {
        error = "no token";
        return false;
      }

      int dot = token.IndexOf('.');
      if (dot <= 0 || dot == token.Length - 1 || token.IndexOf('.', dot + 1) >= 0)
      {
        error = "the token is not well formed";
        return false;
      }

      byte[] body;
      byte[] signature;
      try
      {
        body = Decode(token.Substring(0, dot));
        signature = Decode(token.Substring(dot + 1));
      }
      catch (FormatException)
      {
        error = "the token is not valid base64url";
        return false;
      }

      byte[] expected = Sign(signingKey, body);
      if (signature.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(signature, expected))
      {
        error = "the signature does not match";
        return false;
      }

      Dictionary<string, string> payload;
      try
      {
        payload = JsonSerializer.Deserialize<Dictionary<string, string>>(body, JsonOptions);
      }
      catch (JsonException)
      {
        error = "the payload is not readable";
        return false;
      }
      if (payload == null)
      {
        error = "the payload is empty";
        return false;
      }

      string issuer;
      payload.TryGetValue("iss", out issuer);
      if (!string.Equals(issuer, ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE, StringComparison.Ordinal))
      {
        error = "the token was not issued for MediaPortal";
        return false;
      }

      string expiresText;
      payload.TryGetValue("exp", out expiresText);
      long expiresSeconds;
      if (!long.TryParse(expiresText, System.Globalization.NumberStyles.Integer,
          System.Globalization.CultureInfo.InvariantCulture, out expiresSeconds))
      {
        error = "the token carries no usable expiry";
        return false;
      }

      DateTimeOffset reference = now ?? DateTimeOffset.UtcNow;
      if (reference.ToUnixTimeSeconds() >= expiresSeconds)
      {
        error = "the token expired at " +
            DateTimeOffset.FromUnixTimeSeconds(expiresSeconds).ToString("u", System.Globalization.CultureInfo.InvariantCulture);
        return false;
      }

      ClaimsIdentity validated = new ClaimsIdentity(ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE);
      string name;
      if (payload.TryGetValue("sub", out name) && !string.IsNullOrEmpty(name))
        validated.AddClaim(new Claim(NAME_CLAIM, name));
      string profileId;
      if (payload.TryGetValue("sid", out profileId) && !string.IsNullOrEmpty(profileId))
        validated.AddClaim(new Claim(PROFILE_ID_CLAIM, profileId));
      string role;
      if (payload.TryGetValue("role", out role) && !string.IsNullOrEmpty(role))
        validated.AddClaim(new Claim(ROLE_CLAIM, role));

      identity = validated;
      return true;
    }

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
      // Writing the payload ourselves rather than through a DTO keeps the wire format stable across refactors of
      // any type whose name would otherwise leak into it.
      WriteIndented = false
    };

    private static byte[] Sign(byte[] signingKey, byte[] body)
    {
      using (HMACSHA256 hmac = new HMACSHA256(signingKey))
      {
        return hmac.ComputeHash(body);
      }
    }

    /// <summary>
    /// Base64url (RFC 4648 §5): no padding, and <c>-</c>/<c>_</c> instead of <c>+</c>/<c>/</c>, so the token can go
    /// into a URL or a header without anything escaping it on the way.
    /// </summary>
    private static string Encode(byte[] bytes)
    {
      return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Decode(string text)
    {
      string padded = text.Replace('-', '+').Replace('_', '/');
      switch (padded.Length % 4)
      {
        case 2:
          padded += "==";
          break;
        case 3:
          padded += "=";
          break;
        case 0:
          break;
        default:
          throw new FormatException("A base64url value cannot have a remainder of one character.");
      }
      return Convert.FromBase64String(padded);
    }
  }
}
