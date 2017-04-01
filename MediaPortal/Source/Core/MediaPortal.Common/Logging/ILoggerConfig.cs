#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Common.Logging
{
  /// <summary>
  /// Extended interface to control behavior of <see cref="ILogger"/> instances.
  /// </summary>
  public interface ILoggerConfig
  {
    /// <summary>
    /// Returns to currently active log level.
    /// </summary>
    /// <returns>Log level</returns>
    LogLevel GetLogLevel();

    /// <summary>
    /// Sets the lowest <see cref="LogLevel"/> that will be written to log. Lower values will be skipped.
    /// </summary>
    /// <param name="level">New log level</param>
    void SetLogLevel(LogLevel level);

    /// <summary>
    /// Register a class as a log wrapper. These classes will subsequently be ignored when traversing
    /// the log stack to work out where a call was from
    /// </summary>
    /// <param name="type">log wrapper class</param>
    /// <param name="relativeFilename">Partial path starting at the git repo directory</param>
    void RegisterLogWrapper(Type type, string relativeFilename);
  }
}
