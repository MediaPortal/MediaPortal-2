using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dokan;

namespace MediaPortal.Common.Services.Dokan.Native
{
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct DOKAN_OPERATIONS
  {
    public Proxy.ZwCreateFileDelegate ZwCreateFile;
    //public Proxy.OpenDirectoryDelegate OpenDirectory;
    //public Proxy.CreateDirectoryDelegate CreateDirectory;
    //public Proxy.CleanupDelegate Cleanup;
    //public Proxy.CloseFileDelegate CloseFile;
    //public Proxy.ReadFileDelegate ReadFile;
    //public Proxy.WriteFileDelegate WriteFile;
    //public Proxy.FlushFileBuffersDelegate FlushFileBuffers;
    //public Proxy.GetFileInformationDelegate GetFileInformation;
    //public Proxy.FindFilesDelegate FindFiles;
    //public IntPtr FindFilesWithPattern; // this is not used in DokanNet
    //public Proxy.SetFileAttributesDelegate SetFileAttributes;
    //public Proxy.SetFileTimeDelegate SetFileTime;
    //public Proxy.DeleteFileDelegate DeleteFile;
    //public Proxy.DeleteDirectoryDelegate DeleteDirectory;
    //public Proxy.MoveFileDelegate MoveFile;
    //public Proxy.SetEndOfFileDelegate SetEndOfFile;
    //public Proxy.SetAllocationSizeDelegate SetAllocationSize;
    //public Proxy.LockFileDelegate LockFile;
    //public Proxy.UnlockFileDelegate UnlockFile;
    //public Proxy.GetDiskFreeSpaceDelegate GetDiskFreeSpace;
    //public Proxy.GetVolumeInformationDelegate GetVolumeInformation;
    //public Proxy.UnmountDelegate Unmount;
    //public Proxy.MountedDelegate Mounted;
    //public Proxy.UnmountedDelegate Unmounted;
    //public Proxy.GetFileSecurityDelegate GetFileSecurity;
    //public Proxy.SetFileSecurityDelegate SetFileSecurity;
  }
}
