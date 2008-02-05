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

namespace MediaPortal.Utilities.Win32
{
  /// <summary>
  /// Contains Methods for Windows Special Folder Handling
  /// </summary>
  class SpecialFolder
  {
    /// <summary>
    /// Returns the Special Folder Path for the giveb CSIDL
    /// </summary>
    /// <param name="csidl"></param>
    /// <returns></returns>
    public static string GetFolderPath(int csidl)
    {
      StringBuilder folder = new System.Text.StringBuilder(256);
      Win32API.SHGetFolderPath(IntPtr.Zero, csidl, IntPtr.Zero, Win32API.SHGFP_TYPE_CURRENT, folder);
      return folder.ToString();
    }
  }
}
