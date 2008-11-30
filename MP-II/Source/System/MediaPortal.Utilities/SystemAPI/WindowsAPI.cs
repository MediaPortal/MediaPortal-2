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
using System.Text;
using MediaPortal.Utilities.Win32;

namespace MediaPortal.Utilities.SystemAPI
{
  /// <summary>
  /// For calls to the Windows API, this class should be used instead of directly using
  /// the underlaying system's API. This class hides the concrete underlaying system and will
  /// use different system functions depending on the underlaying system.
  /// </summary>
  public static class WindowsAPI
  {
    public static int S_OK = 0x0;
    public static int S_FALSE = 0x1;

    public static int MAX_PATH = 260;

    /// <summary>
    /// Use this enum to denote special system folders.
    /// </summary>
    public enum SpecialFolder
    {
      MyMusic,
      MyVideos,
      MyPictures,
    }

    /// <summary>
    /// Returns the path of the given system's special folder.
    /// </summary>
    /// <param name="folder">Folder to retrieve.</param>
    /// <param name="folderPath">Will be set to the folder path if the result value is <c>true</c>.</param>
    /// <returns><c>true</c>, if the specified special folder could be retrieved. Else <c>false</c>
    /// will be returned.</returns>
    public static bool GetSpecialFolder(SpecialFolder folder, out string folderPath)
    {
      folderPath = null;
      switch (folder)
      {
        case SpecialFolder.MyMusic:
          folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
          return true;
        case SpecialFolder.MyPictures:
          folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
          return true;
        case SpecialFolder.MyVideos:
          StringBuilder sb = new StringBuilder(MAX_PATH);
          if (Win32API.SHGetFolderPath(IntPtr.Zero, Win32API.CSIDL_MYVIDEO, IntPtr.Zero, Win32API.SHGFP_TYPE_CURRENT, sb) == S_OK)
          {
            folderPath = sb.ToString();
            return true;
          }
          return false;
        default:
          throw new NotImplementedException(string.Format(
              "The handling for special folder '{0}' isn't implemented yet", folder.ToString()));
      }
    }
  }
}