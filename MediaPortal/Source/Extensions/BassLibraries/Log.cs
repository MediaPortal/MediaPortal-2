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

using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.BassLibraries
{
  /// <summary>
  /// Logger service wrapper.
  /// </summary>
  public static class Log
  {
    #region Fields

    private const string PREFIX = "BassLibrary: ";

    #endregion

    #region Public Members

    /// <summary>
    /// Returns the active logger object.
    /// </summary>
    public static ILogger Instance
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Info(string format, params object[] args)
    {
      Instance.Info(PREFIX + format, args);
    }

    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Debug(string format, params object[] args)
    {
      Instance.Debug(PREFIX + format, args);
    }

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Error(string format, params object[] args)
    {
      Instance.Error(PREFIX + format, args);
    }

    /// <summary>
    /// Writes a warning message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Warn(string format, params object[] args)
    {
      Instance.Warn(PREFIX + format, args);
    }

    /// <summary>
    /// Writes a critical error to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Critical(string format, params object[] args)
    {
      Instance.Critical(PREFIX + format, args);
    }

    #endregion
  }
}
