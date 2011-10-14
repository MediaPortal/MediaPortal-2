#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Registry;
using MediaPortal.Common.Services.Localization;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.MediaManagement;
using MediaPortal.Common.Services.Messaging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.Threading;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Common.Threading;

namespace MediaPortal.Common
{
  /// <summary>
  /// Core services registration class.
  /// </summary>
  public class ApplicationCore
  {
    public static void RegisterCoreServices()
    {
      // Insert a dummy while loading the path manager to break circular dependency of logger and path manager. This should not
      // be considered as a hack - simply the logger needs a path managed by the path manager and I don't want to remove log
      // output from the path manager only to prevent the dependency. Maybe we have a better solution in the future.
      ServiceRegistration.Set<ILogger>(new NoLogger());

      Services.PathManager.PathManager pathManager = new Services.PathManager.PathManager();
      pathManager.InitializeDefaults();
      ServiceRegistration.Set<IPathManager>(pathManager);

      ILogger logger = new Log4NetLogger(pathManager.GetPath(@"<LOG>")); 
      logger.Info("ApplicationCore: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);

      logger.Debug("ApplicationCore: Registering ILogger service");
      ServiceRegistration.Set<ILogger>(logger);

      logger.Debug("ApplicationCore: Registering IRegistry service");
      ServiceRegistration.Set<IRegistry>(new Services.Registry.Registry());

      logger.Debug("ApplicationCore: Registering IThreadPool service");
      ServiceRegistration.Set<IThreadPool>(new ThreadPool());

      logger.Debug("ApplicationCore: Registering IMessageBroker service");
      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());

      logger.Debug("ApplicationCore: Registering IPluginManager service");
      ServiceRegistration.Set<IPluginManager>(new Services.PluginManager.PluginManager());

      logger.Debug("ApplicationCore: Registering ISettingsManager service");
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

      logger.Debug("ApplicationCore: Registering ILocalization service");
      ServiceRegistration.Set<ILocalization>(new StringManager());

      logger.Debug("ApplicationCore: Registering ITaskScheduler service");
      ServiceRegistration.Set<ITaskScheduler>(new Services.TaskScheduler.TaskScheduler());

      logger.Debug("ApplicationCore: Registering IMediaAccessor service");
      ServiceRegistration.Set<IMediaAccessor>(new MediaAccessor());

      logger.Debug("ApplicationCore: Registering IImporterWorker service");
      ServiceRegistration.Set<IImporterWorker>(new ImporterWorker());

      logger.Debug("ApplicationCore: Registering IResourceServer service");
      ServiceRegistration.Set<IResourceServer>(new ResourceServer());

      logger.Debug("ApplicationCore: Registering IResourceMountingService");
      ServiceRegistration.Set<IResourceMountingService>(new ResourceMountingService());

      logger.Debug("ApplicationCore: Registering IRemoteResourceInformationService");
      ServiceRegistration.Set<IRemoteResourceInformationService>(new RemoteResourceInformationService());

      logger.Debug("ApplicationCore: Registering IThumbnailGenerator service");
      ServiceRegistration.Set<IThumbnailGenerator>(new ThumbnailGenerator());
    }

    public static void StartCoreServices()
    {
      ServiceRegistration.Get<ILocalization>().Startup();
      ServiceRegistration.Get<ITaskScheduler>().Startup();
      ServiceRegistration.Get<IImporterWorker>().Startup();
      ServiceRegistration.Get<IResourceServer>().Startup();
      ServiceRegistration.Get<IResourceMountingService>().Startup();
      ServiceRegistration.Get<IRemoteResourceInformationService>().Startup();
    }

    public static void StopCoreServices()
    {
      ResourcePath.ClearResourceCache();
      ServiceRegistration.Get<IRemoteResourceInformationService>().Shutdown();
      ServiceRegistration.Get<IResourceMountingService>().Shutdown();
      ServiceRegistration.Get<IResourceServer>().Shutdown();
      ServiceRegistration.Get<IImporterWorker>().Shutdown();
      ServiceRegistration.Get<ITaskScheduler>().Shutdown();
      ServiceRegistration.Get<IThreadPool>().Stop();
    }

    public static void RegisterDefaultMediaItemAspectTypes()
    {
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(ProviderResourceAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ImporterAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(DirectoryAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(MediaAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(VideoAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(AudioAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(PictureAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ThumbnailSmallAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ThumbnailLargeAspect.Metadata);
    }

    public static void DisposeCoreServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("ApplicationCore: Removing IThumbnailGenerator service");
      ServiceRegistration.RemoveAndDispose<IThumbnailGenerator>();

      logger.Debug("ApplicationCore: Removing IRemoteResourceInformationService");
      ServiceRegistration.RemoveAndDispose<IRemoteResourceInformationService>();

      logger.Debug("ApplicationCore: Removing IResourceMountingService");
      ServiceRegistration.RemoveAndDispose<IResourceMountingService>();

      logger.Debug("ApplicationCore: Removing IResourceServer service");
      ServiceRegistration.RemoveAndDispose<IResourceServer>();

      logger.Debug("ApplicationCore: Removing IImporterWorker service");
      ServiceRegistration.RemoveAndDispose<IImporterWorker>();

      logger.Debug("ApplicationCore: Removing IMediaAccessor service");
      ServiceRegistration.RemoveAndDispose<IMediaAccessor>();

      logger.Debug("ApplicationCore: Removing ITaskScheduler service");
      ServiceRegistration.RemoveAndDispose<ITaskScheduler>();

      logger.Debug("ApplicationCore: Removing ILocalization service");
      ServiceRegistration.RemoveAndDispose<ILocalization>();

      logger.Debug("ApplicationCore: Removing ISettingsManager service");
      ServiceRegistration.RemoveAndDispose<ISettingsManager>();

      logger.Debug("ApplicationCore: Removing IPluginManager service");
      ServiceRegistration.RemoveAndDispose<IPluginManager>();

      logger.Debug("ApplicationCore: Removing IMessageBroker service");
      ServiceRegistration.RemoveAndDispose<IMessageBroker>();

      logger.Debug("ApplicationCore: Removing IThreadPool service");
      ServiceRegistration.RemoveAndDispose<IThreadPool>();

      logger.Debug("ApplicationCore: Removing IRegistry service");
      ServiceRegistration.RemoveAndDispose<IRegistry>();

      logger.Debug("ApplicationCore: Removing IPathManager service");
      ServiceRegistration.RemoveAndDispose<IPathManager>();

      logger.Debug("ApplicationCore: Removing ILogger service");
      ServiceRegistration.RemoveAndDispose<ILogger>();
    }
  }
}
