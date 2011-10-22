#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Utilities;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Helper class for resource provider paths which are built in a standard way:
  /// <list type="bullet">
  /// <item><c>"/"</c> is the root element.</item>
  /// <item>Path elements are separated by <c>'/'</c> characters.</item>
  /// <item>A <c>".."</c> path element refers to the parent path element.</item>
  /// </list>
  /// </summary>
  public static class ProviderPathHelper
  {
    public static string Combine(string rootPath, string path)
    {
      while (path.StartsWith("."))
      {
        if (path.StartsWith("./"))
          path = path.Substring(2);
        if (path.StartsWith("../"))
        {
          path = path.Substring(3);
          rootPath = GetDirectoryName(rootPath);
          if (rootPath == null)
            throw new ArgumentException(string.Format("Paths '{0}' and '{1}' cannot be concatenated", rootPath, path));
        }
        while (path.StartsWith("/"))
          // Remove double / characters in the middle of the path
          path = path.Substring(1);
      }
      return path.StartsWith("/") ? path : (StringUtils.CheckSuffix(rootPath, "/") + path);
    }

    public static string GetDirectoryName(string path)
    {
      path = StringUtils.RemoveSuffixIfPresent(path, "/");
      int index = path.LastIndexOf('/');
      return path.Substring(0, index + 1);
    }

    public static string GetFileName(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      int sepIndex = path.LastIndexOf('/');
      return path.Substring(sepIndex + 1);
    }
  }
}