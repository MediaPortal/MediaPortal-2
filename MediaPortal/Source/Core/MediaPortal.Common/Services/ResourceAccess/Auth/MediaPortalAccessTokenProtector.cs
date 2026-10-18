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
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Services.ResourceAccess.Auth
{
  /// <summary>
  /// Issues and validates <see cref="MediaPortalAccessToken"/>s with the server's signing key.
  /// </summary>
  /// <remarks>
  /// An interface and a registered service rather than static calls, so that a test can hand a handler a protector
  /// whose key it knows and can make up an expired token on purpose. It is also the seam a future deployment can
  /// move to a hardware key or a rotated key pair behind, without touching the handler or the endpoint.
  /// </remarks>
  public interface IMediaPortalAccessTokenProtector
  {
    /// <summary>
    /// Signs <paramref name="identity"/> into a token that stays valid for <paramref name="lifetime"/>.
    /// </summary>
    string Create(ClaimsIdentity identity, TimeSpan lifetime);

    /// <summary>
    /// Convenience overload for the lifetime the server hands out by default.
    /// </summary>
    string Create(ClaimsIdentity identity);

    /// <summary>
    /// Verifies signature, issuer and expiry. <paramref name="error"/> says why not, without repeating the token.
    /// </summary>
    bool TryValidate(string token, out ClaimsIdentity identity, out string error);

    /// <summary>
    /// The lifetime <see cref="Create(ClaimsIdentity)"/> uses.
    /// </summary>
    TimeSpan Lifetime { get; }
  }

  /// <summary>
  /// The default protector: HMAC-SHA256 with the key in <see cref="Settings.ServerSettings"/>.
  /// </summary>
  public class MediaPortalAccessTokenProtector : IMediaPortalAccessTokenProtector
  {
    private readonly object _keyLock = new object();
    private byte[] _key;

    public MediaPortalAccessTokenProtector()
    {
    }

    /// <summary>
    /// For tests and for a host that keeps its own settings store: use exactly this key and go nowhere else.
    /// </summary>
    public MediaPortalAccessTokenProtector(byte[] signingKey)
    {
      _key = signingKey;
    }

    public TimeSpan Lifetime
    {
      get { return MediaPortalAccessToken.DefaultLifetime; }
    }

    public string Create(ClaimsIdentity identity)
    {
      return Create(identity, Lifetime);
    }

    public string Create(ClaimsIdentity identity, TimeSpan lifetime)
    {
      return MediaPortalAccessToken.Create(Key(), identity, lifetime);
    }

    public bool TryValidate(string token, out ClaimsIdentity identity, out string error)
    {
      return MediaPortalAccessToken.TryValidate(Key(), token, out identity, out error);
    }

    /// <summary>
    /// The signing key, read once.
    /// </summary>
    /// <remarks>
    /// Read lazily and cached rather than resolved in the constructor: this object is built by ASP.NET's
    /// container, which may do so before MP2's services are all in place, and the key belongs in the settings
    /// store - which is a database the server may still be opening. The lock is around a one-time read, not around
    /// the signing, so requests do not queue behind each other.
    /// </remarks>
    private byte[] Key()
    {
      byte[] key = _key;
      if (key != null)
        return key;

      lock (_keyLock)
      {
        if (_key == null)
        {
          bool created;
          _key = MediaPortalSigningKey.Resolve(ServiceRegistration.Get<ISettingsManager>(), out created);
        }
        return _key;
      }
    }
  }

  /// <summary>
  /// Registers the MediaPortal bearer scheme with ASP.NET Core's authentication stack.
  /// </summary>
  public static class MediaPortalAuthenticationExtensions
  {
    /// <summary>
    /// Adds the scheme and makes it the default, so that <c>[Authorize]</c> and
    /// <see cref="MediaPortalAuthorizeAttribute"/> both see it.
    /// </summary>
    /// <remarks>
    /// Safe to call twice (the protector is added with <c>TryAdd</c> and the scheme overwrites itself), which
    /// matters because two listeners on one process - the resource server and the UPnP server - each configure
    /// their own services.
    /// </remarks>
    public static IServiceCollection AddMediaPortalTokenAuthentication(this IServiceCollection services)
    {
      services.TryAddSingleton<IMediaPortalAccessTokenProtector, MediaPortalAccessTokenProtector>();
      services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = MediaPortalBearerHandler.SCHEME;
        options.DefaultChallengeScheme = MediaPortalBearerHandler.SCHEME;
      })
      .AddScheme<AuthenticationSchemeOptions, MediaPortalBearerHandler>(MediaPortalBearerHandler.SCHEME, null);
      return services;
    }
  }
}
