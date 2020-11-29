#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MediaPortal.Common.Logging;

namespace MediaPortal.Common.Exceptions
{
  /// <summary>
  /// Handling methods for uncaught exceptions - can be registered in application launcher classes.
  /// </summary>
  public class LauncherExceptionHandling
  {
    public static string LogPath { get; set; }

    public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
      HandleException("Unhandled Thread Exception", string.Format("Unhandled thread exception in thread '{0}'", sender), e.Exception);
    }

    public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      HandleException("Unhandled Exception", "Unhandled exception in application", (Exception)e.ExceptionObject);
    }

    public static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
      HandleException("Unhandled Task Exception", "Unhandled task exception in application", e.Exception);
    }

    public static void HandleException(string caption, string text, Exception ex)
    {
      try
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
        {
          logger.Error("ApplicationLauncher: " + text, ex);
          return;
        }
      }
      catch {}

      try
      {
        if (Directory.Exists(LogPath))
        {
          var file = Path.Combine(LogPath, "UnhandledException.log");
          StringBuilder sb = new StringBuilder();
          sb.AppendLine($"[{DateTime.Now}] - {caption}");
          sb.AppendLine(ExceptionInfo(text, ex, true));
          File.AppendAllText(file, sb.ToString());
          return;
        }
      }
      catch { }

      MessageBox.Show(ExceptionInfo(text, ex, false), caption);
    }

    public static string ExceptionInfo(string text, Exception ex, bool full)
    {
      StringBuilder exceptionInfo = new StringBuilder();
      exceptionInfo.AppendLine(text);
      exceptionInfo.AppendLine("  Exception: " + ex.GetType());
      exceptionInfo.AppendLine("  Message: " + ex.Message);
      exceptionInfo.AppendLine("  Site   : " + ex.TargetSite);
      exceptionInfo.AppendLine("  Source : " + ex.Source);
      exceptionInfo.AppendLine("Stack Trace:");
      exceptionInfo.AppendLine(ex.StackTrace);
      if (ex.InnerException != null)
      {
        if (!full)
        {
          exceptionInfo.AppendLine("Inner Exception(s):");
          exceptionInfo.AppendLine(WriteInnerException(ex.InnerException));
        }
        else
        {
          exceptionInfo.AppendLine(ExceptionInfo("Inner Exception:", ex.InnerException, true));
        }
      }

      return exceptionInfo.ToString();
    }

    public static string WriteInnerException(Exception exception)
    {
      StringBuilder exceptionInfo = new StringBuilder();
      if (exception != null)
      {
        exceptionInfo.AppendLine("  " + exception.Message);
        if (exception.InnerException != null)
        {
          exceptionInfo.AppendLine(WriteInnerException(exception));
        }
      }
      return exceptionInfo.ToString();
    }
  }
}
