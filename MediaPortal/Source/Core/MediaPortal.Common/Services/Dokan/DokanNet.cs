using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MediaPortal.Common.Services.Dokan;
using MediaPortal.Common.Services.Dokan.Native;

namespace Dokan
{
    public class DokanOptions
    {
        public ushort Version;
        public ushort ThreadCount;
        public bool DebugMode;
        public bool UseStdErr;
        public bool UseAltStream;
        public bool NetworkDrive;
        public bool RemovableDrive;
        public string VolumeLabel;
        public string MountPoint;
  }

    public class DokanNet
    {
      private readonly IDokanOperations operations;

    public const long STATUS_OBJECTID_NOT_FOUND = 0xC00002F0;
    public const long STATUS_OBJECT_PATH_NOT_FOUND = 0xC000003A;
    public const long STATUS_ACCESS_DENIED = 0xC0000022;
    public const long STATUS_SHARING_VIOLATION = 0xC0000043;
    public const long STATUS_OBJECT_NAME_INVALID = 0xC0000033;
    public const long STATUS_OBJECT_NAME_COLLISION = 0xC0000035;

    //public const int ERROR_FILE_NOT_FOUND = 2;
    //public const int ERROR_PATH_NOT_FOUND = 3;
    //public const int ERROR_ACCESS_DENIED = 5;
    //public const int ERROR_SHARING_VIOLATION = 32;
    //public const int ERROR_INVALID_NAME = 123;
    //public const int ERROR_FILE_EXISTS = 80;
    //public const int ERROR_ALREADY_EXISTS = 183;

    public const int DOKAN_SUCCESS = 0;
        public const int DOKAN_ERROR = -1; // General Error
        public const int DOKAN_DRIVE_LETTER_ERROR = -2; // Bad Drive letter
        public const int DOKAN_DRIVER_INSTALL_ERROR = -3; // Can't install driver
        public const int DOKAN_START_ERROR = -4; // Driver something wrong
        public const int DOKAN_MOUNT_ERROR = -5; // Can't a5sign drive letter

        public const int DOKAN_VERSION = 100; // ver 1.0.0

        private const uint DOKAN_OPTION_DEBUG = 1;
        private const uint DOKAN_OPTION_STDERR = 2;
        private const uint DOKAN_OPTION_ALT_STREAM = 4;
        private const uint DOKAN_OPTION_NETWORK = 16;
        private const uint DOKAN_OPTION_REMOVABLE = 32;
        
        public static int DokanMain(DOKAN_OPTIONS options, DOKAN_OPERATIONS operations)
        {
            
            //if (options.VolumeLabel == null)
            //{
            //    options.VolumeLabel = "DOKAN";
            //}

            //Proxy proxy = new Proxy(operations);

            //DOKAN_OPTIONS dokanOptions = new DOKAN_OPTIONS();

            //dokanOptions.Version = options.Version;
            //if (dokanOptions.Version == 0)
            //{
            //  dokanOptions.Version = DOKAN_VERSION;
            //}

            //dokanOptions.MountPoint = options.MountPoint;
            //dokanOptions.ThreadCount = options.ThreadCount;
            //dokanOptions.Options |= options.DebugMode ? DOKAN_OPTION_DEBUG : 0;
            //dokanOptions.Options |= options.UseStdErr ? DOKAN_OPTION_STDERR : 0;
            //dokanOptions.Options |= options.UseAltStream ? DOKAN_OPTION_ALT_STREAM : 0;
            //dokanOptions.Options |= options.NetworkDrive ? DOKAN_OPTION_NETWORK : 0;
            //dokanOptions.Options |= options.RemovableDrive ? DOKAN_OPTION_REMOVABLE : 0;

         //   DOKAN_OPERATIONS dokanOperations = new DOKAN_OPERATIONS();
         // dokanOperations.ZwCreateFile = proxy.ZwCreateFileProxy();
           // dokanOperations.OpenDirectory = proxy.OpenDirectoryProxy;
           // dokanOperations.CreateDirectory = proxy.CreateDirectoryProxy;
           // dokanOperations.Cleanup = proxy.CleanupProxy;
           // dokanOperations.CloseFile = proxy.CloseFileProxy;
           // dokanOperations.ReadFile = proxy.ReadFileProxy;
           // dokanOperations.WriteFile = proxy.WriteFileProxy;
           // dokanOperations.FlushFileBuffers = proxy.FlushFileBuffersProxy;
           // dokanOperations.GetFileInformation = proxy.GetFileInformationProxy;
           // dokanOperations.FindFiles = proxy.FindFilesProxy;
           // dokanOperations.SetFileAttributes = proxy.SetFileAttributesProxy;
           // dokanOperations.SetFileTime = proxy.SetFileTimeProxy;
           // dokanOperations.DeleteFile = proxy.DeleteFileProxy;
           // dokanOperations.DeleteDirectory = proxy.DeleteDirectoryProxy;
           // dokanOperations.MoveFile = proxy.MoveFileProxy;
           // dokanOperations.SetEndOfFile = proxy.SetEndOfFileProxy;
           // dokanOperations.SetAllocationSize = proxy.SetAllocationSizeProxy;
           // dokanOperations.LockFile = proxy.LockFileProxy;
           // dokanOperations.UnlockFile = proxy.UnlockFileProxy;
           //// dokanOperations.GetDiskFreeSpace = proxy.GetDiskFreeSpaceProxy;           
           // dokanOperations.GetVolumeInformation = proxy.GetVolumeInformationProxy;        
           // dokanOperations.Unmount = proxy.UnmountProxy;
           // dokanOperations.Mounted = proxy.MountedProxy;
           // dokanOperations.Unmounted = proxy.UnmountedProxy;

            return NativeMethods.DokanMain(ref options, ref operations);
        }


        public static int DokanUnmount(char driveLetter)
        {
            return NativeMethods.DokanUnmount(driveLetter);
        }

        public static int DokanRemoveMountPoint(string mountPoint)
        {
            return NativeMethods.DokanRemoveMountPoint(mountPoint);
        }

        public static uint DokanVersion()
        {
            return NativeMethods.DokanVersion();
        }

        public static uint DokanDriverVersion()
        {
            return NativeMethods.DokanDriveVersion();
        }

        public static bool DokanResetTimeout(uint timeout, DokanFileInfo fileinfo)
        {
            return NativeMethods.DokanResetTimeout(timeout, fileinfo);
        }

        public static IntPtr DokanOpenRequestorToken(DokanFileInfo fileinfo)
        {
          return NativeMethods.DokanOpenRequestorToken(fileinfo);
        }
    }
}
