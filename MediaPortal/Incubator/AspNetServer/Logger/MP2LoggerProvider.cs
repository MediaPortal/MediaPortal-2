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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using Microsoft.Extensions.Logging;

using IMP2Logger = MediaPortal.Common.Logging.ILogger;
using MP2LogLevel = MediaPortal.Common.Logging.LogLevel;
using IAspNetLogger = Microsoft.Extensions.Logging.ILogger;

namespace MediaPortal.Plugins.AspNetServer.Logger
{
  /// <summary>
  /// <see cref="ILoggerProvider"/> that provides Loggers using the MP2-Logging infrastructure
  /// </summary>
  /// <remarks>
  /// If AspNetServerSettings.EnableDebugLog is false, it uses a <see cref="NoLogger"/> so that nothing is logged.
  /// If AspNetServerSettings.EnableDebugLog is true, it uses a <see cref="FileLogger"/> that creates an AspNetServerDebug.log
  ///    file in the default log directory of MP2; the applicable LogLevel can be set in AspNetServerSettings.LogLevel.
  /// Every instance of this class creates Asp.Net loggers that write into the same AspNetServerDebug.log file.
  /// In the constructor of this class, a WebApplicationName must be passed, which is prepended to every log message of the
  /// respective instance so that log messages from different WebApplications can be distinguished in the AspNetServerDebug.log file.
  /// </remarks>
  public class MP2LoggerProvider : ILoggerProvider
  {
    #region Private fields

    private static readonly IMP2Logger LOGGER;
    private static readonly MP2LogLevel LOG_LEVEL;

    private readonly string _webApplicationName;

    #endregion

    #region Constructors

    static MP2LoggerProvider()
    {
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<AspNetServerSettings>();
      LOG_LEVEL = settings.LogLevel;
      if (settings.EnableDebugLogging)
        LOGGER = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\AspNetServerDebug.log"), LOG_LEVEL, false, true);
      else
        LOGGER = new NoLogger();
    }

    public MP2LoggerProvider(string webApplicationName)
    {
      _webApplicationName = webApplicationName;
    }

    #endregion

    #region ILoggerProvider implementation

    public IAspNetLogger CreateLogger(string categoryName)
    {
      return new MP2Logger(LOGGER, LOG_LEVEL, _webApplicationName, categoryName);
    }

    public void Dispose()
    {
      LOGGER.Debug("[{0}] Disposing MP2LoggerProvider", _webApplicationName);
    }

    #endregion
  }
}
