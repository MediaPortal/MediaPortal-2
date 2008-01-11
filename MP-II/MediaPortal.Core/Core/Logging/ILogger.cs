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

namespace MediaPortal.Core.Logging
{
  /// <summary>
  /// Interface for all logger implementations
  /// </summary>
  public interface ILogger
  {
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    /// <value>A <see cref="LogLevel"/> value that indicates the minimum level messages must have to be 
    /// written to the file.</value>
    LogLevel Level { get; set; }

    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    void Info(string format, params object[] args);

    /// <summary>
    /// Writes a warning to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    void Warn(string format, params object[] args);

    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    void Debug(string format, params object[] args);

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    void Error(string format, params object[] args);

    /// <summary>
    /// Writes an error message to the log, passing the original <see cref="Exception"/>.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="ex">The <see cref="Exception"/> that caused the message.</param>
    /// <param name="args">An array of objects to write using format.</param>
    void Error(string format, Exception ex, params object[] args);

    /// <summary>
    /// Writes an <see cref="Exception"/> to the log.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to write.</param>
    void Error(Exception ex);

    /// <summary>
    /// Writes a critical system message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    void Critical(string format, params object[] args);
  }
}
