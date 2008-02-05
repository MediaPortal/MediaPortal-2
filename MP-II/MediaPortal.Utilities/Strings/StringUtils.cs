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
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Utilities.Strings
{
  /// <summary>
  /// Contains String related methods
  /// </summary>
  public class StringUtils
  {
    /// <summary>
    /// Removes a Trailing Slash from the given string
    /// </summary>
    /// <param name="strLine"></param>
    /// <returns></returns>
    public static string RemoveTrailingSlash(string strLine)
    {
      if (strLine == null) return string.Empty;
      if (strLine.Length == 0) return string.Empty;
      string strPath = strLine;
      while (strPath.Length > 0)
      {
        if (strPath[strPath.Length - 1] == '\\' || strPath[strPath.Length - 1] == '/')
        {
          strPath = strPath.Substring(0, strPath.Length - 1);
        }
        else break;
      }
      return strPath;
    }

  }
}
