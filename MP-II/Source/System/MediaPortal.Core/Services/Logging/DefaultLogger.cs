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

namespace MediaPortal.Core.Services.Logging
{
  /// <summary>
  /// A <see cref="ILogger"/> implementation that writes messages to a <see cref="TextWriter"/>.
  /// </summary>
  public class DefaultLogger : ILogger
  {
    protected LogLevel _level; // Treshold for the log level
    protected TextWriter _writer; // The writer to write the messages to
    protected static readonly object _syncObject = new object();
    protected bool _logMethodNames = false;
    protected readonly string _myClassName;

    /// <summary>
    /// Creates a new <see cref="DefaultLogger"/> instance and initializes it with the given
    /// <paramref name="writer"/> and <paramref name="level"/>.
    /// </summary>
    /// <param name="writer">The writer to write the messages to.</param>
    /// <param name="level">The minimum level messages must have to be written to the file.</param>
    /// <param name="logMethodNames">Indicates whether to log the calling method's name.</param>
    /// <remarks>
    /// <para><b><u>Warning!</u></b></para>
    /// <para>Turning on logging of method names causes a severe performance degradation. Each call to the
    /// logger will add an extra amount of time, for example 10 to 40 milliseconds for a file output,
    /// depending on the length of the stack trace.</para>
    /// </remarks>
    public DefaultLogger(TextWriter writer, LogLevel level, bool logMethodNames)
    {
      _myClassName = GetType().Name;
      _writer = writer;
      _level = level;
      _logMethodNames = logMethodNames;
    }

    #region ILogger implementation

    public bool LogMethodNames
    {
      get { return _logMethodNames; }
      set { _logMethodNames = value; }
    }

    public LogLevel Level
    {
      get { return _level; }
      set { _level = value; }
    }

    public void Info(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Information);
    }

    public void Warn(string format, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Warning);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      Write(string.Format(format, args), LogLevel.Warning);
      Error(ex);
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
      if (_level >= LogLevel.Error)
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
      // Force a flush, otherwise the exception will not get logged 
      // if the next thing we do is terminate MP.
      Write("", true);
    }

    #endregion

    /// <summary>
    /// Formats the specified <paramref name="message"/> to be written for the specified
    /// <paramref name="messageLevel"/> log level.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="messageLevel">The <see cref="LogLevel"/> of the message to write.</param>
    protected void Write(string message, LogLevel messageLevel)
    {
      if (messageLevel > _level) // Levels are in reversed order
        return;
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
      if (_logMethodNames)
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
        } while (className.Equals(_myClassName));
        messageBuilder.Append("[");
        messageBuilder.Append(className);
        messageBuilder.Append(".");
        messageBuilder.Append(methodName);
        messageBuilder.Append("]");
      }
      messageBuilder.Append(": ");
      messageBuilder.Append(message);

      Write(messageBuilder.ToString(), messageLevel == LogLevel.Critical);
    }

    protected void Write(string message)
    {
      Write(message, false);
    }

    /// <summary>
    /// Writes an <see cref="Exception"/> instance.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to write.</param>
    protected void WriteException(Exception ex)
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
    /// Writes any existing inner exceptions.
    /// </summary>
    /// <param name="exception"></param>
    protected void WriteInnerException(Exception exception)
    {
      if (exception == null)
      {
        throw new ArgumentNullException("exception");
      }
      Write(exception.Message);
      if (exception.InnerException != null)
      {
        WriteInnerException(exception.InnerException);
      }
    }

    /// <summary>
    /// Does the actual writing of the message to the writer.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="flush">If set to <c>true</c>, the output will be flushed at once.
    /// If set to <c>false</c>, the underlaying writer will handle the buffer management and might
    /// delay the writing of its buffer.</param>
    protected void Write(string message, bool flush)
    {
      lock (_syncObject)
      {
        _writer.WriteLine(message);
        if (flush)
          _writer.Flush();
      }
    }
  }
}
