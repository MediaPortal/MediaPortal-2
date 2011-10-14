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
using System.IO;

namespace MediaPortal.Common.ResourceAccess
{
  public class DosPathHelper
  {
    /// <summary>
    /// Returns the file name of the given <paramref name="path"/> (inclusive extension).
    /// </summary>
    /// <param name="path">DOS path to examine.</param>
    /// <returns>File name of the given <paramref name="path"/> or <c>null</c> if the given path doesn't denote a file name.</returns>
    public static string GetFileName(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      int sepIndex = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
      return path.Substring(sepIndex + 1);
    }

    /// <summary>
    /// Returns the file extension of the given <paramref name="path"/> inclusive <c>'.'</c>.
    /// </summary>
    /// <remarks>
    /// This method works similar to <see cref="Path.GetExtension"/> but doesn't throw exceptions when the given path contains illegal characters.
    /// </remarks>
    /// <param name="path">DOS path to examine.</param>
    /// <returns>Extension inclusive <c>'.'</c>, like <c>".txt"</c>.</returns>
    public static string GetExtension(string path)
    {
      string fileName = GetFileName(path);
      if (fileName == null)
        return string.Empty;
      int extIndex = fileName.LastIndexOf('.');
      return extIndex == -1 ? string.Empty : fileName.Substring(extIndex);
    }

    /// <summary>
    /// Returns the file name of the given <paramref name="path"/> without extension.
    /// </summary>
    /// <remarks>
    /// This method works similar to <see cref="Path.GetFileNameWithoutExtension"/> but doesn't throw exceptions when the
    /// given path contains illegal characters.
    /// </remarks>
    /// <param name="path">DOS path to examine.</param>
    /// <returns>Extension without extension.</returns>
    public static string GetFileNameWithoutExtension(string path)
    {
      string fileName = GetFileName(path);
      if (fileName == null)
        return string.Empty;
      int extIndex = fileName.LastIndexOf('.');
      return extIndex == -1 ? fileName : fileName.Substring(0, extIndex);
    }
  }
}