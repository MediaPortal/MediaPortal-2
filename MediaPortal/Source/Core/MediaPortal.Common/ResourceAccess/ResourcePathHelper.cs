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
using System.IO;
using MediaPortal.Utilities;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Helper class for <see cref="ResourcePath"/>s that supports important features for path and filename handling
  /// like <see cref="System.IO.Path"/> does for regular filesystem paths.
  /// </summary>
  /// <remarks>
  /// This class supports the common format:
  /// <list type="bullet">
  /// <item><c>"/"</c> is the root element.</item>
  /// <item>Path elements are separated by <c>'/'</c> characters.</item>
  /// <item>A <c>".."</c> path element refers to the parent path element.</item>
  /// </list>
  /// When using <c>".."</c> in chained ResourcePaths, it is possible to address the parent provider path like:
  /// <example>
  /// string path =                                       "{e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}:///C:/Videos/ISOs/VideoDisc.iso>{112728b1-f71d-4284-9e5c-3462e8d3c74d}:///VIDEO_TS/";
  /// ResourcePathHelper.Combine(path, "../movie.nfo"):    {e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}:///C:/Videos/ISOs/VideoDisc.iso>{112728b1-f71d-4284-9e5c-3462e8d3c74d}:///movie.nfo"
  /// ResourcePathHelper.Combine(path, "../../movie.nfo"): {e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}:///C:/Videos/ISOs/movie.nfo"
  /// </example>
  /// </remarks>
  public static class ResourcePathHelper
  {
    public static ResourcePath Combine(ResourcePath rootPath, string path)
    {
      return ResourcePath.Deserialize(Combine(rootPath.Serialize(), path));
    }

    public static string Combine(string rootPath, string path)
    {
      while (path.StartsWith("."))
      {
        if (path.StartsWith("./"))
          path = path.Substring(2);
        else if (path.StartsWith("../"))
        {
          path = path.Substring(3);
          rootPath = GetDirectoryName(rootPath);
          if (rootPath == null)
            throw new ArgumentException(string.Format("Paths '{0}' and '{1}' cannot be concatenated", rootPath, path));
        }
        else
            // First path segment starts with a '.', we don't need to adjust
          break;
        // We only come here if at least one './' or '../' was removed
        while (path.StartsWith("/"))
            // Remove double / characters in the middle of the path
          path = path.Substring(1);
      }
      return path.StartsWith("/") ? path : (StringUtils.CheckSuffix(rootPath, "/") + path);
    }

    public static string GetDirectoryName(string path)
    {
      if (path.EndsWith(":///"))
      {
        if (path.Contains(">"))
          path = path.Substring(0, path.LastIndexOf(">") - 1);
        else
          return null;
      }
      else
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

    /// <summary>
    /// Returns the file extension of the given <paramref name="path"/> inclusive <c>'.'</c>. Only the last segment of a chained ResourcePath is handled.
    /// </summary>
    /// <remarks>
    /// This method works similar to <see cref="Path.GetExtension"/> but doesn't throw exceptions when the given path contains illegal characters.
    /// </remarks>
    /// <param name="path">Provider path to examine.</param>
    /// <returns>Extension inclusive <c>'.'</c>, like <c>".txt"</c>, or <see cref="string.Empty"/> if the given <paramref name="path"/>
    /// doesn't have a file extension.</returns>
    public static string GetExtension(string path)
    {
      if (string.IsNullOrEmpty(path))
        return string.Empty;
      int extIndex = path.LastIndexOf('.');
      int chainedIndex = path.LastIndexOf('>');
      return extIndex != -1 && extIndex > chainedIndex ? path.Substring(extIndex) : string.Empty;
    }

    /// <summary>
    /// Returns the file name of the given <paramref name="path"/> without extension.
    /// </summary>
    /// <remarks>
    /// This method works similar to <see cref="Path.GetFileNameWithoutExtension"/> but doesn't throw exceptions when the
    /// given path contains illegal characters.
    /// </remarks>
    /// <param name="path">Provider path to examine.</param>
    /// <returns>File name without extension or <see cref="string.Empty"/>, if the given <paramref name="path"/> doesn't have a file name.</returns>
    public static string GetFileNameWithoutExtension(string path)
    {
      string fileName = GetFileName(path);
      if (fileName == null)
        return string.Empty;
      int extIndex = fileName.LastIndexOf('.');
      return extIndex == -1 ? fileName : fileName.Substring(0, extIndex);
    }

    /// <summary>
    /// Replaces the extension of the given <paramref name="path"/> with the new <paramref name="extension"/>.
    /// </summary>
    /// <remarks>
    /// This method works similar to <see cref="Path.ChangeExtension"/> but doesn't throw exceptions when the
    /// given path contains illegal characters.
    /// </remarks>
    /// <param name="path">Provider path.</param>
    /// <param name="extension">New extension for path.</param>
    /// <returns>Replaced path.</returns>
    public static string ChangeExtension(string path, string extension)
    {
      if (string.IsNullOrEmpty(path))
        return path;
      string oldExtension = GetExtension(path);
      return path.Substring(0, path.Length - oldExtension.Length) + extension;
    }
  }
}