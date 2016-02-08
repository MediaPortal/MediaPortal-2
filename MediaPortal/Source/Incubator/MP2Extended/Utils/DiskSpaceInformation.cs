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
