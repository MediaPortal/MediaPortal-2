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

using System.IO;
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Services.Logging
{
  /// <summary>
  /// A <see cref="ILogger"/> implementation that writes messages to a text file.
  /// </summary>
  /// <remarks>If the text file exists it will be truncated.</remarks>
  public class FileLogger : DefaultLogger
  {
    /// <summary>
    /// Creates a new <see cref="FileLogger"/> instance and initializes it with the given filename and
    /// <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="filePath">The file path to write the messages to.</param>
    /// <param name="level">The minimum level messages must have to be written to the file.</param>
    /// <param name="logMethodNames">Indicates whether to log the calling method's name.</param>
    /// <remarks>
    /// <para><b><u>Warning!</u></b></para>
    /// <para>Turning on logging of method names causes a severe performance degradation. Each call to the
    /// logger will add an extra 10 to 40 milliseconds, depending on the length of the stack trace.</para>
    /// </remarks>
    private FileLogger(string filePath, LogLevel level, bool logMethodNames):
        base(new StreamWriter(filePath, true), level, logMethodNames)
    {
    }

    public static FileLogger CreateFileLogger(string filePath, LogLevel level, bool logMethodNames)
    {
      string dir = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      if (level > LogLevel.None)
      {
        if (File.Exists(filePath))
          File.Delete(filePath);
      }
      return new FileLogger(filePath, level, logMethodNames);
    }
  }
}
