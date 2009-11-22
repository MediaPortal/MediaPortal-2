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
using System.IO;
using System.Management;
using System.Text;

using MediaPortal.Utilities.Win32;

namespace MediaPortal.Utilities.FileSystem
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
      if (GetDriveType(strDrive) == DriveType.Network) return true;
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
      if (GetDriveType(strDrive) == DriveType.Fixed) return true;
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
      if (GetDriveType(strDrive) == DriveType.CDRom) return true;
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
      if (GetDriveType(strDrive) == DriveType.Removable) return true;
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

      bool retval = Win32API.GetVolumeInformation(drive.Substring(0, 2), volname, 256, out sn, out maxcomplen, out sysflags, sysname, 256);

      if (retval)
      {
        return String.Format("{0:X}", sn);
      }
      else return "";
    }

    /// <summary>
    /// Gets the Drive Name.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <param name="drive"></param>
    /// <returns>The Name of the Drive</returns>
    public static string GetDriveName(string drive)
    {
      if (drive == null) return string.Empty;
      return new DriveInfo(drive).Name;
    }

    /// <summary>
    /// Gets the Drive Type.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <param name="drive"></param>
    /// <returns></returns>
    public static DriveType GetDriveType(string drive)
    {
      try
      {
        DriveInfo nfo = new DriveInfo(drive);
        return nfo.DriveType;
      }
      catch (IOException)
      {
        return DriveType.Unknown;
      }
    }

    /// <summary>
    /// Gets the Size of a Disk
    /// </summary>
    /// <param name="drive"></param>
    /// <returns></returns>
    public static long GetDiskSize(string drive)
    {
      try
      {
        string cmd = String.Format("win32_logicaldisk.deviceid=\"{0}:\"", drive[0]);
        using (ManagementObject disk = new ManagementObject(cmd))
        {
          disk.Get();
          return Int64.Parse(disk["Size"].ToString());
        }
      }
      catch (Exception)
      {
        return -1;
      }
    }

    /// <summary>
    /// Returns the Free disk space of a drive
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <param name="drive"></param>
    /// <returns></returns>
    public static long GetFreeDiskSpace(string drive)
    {
      if (!drive.StartsWith(@"\\"))
      {
        // This is not a UNC path.
        try
        {
          DriveInfo nfo = new DriveInfo(drive[0] + @":\");
          return nfo.AvailableFreeSpace;
        }
        catch (IOException)
        {
          return 0;
        }
      }
      else
      {
        // UNC path.
        ulong freeBytesAvailable;
        ulong totalNumberOfBytes;
        ulong totalNumberOfFreeBytes;
        Win32API.GetDiskFreeSpaceEx(
            drive,
            out freeBytesAvailable,
            out totalNumberOfBytes,
            out totalNumberOfFreeBytes);
        return (long)freeBytesAvailable;
      }
    }
  }
}