/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  /// <summary>
  /// Logs internal messages
  /// </summary>
  public static class Log
  {
    /// <summary>
    /// Logs the message at level Debug
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    internal static void Debug(String logMessage)
    {
      ServiceRegistration.Get<ILogger>().Debug(logMessage);
    }

    /// <summary>
    /// Logs the message at level Debug
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    /// <param name="ex">Exception to log</param>
    internal static void Debug(String logMessage, Exception ex)
    {
      ServiceRegistration.Get<ILogger>().Debug(logMessage, ex);
    }

    /// <summary>
    /// Logs the message at level info
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    internal static void Info(String logMessage)
    {
      ServiceRegistration.Get<ILogger>().Info(logMessage);
    }

    /// <summary>
    /// Logs the message at level info
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    /// <param name="ex">Exception to log</param>
    internal static void Info(String logMessage, Exception ex)
    {
      ServiceRegistration.Get<ILogger>().Info(logMessage, ex);
    }

    /// <summary>
    /// Logs the message at level Warn
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    internal static void Warn(String logMessage)
    {
      ServiceRegistration.Get<ILogger>().Warn(logMessage);
    }

    /// <summary>
    /// Logs the message at level Warn
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    /// <param name="ex">Exception to log</param>
    internal static void Warn(String logMessage, Exception ex)
    {
      ServiceRegistration.Get<ILogger>().Warn(logMessage, ex);
    }

    /// <summary>
    /// Logs the message at level Warn
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    /// <param name="ex">Exception to log</param>
    /// <param name="args"></param>
    internal static void Warn(string logMessage, Exception ex, object[] args)
    {
      ServiceRegistration.Get<ILogger>().Warn(logMessage, ex, args);
    }

    /// <summary>
    /// Logs the message at level Error
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    internal static void Error(String logMessage)
    {
      ServiceRegistration.Get<ILogger>().Error(logMessage);
    }

    /// <summary>
    /// Logs the message at level Error
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    /// <param name="ex">Exception to log</param>
    internal static void Error(String logMessage, Exception ex)
    {
      ServiceRegistration.Get<ILogger>().Error(logMessage, ex);
    }

    /// <summary>
    /// Logs the message at level Fatal
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    internal static void Fatal(String logMessage)
    {
      Error(logMessage);
    }

    /// <summary>
    /// Logs the message at level Fatal
    /// </summary>
    /// <param name="logMessage">Message to log</param>
    /// <param name="ex">Exception to log</param>
    internal static void Fatal(String logMessage, Exception ex)
    {
      Error(logMessage, ex);
    }
  }
}
