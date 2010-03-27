#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Utilities.FileSystem
{
  /// <summary>
  /// Class which can be used as a comparator for filesystem path strings on windows
  /// platforms. This comparator compares file paths case-insensitive.
  /// </summary>
  public class WindowsFilesystemPathComparer : IComparer<string>
  {
    private static WindowsFilesystemPathComparer instance = null;

    public int Compare(string path1, string path2)
    {
      return string.Compare(path1 == null ? null : path1.ToLower(), path2 == null ? null : path2.ToLower());
    }

    public static WindowsFilesystemPathComparer Instance
    {
      get { return instance ?? (instance = new WindowsFilesystemPathComparer()); }
    }
  }
}
