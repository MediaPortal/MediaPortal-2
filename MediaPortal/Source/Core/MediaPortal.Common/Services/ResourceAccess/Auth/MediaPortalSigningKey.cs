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
using System.Security.Cryptography;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Services.ResourceAccess.Auth
{
  /// <summary>
  /// The key <see cref="MediaPortalAccessToken"/> is signed with, kept in the server's own settings.
  /// </summary>
  /// <remarks>
  /// <para>
  /// On .NET Framework this was not a thing MP2 had to have: Owin's access token format protected the ticket with
  /// <c>System.Web.Security.MachineKey</c>, whose key comes from IIS or from <c>machineKey</c> in web.config. Both
  /// of those are Windows-only, and the .NET Core replacement (<c>DataProtection</c>) brings a key ring of its own -
  /// a folder, its access rules, and an encryption provider that differ per platform. A 32-byte key of MP2's own,
  /// in MP2's settings database, is the smaller and more legible thing: it can be read on any platform, backed up
  /// with the rest of the configuration, and inspected by a human.
  /// </para>
  /// <para>
  /// It is generated on first use rather than required in a configuration file, because a headless server that
  /// refuses to start until somebody has generated a random string is a server that does not start.
  /// </para>
  /// </remarks>
  public static class MediaPortalSigningKey
  {
    /// <summary>
    /// Size of the HMAC-SHA256 key. RFC 2104 recommends the block size of the hash, which for SHA-256 is 64 bytes;
    /// 32 bytes is the key length at which HMAC-SHA256's security equals the hash's own, and it is what every
    /// other JOSE-style token in the tree of this project uses.
    /// </summary>
    public const int KEY_BYTES = 32;

    /// <summary>
    /// A fresh key from the operating system's cryptographic random number generator.
    /// </summary>
    public static byte[] Create()
    {
      return RandomNumberGenerator.GetBytes(KEY_BYTES);
    }

    /// <summary>
    /// Reads a stored key. False unless it is valid base64 for at least <see cref="KEY_BYTES"/> bytes.
    /// </summary>
    public static bool TryDecode(string stored, out byte[] key)
    {
      key = null;
      if (string.IsNullOrWhiteSpace(stored))
        return false;
      try
      {
        byte[] decoded = Convert.FromBase64String(stored.Trim());
        if (decoded.Length < KEY_BYTES)
          return false;
        key = decoded;
        return true;
      }
      catch (FormatException)
      {
        return false;
      }
    }

    /// <summary>
    /// The server's key: the stored one if it is usable, otherwise a new one which is stored and reported as
    /// <paramref name="created"/>.
    /// </summary>
    /// <remarks>
    /// A stored value that cannot be read is replaced rather than fallen back to. Deriving a key from it, or
    /// hashing an empty string and using that, would mean a corrupted setting silently becomes a key an attacker
    /// can compute; regenerating loses the outstanding tokens, which is the correct failure - clients ask for a
    /// new one.
    /// </remarks>
    /// <param name="settings">Settings manager to read and write through. May be null, in which case the key is
    /// ephemeral: it lives as long as the process, which is the right answer for a test host.</param>
    /// <param name="created">True if the returned key is new and therefore invalidates every token issued
    /// before.</param>
    public static byte[] Resolve(ISettingsManager settings, out bool created)
    {
      if (settings != null)
      {
        Settings.ServerSettings serverSettings = settings.Load<Settings.ServerSettings>();
        byte[] stored;
        if (serverSettings != null && TryDecode(serverSettings.WebAccessTokenSigningKey, out stored))
        {
          created = false;
          return stored;
        }

        byte[] fresh = Create();
        if (serverSettings != null)
        {
          serverSettings.WebAccessTokenSigningKey = Convert.ToBase64String(fresh);
          settings.Save(serverSettings);
          created = true;
          return fresh;
        }
      }

      created = true;
      return Create();
    }
  }
}
