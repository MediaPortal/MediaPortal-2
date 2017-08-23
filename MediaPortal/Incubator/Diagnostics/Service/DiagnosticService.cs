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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.ServerSettings;
using ILogger = MediaPortal.Common.Logging.ILogger;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
  /// <summary>
  /// Provide Diagnostics toolbox:
  /// - Enable DEBUG logging in Client
  /// - Enable logging of potential focus stealing
  /// - Collect log files
  /// </summary>
  public class DiagnosticsHandler : IDisposable
  {
    #region Private Fields

    private static FocusStealingMonitor _focusStealingInstance;
    private static FormLogMonitor _logViewerInstance;

    #endregion Private Fields

    #region Internal Properties

    /// <summary>
    /// Guaranteed unique access to focus stealing mechanism
    /// </summary>
    internal static FocusStealingMonitor FocusStealingInstance
    {
      get { return _focusStealingInstance ?? (_focusStealingInstance = new FocusStealingMonitor()); }
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
      FocusStealingInstance.Dispose();
      LogViewerInstance.Dispose();
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Retrieve log level
    /// </summary>
    /// <returns></returns>
    internal static LogLevel GetLogLevel()
    {
      ILoggerConfig config = ServiceRegistration.Get<ILoggerConfig>();
      return config != null ? config.GetLogLevel() : LogLevel.Information;
    }

    /// <summary>
    /// Set Log Level
    /// </summary>
    /// <param name="level">desired log level</param>
    internal static void SetLogLevel(LogLevel level)
    {
      ILoggerConfig config = ServiceRegistration.Get<ILoggerConfig>();
      if (config != null)
        config.SetLogLevel(level);

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings != null)
      {
        // Forward the local settings to server
        LogSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LogSettings>();
        serverSettings.Save(settings);
      }
    }

    #endregion Internal Methods
  }
}
