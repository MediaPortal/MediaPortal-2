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