#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;


namespace MP2_PluginWizard
{
  /// <summary>
  /// Core services registration class.
  /// </summary>
  public class ApplicationCore
  {
    public static void RegisterCoreServices()
    {
      CheckConfigFiles();

      // Insert a dummy while loading the path manager to break circular dependency of logger and path manager. This should not
      // be considered as a hack - simply the logger needs a path managed by the path manager and I don't want to remove log
      // output from the path manager only to prevent the dependency. Maybe we have a better solution in the future.
      ServiceRegistration.Set<ILogger>(new NoLogger());

      var pathManager = new PathManager();
      pathManager.InitializeDefaults();

      var loggerPath = pathManager.GetPath(@"<LOG>");

      ILogger logger = new Log4NetLogger(loggerPath); 
      logger.Info("ApplicationCore: Launching CORE in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);
      
      logger.Debug("ApplicationCore: Registering IPathManager service");
      ServiceRegistration.Set<IPathManager>(pathManager);

      logger.Debug("ApplicationCore: Registering ILogger service");
      ServiceRegistration.Set<ILogger>(logger);

      logger.Debug("ApplicationCore: Registering ISettingsManager service");
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());
      
    }

    
    public static void DisposeCoreServices()
    {
      var logger = ServiceRegistration.Get<ILogger>();
      logger.Info("ApplicationCore: Disposing CORE in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);

      logger.Debug("ApplicationCore: Removing ISettingsManager service");
      ServiceRegistration.RemoveAndDispose<ISettingsManager>();

      logger.Debug("ApplicationCore: Removing IPathManager service");
      ServiceRegistration.RemoveAndDispose<IPathManager>();

      logger.Debug("ApplicationCore: Removing ILogger service");
      ServiceRegistration.RemoveAndDispose<ILogger>();
    }


    #region Config Files

    private static void CheckConfigFiles()
    {
      var applicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      var defaultsPath = Path.Combine(applicationPath, "Defaults");
      var logFilePathName = Application.ExecutablePath + ".config";
      var pathsFilePathName = Path.Combine(defaultsPath, "Paths.xml");


      if (!Directory.Exists(defaultsPath)) 
        Directory.CreateDirectory(defaultsPath);
      
      if (!File.Exists(logFilePathName)) 
        CreateDefaultLogConfigFile(logFilePathName);
      
      if (!File.Exists(pathsFilePathName)) 
        CreateDefaultPathsFile(pathsFilePathName);
    }

    private static void CreateDefaultPathsFile(string configFilePath)
    {
      TextWriter tw = new StreamWriter(configFilePath);
      tw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
      tw.WriteLine("<Paths>");
      tw.WriteLine("  <!-- Default paths can be overridden here -->");
      tw.WriteLine("  <Path name=\"DATA\" value=\"&lt;APPLICATION_ROOT&gt;\" />");
      tw.WriteLine("  <Path name=\"CONFIG\" value=\"&lt;DATA&gt;\\Config\" />");
      tw.WriteLine("  <Path name=\"LOG\" value=\"&lt;DATA&gt;\\Log\" />");
      tw.WriteLine("</Paths>");
      tw.Flush();
      tw.Close();
    }

    private static void CreateDefaultLogConfigFile(string configFilePath)
    {
      TextWriter tw = new StreamWriter(configFilePath);
      tw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
      tw.WriteLine("<configuration>)");
      tw.WriteLine("  <configSections>");
      tw.WriteLine("    <section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler, log4net\"/>");
      tw.WriteLine("  </configSections>");
      tw.WriteLine(" ");
      tw.WriteLine("  <log4net>");
      tw.WriteLine("    <appender name=\"DefaultLogAppender\" type=\"log4net.Appender.RollingFileAppender\">");
      tw.WriteLine("      <file value=\"[Name].log\" />");
      tw.WriteLine("      <appendToFile value=\"true\" />");
      tw.WriteLine("      <rollingStyle value=\"Once\" />");
      tw.WriteLine("      <maxSizeRollBackups value=\"4\" />");
      tw.WriteLine("      <maximumFileSize value=\"1MB\" />");
      tw.WriteLine("      <staticLogFileName value=\"true\" />");
      tw.WriteLine("      <layout type=\"log4net.Layout.PatternLayout\">");
      tw.WriteLine("        <conversionPattern value=\"[%date] [%-7logger] [%-9thread] [%-5level] - %message%newline\" />");
      tw.WriteLine("      </layout>");
      tw.WriteLine("    </appender>");
      tw.WriteLine(" ");
      tw.WriteLine("    <appender name=\"ErrorLogAppender\" type=\"log4net.Appender.RollingFileAppender\">");
      tw.WriteLine("      <file value=\"[Name]-Error.log\" />");
      tw.WriteLine("      <appendToFile value=\"true\" />");
      tw.WriteLine("      <rollingStyle value=\"Once\" />");
      tw.WriteLine("      <maxSizeRollBackups value=\"4\" />");
      tw.WriteLine("      <maximumFileSize value=\"1MB\" />");
      tw.WriteLine("      <staticLogFileName value=\"true\" />");
      tw.WriteLine("      <layout type=\"log4net.Layout.PatternLayout\">");
      tw.WriteLine("        <conversionPattern value=\"[%date] [%-7logger] [%-9thread] [%-5level] - %message%newline\" />");
      tw.WriteLine("      </layout>");
      tw.WriteLine("    </appender>");
      tw.WriteLine(" ");
      tw.WriteLine("    <appender name=\"ErrorLossyFileAppender\" type=\"log4net.Appender.BufferingForwardingAppender\">");
      tw.WriteLine("      <bufferSize value=\"1\" />");
      tw.WriteLine("      <lossy value=\"true\"/>");
      tw.WriteLine("      <evaluator type=\"log4net.Core.LevelEvaluator\">");
      tw.WriteLine("      <threshold value=\"ERROR\" />");
      tw.WriteLine("      </evaluator>");
      tw.WriteLine("      <appender-ref ref=\"ErrorLogAppender\" />");
      tw.WriteLine("    </appender>");
      tw.WriteLine(" ");
      tw.WriteLine("    <appender name=\"ConsoleAppender\" type=\"log4net.Appender.ConsoleAppender\">");
      tw.WriteLine("      <layout type=\"log4net.Layout.PatternLayout\">");
      tw.WriteLine("        <conversionPattern value=\"[%date] [%-7logger] [%-9thread] [%-5level] - %message%newline\" />");
      tw.WriteLine("      </layout>");
      tw.WriteLine("    </appender>");
      tw.WriteLine(" ");
      tw.WriteLine("    <root>");
      tw.WriteLine("      <level value=\"ALL\" />");
      tw.WriteLine("      <appender-ref ref=\"ConsoleAppender\" />");
      tw.WriteLine("      <appender-ref ref=\"ErrorLossyFileAppender\" />");
      tw.WriteLine("      <appender-ref ref=\"DefaultLogAppender\" />");
      tw.WriteLine("    </root>");
      tw.WriteLine("  </log4net>");
      tw.WriteLine(" ");
      tw.WriteLine("</configuration>");
      tw.WriteLine("<startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.0\"/></startup></configuration>");
      tw.Flush();
      tw.Close();
    }


    #endregion


  }
}