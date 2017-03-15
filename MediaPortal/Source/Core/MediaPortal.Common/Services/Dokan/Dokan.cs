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
using System.Threading;
using Dokan;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;
using System.Security.AccessControl;
using MediaPortal.Common.Services.Dokan.Native;

namespace MediaPortal.Common.Services.Dokan
{
  public class Dokan : IDokanOperations, IDisposable
  {
    #region Consts

    /// <summary>
    /// Dokany library version
    /// </summary>
    private const ushort DOKAN_VERSION = 100; // ver 1.0.0

    /// <summary>
    /// Dokany library thread count
    /// </summary>
    private const ushort THREAD_COUNT = 5;

    /// <summary>
    /// Volume label for the virtual drive if our mount point is a drive letter.
    /// </summary>
    private static string VOLUME_LABEL = "MP2_RESOURCE";

    /// <summary>
    /// Drive format that is returned by <see cref="DriveInfo.DriveFormat"/> for Dokan drives.
    /// </summary>
    private static string DOKAN_FORMAT = "DOKAN";

    private const int DOKAN_SUCCESS = 0;
    private const int DOKAN_ERROR = -1;                 /* General Error */
    private const int DOKAN_DRIVE_LETTER_ERROR = -2;    /* Bad Drive letter */
    private const int DOKAN_DRIVER_INSTALL_ERROR = -3;  /* Can't install driver */
    private const int DOKAN_START_ERROR = -4;           /* Driver something wrong */
    private const int DOKAN_MOUNT_ERROR = -5;           /* Can't assign a drive letter or mount point */
    private const int DOKAN_MOUNT_POINT_ERROR = -6;     /* Mount point is invalid */
    private const int DOKAN_VERSION_ERROR = -7;         /* Requested an incompatible version */

    private const FileAttributes FILE_ATTRIBUTES = FileAttributes.ReadOnly;
    private const FileAttributes DIRECTORY_ATTRIBUTES = FileAttributes.ReadOnly | FileAttributes.NotContentIndexed | FileAttributes.Directory;

    private static readonly DateTime MIN_FILE_DATE = DateTime.FromFileTime(0); // If we use lower values, resources are not recognized by the Explorer/Windows API

    #endregion Consts

    private object _syncObj = new object();
    private string _mountPoint;
    private Thread _mountThread;
    private VirtualRootDirectory _root = new VirtualRootDirectory("/");

    protected Dokan(string mountPoint)
    {
      _mountPoint = mountPoint;
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
      {
        return;
      }
      try
      {
        if (DokanRemoveMountPoint(_mountPoint) == DOKAN_SUCCESS)
        {
         
          ServiceRegistration.Get<ILogger>().Info("Dokan: Successfully unmounted resource '{0}'", _mountPoint);
        }
        else
        {
          ServiceRegistration.Get<ILogger>().Error("Dokan: Failed to unmount resource '{0}'", _mountPoint);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Dokan: Error unmounting resource '{0}'", e, _mountPoint);
      }
      lock (_syncObj)
        _root.Dispose();
      _root = null;
    }

    // Could be helpful to move the DokanOperations implementation to a separate class to make it possible to
    // write a sensible destructor in this class which removes the Dokan drive

    protected static bool Prepare(string mountPoint)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      // First check if the configured driveLetter refers to a Dokan drive, then do an unmount to remove a possibly
      // lost Dokan mount from a formerly crashed MediaPortal
      //if (IsDokanDrive(driveLetter))
      //{
        if (DokanRemoveMountPoint(mountPoint) == DOKAN_SUCCESS)
        {
          logger.Info("Dokan: Successfully unmounted remote resource '{0}' from former unclean shutdown", mountPoint);
        }
        else
        {
          logger.Warn("Dokan: Could not unmount resource '{0}' from former unclean shutdown", mountPoint);
        }
    //  }
      return true;
    }

