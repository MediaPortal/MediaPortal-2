#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using log4net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using NUnit.Framework;

namespace Tests.Server.Services.Logging
{
  /// <summary>
  /// <see cref="Log4NetLogger"/> is the only place MP2 talks to log4net, so when that library changes
  /// major version this is the behaviour that has to survive: a log call reaching the file the
  /// configuration names, at the level the configuration allows.
  /// </summary>
  [TestFixture]
  public class Log4NetLoggerTests
  {
    private const string Appender = @"
    <appender name=""TestFileAppender"" type=""log4net.Appender.FileAppender"">
      <file value=""test.log"" />
      <appendToFile value=""false"" />
      <immediateFlush value=""true"" />
      <layout type=""log4net.Layout.PatternLayout"">
        <conversionPattern value=""%level %message%newline"" />
      </layout>
    </appender>";

    private const string ConfigurationAll = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <log4net>" + Appender + @"
    <root>
      <level value=""ALL"" />
      <appender-ref ref=""TestFileAppender"" />
    </root>
  </log4net>
</configuration>";

    private const string ConfigurationErrorsOnly = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <log4net>" + Appender + @"
    <root>
      <level value=""ERROR"" />
      <appender-ref ref=""TestFileAppender"" />
    </root>
  </log4net>
</configuration>";

    private string _dir;
    private ISettingsManager _ambientSettings;
    private ILogger _ambientLogger;

