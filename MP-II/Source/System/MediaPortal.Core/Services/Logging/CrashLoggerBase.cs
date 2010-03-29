#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Management;
using Microsoft.Win32;

namespace MediaPortal.Core.Services.Logging
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
      systemInfo.AppendLine("  Network attached?\t" + SystemInformation.Network);

      systemInfo.AppendLine();
      systemInfo.AppendLine("== Hardware");
      systemInfo.AppendLine("  CPU details:\t" + GetCPUInfos());

      return systemInfo.ToString();
    }

    protected static string DriveInfo()
    {
      StringBuilder driveInfo = new StringBuilder();
      ManagementClass mc = new ManagementClass("Win32_LogicalDisk");
      ManagementObjectCollection moc = mc.GetInstances();

      // more info on extra disk parameters here: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/wmisdk/wmi/win32_logicaldisk.asp?frame=true

      foreach (ManagementObject mo in moc)
      {
        string driveType;
        switch ((UInt32)mo["DriveType"])
        {
          case 1:
            driveType = "No Root Directory";
            break;
          case 2:
            driveType = "Removable Disk";
            break;
          case 3:
            driveType = "Local Disk";
            break;
          case 4:
            driveType = "Network Drive";
            break;
          case 5:
            driveType = "CD Drive";
            break;
          case 6:
            driveType = "RAM Disk";
            break;
          case 0:
          default:
            driveType = "Unknown";
            break;
        }

        string size = GetSizeString(mo["Size"]);
        string free = GetSizeString(mo["FreeSpace"]);

        driveInfo.AppendLine(String.Format("  Volume: {0} - {1}({2})\tSize: {3} Free: {4}", mo["DeviceID"], driveType, mo["FileSystem"], size, free));
      }

      return driveInfo.ToString();
    }

    protected static string GetSizeString(object size)
    {
      string stringSize = "-";
      if (size != null)
      {
        string end;

        UInt64 uBytes = (UInt64)size;
        double bytes = uBytes;

        end = "B";
        if (bytes > 1024)
        {
          end = "KB";
          bytes /= 1024;
        }

        if (bytes > 1024)
        {
          end = "MB";
          bytes /= 1024;
        }

        if (bytes > 1024)
        {
          end = "GB";
          bytes /= 1024;
        }

        stringSize = bytes.ToString("F02") + end;
      }
      return stringSize;
    }

    protected static string GetCPUInfos()
    {
      RegistryKey mainKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor");
      if (mainKey == null)
        return string.Empty;
      string[] subKeys = mainKey.GetSubKeyNames();
      string cpuInfos = string.Empty;
      for (int i = 0; i < subKeys.Length; i++)
      {
        RegistryKey key = mainKey.OpenSubKey(subKeys[i]);
        if (key == null)
          continue;
        string cpuType = (string) key.GetValue("ProcessorNameString", "<unknown>");
        int cpuSpeed = (int) key.GetValue("~MHz", 0);
        cpuInfos += cpuType + " running at ~" + cpuSpeed + " MHz.";
        key.Close();
      }
      mainKey.Close();
      return cpuInfos;
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
