#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Registry;
using MediaPortal.Core.Services.Localization;
using MediaPortal.Core.Services.Logging;
using MediaPortal.Core.Services.Messaging;
using MediaPortal.Core.Services.Settings;
using MediaPortal.Core.Settings;
using MediaPortal.Core.TaskScheduler;

namespace MediaPortal.Core
{
  /// <summary>
  /// Core services registration class.
  /// </summary>
  public class ApplicationCore
  {
    public static void RegisterCoreServices(LogLevel logLevel, bool logMethodNames, bool flushLog)
    {
      // Insert a dummy while loading the path manager to break circular dependency of logger and path manager. This should not
      // be considered as a hack - simply the logger needs a path managed by the path manager and I don't want to remove log
      // output from the path manager only to prevent the dependency. Maybe we have a better solution in the future.
      ServiceScope.Add<ILogger>(new NoLogger());

      IPathManager pathManager = new Services.PathManager.PathManager();
      ServiceScope.Add<IPathManager>(pathManager);

#if DEBUG
      ILogger logger = new GroupLogger(new ConsoleLogger(logLevel, logMethodNames));
      FileLogger.DeleteLogFiles(pathManager.GetPath(@"<LOG>\"), "*.log");
      (logger as GroupLogger).Add(FileLogger.CreateFileLogger(pathManager.GetPath(@"<LOG>\MediaPortal.log"), logLevel, logMethodNames, true));  // Always Flush log files in Debug Mode
#else
      FileLogger.DeleteLogFiles(pathManager.GetPath(@"<LOG>\"), "*.log");
      ILogger logger = FileLogger.CreateFileLogger(pathManager.GetPath(@"<LOG>\MediaPortal.log"), logLevel, logMethodNames);
#endif
      logger.Info("ApplicationCore: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);

      logger.Debug("ApplicationCore: Registering ILogger service");
      ServiceScope.Add<ILogger>(logger);

      logger.Debug("ApplicationCore: Registering IRegistry service");
      ServiceScope.Add<IRegistry>(new Services.Registry.Registry());

      logger.Debug("ApplicationCore: Registering IThreadPool service");
      ServiceScope.Add<Threading.IThreadPool>(new Services.Threading.ThreadPool());

      logger.Debug("ApplicationCore: Registering IMessageBroker service");
      ServiceScope.Add<IMessageBroker>(new MessageBroker());

      logger.Debug("ApplicationCore: Registering IPluginManager service");
      ServiceScope.Add<IPluginManager>(new Services.PluginManager.PluginManager());

      logger.Debug("ApplicationCore: Registering ISettingsManager service");
      ServiceScope.Add<ISettingsManager>(new SettingsManager());

      logger.Debug("UiExtension: Registering ILocalization service");
      ServiceScope.Add<ILocalization>(new StringManager());

      logger.Debug("ApplicationCore: Registering ITaskScheduler service");
      ServiceScope.Add<ITaskScheduler>(new Services.TaskScheduler.TaskScheduler());

      logger.Debug("ApplicationCore: Registering IMediaAccessor service");
      ServiceScope.Add<IMediaAccessor>(new Services.MediaManagement.MediaAccessor());

      logger.Debug("ApplicationCore: Registering IImporterWorker service");
      ServiceScope.Add<IImporterWorker>(new Services.MediaManagement.ImporterWorker());
    }

    public static void StartCoreServices()
    {
      ServiceScope.Get<ILocalization>().Startup();
      ServiceScope.Get<IImporterWorker>().Startup();
    }

    public static void StopCoreServices()
    {
      ServiceScope.Get<IImporterWorker>().Shutdown();
    }

    public static void RegisterDefaultMediaItemAspectTypes()
    {
      IMediaItemAspectTypeRegistration miatr = ServiceScope.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(ProviderResourceAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ImporterAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(MediaAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(MovieAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(MusicAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(PictureAspect.Metadata);
    }

    public static void DisposeCoreServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("ApplicationCore: Removing IImporterWorker service");
      ServiceScope.RemoveAndDispose<IImporterWorker>();

      logger.Debug("ApplicationCore: Removing IMediaAccessor service");
      ServiceScope.RemoveAndDispose<IMediaAccessor>();

      logger.Debug("ApplicationCore: Removing ITaskScheduler service");
      ServiceScope.RemoveAndDispose<ITaskScheduler>();

      logger.Debug("UiExtension: Removing ILocalization service");
      ServiceScope.RemoveAndDispose<ILocalization>();

      logger.Debug("ApplicationCore: Removing ISettingsManager service");
      ServiceScope.RemoveAndDispose<ISettingsManager>();

      logger.Debug("ApplicationCore: Removing IPluginManager service");
      ServiceScope.RemoveAndDispose<IPluginManager>();

      logger.Debug("ApplicationCore: Removing IMessageBroker service");
      ServiceScope.RemoveAndDispose<IMessageBroker>();

      logger.Debug("ApplicationCore: Removing IThreadPool service");
      ServiceScope.RemoveAndDispose<Threading.IThreadPool>();

      logger.Debug("ApplicationCore: Removing IRegistry service");
      ServiceScope.RemoveAndDispose<IRegistry>();

      logger.Debug("ApplicationCore: Removing IPathManager service");
      ServiceScope.RemoveAndDispose<IPathManager>();

      logger.Debug("ApplicationCore: Removing ILogger service");
      ServiceScope.RemoveAndDispose<ILogger>();
    }
  }
}
