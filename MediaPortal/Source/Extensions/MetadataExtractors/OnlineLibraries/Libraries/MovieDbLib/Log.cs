/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib
{
  internal class Log
  {
    /// <summary>
    /// Loglevel
    /// </summary>
    internal enum LOGLEVEL { Debug = 0, Info = 1, Warn = 2, Error = 3, Fatal = 4 }

    /// <summary>
    /// The loglevel that is currently used
    /// </summary>
    internal const LOGLEVEL CURRENT_LEVEL = LOGLEVEL.Debug;

    /// <summary>
    /// Logs the message at level Debug
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    internal static void Debug(String _logMessage)
    {
      Debug(_logMessage, LOGLEVEL.Debug);
    }

    /// <summary>
    /// Logs the message at level Debug
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    /// <param name="_ex">Exception to log</param>
    internal static void Debug(String _logMessage, Exception _ex)
    {
      Debug(_logMessage + _ex.ToString(), LOGLEVEL.Debug);
    }

    /// <summary>
    /// Logs the message at level info
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    internal static void Info(String _logMessage)
    {
      Debug(_logMessage, LOGLEVEL.Info);
    }

    /// <summary>
    /// Logs the message at level info
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    /// <param name="_ex">Exception to log</param>
    internal static void Info(String _logMessage, Exception _ex)
    {
      Debug(_logMessage + _ex.ToString(), LOGLEVEL.Info);
    }

    /// <summary>
    /// Logs the message at level Warn
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    internal static void Warn(String _logMessage)
    {
      Debug(_logMessage, LOGLEVEL.Warn);
    }

    /// <summary>
    /// Logs the message at level Warn
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    /// <param name="_ex">Exception to log</param>
    internal static void Warn(String _logMessage, Exception _ex)
    {
      Debug(_logMessage + _ex.ToString(), LOGLEVEL.Warn);
    }

    /// <summary>
    /// Logs the message at level Error
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    internal static void Error(String _logMessage)
    {
      Debug(_logMessage, LOGLEVEL.Error);
    }

    /// <summary>
    /// Logs the message at level Error
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    /// <param name="_ex">Exception to log</param>
    internal static void Error(String _logMessage, Exception _ex)
    {
      Debug(_logMessage + _ex.ToString(), LOGLEVEL.Error);
    }

    /// <summary>
    /// Logs the message at level Fatal
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    internal static void Fatal(String _logMessage)
    {
      Debug(_logMessage, LOGLEVEL.Fatal);
    }

    /// <summary>
    /// Logs the message at level Fatal
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    /// <param name="_ex">Exception to log</param>
    internal static void Fatal(String _logMessage, Exception _ex)
    {
      Debug(_logMessage + _ex.ToString(), LOGLEVEL.Fatal);
    }

    /// <summary>
    /// Logs the message at the given level
    /// </summary>
    /// <param name="_logMessage">Message to log</param>
    /// <param name="_level">Level to log</param>
    internal static void Debug(String _logMessage, LOGLEVEL _level)
    {
      if (_level >= CURRENT_LEVEL)
      {
        switch (_level)
        {
          case LOGLEVEL.Debug:
            //debug log processing
            Console.WriteLine(_logMessage);
            break;
          case LOGLEVEL.Info:
            //debug log processing
            Console.WriteLine(_logMessage);
            break;
          case LOGLEVEL.Warn:
            //debug log processing
            Console.WriteLine(_logMessage);
            break;
          case LOGLEVEL.Error:
            //debug log processing
            Console.WriteLine(_logMessage);
            break;
          case LOGLEVEL.Fatal:
            //debug log processing
            Console.WriteLine(_logMessage);
            break;
        }
      }
    }
  }
}
