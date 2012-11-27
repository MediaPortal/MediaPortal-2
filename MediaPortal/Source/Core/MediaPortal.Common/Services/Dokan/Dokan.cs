#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Dokan;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;

namespace MediaPortal.Common.Services.Dokan
{
  public class Dokan : DokanOperations, IDisposable
  {
    #region Consts

    /// <summary>
    /// Volume label for the virtual drive if our mount point is a drive letter.
    /// </summary>
    public static string VOLUME_LABEL = "MediaPortal 2 resource access";

    /// <summary>
    /// Drive format that is returned by <see cref="DriveInfo.DriveFormat"/> for Dokan drives.
    /// </summary>
    public static string DOKAN_FORMAT = "DOKAN";

    /// <summary>
    /// Timeout for the drive check before we assume the drive is an orphaned DOKAN drive.
    /// </summary>
    protected const int DRIVE_TIMEOUT_MS = 5000;

    protected const FileAttributes FILE_ATTRIBUTES = FileAttributes.ReadOnly;
    protected const FileAttributes DIRECTORY_ATTRIBUTES = FileAttributes.ReadOnly | FileAttributes.NotContentIndexed | FileAttributes.Directory;

    protected static readonly DateTime MIN_FILE_DATE = DateTime.FromFileTime(0); // If we use lower values, resources are not recognized by the Explorer/Windows API

    #endregion

    protected object _syncObj = new object();
    protected char _driveLetter;
    protected Thread _mountThread;
    protected VirtualRootDirectory _root = new VirtualRootDirectory("/");

    public static HashSet<char> _dokanDriveLetters = new HashSet<char>();

    protected Dokan(char driveLetter)
    {
      _driveLetter = driveLetter;
      _mountThread = new Thread(Run) { Name = "Dokan" };
      _mountThread.Start();
    }

