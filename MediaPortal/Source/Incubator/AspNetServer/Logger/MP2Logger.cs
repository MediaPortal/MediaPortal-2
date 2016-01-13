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
using Microsoft.Extensions.Logging;

using IMP2Logger = MediaPortal.Common.Logging.ILogger;
using MP2LogLevel = MediaPortal.Common.Logging.LogLevel;
using IAspNetLogger = Microsoft.Extensions.Logging.ILogger;
using AspNetLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MediaPortal.Plugins.AspNetServer.Logger
{
  /// <summary>
  /// <see cref="IAspNetLogger"/> that uses the MP2-Logger infrastructure to log Asp.Net messages
  /// </summary>
  public class MP2Logger : IAspNetLogger
  {
    #region Consts

    /// <summary>
    /// Format string used to write messages into the log file.
    /// </summary>
    /// <remarks>
    /// {0}: Name of the WebApplication
    /// {1}: So called "CategoryName", which in Asp.Net is the name of the class that logs the message.
    /// {2}: Message to log
    /// </remarks>
    private const string LOG_FORMAT = "[{0}] {1} - {2}";

    #endregion

    #region Private fields

    private readonly IMP2Logger _logger;
    private readonly MP2LogLevel _logLevel;
    private readonly string _webApplicationName;
    private readonly string _categoryName;

    #endregion

    #region Inner classes

    private class NoopDisposable : IDisposable
    {
      public void Dispose()
      {
      }
    }

    #endregion

    #region Constructor

    public MP2Logger(IMP2Logger logger, MP2LogLevel logLevel, string webApplicationName, string categoryName)
    {
      _logger = logger;
      _logLevel = logLevel;
      _webApplicationName = webApplicationName;
      _categoryName = categoryName;
    }

    #endregion

    #region IAspNetLogger implementation

    public void Log(AspNetLogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
    {
      if (!IsEnabled(logLevel))
        return;

      string message;
      var values = state as ILogValues;
      if (formatter != null)
        message = formatter(state, exception);
      else if (values != null)
      {
        message = LogFormatter.FormatLogValues(values);
        if (exception != null)
          message += Environment.NewLine + exception;
      }
      else 
        message = LogFormatter.Formatter(state, exception);

      if (string.IsNullOrEmpty(message))
        return;

      // MP2's ILogger does not know a log level "Trace". We translate this into "Debug" so that no messages are lost.
      switch (logLevel)
      {
        case AspNetLogLevel.Trace:
          _logger.Debug(LOG_FORMAT, _webApplicationName, _categoryName, message);
          break;
        case AspNetLogLevel.Debug:
          _logger.Debug(LOG_FORMAT, _webApplicationName, _categoryName, message);
          break;
        case AspNetLogLevel.Information:
          _logger.Info(LOG_FORMAT, _webApplicationName, _categoryName, message);
          break;
        case AspNetLogLevel.Warning:
          _logger.Warn(LOG_FORMAT, _webApplicationName, _categoryName, message);
          break;
        case AspNetLogLevel.Error:
          _logger.Error(LOG_FORMAT, _webApplicationName, _categoryName, message);
          break;
        case AspNetLogLevel.Critical:
          _logger.Critical(LOG_FORMAT, _webApplicationName, _categoryName, message);
          break;
      }
    }

    public bool IsEnabled(AspNetLogLevel logLevel)
    {
      // MP2's ILogger does not know a log level "Trace". We translate this into "Debug" so that no messages are lost.
      switch (logLevel)
      {
        case AspNetLogLevel.Trace:
          return _logLevel >= MP2LogLevel.Debug;
        case AspNetLogLevel.Debug:
          return _logLevel >= MP2LogLevel.Debug;
        case AspNetLogLevel.Information:
          return _logLevel >= MP2LogLevel.Information;
        case AspNetLogLevel.Warning:
          return _logLevel >= MP2LogLevel.Warning;
        case AspNetLogLevel.Error:
          return _logLevel >= MP2LogLevel.Error;
        case AspNetLogLevel.Critical:
          return _logLevel >= MP2LogLevel.Critical;
      }
      return false;
    }

    public IDisposable BeginScopeImpl(object state)
    {
      return new NoopDisposable();
    }

    #endregion
  }
}
