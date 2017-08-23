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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Dokan;
using MediaPortal.Common.Services.Dokan.Native;
using FileAccess = MediaPortal.Common.Services.Dokan.FileAccess;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Dokan
{
  internal class Proxy
  {
    #region Delegates

    internal delegate NtStatus ZwCreateFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, IntPtr SecurityContext, uint rawDesiredAccess, uint rawFileAttributes,
      uint rawShareAccess, uint rawCreateDisposition, uint rawCreateOptions,
      [MarshalAs(UnmanagedType.LPStruct), In, Out] DokanFileInfo dokanFileInfo);

    internal delegate void CleanupDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPStruct), In, Out] DokanFileInfo rawFileInfo);

    internal delegate void CloseFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPStruct), In, Out] DokanFileInfo rawFileInfo);

    internal delegate NtStatus ReadFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] byte[] rawBuffer, uint rawBufferLength,
      ref int rawReadLength, long rawOffset,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus WriteFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] rawBuffer, uint rawNumberOfBytesToWrite,
      ref int rawNumberOfBytesWritten, long rawOffset,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus FlushFileBuffersDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus GetFileInformationDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string fileName, ref BY_HANDLE_FILE_INFORMATION handleFileInfo,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo fileInfo);

    internal delegate NtStatus FindFilesDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, IntPtr rawFillFindData, // function pointer
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus FindFilesWithPatternDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPWStr)] string rawSearchPattern,
      IntPtr rawFillFindData, // function pointer
      [MarshalAs(UnmanagedType.LPStruct), In, Out] DokanFileInfo rawFileInfo);

    internal delegate NtStatus SetFileAttributesDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, uint rawAttributes,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus SetFileTimeDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      ref FILETIME rawCreationTime, ref FILETIME rawLastAccessTime, ref FILETIME rawLastWriteTime,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus DeleteFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus DeleteDirectoryDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus MoveFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName,
      [MarshalAs(UnmanagedType.LPWStr)] string rawNewFileName,
      [MarshalAs(UnmanagedType.Bool)] bool rawReplaceIfExisting,
      [MarshalAs(UnmanagedType.LPStruct), In, Out] DokanFileInfo rawFileInfo);

    internal delegate NtStatus SetEndOfFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, long rawByteOffset,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus SetAllocationSizeDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, long rawLength,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus LockFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, long rawByteOffset, long rawLength,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus UnlockFileDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, long rawByteOffset, long rawLength,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus GetDiskFreeSpaceDelegate(
      ref long rawFreeBytesAvailable, ref long rawTotalNumberOfBytes, ref long rawTotalNumberOfFreeBytes,
      [MarshalAs(UnmanagedType.LPStruct), In] DokanFileInfo rawFileInfo);

    internal delegate NtStatus GetVolumeInformationDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] StringBuilder rawVolumeNameBuffer, uint rawVolumeNameSize,
      ref uint rawVolumeSerialNumber, ref uint rawMaximumComponentLength, ref FileSystemFeatures rawFileSystemFlags,
      [MarshalAs(UnmanagedType.LPWStr)] StringBuilder rawFileSystemNameBuffer,
      uint rawFileSystemNameSize, [MarshalAs(UnmanagedType.LPStruct), In] DokanFileInfo rawFileInfo);

    internal delegate NtStatus GetFileSecurityDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, [In] ref SECURITY_INFORMATION rawRequestedInformation,
      IntPtr rawSecurityDescriptor, uint rawSecurityDescriptorLength,
      ref uint rawSecurityDescriptorLengthNeeded,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus SetFileSecurityDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, [In] ref SECURITY_INFORMATION rawSecurityInformation,
      IntPtr rawSecurityDescriptor, uint rawSecurityDescriptorLength,
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus FindStreamsDelegate(
      [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, IntPtr rawFillFindData, // function pointer
      [MarshalAs(UnmanagedType.LPStruct), In /*, Out*/] DokanFileInfo rawFileInfo);

    internal delegate NtStatus MountedDelegate(
      [MarshalAs(UnmanagedType.LPStruct), In] DokanFileInfo rawFileInfo);

    internal delegate NtStatus UnmountedDelegate(
      [MarshalAs(UnmanagedType.LPStruct), In] DokanFileInfo rawFileInfo);

    #endregion Delegates

    #region Fields

    private readonly IDokanOperations _operations;

    private readonly uint _serialNumber;

    #endregion Fields

    public Proxy(IDokanOperations operations)
    {
      _operations = operations;
      _serialNumber = (uint)this._operations.GetHashCode();
    }

    internal NtStatus ZwCreateFileProxy(string rawFileName, IntPtr SecurityContext, uint rawDesiredAccess, uint rawFileAttributes, uint rawShareAccess, uint rawCreateDisposition, uint rawCreateOptions, DokanFileInfo dokanFileInfo)
    {
      try
      {
        FileOptions fileOptions = 0;
        FileAttributes fileAttributes = 0;
        int fileAttributesAndFlags = 0;
        int creationDisposition = 0;
        DokanNativeMethods.DokanMapKernelToUserCreateFileFlags(rawFileAttributes, rawCreateOptions, rawCreateDisposition, ref fileAttributesAndFlags, ref creationDisposition);

        foreach (FileOptions fileOption in Enum.GetValues(typeof(FileOptions)))
        {
          if (((FileOptions)(fileAttributesAndFlags & 0xffffc000) & fileOption) == fileOption)
            fileOptions |= fileOption;
        }

        foreach (FileAttributes fileAttribute in Enum.GetValues(typeof(FileAttributes)))
        {
          if (((FileAttributes)(fileAttributesAndFlags & 0x3fff) & fileAttribute) == fileAttribute)
            fileAttributes |= fileAttribute;
        }

        NtStatus result = _operations.CreateFile(rawFileName, (FileAccess)rawDesiredAccess, (FileShare)rawShareAccess, (FileMode)creationDisposition, fileOptions, fileAttributes, dokanFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.Unsuccessful;
      }
    }

    ////

    internal void CleanupProxy(string rawFileName, DokanFileInfo rawFileInfo)
    {
      try
      {
        _operations.Cleanup(rawFileName, rawFileInfo);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
      }
    }

    ////

    internal void CloseFileProxy(string rawFileName, DokanFileInfo rawFileInfo)
    {
      try
      {
        _operations.CloseFile(rawFileName, rawFileInfo);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
      }
    }

    ////

    internal NtStatus ReadFileProxy(string rawFileName, byte[] rawBuffer, uint rawBufferLength, ref int rawReadLength, long rawOffset, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.ReadFile(rawFileName, rawBuffer, out rawReadLength, rawOffset, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus WriteFileProxy(string rawFileName, byte[] rawBuffer, uint rawNumberOfBytesToWrite, ref int rawNumberOfBytesWritten, long rawOffset, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.WriteFile(rawFileName, rawBuffer, out rawNumberOfBytesWritten, rawOffset, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus FlushFileBuffersProxy(string rawFileName, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.FlushFileBuffers(rawFileName, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus GetFileInformationProxy(string rawFileName, ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation, DokanFileInfo rawFileInfo)
    {
      FileInformation fileInformation;
      try
      {
        NtStatus result = _operations.GetFileInformation(rawFileName, out fileInformation, rawFileInfo);

        if (result == DokanResult.Success)
        {
          rawHandleFileInformation.dwFileAttributes = (uint)fileInformation.Attributes;

          long ctime = fileInformation.CreationTime.ToFileTime();
          long atime = fileInformation.LastAccessTime.ToFileTime();
          long mtime = fileInformation.LastWriteTime.ToFileTime();
          rawHandleFileInformation.ftCreationTime.dwHighDateTime = (int)(ctime >> 32);
          rawHandleFileInformation.ftCreationTime.dwLowDateTime = (int)(ctime & 0xffffffff);

          rawHandleFileInformation.ftLastAccessTime.dwHighDateTime = (int)(atime >> 32);
          rawHandleFileInformation.ftLastAccessTime.dwLowDateTime = (int)(atime & 0xffffffff);

          rawHandleFileInformation.ftLastWriteTime.dwHighDateTime = (int)(mtime >> 32);
          rawHandleFileInformation.ftLastWriteTime.dwLowDateTime = (int)(mtime & 0xffffffff);

          rawHandleFileInformation.dwVolumeSerialNumber = _serialNumber;

          rawHandleFileInformation.nFileSizeLow = (uint)(fileInformation.Length & 0xffffffff);
          rawHandleFileInformation.nFileSizeHigh = (uint)(fileInformation.Length >> 32);
          rawHandleFileInformation.dwNumberOfLinks = 1;
          rawHandleFileInformation.nFileIndexHigh = 0;
          rawHandleFileInformation.nFileIndexLow = (uint)fileInformation.FileName.GetHashCode();
        }
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus FindFilesProxy(string rawFileName, IntPtr rawFillFindData, DokanFileInfo rawFileInfo)
    {
      try
      {
        IList<FileInformation> files;
        NtStatus result = _operations.FindFiles(rawFileName, out files, rawFileInfo);

        if (result == DokanResult.Success && files.Count != 0)
        {
          var fill = (FILL_FIND_FILE_DATA)Marshal.GetDelegateForFunctionPointer(rawFillFindData, typeof(FILL_FIND_FILE_DATA));
          // used a single entry call to speed up the "enumeration" of the list
          for (int index = 0; index < files.Count; index++)
          {
            Addto(fill, rawFileInfo, files[index]);
          }
        }
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    private static void Addto(FILL_FIND_FILE_DATA fill, DokanFileInfo rawFileInfo, FileInformation fi)
    {
      long ctime = fi.CreationTime.ToFileTime();
      long atime = fi.LastAccessTime.ToFileTime();
      long mtime = fi.LastWriteTime.ToFileTime();
      var data = new WIN32_FIND_DATA
      {
        dwFileAttributes = fi.Attributes,
        ftCreationTime =
        {
          dwHighDateTime = (int)(ctime >> 32),
          dwLowDateTime = (int)(ctime & 0xffffffff)
        },
        ftLastAccessTime =
        {
          dwHighDateTime = (int)(atime >> 32),
          dwLowDateTime = (int)(atime & 0xffffffff)
        },
        ftLastWriteTime =
        {
          dwHighDateTime = (int)(mtime >> 32),
          dwLowDateTime = (int)(mtime & 0xffffffff)
        },
        nFileSizeLow = (uint)(fi.Length & 0xffffffff),
        nFileSizeHigh = (uint)(fi.Length >> 32),
        cFileName = fi.FileName
      };
      //ZeroMemory(&data, sizeof(WIN32_FIND_DATAW));

      fill(ref data, rawFileInfo);
    }

    #region Nested type: FILL_FIND_FILE_DATA

    private delegate long FILL_FIND_FILE_DATA(
      ref WIN32_FIND_DATA rawFindData, [MarshalAs(UnmanagedType.LPStruct), In] DokanFileInfo rawFileInfo);

    #endregion Nested type: FILL_FIND_FILE_DATA

    internal NtStatus FindFilesWithPatternProxy(string rawFileName, string rawSearchPattern, IntPtr rawFillFindData, DokanFileInfo rawFileInfo)
    {
      try
      {
        IList<FileInformation> files;

        NtStatus result = _operations.FindFilesWithPattern(rawFileName, rawSearchPattern, out files, rawFileInfo);

        if (result == DokanResult.Success && files.Count != 0)
        {
          var fill = (FILL_FIND_FILE_DATA)Marshal.GetDelegateForFunctionPointer(rawFillFindData, typeof(FILL_FIND_FILE_DATA));
          // used a single entry call to speed up the "enumeration" of the list
          for (int index = 0; index < files.Count; index++)
          {
            Addto(fill, rawFileInfo, files[index]);
          }
        }
        return result;
      }

      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus FindStreamsProxy(string rawFileName, IntPtr rawFillFindData, DokanFileInfo rawFileInfo)
    {
      try
      {
        IList<FileInformation> files;

        NtStatus result = _operations.FindStreams(rawFileName, out files, rawFileInfo);

        if (result == DokanResult.Success && files.Count != 0)
        {
          var fill =
            (FILL_FIND_STREAM_DATA)Marshal.GetDelegateForFunctionPointer(rawFillFindData, typeof(FILL_FIND_STREAM_DATA));
          // used a single entry call to speed up the "enumeration" of the list
          for (int index = 0; index < files.Count; index++)
          {
            Addto(fill, rawFileInfo, files[index]);
          }
        }
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    private static void Addto(FILL_FIND_STREAM_DATA fill, DokanFileInfo rawFileInfo, FileInformation fi)
    {
      var data = new WIN32_FIND_STREAM_DATA
      {
        StreamSize = fi.Length,
        cStreamName = fi.FileName
      };
      //ZeroMemory(&data, sizeof(WIN32_FIND_DATAW));

      fill(ref data, rawFileInfo);
    }

    #region Nested type: FILL_FIND_STREAM_DATA

    private delegate long FILL_FIND_STREAM_DATA(
      ref WIN32_FIND_STREAM_DATA rawFindData, [MarshalAs(UnmanagedType.LPStruct), In] DokanFileInfo rawFileInfo);

    #endregion Nested type: FILL_FIND_STREAM_DATA

    ////

    internal NtStatus SetEndOfFileProxy(string rawFileName, long rawByteOffset, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.SetEndOfFile(rawFileName, rawByteOffset, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus SetAllocationSizeProxy(string rawFileName, long rawLength, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.SetAllocationSize(rawFileName, rawLength, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus SetFileAttributesProxy(string rawFileName, uint rawAttributes, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.SetFileAttributes(rawFileName, (FileAttributes)rawAttributes, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus SetFileTimeProxy(string rawFileName, ref FILETIME rawCreationTime, ref FILETIME rawLastAccessTime, ref FILETIME rawLastWriteTime, DokanFileInfo rawFileInfo)
    {
      var ctime = (rawCreationTime.dwLowDateTime != 0 || rawCreationTime.dwHighDateTime != 0) && (rawCreationTime.dwLowDateTime != -1 || rawCreationTime.dwHighDateTime != -1)
        ? DateTime.FromFileTime(((long)rawCreationTime.dwHighDateTime << 32) | (uint)rawCreationTime.dwLowDateTime)
        : (DateTime?)null;
      var atime = (rawLastAccessTime.dwLowDateTime != 0 || rawLastAccessTime.dwHighDateTime != 0) && (rawLastAccessTime.dwLowDateTime != -1 || rawLastAccessTime.dwHighDateTime != -1)
        ? DateTime.FromFileTime(((long)rawLastAccessTime.dwHighDateTime << 32) | (uint)rawLastAccessTime.dwLowDateTime)
        : (DateTime?)null;
      var mtime = (rawLastWriteTime.dwLowDateTime != 0 || rawLastWriteTime.dwHighDateTime != 0) && (rawLastWriteTime.dwLowDateTime != -1 || rawLastWriteTime.dwHighDateTime != -1)
        ? DateTime.FromFileTime(((long)rawLastWriteTime.dwHighDateTime << 32) | (uint)rawLastWriteTime.dwLowDateTime)
        : (DateTime?)null;

      try
      {
        NtStatus result = _operations.SetFileTime(rawFileName, ctime, atime, mtime, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////
    internal NtStatus DeleteFileProxy(string rawFileName, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.DeleteFile(rawFileName, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus DeleteDirectoryProxy(string rawFileName, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.DeleteDirectory(rawFileName, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus MoveFileProxy(string rawFileName, string rawNewFileName, bool rawReplaceIfExisting, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.MoveFile(rawFileName, rawNewFileName, rawReplaceIfExisting, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    //// 

    internal NtStatus LockFileProxy(string rawFileName, long rawByteOffset, long rawLength, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.LockFile(rawFileName, rawByteOffset, rawLength, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus UnlockFileProxy(string rawFileName, long rawByteOffset, long rawLength, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.UnlockFile(rawFileName, rawByteOffset, rawLength, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus GetDiskFreeSpaceProxy(ref long rawFreeBytesAvailable, ref long rawTotalNumberOfBytes, ref long rawTotalNumberOfFreeBytes, DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.GetDiskFreeSpace(out rawFreeBytesAvailable, out rawTotalNumberOfBytes, out rawTotalNumberOfFreeBytes, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus GetVolumeInformationProxy(StringBuilder rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber, ref uint rawMaximumComponentLength, ref FileSystemFeatures rawFileSystemFlags, StringBuilder rawFileSystemNameBuffer, uint rawFileSystemNameSize, DokanFileInfo rawFileInfo)
    {
      rawMaximumComponentLength = 256;
      rawVolumeSerialNumber = _serialNumber;
      string label;
      string name;
      try
      {
        NtStatus result = _operations.GetVolumeInformation(out label, out rawFileSystemFlags, out name, rawFileInfo);

        if (result == DokanResult.Success)
        {
          rawVolumeNameBuffer.Append(label);
          rawFileSystemNameBuffer.Append(name);
        }
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus MountedProxy(DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.Mounted(rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus UnmountedProxy(DokanFileInfo rawFileInfo)
    {
      try
      {
        NtStatus result = _operations.Unmounted(rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus GetFileSecurityProxy(string rawFileName, ref SECURITY_INFORMATION rawRequestedInformation, IntPtr rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo rawFileInfo)
    {
      FileSystemSecurity sec;

      var sect = AccessControlSections.None;
      if (rawRequestedInformation.HasFlag(SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Owner;
      }
      if (rawRequestedInformation.HasFlag(SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Group;
      }
      if (rawRequestedInformation.HasFlag(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) ||
          rawRequestedInformation.HasFlag(SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) ||
          rawRequestedInformation.HasFlag(SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Access;
      }
      if (rawRequestedInformation.HasFlag(SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) ||
          rawRequestedInformation.HasFlag(SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) ||
          rawRequestedInformation.HasFlag(SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Audit;
      }
      try
      {
        NtStatus result = _operations.GetFileSecurity(rawFileName, out sec, sect, rawFileInfo);
        if (result == DokanResult.Success && sec != null)
        {
          var buffer = sec.GetSecurityDescriptorBinaryForm();
          rawSecurityDescriptorLengthNeeded = (uint)buffer.Length;
          if (buffer.Length > rawSecurityDescriptorLength)
            return DokanResult.BufferOverflow;

          Marshal.Copy(buffer, 0, rawSecurityDescriptor, buffer.Length);
        }
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }

    ////

    internal NtStatus SetFileSecurityProxy(string rawFileName, ref SECURITY_INFORMATION rawSecurityInformation, IntPtr rawSecurityDescriptor, uint rawSecurityDescriptorLength, DokanFileInfo rawFileInfo)
    {
      var sect = AccessControlSections.None;
      if (rawSecurityInformation.HasFlag(SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Owner;
      }
      if (rawSecurityInformation.HasFlag(SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Group;
      }
      if (rawSecurityInformation.HasFlag(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) ||
          rawSecurityInformation.HasFlag(SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) ||
          rawSecurityInformation.HasFlag(SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Access;
      }
      if (rawSecurityInformation.HasFlag(SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) ||
          rawSecurityInformation.HasFlag(SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) ||
          rawSecurityInformation.HasFlag(SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION))
      {
        sect |= AccessControlSections.Audit;
      }
      var buffer = new byte[rawSecurityDescriptorLength];
      try
      {
        Marshal.Copy(rawSecurityDescriptor, buffer, 0, (int)rawSecurityDescriptorLength);
        var sec = rawFileInfo.IsDirectory ? (FileSystemSecurity)new DirectorySecurity() : new FileSecurity();
        sec.SetSecurityDescriptorBinaryForm(buffer);

        NtStatus result = _operations.SetFileSecurity(rawFileName, sec, sect, rawFileInfo);
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan exception: ", ex);
        return DokanResult.InvalidParameter;
      }
    }
  }
}
