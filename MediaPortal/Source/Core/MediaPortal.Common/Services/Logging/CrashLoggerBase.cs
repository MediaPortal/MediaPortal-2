#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaPortal.Common.Services.Logging
{
  /// <summary>
  /// Base class for crash loggers.
  /// </summary>
  public class CrashLoggerBase
  {
    protected string _filename;
    protected DateTime _crashTime;

    public CrashLoggerBase(string logFilesPath)
    {
      _crashTime = DateTime.Now;

      string crashLogPath = Path.Combine(logFilesPath, "Crash_" + _crashTime.ToString("dd.MM.yyyy_HHmm"));
      if (!Directory.Exists(crashLogPath))
        Directory.CreateDirectory(crashLogPath);

      CopyLogFiles(logFilesPath, crashLogPath);

      _filename = Path.Combine(crashLogPath, "Crash.log");
    }

    protected string ExceptionInfo(Exception ex)
    {
      StringBuilder exceptionInfo = new StringBuilder();
      exceptionInfo.AppendLine("== Exception: " + ex);
      exceptionInfo.AppendLine("  Message: " + ex.Message);
      exceptionInfo.AppendLine("  Site   : " + ex.TargetSite);
      exceptionInfo.AppendLine("  Source : " + ex.Source);
      if (ex.InnerException != null)
      {
        exceptionInfo.AppendLine("== Inner Exception(s):");
        exceptionInfo.AppendLine(WriteInnerException(ex.InnerException));
      }
      exceptionInfo.AppendLine("== Stack Trace:");
      exceptionInfo.AppendLine(ex.StackTrace);

      return exceptionInfo.ToString();
    }

    /// <summary>
    /// Writes any existing inner exceptions to the file.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    protected static string WriteInnerException(Exception exception)
    {
      StringBuilder exceptionInfo = new StringBuilder();
      if (exception != null)
      {
        exceptionInfo.AppendLine(exception.Message);
        if (exception.InnerException != null)
        {
          exceptionInfo.AppendLine(WriteInnerException(exception));
        }
      }
      return exceptionInfo.ToString();
    }

    protected static string SystemInfo()
    {
      StringBuilder systemInfo = new StringBuilder();
      systemInfo.AppendLine("== Software");
      systemInfo.AppendLine("  OS Version:\t\t" + Environment.OSVersion);
      systemInfo.AppendLine("  Hostname:\t\t" + Environment.MachineName);
      // SystemInformation.Network came from System.Windows.Forms, which is Windows-only. This is
      // a diagnostic line in a crash report; NetworkInterface.GetIsNetworkAvailable() answers the
      // same question ("is any non-loopback interface up?") on every platform.
      systemInfo.AppendLine("  Network attached?\t" + NetworkInterface.GetIsNetworkAvailable());

      systemInfo.AppendLine();
      systemInfo.AppendLine("== Hardware");
      systemInfo.AppendLine("  CPU details:\t" + GetCPUInfos());

      return systemInfo.ToString();
    }

    /// <summary>
    /// One line per mounted volume: name, type, file system, total size and free space.
    /// </summary>
    /// <remarks>
    /// This enumerated the WMI class <c>Win32_LogicalDisk</c>, which is the only reason
    /// <c>MediaPortal.Common</c> referenced <c>System.Management</c>. <see cref="System.IO.DriveInfo"/>
    /// answers the same four questions on every platform (<see cref="System.IO.DriveInfo.Name"/>,
    /// <c>DriveType</c>, <c>DriveFormat</c>, <c>TotalSize</c>/<c>TotalFreeSpace</c>), so the package left
    /// the portable core together with the code that needed it.
    /// </remarks>
    protected static string DriveInfo()
    {
      StringBuilder driveInfo = new StringBuilder();

      foreach (System.IO.DriveInfo disk in System.IO.DriveInfo.GetDrives())
      {
        string format = "-";
        string size = "-";
        string free = "-";
        try
        {
          // Unlike WMI, which handed back null properties, reading an unready or vanishing volume
          // throws here. A crash report that fails to be written because one card reader was pulled
          // is the worse outcome, so an unreadable volume is reported as such and the loop continues.
          if (disk.IsReady)
          {
            format = disk.DriveFormat;
            size = GetSizeString(disk.TotalSize);
            free = GetSizeString(disk.TotalFreeSpace);
          }
        }
        catch (Exception ex)
        {
          size = "unavailable (" + ex.GetType().Name + ")";
        }

        driveInfo.AppendLine(String.Format("  Volume: {0} - {1}({2})\tSize: {3} Free: {4}", disk.Name,
            DescribeDriveType(disk.DriveType), format, size, free));
      }

      return driveInfo.ToString();
    }

    /// <summary>
    /// Keeps the wording the WMI enumeration used ("Local Disk" rather than "Fixed"), so crash logs
    /// can still be compared against the reports MP2 has produced for years.
    /// </summary>
    private static string DescribeDriveType(System.IO.DriveType driveType)
    {
      switch (driveType)
      {
        case System.IO.DriveType.Unknown: return "Unknown";
        case System.IO.DriveType.NoRootDirectory: return "No Root Directory";
        case System.IO.DriveType.Removable: return "Removable Disk";
        case System.IO.DriveType.Fixed: return "Local Disk";
        case System.IO.DriveType.Network: return "Network Drive";
        case System.IO.DriveType.Ram: return "RAM Disk";
        case System.IO.DriveType.CDRom: return "CD Drive";
        default: return driveType.ToString();
      }
    }

    protected static string GetSizeString(long bytes)
    {
      double size = bytes;
      string end = "B";
      if (size > 1024)
      {
        end = "KB";
        size /= 1024;
      }

      if (size > 1024)
      {
        end = "MB";
        size /= 1024;
      }

      if (size > 1024)
      {
        end = "GB";
        size /= 1024;
      }

      return size.ToString("F02") + end;
    }

    /// <summary>
    /// CPU model, clock and logical CPU count for the crash report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows keeps these in <c>HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor</c>
    /// (<c>ProcessorNameString</c> and <c>~MHz</c>), and the old code read that key once per logical
    /// CPU and concatenated the results - so an eight-core machine got the same sentence eight times
    /// with nothing between them. <c>/proc/cpuinfo</c> carries the same two values on Linux and
    /// Android. There is no portable BCL call for a CPU brand string, so where neither source exists
    /// (macOS, iOS) the line reports what is known - architecture and core count - rather than
    /// spawning <c>sysctl</c> from a process that is already dying.
    /// </para>
    /// <para>
    /// This must not throw; every source is read defensively.
    /// </para>
    /// </remarks>
    protected static string GetCPUInfos()
    {
      string brand = null;
      string clock = null;
      try
      {
        if (OperatingSystem.IsWindows())
        {
          using (Microsoft.Win32.RegistryKey mainKey =
                     Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor"))
          {
            string[] subKeys = mainKey == null ? null : mainKey.GetSubKeyNames();
            if (subKeys != null && subKeys.Length > 0)
            {
              using (Microsoft.Win32.RegistryKey key = mainKey.OpenSubKey(subKeys[0]))
              {
                if (key != null)
                {
                  brand = key.GetValue("ProcessorNameString") as string;
                  object mhz = key.GetValue("~MHz");
                  if (mhz != null)
                    clock = mhz.ToString();
                }
              }
            }
          }
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
        {
          const string cpuInfoPath = "/proc/cpuinfo";
          if (File.Exists(cpuInfoPath))
          {
            foreach (string line in File.ReadLines(cpuInfoPath))
            {
              int colon = line.IndexOf(':');
              if (colon <= 0)
              {
                if (brand != null && clock != null)
                  break; // an empty line separates processors; stop once both values are found
                continue;
              }
              string field = line.Substring(0, colon).Trim();
              string value = line.Substring(colon + 1).Trim();
              if (brand == null && field.Equals("model name", StringComparison.OrdinalIgnoreCase))
                brand = value;
              else if (clock == null && field.Equals("cpu MHz", StringComparison.OrdinalIgnoreCase))
                clock = value;
              if (brand != null && clock != null)
                break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        brand = "unreadable (" + ex.GetType().Name + ")";
      }

      string detail = string.IsNullOrEmpty(brand) ? "unknown CPU" : brand.Trim();
      if (!string.IsNullOrEmpty(clock))
        detail += " running at ~" + clock + " MHz";
      return detail + " (" + RuntimeInformation.ProcessArchitecture + ", " + Environment.ProcessorCount +
             " logical CPUs)";
    }

    protected static void CopyLogFiles(string logPath, string crashLogPath)
    {
      foreach (string logFilePath in Directory.GetFiles(logPath, "*.log"))
      {
        string destPath = Path.Combine(crashLogPath, Path.GetFileName(logFilePath));
        File.Delete(destPath);
        File.Copy(logFilePath, destPath);
      }
    }
  }
}
