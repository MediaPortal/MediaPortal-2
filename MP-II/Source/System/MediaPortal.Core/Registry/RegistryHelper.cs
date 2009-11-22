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
using MediaPortal.Utilities;

namespace MediaPortal.Core.Registry
{
  public static class RegistryHelper
  {
    /// <summary>
    /// Helper method to concatenate two registry paths.
    /// </summary>
    /// <param name="parent">Parent path, may be absolute, relative, empty string or <c>null</c>.</param>
    /// <param name="child">Relative child path or empty string or <c>null</c>.</param>
    /// <returns>Concatenated path.</returns>
    public static string ConcatenatePaths(string parent, string child)
    {
      CheckRelative(child);
      parent = RemoveTrailingSlash(parent);
      child = RemoveTrailingSlash(child);
      if (string.IsNullOrEmpty(parent))
        return child;
      if (string.IsNullOrEmpty(child))
        return parent;
      return parent + "/" + child;
    }

    /// <summary>
    /// Helper method to remove a trailing slash from a path, if it ends with one.
    /// </summary>
    /// <param name="path">Path or <c>null</c>.</param>
    /// <returns>Path without a trailing slash or <c>null</c>, if <paramref name="path"/> is null.</returns>
    public static string RemoveTrailingSlash(string path)
    {
      return StringUtils.RemoveSuffixIfPresent(path, "/");
    }

    public static bool IsAbsolutePath(string path)
    {
      return path.StartsWith("/");
    }

    public static void CheckRelative(string path)
    {
      if (IsAbsolutePath(path))
        throw new ArgumentException("Registry path expression has to be a relative path (must not start with a '/' character)");
    }

    public static void CheckAbsolute(string path)
    {
      if (!IsAbsolutePath(path))
        throw new ArgumentException("Registry path expression has to be an absolute path (should start with a '/' character)");
    }

    public static string RemoveRootFromAbsolutePath(string path)
    {
      CheckAbsolute(path);
      int i = 0;
      while (path.Length > i && path[i] == '/')
        i++;
      return path.Substring(i);
    }

    public static string GetLastPathSegment(string path)
    {
      int i = path.LastIndexOf('/');
      return i == -1 ? path : path.Substring(i + 1);
    }

    public static string GetParentPath(string path)
    {
      int i = path.LastIndexOf('/');
      return i > 0 ? path.Substring(0, i - 1) : string.Empty;
    }
  }
}
