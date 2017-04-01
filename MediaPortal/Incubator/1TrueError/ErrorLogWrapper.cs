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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.OneTrueError.Settings;
using OneTrueError.Client;

namespace MediaPortal.Plugins.OneTrueError
{
  public class ErrorLogWrapper : ILogger, ILoggerConfig
  {
    private readonly ILogger _logger;
    private SettingsChangeWatcher<ErrorReportingServiceSettings> _settings = new SettingsChangeWatcher<ErrorReportingServiceSettings>();
    private LogLevel _minReportLevel = LogLevel.Information;
    private ICollection<string> _exceptionExcludeFilter = new HashSet<string>();

    static ErrorLogWrapper()
    {
      ServiceRegistration.Get<ILoggerConfig>().RegisterLogWrapper(typeof(ErrorLogWrapper), @"\Incubator\1TrueError\ErrorLogWrapper.cs");
    }

    /// <summary>
    /// Creates a new <see cref="ErrorLogWrapper"/> instance and initializes it with the given <paramref name="parentLogger"/>.
    /// All logging calls that contain an exception will be reported.
    /// </summary>
    /// <param name="parentLogger">Current logger to be wrapped around.</param>
    public ErrorLogWrapper(ILogger parentLogger)
    {
      _logger = parentLogger;
      _settings.SettingsChanged += UpdateSettings;
      UpdateSettings();
    }

    protected string TryFormat(string format, params object[] args)
    {
      if (args == null || args.Length == 0)
        return format;
      try
      {
        return string.Format(format, args);
      }
      catch (Exception)
      {
        return format;
      }
    }

    #region ILogger implementation

    public void Debug(string format, params object[] args)
    {
      _logger.Debug(format, args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      _logger.Debug(format, ex, args);
      if (_minReportLevel >= LogLevel.Debug)
        FilterAndSubmit(format, ex, args);
    }

    public void Info(string format, params object[] args)
    {
      _logger.Info(format, args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      _logger.Info(format, ex, args);
      if (_minReportLevel >= LogLevel.Information)
        FilterAndSubmit(format, ex, args);
    }

    public void Warn(string format, params object[] args)
    {
      _logger.Warn(format, args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      _logger.Warn(format, ex, args);
      if (_minReportLevel >= LogLevel.Warning)
        FilterAndSubmit(format, ex, args);
    }

    public void Error(string format, params object[] args)
    {
      _logger.Error(format, args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      _logger.Error(format, ex, args);
      if (_minReportLevel >= LogLevel.Error)
        FilterAndSubmit(format, ex, args);
    }

    public void Error(Exception ex)
    {
      _logger.Error("", ex);
      if (_minReportLevel >= LogLevel.Error)
        FilterAndSubmit(ex);
    }

    public void Critical(string format, params object[] args)
    {
      _logger.Critical(format, args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      _logger.Critical(format, ex, args);
      if (_minReportLevel >= LogLevel.Critical)
        FilterAndSubmit(format, ex, args);
    }

    public void Critical(Exception ex)
    {
      _logger.Critical("", ex);
      if (_minReportLevel >= LogLevel.Critical)
        FilterAndSubmit(ex);
    }

    #endregion

    #region Settings and exception filtering

    private void UpdateSettings(object sender = null, EventArgs e = null)
    {
      _minReportLevel = _settings.Settings.MinReportLevel;
      _exceptionExcludeFilter = new HashSet<string>(_settings.Settings.ExceptionExcludedList, StringComparer.InvariantCultureIgnoreCase);
    }

    private bool ShouldSubmit(Exception ex)
    {
      return !_exceptionExcludeFilter.Contains(ex.GetType().ToString());
    }

    private void FilterAndSubmit(string format, Exception ex, object[] args)
    {
      if (ShouldSubmit(ex))
        OneTrue.Report(ex, TryFormat(format, args));
    }

    private void FilterAndSubmit(Exception ex)
    {
      if (ShouldSubmit(ex))
        OneTrue.Report(ex);
    }

    #endregion

    public LogLevel GetLogLevel()
    {
      ILoggerConfig loggerConfig = _logger as ILoggerConfig;
      return loggerConfig != null ? loggerConfig.GetLogLevel() : LogLevel.Information;
    }

    public void SetLogLevel(LogLevel level)
    {
      ILoggerConfig loggerConfig = _logger as ILoggerConfig;
      if (loggerConfig != null) loggerConfig.SetLogLevel(level);
    }

    public void RegisterLogWrapper(Type type, string relativeFilename)
    {
      _logger.Warn("Unable to register log wrapper " + relativeFilename);
    }
  }
}
