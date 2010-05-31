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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Services.Logging;

namespace MediaPortal.UI.Services.Logging
{
  /// <summary>
  /// Logs as much information about an UI crash as possible.
  /// Creates a directory where it copies all application logs.
  /// Creates new crash log with system information.
  /// </summary>
  public class UiCrashLogger : CrashLoggerBase
  {
    /// <summary>
    /// Creates a new <see cref="UiCrashLogger"/> instance which will copy all
    /// log files found in the specified <paramref name="logFilesPath"/>.
    /// </summary>
    /// <param name="logFilesPath">Path of the application's log files to be copied to the
    /// created crash directory.</param>
    public UiCrashLogger(string logFilesPath) : base(logFilesPath) { }

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

          writer.WriteLine("= Screen Information");
          writer.Write(ScreenInfo());
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
        //CreateDxDiagLog(logPath.FullName); -- Too slow
      }
      catch (Exception e)
      {
        Console.WriteLine("UiCrashLogger crashed:");
        Console.WriteLine(e.ToString());
      }
    }

    protected static string ScreenInfo()
    {
      StringBuilder screenInfo = new StringBuilder();
      screenInfo.AppendLine("== Screen Information");
      screenInfo.AppendLine("  Screen Resolution:\t" + SystemInformation.PrimaryMonitorSize.ToString());

      return screenInfo.ToString();
    }

    protected static void CreateDxDiagLog(string destinationFolder)
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
