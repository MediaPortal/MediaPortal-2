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
    public static void RegisterCoreServices()
    {
      Services.PathManager.PathManager pathManager = new Services.PathManager.PathManager();
      ServiceScope.Add<IPathManager>(pathManager);

#if DEBUG
      ILogger logger = new ConsoleLogger(LogLevel.Information, false);
#else
      ILogger logger = FileLogger.CreateFileLogger(pathManager.GetPath(@"<LOG>\MediaPortal.log"), LogLevel.Information, false);
#endif
      logger.Debug("ApplicationCore: Registering ILogger");
      ServiceScope.Add(logger);

      logger.Debug("ApplicationCore: Registering IRegistry");
      ServiceScope.Add<IRegistry>(new Services.Registry.Registry());

      logger.Debug("ApplicationLauncher: Registering IThreadPool");
      Services.Threading.ThreadPool pool = new Services.Threading.ThreadPool();
      pool.ErrorLog += ServiceScope.Get<ILogger>().Error;
      pool.WarnLog += ServiceScope.Get<ILogger>().Warn;
      pool.InfoLog += ServiceScope.Get<ILogger>().Info;
      pool.DebugLog += ServiceScope.Get<ILogger>().Debug;
      ServiceScope.Add<MediaPortal.Core.Threading.IThreadPool>(pool);

      logger.Debug("ApplicationLauncher: Registering IMessageBroker");
      ServiceScope.Add<IMessageBroker>(new MessageBroker());

      logger.Debug("ApplicationLauncher: Registering IPluginManager");
      ServiceScope.Add<IPluginManager>(new Services.PluginManager.PluginManager());

      logger.Debug("ApplicationLauncher: Registering ISettingsManager");
      ServiceScope.Add<ISettingsManager>(new SettingsManager());

      logger.Debug("ApplicationLauncher: Registering ITaskScheduler");
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
  }
}