    protected void Run()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      try
      {
        var dokanOperationProxy = new Proxy(this);
        var dokanOptions = new DOKAN_OPTIONS
        {
          Version = DOKAN_VERSION,
          MountPoint = _mountPoint,
          ThreadCount = THREAD_COUNT,
          Options = (uint)DokanOptions.FixedDrive,
          Timeout = 0
        };

        DOKAN_OPERATIONS dokanOperations = new DOKAN_OPERATIONS();
        dokanOperations.ZwCreateFile = dokanOperationProxy.ZwCreateFileProxy;
        dokanOperations.Cleanup = dokanOperationProxy.CleanupProxy;
        dokanOperations.CloseFile = dokanOperationProxy.CloseFileProxy;
        dokanOperations.ReadFile = dokanOperationProxy.ReadFileProxy;
        dokanOperations.WriteFile = dokanOperationProxy.WriteFileProxy;
        dokanOperations.FlushFileBuffers = dokanOperationProxy.FlushFileBuffersProxy;
        dokanOperations.GetFileInformation = dokanOperationProxy.GetFileInformationProxy;
        dokanOperations.FindFiles = dokanOperationProxy.FindFilesProxy;
        dokanOperations.SetFileAttributes = dokanOperationProxy.SetFileAttributesProxy;
        dokanOperations.SetFileTime = dokanOperationProxy.SetFileTimeProxy;
        dokanOperations.DeleteFile = dokanOperationProxy.DeleteFileProxy;
        dokanOperations.DeleteDirectory = dokanOperationProxy.DeleteDirectoryProxy;
        dokanOperations.MoveFile = dokanOperationProxy.MoveFileProxy;
        dokanOperations.SetEndOfFile = dokanOperationProxy.SetEndOfFileProxy;
        dokanOperations.SetAllocationSize = dokanOperationProxy.SetAllocationSizeProxy;
        dokanOperations.LockFile = dokanOperationProxy.LockFileProxy;
        dokanOperations.UnlockFile = dokanOperationProxy.UnlockFileProxy;
        dokanOperations.GetDiskFreeSpace = dokanOperationProxy.GetDiskFreeSpaceProxy;
        dokanOperations.GetVolumeInformation = dokanOperationProxy.GetVolumeInformationProxy;
        dokanOperations.Mounted = dokanOperationProxy.MountedProxy;
        dokanOperations.Unmounted = dokanOperationProxy.UnmountedProxy;
        dokanOperations.GetFileSecurity = dokanOperationProxy.GetFileSecurityProxy;
        dokanOperations.SetFileSecurity = dokanOperationProxy.SetFileSecurityProxy;
        dokanOperations.FindStreams = dokanOperationProxy.FindStreamsProxy;

        // DokanMain will return when a "DokanUnmount" call is done from ResMount thread (or in case of errors?)
        int status = DokanNativeMethods.DokanMain(ref dokanOptions, ref dokanOperations);

        switch (status)
        {
          case DOKAN_SUCCESS:
            logger.Info("Dokan: DokanMain returned successfully!");
            break;
          case DOKAN_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - General Error. Remote resources may not be available in this session", status);
            break;
          case DOKAN_DRIVE_LETTER_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - Bad Drive letter. Remote resources may not be available in this session", status);
            break;
          case DOKAN_DRIVER_INSTALL_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - Can not install driver. Remote resources may not be available in this session", status);
            break;
          case DOKAN_MOUNT_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - Can not assign mount point. Remote resources may not be available in this session", status);
            break;
          case DOKAN_START_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - Driver something wrong. Remote resources may not be available in this session", status);
            break;
          case DOKAN_MOUNT_POINT_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - Mount point is invalid. Remote resources may not be available in this session", status);
            break;
          case DOKAN_VERSION_ERROR:
            logger.Warn("Dokan: DokanMain returned with error code {0} - Requested an incompatible version. Remote resources may not be available in this session", status);
            break;
        } 
      }
      catch (Exception e)
      {
        logger.Error("Dokan: Error mounting virtual filesystem at '{0}' (is DOKAN not installed?)", e, _mountPoint);
      }
    }

