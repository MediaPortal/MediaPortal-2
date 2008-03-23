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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using MediaPortal.Core.Logging;

namespace MediaPortal.Services.Logging
{
  /// <summary>
  /// A <see cref="ILogger"/> implementation that writes messages to a text file.
  /// </summary>
  /// <remarks>If the text file exists it will be truncated.</remarks>
  public class FileLogger : ILogger
  {
    private LogLevel _Level; //holds the treshold for the log level.
    private readonly string _FileName; //holds the file to write to.
    private static readonly object _SyncObject = new object();
    private bool _LogMethodNames = false;
    private readonly string _MyClassName;

    /// <summary>
    /// Creates a new <see cref="FileLogger"/> instance and initializes it with the given filename and <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="fileName">The full path of the file to write the messages to.</param>
    /// <param name="level">The minimum level messages must have to be written to the file.</param>
    /// <param name="logMethodNames">Indicates whether to log the calling method's name.</param>
    /// <remarks>
    /// <para><b><u>Warning!</u></b></para>
    /// <para>Turning on logging of method names causes a severe performance degradation.  Each call to the
    /// logger will add an extra 10 to 40 milliseconds, depending on the length of the stack trace.</para>
    /// </remarks>
    public FileLogger(string fileName, LogLevel level, bool logMethodNames)
    {
      _MyClassName = GetType().Name;
      _FileName = fileName;
      _Level = level;
      _LogMethodNames = logMethodNames;
      FileInfo logFile = new FileInfo(fileName);
      if (!logFile.Directory.Exists)
        logFile.Directory.Create();

      if (level > LogLevel.None)
      {
        using (new StreamWriter(fileName, false)) {}
      }
    }


    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    /// <value>A <see cref="LogLevel"/> value that indicates the minimum level messages must have to be 
    /// written to the file.</value>
    public LogLevel Level
    {
      get { return _Level; }
      set { _Level = value; }
    }


    /// <summary>
    /// Indicates whether the logger will append the calling classname and method before each log line.
    /// </summary>
    /// <remarks>
    /// Warning!!  Turning this option on causes a severe performance degradation!!!</remarks>
    public bool LogMethodNames
    {
      get { return _LogMethodNames; }
      set { _LogMethodNames = value; }
    }

    #region ILogger implementation

    public void Info(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Information);
    }

    public void Warn(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Warning);
    }

    public void Debug(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Debug);
    }

    public void Error(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Error);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Error);
      Error(ex);
    }

    public void Error(Exception ex)
    {
      if (_Level >= LogLevel.Error)
      {
        WriteException(ex);
      }
    }

    public void Critical(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Critical);

    }

    public void Critical(Exception ex)
    {
      WriteException(ex);
    }

    #endregion

    /// <summary>
    /// Does the actual writing of the message to the file.
    /// </summary>
    /// <param name="message"></param>
    private void Write(string message)
    {
      Monitor.Enter(_SyncObject);
      try
      {
        using (StreamWriter writer = new StreamWriter(_FileName, true))
        {
          writer.WriteLine(message);
        }
      }
      finally
      {
        Monitor.Exit(_SyncObject);
      }
    }

    /// <summary>
    /// Does the actual writing of the message to the file.
    /// </summary>
    /// <param name="message">The message to write</param>
    /// <param name="messageLevel">The <see cref="LogLevel"/> of the message to write</param>
    private void Write(string message, LogLevel messageLevel)
    {
      if (messageLevel > _Level)
      {
        return;
      }
      StringBuilder messageBuilder = new StringBuilder();
      messageBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
      string levelShort;
      switch (messageLevel)
      {
        case LogLevel.Critical:
          levelShort = "Crit.";
          break;
        case LogLevel.Information:
          levelShort = "Info.";
          break;
        case LogLevel.Warning:
          levelShort = "Warn.";
          break;
        default:
          levelShort = messageLevel.ToString();
          break;
      }
      messageBuilder.Append(" [");
      messageBuilder.Append(levelShort);
      messageBuilder.Append("][");

      string thread = Thread.CurrentThread.Name;
      if (thread == null)
      {
        thread = Thread.CurrentThread.ManagedThreadId.ToString();
      }
      messageBuilder.Append(thread);
      messageBuilder.Append("]");
      if (_LogMethodNames)
      {
        StackTrace trace = new StackTrace(false);
        int step = 1;
        string className;
        string methodName;
        do
        {
          MethodBase method = trace.GetFrame(step++).GetMethod();
          className = method.DeclaringType.Name;
          methodName = method.Name;
        } while (className.Equals(_MyClassName));
        messageBuilder.Append("[");
        messageBuilder.Append(className);
        messageBuilder.Append(".");
        messageBuilder.Append(methodName);
        messageBuilder.Append("]");
      }
      messageBuilder.Append(": ");
      messageBuilder.Append(message);

      Monitor.Enter(_SyncObject);
      try
      {
        using (StreamWriter writer = new StreamWriter(_FileName, true))
        {
          writer.WriteLine(messageBuilder.ToString());
          if (messageLevel == LogLevel.Critical)
          {
            writer.Flush();
          }
        }
      }
      finally
      {
        Monitor.Exit(_SyncObject);
      }
    }

    /// <summary>
    /// Writes an <see cref="Exception"/> instance to the file.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to write.</param>
    private void WriteException(Exception ex)
    {
      Write("Exception: " + ex);
      Write("  Message: " + ex.Message);
      Write("  Site   : " + ex.TargetSite);
      Write("  Source : " + ex.Source);
      if (ex.InnerException != null)
      {
        Write("Inner Exception(s):");
        WriteInnerException(ex.InnerException);
      }
      Write("Stack Trace:");
      Write(ex.StackTrace);
    }

    /// <summary>
    /// Writes any existing inner exceptions to the file.
    /// </summary>
    /// <param name="exception"></param>
    private void WriteInnerException(Exception exception)
    {
      if (exception == null)
      {
        throw new ArgumentNullException("exception");
      }
      Write(exception.Message);
      if (exception.InnerException != null)
      {
        WriteInnerException(exception);
      }
    }
  }
}
