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
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MediaPortal.Utilities.FileSystem
{
  /// <summary>
  /// Contains Methods for all kinds of Drive handling
  /// </summary>
  public class DriveUtils
  {
    public enum DriveEjectError
    {
      None,
      InvalidMediaError,
      NotFoundError,
      LockError,
      DismountError,
      PreventRemovalError,
      EjectError
    }

    #region Windows API functions

    private const int OPEN_EXISTING = 3;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FSCTL_LOCK_VOLUME = 0x00090018;
    private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
    private const uint IOCTL_STORAGE_EJECT_MEDIA = 0x002D4808;
    private const uint IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;
    private const long INVALID_HANDLE = -1;

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool GetVolumeInformation(string RootPathName, StringBuilder VolumeNameBuffer, int VolumeNameSize,
      out uint VolumeSerialNumber, out uint MaximumComponentLength, out uint FileSystemFlags, StringBuilder FileSystemNameBuffer,
      int nFileSystemNameSize);

    [DllImport("kernel32.dll")]
    public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out UInt64 lpFreeBytesAvailable, out UInt64 lpTotalNumberOfBytes,
        out UInt64 lpTotalNumberOfFreeBytes);

    [DllImport("kernel32", SetLastError = true)]
    public static extern IntPtr CreateFile(string filename, uint desiredAccess, uint shareMode, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(IntPtr deviceHandle, uint ioControlCode, byte[] inBuffer, int inBufferSize, byte[] outBuffer, int outBufferSize, ref int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    #endregion

    public static string GetDriveNameWithoutRootDirectory(DriveInfo driveInfo)
    {
      return driveInfo.Name.Substring(0, 2);
    }

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

      bool retval = GetVolumeInformation(drive.Substring(0, 2), volname, 256, out sn, out maxcomplen, out sysflags, sysname, 256);

      if (retval)
      {
        return String.Format("{0:X}", sn);
      }
      else return string.Empty;
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
        GetDiskFreeSpaceEx(drive, out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
        return (long)freeBytesAvailable;
      }
    }

    /// <summary>
    /// Ejects the drive for the specified file
    /// </summary>
    /// <returns>Error if any</returns>
    public static DriveEjectError EjectDrive(string strFile)
    {
      if (!IsDVD(strFile) && !IsRemovable(strFile))
        return DriveEjectError.InvalidMediaError;

      bool success = false;
      string sPhysicalDrive = $@"\\.\{strFile.Substring(0, 2)}";

      // Open drive (prepare for eject)
      IntPtr handle = CreateFile(sPhysicalDrive, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
        IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
      if (handle.ToInt64() == INVALID_HANDLE)
        return DriveEjectError.NotFoundError;

      try
      {
        int dummy = 0;
        if (IsRemovable(strFile))
        {
          // Lock Volume (retry 10 times - 5 seconds)
          for (int i = 0; i < 4; i++)
          {
            success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, null, 0, null, 0, ref dummy, IntPtr.Zero);
            if (success)
              break;
            Thread.Sleep(500);
          }
          if (!success)
            return DriveEjectError.LockError;

          // Volume dismount
          dummy = 0;
          success = DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, null, 0, null, 0, ref dummy, IntPtr.Zero);
          if (!success)
            return DriveEjectError.DismountError;

          // Prevent Removal Of Volume
          byte[] flag = new byte[1];
          flag[0] = 0; // 0 = false
          dummy = 0;
          success = DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, flag, 1, null, 0, ref dummy, IntPtr.Zero);
          if (!success)
            return DriveEjectError.PreventRemovalError;
        }

        // Eject Media
        dummy = 0;
        success = DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, null, 0, null, 0, ref dummy, IntPtr.Zero);
        if (!success)
          return DriveEjectError.EjectError;
      }
      finally
      {
        // Close Handle
        CloseHandle(handle);
      }

      return DriveEjectError.None;
    }
  }
}
