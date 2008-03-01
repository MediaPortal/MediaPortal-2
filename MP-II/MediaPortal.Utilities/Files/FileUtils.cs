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

using MediaPortal.Utilities.Strings;

namespace MediaPortal.Utilities.Files
{
  /// <summary>
  /// Contains File and Directory related Methods
  /// </summary>
  public class FileUtils
  {
    /// <summary>
    /// Combines the given Path and Filename into a Full qualified name
    /// </summary>
    /// <param name="strBasePath"></param>
    /// <param name="strFileName"></param>
    public static void GetQualifiedFilename(string strBasePath, ref string strFileName)
    {
      if (strFileName == null) return;
      if (strFileName.Length <= 2) return;
      if (strFileName[1] == ':') return;
      strBasePath = StringUtils.RemoveTrailingSlash(strBasePath);
      while (strFileName.StartsWith(@"..\") || strFileName.StartsWith("../"))
      {
        strFileName = strFileName.Substring(3);
        // Find the last / or \ character, then trim the base path
        int pos = Math.Max(strBasePath.LastIndexOf(@"\"), strBasePath.LastIndexOf(@"/"));
        if (pos > 0)
          strBasePath = strBasePath.Remove(pos);
      }
      if (strBasePath.Length == 2 && strBasePath[1] == ':')
        strBasePath += @"\";
      strFileName = System.IO.Path.Combine(strBasePath, strFileName);
    }
  }
}
