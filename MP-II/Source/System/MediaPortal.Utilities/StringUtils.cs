#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaPortal.Utilities
{
  /// <summary>
  /// Contains String related utility methods.
  /// </summary>
  public class StringUtils
  {
    /// <summary>
    /// Escapes each occurence of special characters in the given string <paramref name="str"/> by a sequence of an
    /// escape character followed by the original special character.
    /// </summary>
    /// <param name="str">String to be escaped.</param>
    /// <param name="specialCharacters">Enumeration of characters which need to be escaped.</param>
    /// <param name="escapeChar">Escape character to be used.</param>
    /// <returns>Escaped string.</returns>
    public static string Escape(string str, IEnumerable<char> specialCharacters, char escapeChar)
    {
      string escapeStr = escapeChar.ToString();
      StringBuilder result = new StringBuilder(str).
          Replace(escapeStr, escapeStr + escapeChar);
      foreach (char specialCharacter in specialCharacters)
        result.Replace(specialCharacter.ToString(), escapeStr + specialCharacter);
      return result.ToString();
    }

    /// <summary>
    /// Replaces the tag 'tag' in the input string with value 'value'
    /// If value is the empty string then the tag is removed from the input string.
    /// </summary>
    /// <param name="input">The string to process.</param>
    /// <param name="tag">The tag to replace.</param>
    /// <param name="value">The value of the replacement.</param>
    /// 
    public static void ReplaceTag(ref string input, string tag, string value)
    {

      Regex r = new Regex(String.Format(@"\[[^%]*{0}[^\]]*[\]]", tag));
      if (value == string.Empty)
      {
        Match match = r.Match(input);
        if (match != null && match.Length > 0)
        {
          input = input.Remove(match.Index, match.Length);
        }
      }
      else
      {
        Match match = r.Match(input);
        if (match != null && match.Length > 0)
        {
          input = input.Remove(match.Index, match.Length);
          string m = match.Value.Substring(1, match.Value.Length - 2);
          input = input.Insert(match.Index, m);
        }
      }
      input = input.Replace(tag, value);
    }

    /// <summary>
    /// Joins the string representations of the given <paramref name="values"/> together with the
    /// specified <paramref name="separator"/>. The method works like <see cref="string.Join(string,string[])"/>,
    /// but takes an enumeration instead of a string array.
    /// </summary>
    /// <param name="separator">Separator to be placed between every two consecutive values.</param>
    /// <param name="values">Enumeration of values to be joined.</param>
    /// <returns>Joined values or <c>string.Empty</c>, if the enumeration is empty.</returns>
    public static string Join(string separator, IEnumerable values)
    {
      if (values == null)
        return string.Empty;
      IEnumerator enumer = values.GetEnumerator();
      if (!enumer.MoveNext())
        return string.Empty;
      StringBuilder result = new StringBuilder();
      while (true)
      {
        result.Append(enumer.Current.ToString());
        if (enumer.MoveNext())
          result.Append(separator);
        else
          return result.ToString();
      }
    }

    public static string TrimToNull(string s)
    {
      return string.IsNullOrEmpty(s) ? null : s;
    }

    public static string TrimToEmpty(string s)
    {
      return string.IsNullOrEmpty(s) ? string.Empty : s;
    }

    /// <summary>
    /// Checks if the specified <paramref name="source"/> string contains the given <paramref name="suffix"/> and removes it,
    /// if present.
    /// </summary>
    /// <param name="source">String to examine.</param>
    /// <param name="suffix">Suffix to be removed.</param>
    /// <returns>String with removed suffix or <c>null</c> if <paramref name="source"/> is null.</returns>
    public static string RemoveSuffixIfPresent(string source, string suffix)
    {
      if (string.IsNullOrEmpty(source))
        return source;
      return source.EndsWith(suffix) ? source.Substring(0, source.Length - suffix.Length) : source;
    }

    /// <summary>
    /// Checks if the specified <paramref name="source"/> string contains the given <paramref name="suffix"/> and adds it if
    /// it isn't present.
    /// </summary>
    /// <param name="source">String to examine.</param>
    /// <param name="suffix">Suffix which must be available.</param>
    /// <returns>String with suffix or <c>null</c> if <paramref name="source"/> is null.</returns>
    public static string CheckSuffix(string source, string suffix)
    {
      if (source == null)
        return null;
      return source.EndsWith(suffix) ? source : source + suffix;
    }

    /// <summary>
    /// Checks if the specified <paramref name="source"/> string contains the given <paramref name="prefix"/> and removes it,
    /// if present.
    /// </summary>
    /// <param name="source">String to examine.</param>
    /// <param name="prefix">Prefix to be removed.</param>
    /// <returns>String with removed prefix or <c>null</c> if <paramref name="source"/> is null.</returns>
    public static string RemovePrefixIfPresent(string source, string prefix)
    {
      if (string.IsNullOrEmpty(source))
        return source;
      return source.StartsWith(prefix) ? source.Substring(prefix.Length) : source;
    }

    /// <summary>
    /// Checks if the specified <paramref name="source"/> string contains the given <paramref name="prefix"/> and adds it if
    /// it isn't present.
    /// </summary>
    /// <param name="source">String to examine.</param>
    /// <param name="prefix">Prefix which must be available.</param>
    /// <returns>String with prefix or <c>null</c> if <paramref name="source"/> is null.</returns>
    public static string CheckPrefix(string source, string prefix)
    {
      if (source == null)
        return null;
      return source.StartsWith(prefix) ? source : prefix + source;
    }

    /// <summary>
    /// Creates a string which consists of <paramref name="count"/> occurences of the given string <paramref name="part"/>.
    /// </summary>
    /// <param name="part">String part to repeat.</param>
    /// <param name="count">Number of times to repeat the given string <paramref name="part"/>.</param>
    /// <returns>String consisting of <paramref name="count"/> repetitions of <paramref name="part"/>.</returns>
    public static string Repeat(string part, int count)
    {
      StringBuilder result = new StringBuilder(part.Length * count);
      for (int i = 0; i < count; i++)
        result.Append(part);
      return result.ToString();
    }
  }
}