    ~Dokan()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_root == null)
        return;
      try
      {
        if (DokanNet.DokanUnmount(_driveLetter) == 0)
          ServiceRegistration.Get<ILogger>().Error("Dokan: Failed to unmount drive '{0}'", _driveLetter);
        else
          ServiceRegistration.Get<ILogger>().Info("Dokan: Successfully unmounted drive '{0}'", _driveLetter);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Dokan: Error unmounting drive '{0}'", e, _driveLetter);
      }
      lock (_syncObj)
        _root.Dispose();
      _root = null;
    }

    // Could be helpful to move the DokanOperations implementation to a separate class to make it possible to
    // write a sensible destructor in this class which removes the Dokan drive

    protected static bool DriveInUse(char driveLetter)
    {
      return Directory.Exists(driveLetter + ":\\");
    }

    protected static bool Prepare(char driveLetter)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      // First check if the configured driveLetter refers to a Dokan drive, then do an unmount to remove a possibly 
      // lost Dokan mount from a formerly crashed MediaPortal
      if (IsDokanDrive(driveLetter))
        if (DokanNet.DokanUnmount(driveLetter) == 1)
          logger.Info("Dokan: Successfully unmounted remote resource drive '{0}' from former unclean shutdown", driveLetter);
        else
          logger.Warn("Dokan: Could not unmount orphaned DOKAN drive '{0}' from former unclean shutdown", driveLetter);

      if (DriveInUse(driveLetter))
      {
        logger.Warn("Dokan: Drive letter '{0}' is already in use", driveLetter);
        return false;
      }
      return true;
    }

    protected void Run()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      try
      {
        DokanOptions opt = new DokanOptions
          {
              DriveLetter = _driveLetter,
              VolumeLabel = VOLUME_LABEL,
              //UseKeepAlive = true,
              //DebugMode = true,
              //ThreadCount = 5,
              //UseAltStream = true,
              //UseStdErr = true
          };

        // DokanMain will return when a "DokanUnmount" call is done from ResMount thread (or in case of errors?)
        int result = DokanNet.DokanMain(opt, this);
        if (result == DokanNet.DOKAN_SUCCESS)
          logger.Debug("Dokan: DokanMain returned successfully");
        else
          logger.Warn("Dokan: DokanMain returned with error code {0} - remote resources may not be available in this session", result);
      }
      catch (Exception e)
      {
        logger.Error("Dokan: Error mounting virtual filesystem at drive '{0}' (is DOKAN not installed?)", e, _driveLetter);
      }
    }

    protected VirtualFileSystemResource ParseFileName(string fileName)
    {
      if (fileName == "\\")
        return _root;
      if (!fileName.StartsWith("\\"))
        return null;
      string[] pathSegments = fileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
      VirtualFileSystemResource resource = _root;
      lock (_syncObj)
        foreach (string pathSegment in pathSegments)
        {
          VirtualBaseDirectory directory = resource as VirtualBaseDirectory;
          if (directory == null || !directory.ChildResources.TryGetValue(pathSegment, out resource))
            return null;
        }
      return resource;
    }

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public char DriveLetter
    {
       get { return _driveLetter; }
    }

    public VirtualRootDirectory RootDirectory
    {
      get { return _root; }
    }

    public static Dokan Install(char driveLetter)
    {
      return Prepare(driveLetter) ? new Dokan(driveLetter) : null;
    }

    /// <summary>
    /// Checks if a mounted drive exists and the filesystem type is <see cref="DOKAN_FORMAT"/>.
    /// </summary>
    /// <param name="driveLetter">Letter of drive to check.</param>
    /// <returns><c>true</c>, if the drive with the given <paramref name="driveLetter"/> is a mounted Dokan drive, else <c>false</c>.</returns>
    public static bool IsDokanDrive(char driveLetter)
    {
      bool result = false;
      // Check if this drive was queried before
      if (_dokanDriveLetters.Contains(driveLetter))
        return true;

      try
      {
        ThreadingUtils.CallWithTimeout(() =>
          {
            // By checking the given drive's format, we'll find ALL Dokan drives, also those which are added by other MP2 instances (Client/Server), which is good, but we even
            // find those which are not added to the system by MediaPortal at all, which is not so good.
            // A better way would be to modify the drive format the DOKAN library uses for its drives. That way, we could use an own drive format type for MP2.
            // But unfortunately, the drive format cannot be configured in the DOKAN library.
            // It would be also possible to check the drive's volume label, but I think the check for the drive format is more elegant.
            DriveInfo driveInfo = new DriveInfo(driveLetter+":");
            // Check the IsReady property to avoid DriveNotFoundException
            result = driveInfo.IsReady && driveInfo.DriveFormat == DOKAN_FORMAT;
          }, DRIVE_TIMEOUT_MS);
      }
      catch (TimeoutException)
      {
        result = true;
      }
      // Cache information only for DOKAN drives, all other (also non-existing) needs to be checked again (i.e. for removable media)
      if (result)
        _dokanDriveLetters.Add(driveLetter);
      return result;
    }

    public VirtualRootDirectory GetRootDirectory(string rootDirectoryName)
    {
      VirtualFileSystemResource directoryResource;
      if (!_root.ChildResources.TryGetValue(rootDirectoryName, out directoryResource))
        return null;
      return (VirtualRootDirectory) directoryResource;
    }

    #region DokanOperations implementation

    public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        VirtualFileSystemResource resource = ParseFileName(filename);
        if (resource == null)
          return -DokanNet.ERROR_FILE_NOT_FOUND;
        FileHandle handle = new FileHandle(resource);
        info.Context = handle;
        resource.AddFileHandle(handle);
        if (resource is VirtualBaseDirectory)
          info.IsDirectory = true; // Necessary for the Dokan driver to set this, see docs
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int OpenDirectory(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        VirtualFileSystemResource resource = ParseFileName(filename);
        if (resource == null || !(resource is VirtualBaseDirectory))
          return -DokanNet.ERROR_PATH_NOT_FOUND;
        FileHandle handle = new FileHandle(resource);
        info.Context = handle;
        resource.AddFileHandle(handle);
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int CreateDirectory(string filename, DokanFileInfo info)
    {
      return DokanNet.ERROR_ACCESS_DENIED;
    }

    public int Cleanup(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        if (handle != null)
          handle.Cleanup();
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int CloseFile(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        if (handle != null)
          handle.Resource.RemoveFileHandle(handle);
        info.Context = null;
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
    {
      FileHandle handle = (FileHandle) info.Context;
      Stream stream;
      lock (_syncObj)
      {
        if (handle == null)
          return -DokanNet.ERROR_FILE_NOT_FOUND;
        try
        {
          stream = handle.GetOrOpenStream();
          if (stream == null)
            return -DokanNet.ERROR_FILE_NOT_FOUND;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Dokan: Error creating file stream of resource '{0}'", e, handle.Resource);
          return DokanNet.DOKAN_ERROR;
        }
      }
      // Do the reading outside the lock - might block when not enough bytes are available
      try
      {
        stream.Seek(offset, SeekOrigin.Begin);
        readBytes = (uint) stream.Read(buffer, 0, buffer.Length);
        return DokanNet.DOKAN_SUCCESS;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan: Error reading from stream of resource '{0}'", e, handle.Resource);
        return DokanNet.DOKAN_ERROR;
      }
    }

    public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int FlushFileBuffers(string filename, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        if (handle == null)
          return -DokanNet.ERROR_FILE_NOT_FOUND;
        VirtualFileSystemResource resource = handle.Resource;
        VirtualFile file = resource as VirtualFile;
        IFileSystemResourceAccessor resourceAccessor = resource.ResourceAccessor;
        VirtualBaseDirectory directory = resource as VirtualBaseDirectory;
        fileinfo.FileName = filename;
        fileinfo.CreationTime = resource.CreationTime;
        fileinfo.LastAccessTime = CorrectTimeValue(resourceAccessor == null ? resource.CreationTime : resourceAccessor.LastChanged);
        fileinfo.LastWriteTime = CorrectTimeValue(resource.CreationTime);
        if (file != null)
        {
          fileinfo.Attributes = FILE_ATTRIBUTES;
          fileinfo.Length = resourceAccessor == null ? 0 : resourceAccessor.Size;
          return DokanNet.DOKAN_SUCCESS;
        }
        if (directory != null)
        {
          fileinfo.Attributes = DIRECTORY_ATTRIBUTES;
          fileinfo.Length = 0;
          return DokanNet.DOKAN_SUCCESS;
        }
        return -DokanNet.ERROR_FILE_NOT_FOUND;
      }
    }

    public int FindFiles(string filename, ArrayList files, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        VirtualBaseDirectory directory = handle == null ? null : handle.Resource as VirtualBaseDirectory;
        if (directory == null)
          return -DokanNet.ERROR_PATH_NOT_FOUND;
        foreach (KeyValuePair<string, VirtualFileSystemResource> entry in directory.ChildResources)
        {
          VirtualFileSystemResource resource = entry.Value;
          IFileSystemResourceAccessor resourceAccessor = resource.ResourceAccessor;
          bool isFile = resource is VirtualFile;
          FileInformation fi = new FileInformation
            {
                Attributes = isFile ? FILE_ATTRIBUTES : DIRECTORY_ATTRIBUTES,
                CreationTime = resource.CreationTime,
                LastAccessTime = CorrectTimeValue(resourceAccessor == null ? resource.CreationTime : resourceAccessor.LastChanged),
                LastWriteTime = CorrectTimeValue(directory.CreationTime),
                Length = resourceAccessor == null || !isFile ? 0 : resourceAccessor.Size,
                FileName = entry.Key
            };
          files.Add(fi);
        }
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    protected DateTime CorrectTimeValue(DateTime time)
    {
      // When using DateTime.MinValue, resources are not recognized
      return time < MIN_FILE_DATE ? MIN_FILE_DATE : time;
    }

    public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int DeleteFile(string filename, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int DeleteDirectory(string filename, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int SetEndOfFile(string filename, long length, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int SetAllocationSize(string filename, long length, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int LockFile(string filename, long offset, long length, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    // Function is called on system shutdown
    public int Unmount(DokanFileInfo info)
    {
      CloseFile(string.Empty, info);
      return DokanNet.DOKAN_SUCCESS;
    }

    #endregion
  }
}