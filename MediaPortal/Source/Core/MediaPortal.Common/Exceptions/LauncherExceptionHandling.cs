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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;

namespace MediaPortal.Common.Exceptions
{
  /// <summary>
  /// Handling methods for uncaught exceptions - can be registered in application launcher classes.
  /// </summary>
  public class LauncherExceptionHandling
  {
    public static string LogPath { get; set; }

    /// <summary>
    /// Last resort, used when there is no logger yet and no writable log directory. The default
    /// writes to the console, which is all a headless host has. A Windows launcher replaces it
    /// with a dialog - see MediaPortal.Server/ApplicationLauncher.cs, the two launchers that still
    /// have a UI. This hook exists so that the crash path of the portable core does not pull in a
    /// UI toolkit: an unhandled exception must be reportable even when the toolkit is the thing
    /// that failed, or when there is no toolkit at all.
    /// </summary>
    public static Action<string, string> LastResortReporter = ReportToConsole;

    private static void ReportToConsole(string caption, string message)
    {
      Console.Error.WriteLine(caption);
      Console.Error.WriteLine(message);
    }

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
      if (IsAbortedSocketIo(e.Exception))
      {
        // Nothing failed here, see IsAbortedSocketIo. Logging it as an error puts two to six lines in the
        // log of every shutdown and hides the failures this handler is for.
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
        {
          logger.Debug("ApplicationLauncher: a socket operation was still pending when its socket was closed: {0}",
              e.Exception.Message);
          return;
        }
      }
      HandleException("Unhandled Task Exception", "Unhandled task exception in application", e.Exception);
    }

    /// <summary>
    /// Tells whether <paramref name="exception"/> only reports socket I/O that was still pending when the
    /// socket was closed.
    /// </summary>
    /// <remarks>
    /// The UPnP listeners are closed at shutdown while a receive is outstanding, and the socket APIs they
    /// use (the APM <c>BeginReceiveFrom</c> bridge, which is built on the Task-based calls) report the
    /// resulting abort through a Task that no one ever looks at. .NET hands that to
    /// <see cref="TaskScheduler_UnobservedTaskException"/> from the finalizer thread, so it arrives here
    /// as an application error once per listener on every shutdown - measured as
    /// <see cref="SocketError.OperationAborted"/>. Aborting pending I/O is what closing a socket means, so
    /// this is teardown, not a fault; anything else keeps going through the normal error path.
    /// </remarks>
    private static bool IsAbortedSocketIo(Exception exception)
    {
      if (exception == null)
        return false;
      SocketException socketException = exception as SocketException;
      if (socketException != null)
        return socketException.SocketErrorCode == SocketError.OperationAborted ||
            socketException.SocketErrorCode == SocketError.Interrupted;
      if (exception is AggregateException)
      {
        foreach (Exception inner in ((AggregateException) exception).Flatten().InnerExceptions)
          if (IsAbortedSocketIo(inner))
            return true;
        return false;
      }
      return IsAbortedSocketIo(exception.InnerException);
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

      // Last resort. Read the hook once: this code runs while the application is already failing,
      // and a null hook here would replace the reported exception with a NullReferenceException.
      Action<string, string> reporter = LastResortReporter ?? ReportToConsole;
      reporter(caption, ExceptionInfo(text, ex, false));
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
