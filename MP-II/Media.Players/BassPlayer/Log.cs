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
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace Media.Players.BassPlayer
{
  /// <summary>
  /// Logger service wrapper.
  /// </summary>
  public static class Log
  {
    #region Fields

    private const string _Prefix = "BassPlayer: ";
    private static ILogger _ExternalLogger;

    #endregion

    #region Public Members

    /// <summary>
    /// Returns the active logger object.
    /// </summary>
    public static ILogger Instance
    {
      get
      {
        if (_ExternalLogger == null)
          return ServiceScope.Get<ILogger>();
        else
          return _ExternalLogger;
      }
    }

    /// <summary>
    /// Sets an alternate logger, for development purposes.
    /// </summary>
    /// <param name="logger"></param>
    public static void SetLogger(ILogger logger)
    {
      _ExternalLogger = logger;
    }

    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Info(string format, params object[] args)
    {
      Instance.Info(_Prefix + format, args);
    }

    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Debug(string format, params object[] args)
    {
      Instance.Debug(_Prefix + format, args);
    }

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Error(string format, params object[] args)
    {
      Instance.Error(_Prefix + format, args);
    }

    /// <summary>
    /// Writes a warning message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public static void Warn(string format, params object[] args)
    {
      Instance.Warn(_Prefix + format, args);
    }

    #endregion
  }
}
