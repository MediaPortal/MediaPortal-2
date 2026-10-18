#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using ILogger = MediaPortal.Common.Logging.ILogger;

namespace MediaPortal.Common.Services.Logging
{
  public class Log4NetLogger : ILogger, ILoggerConfig
  {
    /// <summary>
    /// Creates a new <see cref="Log4NetLogger"/> instance and initializes it with the given parameters.
    /// </summary>
    /// <param name="logPath">Path where the logfiles should be written to.</param>
    public Log4NetLogger(string logPath)
      : this(logPath, FindConfigurationFile())
    {
    }

    /// <summary>
    /// Creates a new <see cref="Log4NetLogger"/> instance that reads its log4net section from a
    /// configuration file the caller names.
    /// </summary>
    /// <param name="logPath">Path where the logfiles should be written to.</param>
    /// <param name="configurationFile">File containing the <c>&lt;log4net&gt;</c> section.</param>
    /// <remarks>
    /// The one-argument constructor has to guess where the configuration lives, and a guess about
    /// file layout is exactly the kind of assumption that does not survive a platform change. Putting
    /// the guess in one method (see <see cref="FindConfigurationFile"/>) keeps every caller that knows
    /// better - hosts, services, and tests - able to say so instead of working around the guess.
    /// </remarks>
    public Log4NetLogger(string logPath, string configurationFile)
    {
      XmlDocument xmlDoc = new XmlDocument();
      using (Stream stream = new FileStream(configurationFile, FileMode.Open, FileAccess.Read))
        xmlDoc.Load(stream);
      XmlNodeList nodeList = xmlDoc.SelectNodes("configuration/log4net/appender/file");
      foreach (XmlNode node in nodeList)
        if (node.Attributes != null)
          foreach (XmlAttribute attribute in node.Attributes)
            if (attribute.Name.Equals("value"))
            {
              attribute.Value = Path.Combine(logPath, Path.GetFileName(attribute.Value));
              break;
            }

      using (MemoryStream stream = new MemoryStream())
      {
        xmlDoc.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);
        log4net.Config.XmlConfigurator.Configure(stream);
      }

      // After init check for overridden settings. Note: SettingsManager is not available inside SlimTV integration provider.
      var settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
      if (settingsManager != null)
      {
        var settings = settingsManager.Load<LogSettings>();
        SetLogLevel(settings.LogLevel);
      }
    }

    /// <summary>
    /// Determines the configuration file of the current process.
    /// </summary>
    /// <remarks>
    /// Application.ExecutablePath came from System.Windows.Forms, which is Windows-only. The entry
    /// assembly location is the portable equivalent and matches both candidates: on .NET Framework it
    /// is the .exe (so "&lt;exe&gt;.config" still resolves), on .NET it is the managed .dll - which is
    /// the file that actually carries a .config there, since the apphost .exe never does. Same
    /// fallback order as before.
    /// </remarks>
    private static string FindConfigurationFile()
    {
      string executablePath = Assembly.GetEntryAssembly()?.Location;
      if (string.IsNullOrEmpty(executablePath))
        executablePath = Assembly.GetExecutingAssembly().Location;
      var configs = new[] { executablePath + ".config", Path.ChangeExtension(executablePath, ".dll") + ".config" };
      var config = configs.FirstOrDefault(c => File.Exists(c));
      if (config == null)
        // Used to fall through as an ArgumentNullException from new FileStream(null, ...). Same
        // outcome - the logger cannot start - but the message says what was looked for, which is the
        // difference between a five-minute and an hour when this happens on a machine you cannot sit at.
        throw new FileNotFoundException(
          "No log4net configuration found. Looked for: " + string.Join(", ", configs), executablePath + ".config");
      return config;
    }

    protected ILog GetLogger
    {
      get { return LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType); }
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
      GetLogger.DebugFormat(format, args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      GetLogger.Debug(TryFormat(format, args), ex);
    }

    public void Info(string format, params object[] args)
    {
      GetLogger.InfoFormat(format, args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      GetLogger.Info(TryFormat(format, args), ex);
    }

    public void Warn(string format, params object[] args)
    {
      GetLogger.WarnFormat(format, args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      GetLogger.Warn(TryFormat(format, args), ex);
    }

    public void Error(string format, params object[] args)
    {
      GetLogger.ErrorFormat(format, args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      GetLogger.Error(TryFormat(format, args), ex);
    }

    public void Error(Exception ex)
    {
      GetLogger.Error("", ex);
    }

    public void Critical(string format, params object[] args)
    {
      GetLogger.FatalFormat(format, args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      GetLogger.Fatal(TryFormat(format, args), ex);
    }

    public void Critical(Exception ex)
    {
      GetLogger.Fatal("", ex);
    }

    #endregion

    #region ILoggerConfig members

    public LogLevel GetLogLevel()
    {
      var loggerRepository = (Hierarchy)LogManager.GetRepository();
      return ToLogLevel(loggerRepository.Root.Level);
    }

    public void SetLogLevel(LogLevel level)
    {
      var loggerRepository = (Hierarchy)LogManager.GetRepository();
      var oldLevel = loggerRepository.Root.Level;
      loggerRepository.Root.Level = ToLog4Net(level);
      // Avoid duplicated saving of same value, as this could cause an endless loop inside ILoggerConfig service
      if (oldLevel != loggerRepository.Root.Level)
      {
        loggerRepository.RaiseConfigurationChanged(EventArgs.Empty);
        ServiceRegistration.Get<ILogger>().Info("Log4NetLogger: Switched LogLevel to {0}", level);
        LogSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LogSettings>();
        settings.LogLevel = level;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    private Level ToLog4Net(LogLevel level)
    {
      switch (level)
      {
        case LogLevel.All: return Level.All;
        case LogLevel.Information: return Level.Info;
        case LogLevel.Debug: return Level.Debug;
        case LogLevel.Warning: return Level.Warn;
        case LogLevel.Error: return Level.Error;
        case LogLevel.Critical: return Level.Critical;
        default:
          return Level.Debug;
      }
    }

    private LogLevel ToLogLevel(Level level)
    {
      if (level == Level.All) return LogLevel.All;
      if (level == Level.Info) return LogLevel.Information;
      if (level == Level.Debug) return LogLevel.Debug;
      if (level == Level.Warn) return LogLevel.Warning;
      if (level == Level.Error) return LogLevel.Error;
      if (level == Level.Critical) return LogLevel.Critical;
      return LogLevel.Debug;
    }
  }

  #endregion
}
