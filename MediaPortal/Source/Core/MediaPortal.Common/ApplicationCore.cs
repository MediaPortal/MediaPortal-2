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
using System.Diagnostics;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginItemBuilders;
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
using MediaPortal.Common.Services.ResourceAccess.ImpersonationService;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.Threading;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Common.Threading;
using MediaPortal.Common.FileEventNotification;
using MediaPortal.Common.Services.FileEventNotification;

namespace MediaPortal.Common
{
  /// <summary>
  /// Core services registration class.
  /// </summary>
  public class ApplicationCore
  {
    /// <summary>
    /// Creates vital core service instances and registers them in <see cref="ServiceRegistration"/>. The optional <paramref name="dataDirectory"/> argument can
    /// be used to startup the application using a custom directory for data storage.
    /// </summary>
    /// <param name="paths"><c>true</c> if the <paramref name="dataDirectory"/> should be set as <c>DATA</c> path</param>
    /// <param name="dataDirectory">Path to custom data directory</param>
    public static void RegisterVitalCoreServices(bool paths, string dataDirectory = null)
    {
      // Insert a dummy while loading the path manager to break circular dependency of logger and path manager. This should not
      // be considered as a hack - simply the logger needs a path managed by the path manager and I don't want to remove log
      // output from the path manager only to prevent the dependency. Maybe we have a better solution in the future.
      ServiceRegistration.Set<ILogger>(new NoLogger());

      // First register settings manager to allow the logger to access settings already
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

      ILogger logger = null;
      if (paths)
      {
        Services.PathManager.PathManager pathManager = new Services.PathManager.PathManager();
        pathManager.InitializeDefaults();
        if (!string.IsNullOrEmpty(dataDirectory))
          pathManager.SetPath("DATA", dataDirectory);

        ServiceRegistration.Set<IPathManager>(pathManager);

        logger = new Log4NetLogger(pathManager.GetPath(@"<LOG>"));
      }
      else
      {
        logger = ServiceRegistration.Get<ILogger>();
      }

      logger.Info("ApplicationCore: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);

      // Assembly and build information
      FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetCallingAssembly().Location);
      logger.Info("ApplicationCore: Comments:   {0}", fileVersionInfo.Comments);
      logger.Info("ApplicationCore: Copyright:  {0}", fileVersionInfo.LegalCopyright);
      logger.Info("ApplicationCore: Version:    {0}", fileVersionInfo.FileVersion);
      logger.Info("ApplicationCore: Source:     {0}", fileVersionInfo.ProductVersion);
      logger.Info("ApplicationCore: ----------------------------------------------------------");

      logger.Debug("ApplicationCore: Registering ILogger service");
      ServiceRegistration.Set<ILogger>(logger);

      logger.Debug("ApplicationCore: Registered ISettingsManager service");
    }

    /// <summary>
    /// Creates core service instances and registers them in <see cref="ServiceRegistration"/>.
    /// </summary>
    public static void RegisterCoreServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("ApplicationCore: Registering IImpersonationService");
      ServiceRegistration.Set<IImpersonationService>(new ImpersonationService());

      logger.Debug("ApplicationCore: Registering IRegistry service");
      ServiceRegistration.Set<IRegistry>(new Services.Registry.Registry());

      logger.Debug("ApplicationCore: Registering IThreadPool service");
      ServiceRegistration.Set<IThreadPool>(new ThreadPool());

      logger.Debug("ApplicationCore: Registering IMessageBroker service");
      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());

      logger.Debug("ApplicationCore: Registering ILoggerConfig service");
      ServiceRegistration.Set<ILoggerConfig>(new LoggerConfig());

      logger.Debug("ApplicationCore: Registering IPluginManager service");
      ServiceRegistration.Set<IPluginManager>(new Services.PluginManager.PluginManager());

      logger.Debug("ApplicationCore: Registering ILocalization service");
      ServiceRegistration.Set<ILocalization>(new StringManager());

      logger.Debug("ApplicationCore: Registering ITaskScheduler service");
      ServiceRegistration.Set<ITaskScheduler>(new Services.TaskScheduler.TaskScheduler());

      logger.Debug("ApplicationCore: Registering IMediaAccessor service");
      ServiceRegistration.Set<IMediaAccessor>(new MediaAccessor());

      logger.Debug("ApplicationCore: Registering IFileEventNotifier service");
      ServiceRegistration.Set<IFileEventNotifier>(new FileEventNotifier());

