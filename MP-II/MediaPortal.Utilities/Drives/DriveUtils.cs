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
using System.Management;
using System.Text;

using MediaPortal.Utilities.Win32;

namespace MediaPortal.Utilities.Drives
{
  /// <summary>
  /// Contains Methods for all kinds of Drive handling
  /// </summary>
  public class DriveUtils
  {
    /// <summary>
    /// Is the Drive a Network Drive?
    /// </summary>
    /// <param name="strPath"></param>
    /// <returns></returns>
    public static bool IsNetwork(string strPath)
    {
      if (strPath == null) return false;
      if (strPath.Length < 2) return false;
      string strDrive = strPath.Substring(0, 2);
      if (GetDriveType(strDrive) == 4) return true;
      return false;
    }

    /// <summary>
    /// Is the Drive a Harddisk?
    /// </summary>
    /// <param name="strPath"></param>
    /// <returns></returns>
    public static bool IsHD(string strPath)
    {
      if (strPath == null) return false;
      if (strPath.Length < 2) return false;
      string strDrive = strPath.Substring(0, 2);
      if (GetDriveType(strDrive) == 3) return true;
      return false;
    }

    /// <summary>
    /// Is the file a CDA file?
    /// </summary>
    /// <param name="strFile"></param>
    /// <returns></returns>
    public static bool IsCDDA(string strFile)
    {
      if (strFile == null) return false;
      if (strFile.Length <= 0) return false;
      if (strFile.IndexOf("cdda:") >= 0) return true;
      if (strFile.IndexOf(".cda") >= 0) return true;
      return false;
    }

    /// <summary>
    /// Do we have a DVD?
    /// </summary>
    /// <param name="strFile"></param>
    /// <returns></returns>
    public static bool IsDVD(string strFile)
    {
      if (strFile == null) return false;
      if (strFile.Length < 2) return false;
      string strDrive = strFile.Substring(0, 2);
      if (GetDriveType(strDrive) == 5) return true;
      return false;
    }

    /// <summary>
    /// Is this a removeable drive?
    /// </summary>
    /// <param name="strFile"></param>
    /// <returns></returns>
    public static bool IsRemovable(string strFile)
    {
      if (strFile == null) return false;
      if (strFile.Length < 2) return false;
      string strDrive = strFile.Substring(0, 2);
      if (GetDriveType(strDrive) == 2) return true;
      return false;
    }

    /// <summary>
    /// Get Serial Number of the drive
    /// </summary>
    /// <param name="drive"></param>
    /// <returns>Serial Number of the Drive</returns>
    public static string GetDriveSerial(string drive)
    {
      if (drive == null) return string.Empty;
      //receives volume name of drive
      StringBuilder volname = new StringBuilder(256);
      //receives serial number of drive,not in case of network drive(win95/98)
      uint sn;
      uint maxcomplen;//receives maximum component length
      uint sysflags;//receives file system flags
      StringBuilder sysname = new StringBuilder(256);//receives the file system name
      bool retval;//return value

      retval = Win32API.GetVolumeInformation(drive.Substring(0, 2), volname, 256, out sn, out maxcomplen, out sysflags, sysname, 256);

      if (retval)
      {
        return String.Format("{0:X}", sn);
      }
      else return "";
    }

    /// <summary>
    /// Gets the Drive Name
    /// </summary>
    /// <param name="drive"></param>
    /// <returns>The Name of the Drive</returns>
    public static string GetDriveName(string drive)
    {
      if (drive == null) return string.Empty;
      //receives volume name of drive
      StringBuilder volname = new StringBuilder(256);
      //receives serial number of drive,not in case of network drive(win95/98)
      uint sn;
      uint maxcomplen;//receives maximum component length
      uint sysflags;//receives file system flags
      StringBuilder sysname = new StringBuilder(256);//receives the file system name
      bool retval;//return value

      retval = Win32API.GetVolumeInformation(drive, volname, 256, out sn, out maxcomplen, out sysflags, sysname, 256);

      if (retval)
      {
        return volname.ToString();
      }
      else return "";
    }


    /// <summary>
    /// Gets the Drive Type
    /// </summary>
    /// <param name="drive"></param>
    /// <returns></returns>
    public static int GetDriveType(string drive)
    {
      if (drive == null) return 2;
      if ((Win32API.GetDriveType(drive) & 5) == 5) return 5;//cd
      if ((Win32API.GetDriveType(drive) & 3) == 3) return 3;//fixed
      if ((Win32API.GetDriveType(drive) & 2) == 2) return 2;//removable
      if ((Win32API.GetDriveType(drive) & 4) == 4) return 4;//remote disk
      if ((Win32API.GetDriveType(drive) & 6) == 6) return 6;//ram disk
      return 0;
    }

    /// <summary>
    /// Gets the Size of a Disk
    /// </summary>
    /// <param name="drive"></param>
    /// <returns></returns>
    public static long GetDiskSize(string drive)
    {
      long diskSize = 0;
      try
      {
        string cmd = String.Format("win32_logicaldisk.deviceid=\"{0}:\"", drive[0]);
        using (ManagementObject disk = new ManagementObject(cmd))
        {
          disk.Get();
          diskSize = Int64.Parse(disk["Size"].ToString());
        }
      }
      catch (Exception)
      {
        return -1;
      }
      return diskSize;
    }

    /// <summary>
    /// Returns the Free disk space of a drive
    /// </summary>
    /// <param name="drive"></param>
    /// <returns></returns>
    public static ulong GetFreeDiskSpace(string drive)
    {
      ulong freeBytesAvailable = 0;
      ulong totalNumberOfBytes = 0;
      ulong totalNumberOfFreeBytes = 0;
      string driveName = string.Empty;

      if (drive.StartsWith(@"\\"))
        // We've got unc notation
        driveName = drive;
      else
        // We've got a drive letter only
        driveName = drive[0] + @":\";

      Win32API.GetDiskFreeSpaceEx(
         driveName,
         out freeBytesAvailable,
         out totalNumberOfBytes,
         out totalNumberOfFreeBytes);
      return freeBytesAvailable;
    }
  }
}
