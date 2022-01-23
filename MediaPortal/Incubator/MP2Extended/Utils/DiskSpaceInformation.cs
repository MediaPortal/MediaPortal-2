#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Runtime.InteropServices;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  internal static class DiskSpaceInformation
  {
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
       out ulong lpFreeBytesAvailable,
       out ulong lpTotalNumberOfBytes,
       out ulong lpTotalNumberOfFreeBytes);

    public static WebDiskSpaceInformation GetSpaceInformation(string directory)
    {
      ulong freeBytes, totalBytes, freeBytesAvailable;
      try
      {
        directory = Path.GetPathRoot(directory);
      }
      catch (Exception ex)
      {
        Logger.Error("GetSpaceInformation Exception! - directory: '{0}'", ex, directory);
        return new WebDiskSpaceInformation();
      }
      if (!GetDiskFreeSpaceEx(directory, out freeBytesAvailable, out totalBytes, out freeBytes))
        Logger.Warn("GetDiskFreeSpaceEx failed (0x{0:x8})", Marshal.GetLastWin32Error());

      return new WebDiskSpaceInformation()
      {
        Disk = directory,
        Available = (float)Math.Round(freeBytes / 1024.0 / 1024 / 1024, 2),
        Size = (float)Math.Round(totalBytes / 1024.0 / 1024 / 1024, 2),
        Used = (float)Math.Round((totalBytes - freeBytes) / 1024.0 / 1024 / 1024, 2),
        PercentageUsed = totalBytes > 0 ? (float)(100 - Math.Round((float)freeBytes / (float)totalBytes * 100, 1)) : (float)0
      };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