      // ToDo: Remove the old ImporterWorker and this setting once the NewGen ImporterWorker actually works
      var importerWorkerSettings = ServiceRegistration.Get<ISettingsManager>().Load<ImporterWorkerSettings>();
      if (importerWorkerSettings.UseNewImporterWorker)
      {
        logger.Debug("ApplicationCore: Registering IImporterWorker NewGen service");
        ServiceRegistration.Set<IImporterWorker>(new ImporterWorkerNewGen());
      }
      else
      {
        logger.Debug("ApplicationCore: Registering IImporterWorker service");
        ServiceRegistration.Set<IImporterWorker>(new ImporterWorker());        
      }

      logger.Debug("ApplicationCore: Registering IResourceServer service");
      ServiceRegistration.Set<IResourceServer>(new ResourceServer());

      logger.Debug("ApplicationCore: Registering IResourceMountingService");
      ServiceRegistration.Set<IResourceMountingService>(new ResourceMountingService());

      logger.Debug("ApplicationCore: Registering IRemoteResourceInformationService");
      ServiceRegistration.Set<IRemoteResourceInformationService>(new RemoteResourceInformationService());

      logger.Debug("ApplicationCore: Registering IThumbnailGenerator service");
      ServiceRegistration.Set<IThumbnailGenerator>(new ThumbnailGenerator());

      AdditionalPluginItemBuilders.Register();
    }

    public static void StartCoreServices()
    {
      ServiceRegistration.Get<ILocalization>().Startup();
      ServiceRegistration.Get<ITaskScheduler>().Startup();
      ServiceRegistration.Get<IImporterWorker>().Startup(); // shutdown in ApplicationLaunchers
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
      ServiceRegistration.Get<ITaskScheduler>().Shutdown();
      ServiceRegistration.Get<IThreadPool>().Shutdown();
    }

    public static void RegisterDefaultMediaItemAspectTypes()
    {
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();

      miatr.RegisterLocallyKnownMediaItemAspectType(ProviderResourceAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ImporterAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(DirectoryAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(MediaAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(VideoAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(GenreAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(VideoStreamAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(VideoAudioStreamAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(SubtitleAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(AudioAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(AudioAlbumAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(ImageAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(EpisodeAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(SeasonAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(SeriesAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(MovieAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(MovieCollectionAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(CompanyAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(PersonAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(CharacterAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(ThumbnailLargeAspect.Metadata);

      miatr.RegisterLocallyKnownMediaItemAspectType(ExternalIdentifierAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(RelationshipAspect.Metadata);
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

      logger.Debug("ApplicationCore: Removing IFileEventNotifier service");
      ServiceRegistration.RemoveAndDispose<IFileEventNotifier>();

      logger.Debug("ApplicationCore: Removing ITaskScheduler service");
      ServiceRegistration.RemoveAndDispose<ITaskScheduler>();

      logger.Debug("ApplicationCore: Removing ILocalization service");
      ServiceRegistration.RemoveAndDispose<ILocalization>();

      logger.Debug("ApplicationCore: Removing ISettingsManager service");
      ServiceRegistration.RemoveAndDispose<ISettingsManager>();

      logger.Debug("ApplicationCore: Removing IPluginManager service");
      ServiceRegistration.RemoveAndDispose<IPluginManager>();

      logger.Debug("ApplicationCore: Removing ILoggerConfig service");
      ServiceRegistration.RemoveAndDispose<ILoggerConfig>();

      logger.Debug("ApplicationCore: Removing IMessageBroker service");
      ServiceRegistration.RemoveAndDispose<IMessageBroker>();

      logger.Debug("ApplicationCore: Removing IThreadPool service");
      ServiceRegistration.RemoveAndDispose<IThreadPool>();

      logger.Debug("ApplicationCore: Removing IRegistry service");
      ServiceRegistration.RemoveAndDispose<IRegistry>();

      logger.Debug("ApplicationCore: Removing IImpersonationService");
      ServiceRegistration.RemoveAndDispose<IImpersonationService>();

      logger.Debug("ApplicationCore: Removing IPathManager service");
      ServiceRegistration.RemoveAndDispose<IPathManager>();

      logger.Debug("ApplicationCore: Removing ILogger service");
      ServiceRegistration.RemoveAndDispose<ILogger>();
    }
  }
}
