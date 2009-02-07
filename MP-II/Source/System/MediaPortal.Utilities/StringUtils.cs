#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
    /// Tries to parse the specified version string in the format <c>#.#</c> or <c>#</c>, where # stands
    /// for an int number.
    /// </summary>
    /// <param name="versionStr">The string to parse. This string should be in the format
    /// <c>#.#</c> or <c>#</c>.</param>
    /// <param name="verHigh">Returns the version high number.</param>
    /// <param name="verHigh">Returns the version low number, if the string contains both. Else, this
    /// parameter will return <c>0</c>.</param>
    /// <returns><c>true</c>, if the version string could correctly be parsed, else <c>false</c>.</returns>
    public static bool TryParseVersionString(string versionStr, out int verHigh, out int verLow)
    {
      string[] numbers = versionStr.Split(new char[] { '.' });
      verLow = 0;
      verHigh = 0;
      if (numbers.Length < 1 || numbers.Length > 2)
        return false;
      if (!Int32.TryParse(numbers[0], out verHigh))
        return false;
      if (numbers.Length > 1)
        if (!Int32.TryParse(numbers[0], out verLow))
          return false;
      return true;
    }

    /// <summary>
    /// Helper method to check the given version string to be equal or greater than the
    /// specified version number.
    /// </summary>
    public static void CheckVersionEG(string versionStr, int expectedHigh, int expectedLow)
    {
      int verHigh;
      int verLow;
      if (!TryParseVersionString(versionStr, out verHigh, out verLow))
        throw new ArgumentException("Illegal version number '" + versionStr + "', expected format: '#.#'");
      if (verHigh >= expectedHigh)
        return;
      if (verLow >= expectedLow)
        return;
      throw new ArgumentException("Version number '" + versionStr +
                                  "' is too low, at least '" + expectedHigh + "." + expectedLow + "' is needed");
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
  }
}