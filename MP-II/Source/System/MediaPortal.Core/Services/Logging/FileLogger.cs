#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.IO;
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Services.Logging
{
  /// <summary>
  /// A <see cref="ILogger"/> implementation that writes messages to a text file.
  /// </summary>
  public class FileLogger : DefaultLogger
  {
    /// <summary>
    /// Number of logfile names to probe.
    /// </summary>
    public const int NUM_ATTEMPTS = 10;

    /// <summary>
    /// Creates a new <see cref="FileLogger"/> instance and initializes it with the given filename and
    /// <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="filePath">The file path to write the messages to.</param>
    /// <param name="level">The minimum level messages must have to be written to the file.</param>
    /// <param name="logMethodNames">Indicates whether to log the calling method's name.</param>
    /// <param name="alwaysFlush">If set to <c>true</c>, method <see cref="TextWriter.Flush"/> will be
    /// called after each log output.</param>
    /// <remarks>
    /// <para>If the text file exists it will be truncated.</para>
    /// <para><b><u>Warning!</u></b><br/>
    /// Turning on logging of method names causes a severe performance degradation. Each call to the
    /// logger will add an extra 10 to 40 milliseconds, depending on the length of the stack trace.</para>
    /// </remarks>
    protected FileLogger(string filePath, LogLevel level, bool logMethodNames, bool alwaysFlush) :
        base(new StreamWriter(filePath, true), level, logMethodNames, alwaysFlush) { }

    protected FileLogger(TextWriter writer, LogLevel level, bool logMethodNames, bool alwaysFlush) :
        base(writer, level, logMethodNames, alwaysFlush) { }

    protected static Stream GrabFile(string filePath)
    {
      try
      {
        if (File.Exists(filePath))
          File.Delete(filePath);
        return File.Open(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
      }
      catch (Exception e)
      {
        if (!(e is IOException) && !(e is UnauthorizedAccessException))
          throw;
        return null;
      }
    }

    protected static Stream FindAvailableFile(string filePath)
    {
      Stream result = GrabFile(filePath);
      if (result != null)
        return result;
      int suffix = 0;
      string baseName = Path.Combine(Path.GetDirectoryName(filePath),
          Path.GetFileNameWithoutExtension(Path.GetFileName(filePath)));
      string ext = Path.GetExtension(filePath);
      while (suffix < NUM_ATTEMPTS)
      {
        result = GrabFile(baseName + "~" + suffix + ext);
        if (result != null)
          return result;
        suffix++;
      }
      throw new IOException(string.Format("Cannot open logfile '{0}'", filePath));
    }

    public static FileLogger CreateFileLogger(string filePath, LogLevel level, bool logMethodNames, bool alwaysFlush)
    {
      string dir = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      Stream s = FindAvailableFile(filePath);
      return new FileLogger(new StreamWriter(s), level, logMethodNames, alwaysFlush);
    }

    public static void DeleteLogFiles(string logFilePath, string searchPattern)
    {
      if (!Directory.Exists(logFilePath))
        return;
      string[] logFiles = Directory.GetFiles(logFilePath, searchPattern);
      foreach (string logFile in logFiles)
      {
        try
        {
          File.Delete(logFile);
        }
        catch (Exception e)
        {
          if (!(e is IOException))
            throw;
          // Ignore IOException - another MP instance might be running and locking the file
        }
      }
    }
  }
}
