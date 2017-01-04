#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Web;

namespace MediaPortal.Common.Network
{
  /// <summary>
  /// <see cref="UriExtension"/> provides a custom escaping methods for special characters like ampersands. Without special handling, properly uri encoded
  /// ampersands are wrongly decoded inside <see cref="Uri.ToString"/> and also <see cref="HttpRequest.Params"/>.
  /// </summary>
  public static class UriExtension
  {
    /// <summary>
    /// Encodes the given <paramref name="value"/> into a valid uri string.
    /// </summary>
    /// <param name="value">Decoded string.</param>
    /// <returns>Encoded string.</returns>
    public static string Encode(this string value)
    {
      return HttpUtility.UrlEncode(Escape(value));
    }

    /// <summary>
    /// Decodes the given <paramref name="value"/> into original text.
    /// </summary>
    /// <param name="value">Encoded string.</param>
    /// <returns>Decoded string.</returns>
    public static string Decode(this string value)
    {
      return Unescape(value);
    }

    private static string Escape(string value)
    {
      return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("&", "|*|");
    }

    private static string Unescape(string value)
    {
      return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|*|", "&");
    }
  }
}
