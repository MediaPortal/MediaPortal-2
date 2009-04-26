#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Windows.Forms;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Registry;
using MediaPortal.Core.Services.Logging;
using MediaPortal.Core.Services.Messaging;
using MediaPortal.Core.Services.Settings;
using MediaPortal.Core.Settings;
using MediaPortal.Core.TaskScheduler;

namespace MediaPortal.Core
{
  /// <summary>
  /// Starter class for the MediaPortal system. Before calling the <see cref="Start"/> method,
  /// the <see cref="ServiceScope"/> has to be configured with all needed services.
  /// </summary>
  public class ApplicationCore
  {
    public static void RegisterCoreServices(LogLevel logLevel, bool logMethodNames)
    {
      IPathManager pathManager = new Services.PathManager.PathManager();
      ServiceScope.Add<IPathManager>(pathManager);

#if DEBUG
      ILogger logger = new ConsoleLogger(logLevel, logMethodNames);
#else
      FileLogger.DeleteLogFiles(pathManager.GetPath(@"<LOG>\"), "*.log");
      ILogger logger = FileLogger.CreateFileLogger(pathManager.GetPath(@"<LOG>\MediaPortal.log"), logLevel, logMethodNames);
#endif
      logger.Info("ApplicationCore: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);

      logger.Debug("ApplicationCore: Registering ILogger");
      ServiceScope.Add<ILogger>(logger);

      logger.Debug("ApplicationCore: Registering IRegistry");
      ServiceScope.Add<IRegistry>(new Services.Registry.Registry());

      logger.Debug("ApplicationCore: Registering IThreadPool");
      Services.Threading.ThreadPool pool = new Services.Threading.ThreadPool();
      pool.ErrorLog += ServiceScope.Get<ILogger>().Error;
      pool.WarnLog += ServiceScope.Get<ILogger>().Warn;
      pool.InfoLog += ServiceScope.Get<ILogger>().Info;
      pool.DebugLog += ServiceScope.Get<ILogger>().Debug;
      ServiceScope.Add<Threading.IThreadPool>(pool);

      logger.Debug("ApplicationCore: Registering IMessageBroker");
      ServiceScope.Add<IMessageBroker>(new MessageBroker());

      logger.Debug("ApplicationCore: Registering IPluginManager");
      ServiceScope.Add<IPluginManager>(new Services.PluginManager.PluginManager());

      logger.Debug("ApplicationCore: Registering ISettingsManager");
      ServiceScope.Add<ISettingsManager>(new SettingsManager());

      logger.Debug("ApplicationCore: Registering ITaskScheduler");
      ServiceScope.Add<ITaskScheduler>(new Services.TaskScheduler.TaskScheduler());
    }

    public void Start()
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.Initialize();
      pluginManager.Startup(false);
      Application.Run();
      pluginManager.Shutdown();
    }

    public static void StopAll()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("ApplicationCore: Shutting down IMessageBroker");
      ServiceScope.Get<IMessageBroker>().Shutdown();
    }

    public static void DisposeCoreServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("ApplicationCore: Removing IPathManager");
      ServiceScope.RemoveAndDispose<IPathManager>();

      logger.Debug("ApplicationCore: Removing ILogger");
      ServiceScope.RemoveAndDispose<ILogger>();

      logger.Debug("ApplicationCore: Removing IRegistry");
      ServiceScope.RemoveAndDispose<IRegistry>();

      logger.Debug("ApplicationCore: Removing IThreadPool");
      ServiceScope.RemoveAndDispose<Threading.IThreadPool>();

      logger.Debug("ApplicationCore: Removing IMessageBroker");
      ServiceScope.RemoveAndDispose<IMessageBroker>();

      logger.Debug("ApplicationCore: Removing IPluginManager");
      ServiceScope.RemoveAndDispose<IPluginManager>();

      logger.Debug("ApplicationCore: Removing ISettingsManager");
      ServiceScope.RemoveAndDispose<ISettingsManager>();

      logger.Debug("ApplicationCore: Removing ITaskScheduler");
      ServiceScope.RemoveAndDispose<ITaskScheduler>();
    }
  }
}
