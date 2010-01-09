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
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Services.Logging
{
  /// <summary>
  /// A <see cref="ILogger"/> implementation that writes messages to the application console.
  /// </summary>
  public class ConsoleLogger : DefaultLogger
  {
    /// <summary>
    /// Creates a new <see cref="ConsoleLogger"/> instance and initializes it with the given
    /// <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The minimum level messages must have to be written to the file.</param>
    /// <param name="logMethodNames">Indicates whether to log the calling method's name.</param>
    public ConsoleLogger(LogLevel level, bool logMethodNames):
        base(Console.Out, level, logMethodNames, false) { }
  }
}