    [SetUp]
    public void Setup()
    {
      _dir = Path.Combine(Path.GetTempPath(), "mp2-log4net-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_dir);

      // The constructor finishes configuring by asking the settings service for LogSettings and
      // applying that level on top of whatever the configuration file said. ServiceRegistration is a
      // process-wide singleton, so any fixture that ran before this one and registered a settings
      // manager - MockCore does - decides the log level of the tests in here. That dependency on test
      // order is the whole of the flakiness this fixture had, so the ambient services are taken out of
      // the way and put back afterwards. See PersistedLogLevelOverridesTheConfigurationFile for the
      // behaviour that this removal hides on purpose.
      _ambientSettings = ServiceRegistration.Get<ISettingsManager>(false);
      _ambientLogger = ServiceRegistration.Get<ILogger>(false);
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    [TearDown]
    public void Teardown()
    {
      if (_ambientSettings != null)
        ServiceRegistration.Set(_ambientSettings);
      else
        ServiceRegistration.Remove<ISettingsManager>();
      if (_ambientLogger != null)
        ServiceRegistration.Set(_ambientLogger);
      else
        ServiceRegistration.Remove<ILogger>();

      // log4net keeps the appender's file handle open until the repository is shut down, which on
      // Windows is the difference between the temp directory going away and leaking one per test.
      try { LogManager.GetRepository().Shutdown(); } catch (Exception) { }
      try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private string WriteConfiguration(string xml)
    {
      string configFile = Path.Combine(_dir, "logger.config");
      File.WriteAllText(configFile, xml);
      return configFile;
    }

    /// <summary>
    /// The appender still owns the file while it is configured, and it opens it for writing with
    /// share Read - so File.ReadAllText() fails with a sharing violation. Opening with
    /// FileShare.ReadWrite is how you read a log another process is still writing to, which is the
    /// normal state of a log file.
    /// </summary>
    private static string ReadShared(string path)
    {
      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      using (var reader = new StreamReader(stream))
        return reader.ReadToEnd();
    }

    /// <summary>
    /// The configuration says <c>&lt;file value="test.log"/&gt;</c> and the constructor rewrites that
    /// to <c>&lt;logPath&gt;/test.log</c>. Both halves matter: an appender that ignored the rewritten
    /// path would still produce a log file, just not one anybody would think to look at.
    /// </summary>
    [Test]
    public void LogCall_WrittenToFileNamedByTheConfiguration()
    {
      var logger = new Log4NetLogger(_dir, WriteConfiguration(ConfigurationAll));

      logger.Info("hello from {0}", "the test suite");

      string logFile = Path.Combine(_dir, "test.log");
      if (!File.Exists(logFile))
        Assert.Fail("the configured appender did not create " + logFile + "; directory contains: " +
                    string.Join(", ", Directory.GetFiles(_dir)));
      string content = ReadShared(logFile);
      StringAssert.Contains("hello from the test suite", content);
      StringAssert.Contains("INFO", content);
    }

    /// <summary>
    /// The level comes from the log4net repository rather than from MP2, so it is the part of the
    /// plumbing a library update can silently break in either direction: everything dropped, or
    /// everything written regardless of configuration. Only true with no persisted override, which is
    /// why <see cref="Setup"/> removes the ambient settings service first.
    /// </summary>
    [Test]
    public void EventsBelowTheConfiguredLevel_AreNotWritten()
    {
      var logger = new Log4NetLogger(_dir, WriteConfiguration(ConfigurationErrorsOnly));

      logger.Info("this event is below the level");
      logger.Error("this event reaches the appender");

      string content = ReadShared(Path.Combine(_dir, "test.log"));
      StringAssert.DoesNotContain("below the level", content);
      StringAssert.Contains("reaches the appender", content);
    }

    /// <summary>
    /// The configuration file is only the starting point: if a settings service is registered, the
    /// constructor loads <see cref="LogSettings"/> and applies that level over the file's, then saves
    /// the level back. Recorded here because it is what the log files in the field are actually
    /// controlled by, and because it is the reason a level test cannot simply assert on the file.
    /// </summary>
    [Test]
    public void PersistedLogLevelOverridesTheConfigurationFile()
    {
      var settings = new StubSettingsManager { Current = new LogSettings { LogLevel = LogLevel.Debug } };
      ServiceRegistration.Set<ISettingsManager>(settings);

      var logger = new Log4NetLogger(_dir, WriteConfiguration(ConfigurationErrorsOnly));

      Assert.That(logger.GetLogLevel(), Is.EqualTo(LogLevel.Debug),
        "the level stored in the settings is supposed to win over the configuration file");

      logger.Debug("this event needs the overridden level");

      StringAssert.Contains("overridden level", ReadShared(Path.Combine(_dir, "test.log")));
      Assert.That(settings.Saves, Is.EqualTo(1), "switching level is supposed to persist exactly once");
    }

    /// <summary>
    /// Holds <see cref="LogSettings"/> in memory. The real settings manager would read and write the
    /// developer's configuration database, which is not something a unit test should do - and it needs
    /// a database, which this fixture deliberately does not set up.
    /// </summary>
    private class StubSettingsManager : ISettingsManager
    {
      public LogSettings Current = new LogSettings();
      public int Saves;

      public SettingsType Load<SettingsType>() where SettingsType : class
      {
        return Current is SettingsType typed ? typed : Activator.CreateInstance<SettingsType>();
      }

      public object Load(Type settingsType)
      {
        return Current.GetType() == settingsType ? Current : Activator.CreateInstance(settingsType);
      }

      public void Save(object settingsObject)
      {
        Saves++;
        if (settingsObject is LogSettings logSettings)
          Current = logSettings;
      }

      public void StartBatchUpdate() { }
      public void EndBatchUpdate() { }
      public void CancelBatchUpdate() { }
      public void ClearCache() { }
      public void ChangeUserContext(string userName) { }
      public void RemoveSettingsData(Type settingsType, bool user, bool global) { }
      public void RemoveAllSettingsData(bool user, bool global) { }
    }

    /// <summary>
    /// The real server configuration names its appenders, layouts and evaluators as strings that
    /// log4net resolves by reflection ("log4net.Appender.RollingFileAppender",
    /// "log4net.Appender.BufferingForwardingAppender", "log4net.Core.LevelEvaluator", ...). Nothing
    /// in a build checks those strings, and when one stops resolving log4net reports an internal error
    /// and configures everything else - so the product keeps running and simply stops writing logs.
    /// This is the only thing standing between a log4net version bump and silent log loss in the field.
    /// </summary>
    [Test]
    public void ServerConfiguration_ResolvesEveryAppenderItNames()
    {
      string shipped = Path.Combine(TestContext.CurrentContext.TestDirectory, "Logging", "Server.app.config");
      if (!File.Exists(shipped))
        Assert.Fail("the server's log configuration was not copied to the test output: " + shipped);
      string copy = Path.Combine(_dir, "server.config");
      File.Copy(shipped, copy);

      new Log4NetLogger(_dir, copy);

      string[] expected = { "ConsoleAppender", "DefaultLogAppender", "ErrorLogAppender", "LossyFileAppender" };
      var configured = LogManager.GetRepository().GetAppenders().Select(a => a.Name).OrderBy(n => n).ToList();
      CollectionAssert.AreEquivalent(expected, configured,
        "an appender the configuration names was not configured: log4net could not resolve one of the " +
        "type strings in the shipped configuration and dropped that appender while configuring the rest");
    }
  }
}
