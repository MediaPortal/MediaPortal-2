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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Settings;

namespace MediaPortal.Common.Services.Logging
{
  /// <summary>
  /// <see cref="LoggerConfig"/> is a system service that takes care for log system configuration.
  /// </summary>
  public class LoggerConfig : ILoggerConfig, IDisposable
  {
    private readonly SettingsChangeWatcher<LogSettings> _settings;

    public LoggerConfig()
    {
      _settings = new SettingsChangeWatcher<LogSettings>();
      _settings.SettingsChanged += SettingsChanged;
    }

    private void SettingsChanged(object sender, EventArgs eventArgs)
    {
      SetLogLevel(_settings.Settings.LogLevel);
    }

    public LogLevel GetLogLevel()
    {
      var logger = ServiceRegistration.Get<ILogger>();
      ILoggerConfig loggerConfig = logger as ILoggerConfig;
      return loggerConfig != null ? loggerConfig.GetLogLevel() : LogLevel.Information;
    }

    public void SetLogLevel(LogLevel level)
    {
      var logger = ServiceRegistration.Get<ILogger>();
      ILoggerConfig loggerConfig = logger as ILoggerConfig;
      if (loggerConfig != null) loggerConfig.SetLogLevel(level);
    }

    public void Dispose()
    {
      if (_settings != null)
        _settings.Dispose();
    }
  }
}
