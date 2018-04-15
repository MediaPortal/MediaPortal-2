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

using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Players.BassPlayer.Utils
{
  public class URLUtils
  {
    /// <summary>
    /// Determines if the given <paramref name="filePath"/> represents a MOD audio file.
    /// </summary>
    /// <param name="filePath">The path of the file to be examined.</param>
    /// <returns><c>true</c> if the extension of the given file path is one of the known MOD file extensions,
    /// else <c>false</c>.</returns>
    public static bool IsMODFile(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        return false;
      string ext = DosPathHelper.GetExtension(filePath).ToLowerInvariant();
      switch (ext)
      {
        case ".mod":
        case ".mo3":
        case ".it":
        case ".xm":
        case ".s3m":
        case ".mtm":
        case ".umx":
          return true;
        default:
          return false;
      }
    }

    public static bool IsLastFMStream(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        return false;
      if (filePath.StartsWith(@"http://"))
      {
        if (filePath.IndexOf(@"/last.mp3?") > 0)
          return true;
        if (filePath.Contains(@"last.fm/"))
          return true;
      }

      return false;
    }

    public static bool IsCDDA(string filePath)
    {
      if (string.IsNullOrEmpty(filePath))
        return false;
      return filePath.IndexOf("cdda:") >= 0 || filePath.IndexOf(".cda") >= 0;
    }
  }
}