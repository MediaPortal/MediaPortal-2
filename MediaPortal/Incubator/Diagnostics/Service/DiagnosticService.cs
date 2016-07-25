#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using MediaPortal.Common;
using ILogger = MediaPortal.Common.Logging.ILogger;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
  /// <summary>
  /// Provide Diagnostics toolbox:
  /// - Enable DEBUG logging in Client
  /// - Enable logging of potential focus steeling
  /// - Collect log files
  /// </summary>
  public class DiagnosticsHandler : IDisposable
  {
    #region Private Fields

    private static FocusSteelingMonitor _focusSteelingInstance;
    private static FormLogMonitor _logViewerInstance;

    #endregion Private Fields

    #region Internal Properties

    /// <summary>
    /// Guaranteed unique access to focus steeling mechanism
    /// </summary>
    internal static FocusSteelingMonitor FocusSteelingInstance
    {
      get { return _focusSteelingInstance ?? (_focusSteelingInstance = new FocusSteelingMonitor()); }
    }

    /// <summary>
    /// Guaranteed unique access to log viewer
    /// </summary>
    internal static FormLogMonitor LogViewerInstance
    {
      get
      {
        if (_logViewerInstance != null && !_logViewerInstance.IsDisposed)
          return _logViewerInstance;
        _logViewerInstance = new FormLogMonitor();
        return _logViewerInstance;
      }
    }

    #endregion Internal Properties

    #region Public Methods

    public void Dispose()
    {
      FocusSteelingInstance.Dispose();
      LogViewerInstance.Dispose();
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Retrieve log level
    /// </summary>
    /// <returns></returns>
    internal static Level GetLogLevel()
    {
      var loggerRepository = (Hierarchy)LogManager.GetRepository();
      var returnValue = loggerRepository.Root.Level;
      return returnValue;
    }

    /// <summary>
    /// Set Log Level
    /// </summary>
    /// <param name="level">desired log level</param>
    internal static void SetLogLevel(Level level)
    {
      var loggerRepository = (Hierarchy)LogManager.GetRepository();
      loggerRepository.Root.Level = level;
      loggerRepository.RaiseConfigurationChanged(EventArgs.Empty);
      ServiceRegistration.Get<ILogger>().Debug(string.Format("DiagnosticService: Switched LogLevel to {0}", level));
    }

    #endregion Internal Methods
  }
}
