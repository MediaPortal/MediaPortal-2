#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Management;
using MediaPortal.Core;
using Microsoft.Win32;

namespace MediaPortal.Services.Logging
{
  /// <summary>
  /// Logs as much information about crash as possible.
  /// Creates a directory where it copies all application logs.
  /// Creates new crash log with system information.
  /// TODO:
  /// log status of all Services
  /// </summary>
  public class CrashLogger
  {
    private string _filename;
    private DateTime _crashTime;
    /// <summary>
    /// Creates a new <see cref="CrashLogger"/> instance which will copy all
    /// log files found in the specified <paramref name="logFilesPath"/>.
    /// </summary>
    /// <param name="logFilesPath">Path of the application's log files to be copied to the
    /// created crash directory.</param>
    public CrashLogger(string logFilesPath)
    {
      _crashTime = DateTime.Now;

      string crashLogPath = Path.Combine(logFilesPath, "Crash_" + _crashTime.ToString("dd.MM.yyyy_HHmm"));
      if (!Directory.Exists(crashLogPath))
        Directory.CreateDirectory(crashLogPath);

      CopyLogFiles(logFilesPath, crashLogPath);
      //CreateDxDiagLog(logPath.FullName); -- Too slow

      _filename = Path.Combine(crashLogPath, "Crash.log");
    }

    public void CreateLog(Exception ex)
    {
      try
      {
        using (TextWriter writer = new StreamWriter(_filename, false))
        {
          writer.WriteLine("Crash Log: {0}", _crashTime.ToString());

          writer.WriteLine("= System Information");
          writer.Write(SystemInfo());
          writer.WriteLine();

          writer.WriteLine("= Disk Information");
          writer.Write(DriveInfo());
          writer.WriteLine();

          writer.WriteLine("= Exception Information");
          writer.Write(ExceptionInfo(ex));
					writer.WriteLine();
					writer.WriteLine();

					writer.WriteLine("= MediaPortal Information");
					writer.WriteLine();
        	IList<string> statusList = ServiceScope.Current.GetStatus();
        	foreach (string status in statusList)
        		writer.WriteLine(status);
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("CrashLogger crashed:");
        Console.WriteLine(e.ToString());
      }
    }

    private string ExceptionInfo(Exception ex)
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
    /// <param name="exception"></param>
    private string WriteInnerException(Exception exception)
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

    private static string SystemInfo()
    {
      StringBuilder systemInfo = new StringBuilder();
      systemInfo.AppendLine("== Software");
      systemInfo.AppendLine("  OS Version:\t\t" + System.Environment.OSVersion.ToString());
      systemInfo.AppendLine("  Hostname:\t\t" + Environment.MachineName);
      systemInfo.AppendLine("  Network attached?\t" + SystemInformation.Network.ToString());

      systemInfo.AppendLine();
      systemInfo.AppendLine("== Hardware");
      systemInfo.AppendLine("  CPU details:\t" + GetCPUInfos());
      systemInfo.AppendLine("  Screen Resolution:\t" + SystemInformation.PrimaryMonitorSize.ToString());

      return systemInfo.ToString();
    }

    private static string DriveInfo()
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

    private static string GetSizeString(object size)
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

    private static string GetCPUInfos()
    {
      RegistryKey mainKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor");
      string[] subKeys = mainKey.GetSubKeyNames();
      string cpuInfos = "";
      for (int i = 0; i < subKeys.Length; i++)
      {
        RegistryKey key = mainKey.OpenSubKey(subKeys[i]);
        string cpuType = (string)key.GetValue("ProcessorNameString", "<unknown>");
        int cpuSpeed = (int)key.GetValue("~MHz", 0);
        cpuInfos += cpuType + " running at ~" + cpuSpeed + " MHz.";
        key.Close();
      }
      mainKey.Close();
      return cpuInfos;
    }

    private static void CopyLogFiles(string logPath, string crashLogPath)
    {
      foreach (string logFilePath in Directory.GetFiles(logPath, "*.log"))
        File.Copy(logFilePath, Path.Combine(crashLogPath, Path.GetFileName(logFilePath)));
    }

    private static void CreateDxDiagLog(string destinationFolder)
    {
      string dstFile = Path.Combine(destinationFolder,  "DxDiag_Info.txt");

      string executable = Environment.GetEnvironmentVariable("windir") + @"\system32\dxdiag.exe";
      string arguments = "/whql:off /t \"" + dstFile+"\"";

      Process pr = new Process();
      pr.StartInfo.FileName = executable;
      pr.StartInfo.Arguments = arguments;
      pr.StartInfo.CreateNoWindow = true;
      pr.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
      pr.Start();
      pr.WaitForExit();
      //lastExitCode = pr.ExitCode;
    }
  }
}
