#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// Thrown if a request of an usupported UPnP version should be handled.
  /// </summary>
  public class EncodingUtils
  {
    /// <summary>
    /// Encodes dots in the given <paramref name="url"/> to hyphens.
    /// </summary>
    /// <param name="url">URL string to encode.</param>
    /// <returns><paramref name="url"/> string with all dots replaced by hyphens.</returns>
    public static string PeriodsToHyphens(string url)
    {
      return url.Replace('.', '-');
    }

    /// <summary>
    /// Parses the given <paramref name="contentType"/> string of the form 'text/xml; charset="utf-8"'.
    /// </summary>
    /// <param name="contentType">Content type header to parse.</param>
    /// <param name="defaultEncoding">Encoding to be returned in the <paramref name="encoding"/> parameter if the given
    /// <paramref name="contentType"/> doesn't contain an encoding parameter.</param>
    /// <param name="mediaType">Parsed media type. This parameter will be set if this method returns <c>true</c>. Else, its
    /// return value is undefined.</param>
    /// <param name="encoding">Parsed encoding. This parameter will be set if this method returns <c>true</c>. Else, its
    /// return value is undefined.</param>
    /// <returns><c>true</c>, if the encoding could correctly be parsed, else <c>false</c>.</returns>
    public static bool TryParseContentTypeEncoding(string contentType, Encoding defaultEncoding,
        out string mediaType, out Encoding encoding)
    {
      encoding = defaultEncoding;
      int index = contentType.IndexOf(';');
      if (index > -1)
      {
        mediaType = contentType.Substring(0, index).Trim();
        try
        {
          encoding = Encoding.GetEncoding(contentType.Substring(index + 1).Trim());
        }
        catch (ArgumentException)
        {
          return false;
        }
      }
      else
        mediaType = contentType;
      return true;
    }

    /// <summary>
    /// Escapes the given string <paramref name="str"/> as XML text. Escapes the characters "<", ">", "&", "\"".
    /// </summary>
    /// <example>"Hello <b>World</b>" will be escaped to &quot;Hello &lt;b&gt;World&lt;/b&gt;&quot;.</example>
    /// <param name="str">String to be escaped.</param>
    /// <returns>Escaped string.</returns>
    public static string XMLEscape(string str)
    {
      return str.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;").Replace("\"", "&quot;");
    }

    /// <summary>
    /// Unescapes the given XML text string <paramref name="str"/>. Unescapes the characters "<", ">", "&", "\"".
    /// </summary>
    /// <example>&quot;Hello &lt;b&gt;World&lt;/b&gt;&quot; will be unescaped to "Hello <b>World</b>".</example>
    /// <param name="str">String to be unescaped.</param>
    /// <returns>Unescaped string.</returns>
    public static string XMLUnescape(string str)
    {
      return str.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"");
    }

    /// <summary>
    /// Encodes the given <paramref name="bytes"/> as a hex string.
    /// </summary>
    /// <example>The byte array {5, 16, 200} will be escaped to "0510C8".</example>
    /// <param name="bytes">Byte array to be converted to hex.</param>
    /// <returns>Hex string representing the given <paramref name="bytes"/>.</returns>
    public static string ToHexString(byte[] bytes)
    {
      StringBuilder result = new StringBuilder(bytes.Length * 2);
      foreach (byte b in bytes)
        result.Append(string.Format("{0:X2}", b));
      return result.ToString();
    }

    /// <summary>
    /// Decodes the given <paramref name="hexString"/> as a byte array.
    /// </summary>
    /// <example>The string "0510C8" will be decoded to the byte array {5, 16, 200}.</example>
    /// <param name="hexString">Hex string to be decoded.</param>
    /// <returns>Byte array which is represented by the given <paramref name="hexString"/>.</returns>
    public static byte[] FromHexString(string hexString)
    {
      int len = hexString.Length;
      if (len % 2 != 0)
        throw new ArgumentException("No hex string");
      List<byte> result = new List<byte>(len / 2);
      for (int i = 0; i < len / 2; i++)
      {
        string str = hexString.Substring(i * 2, 2);
        result.Add(Convert.ToByte(str, 16));
      }
      return result.ToArray();
    }

    // Albert 2009-06-27: Contained in System.Web.HttpUtility class
    //public static string UrlEncode(string str)
    //{
    //  byte[] bytes = Encoding.UTF8.GetBytes(str);
    //  StringBuilder builder = new StringBuilder();
    //  foreach (byte num in bytes)
    //    if ((num >= 0x3f && num <= 0x5a) || // ?, @, A-Z
    //       (num >= 0x61 && num <= 0x7a) || // a-z
    //       (num >= 0x2f && num <= 0x39) || // /, 0-9
    //       num == 0x3a || // :
    //       num == 0x3b || // ;
    //       num == 0x3d || // =
    //       num == 0x2b || // +
    //       num == 0x24 || // $
    //       num == 0x2d || // -
    //       num == 0x5f || // _
    //       num == 0x2e || // .
    //       num == 0x2a) // *
    //      builder.Append((char) num);
    //    else
    //      builder.Append("%" + num.ToString("X"));
    //  return builder.ToString();
    //}

    // Albert 2009-06-27: Contained in System.Web.HttpUtility class
    //public static string UrlDecode(string str)
    //{
    //  IEnumerator<char> enumerator = str.GetEnumerator();
    //  List<byte> list = new List<byte>(str.Length);
    //  while (enumerator.MoveNext())
    //  {
    //    if ((enumerator.Current) == '%')
    //    {
    //      if (!enumerator.MoveNext())
    //        throw new ArgumentException(string.Format("Invalid string to unescape '{0}'", str));
    //      char ch = enumerator.Current;
    //      enumerator.MoveNext();
    //      byte num = Convert.ToByte(string.Format("0x{0}{1}", ch, enumerator.Current));
    //      list.Add(num);
    //    }
    //    else
    //      list.Add((byte) enumerator.Current);
    //  }
    //  return Encoding.UTF8.GetString(list.ToArray());
    //}
  }
}