    private static int DokanRemoveMountPoint(string mountPoint)
    {
      return DokanNativeMethods.DokanRemoveMountPoint(mountPoint);
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

    public string MountPoint
    {
      get { return _mountPoint; }
    }

    public VirtualRootDirectory RootDirectory
    {
      get { return _root; }
    }

    public static Dokan Install(string mountPoint)
    {
      return new Dokan(mountPoint); //: null;
    }

    public VirtualRootDirectory GetRootDirectory(string rootDirectoryName)
    {
      VirtualFileSystemResource directoryResource;
      if (!_root.ChildResources.TryGetValue(rootDirectoryName, out directoryResource))
        return null;
      return (VirtualRootDirectory) directoryResource;
    }

    #region DokanOperations implementation

    public NtStatus CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        VirtualFileSystemResource resource = ParseFileName(filename);
        if (resource == null)
        {
          return DokanResult.Error;
        }
        FileHandle handle = new FileHandle(resource);
        info.Context = handle;
        resource.AddFileHandle(handle);
        if (resource is VirtualBaseDirectory)
        {
          info.IsDirectory = true;
        }
        return DokanResult.Success;
      }
    }

    public void Cleanup(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = info.Context as FileHandle;
        if (handle != null)
        {
          handle.Cleanup();
        }
      }
    }

    public void CloseFile(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = info.Context as FileHandle;
        if (handle != null)
        {
          handle.Resource.RemoveFileHandle(handle);
        }
        info.Context = null;
      }
    }

    public NtStatus ReadFile(string filename, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
    {
      FileHandle handle = info.Context as FileHandle;
      Stream stream;
      bytesRead = 0;
      lock (_syncObj)
      {
        if (handle == null)
        {
          return DokanResult.FileNotFound;
        }
        try
        {
          stream = handle.GetOrOpenStream();
          if (stream == null)
          {
            return DokanResult.FileNotFound;
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Dokan: Error creating file stream of resource '{0}'", e, handle.Resource);
          return DokanResult.Error;
        }
      }
      // Do the reading outside the lock - might block when not enough bytes are available
      try
      {
        stream.Seek(offset, SeekOrigin.Begin);
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        return DokanResult.Success;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan: Error reading from stream of resource '{0}'", ex, handle.Resource);
        return DokanResult.Error;
      }
    }

    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
    {
      bytesWritten = 0;
      return DokanResult.AccessDenied;
    }

    public NtStatus FlushFileBuffers(string filename, DokanFileInfo info)
    {
      return DokanResult.Success;
    }

    public NtStatus GetFileInformation(string filename, out FileInformation fileinfo, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        fileinfo = new FileInformation();
        FileHandle handle = info.Context as FileHandle;
        if (handle == null)
        {
          return DokanResult.FileNotFound;
        }

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
          return DokanResult.Success;
        }
        if (directory != null)
        {
          fileinfo.Attributes = DIRECTORY_ATTRIBUTES;
          fileinfo.Length = 0;
          return DokanResult.Success;
        }
        return DokanResult.FileNotFound;
      }
    }

    public NtStatus FindFiles(string filename, out IList<FileInformation> files, DokanFileInfo info)
    {
      files = new List<FileInformation>();
      lock (_syncObj)
      {
        FileHandle handle = info.Context as FileHandle;
        VirtualBaseDirectory directory = handle == null ? null : handle.Resource as VirtualBaseDirectory;
        if (directory == null)
        {
          files = null;
          return DokanResult.FileNotFound;
        }
          
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
        return DokanResult.Success;
      }
    }

    protected DateTime CorrectTimeValue(DateTime time)
    {
      // When using DateTime.MinValue, resources are not recognized
      return time < MIN_FILE_DATE ? MIN_FILE_DATE : time;
    }

    public NtStatus SetFileAttributes(string filename, FileAttributes attributes, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus SetFileTime(string filename, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus DeleteFile(string filename, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus DeleteDirectory(string filename, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus SetEndOfFile(string filename, long length, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus SetAllocationSize(string filename, long length, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus LockFile(string filename, long offset, long length, DokanFileInfo info)
    {
      return DokanResult.Success;
    }

    public NtStatus UnlockFile(string filename, long offset, long length, DokanFileInfo info)
    {
      return DokanResult.Success;
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalBytes, out long totalFreeBytes, DokanFileInfo info)
    {
      freeBytesAvailable = 0;
      totalBytes = 0;
      totalFreeBytes = 0;
      return DokanResult.Success;
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info)
    {
      volumeLabel = VOLUME_LABEL;
      fileSystemName = DOKAN_FORMAT;
      features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage |
                       FileSystemFeatures.UnicodeOnDisk;
      return DokanResult.Success;
    }

    public NtStatus GetFileSecurity(string filename, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
    {
      security = null;
      return DokanResult.Success;
    }

    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
    {
      return DokanResult.AccessDenied;
    }

    public NtStatus Mounted(DokanFileInfo info)
    {
      return DokanResult.Success;
    }

    // Function is called on system shutdown
    public NtStatus Unmounted(DokanFileInfo info)
    {
      CloseFile(string.Empty, info);
      return DokanResult.Success;
    }

    public NtStatus FindStreams(string fileName, IntPtr enumContext, out string streamName, out long streamSize, DokanFileInfo info)

    {
      streamName = String.Empty;
      streamSize = 0;
      return DokanResult.NotImplemented;
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
    {
      streams = new FileInformation[0];
      return DokanResult.NotImplemented;
    }

    public NtStatus FindFilesWithPattern(string fileName, string searchpattern, out IList<FileInformation> files, DokanFileInfo info)
    {
      files = new FileInformation[0];
      return DokanResult.NotImplemented;
    }

    #endregion
  }
}
