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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Runtime;
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
using MediaPortal.Common.Services.ResourceAccess.Api;
using MediaPortal.Common.Services.ResourceAccess.ImpersonationService;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.Threading;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Common.Threading;
using MediaPortal.Common.FileEventNotification;
using MediaPortal.Common.Services.FileEventNotification;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Services.GenreConverter;

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
      logger.Info("ApplicationCore: Comments:         {0}", fileVersionInfo.Comments);
      logger.Info("ApplicationCore: Copyright:        {0}", fileVersionInfo.LegalCopyright);
      logger.Info("ApplicationCore: Version:          {0}", fileVersionInfo.FileVersion);
      logger.Info("ApplicationCore: Source:           {0}", fileVersionInfo.ProductVersion);
      logger.Info("ApplicationCore: Architecture:     {0}", RuntimeInformation.ProcessArchitecture);
      // Operating system info
      logger.Info("ApplicationCore: OS version:       {0}", Environment.OSVersion);
      if (OperatingSystem.IsWindows())
      {
        // These five values are what a user sees in winver.exe and what a support thread quotes.
        // Environment.OSVersion reports "Microsoft Windows NT 6.2.9200.0" on every Windows 10/11
        // build regardless of compatibility switches, and RuntimeInformation.OSDescription gives
        // "Windows 10.0.22631" - neither tells you whether it is Home or Pro, or which feature
        // update is installed, which is exactly what a crash report needs.
        foreach (string key in new[] { "ProductName", "ReleaseId", "BuildLab", "InstallationType", "EditionID" })
        {
          try
          {
            var value = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", key, string.Empty).ToString();
            logger.Info("ApplicationCore: {0,-18}{1}", key + ":", value);
          }
          catch { break; }
        }
      }
      else
      {
        // No registry here. RuntimeInformation.OSDescription is the distribution string on Linux
        // ("Linux 6.6.87.2-microsoft-standard-WSL2" under WSL2), so the log still identifies the box.
        logger.Info("ApplicationCore: OS description:   {0}", RuntimeInformation.OSDescription);
      }
      logger.Info("ApplicationCore: Runtime:          {0}", RuntimeInformation.FrameworkDescription);
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

      // Impersonation is a Windows logon session (LogonUser + a token on the calling thread). The
      // portable core runs everything as the current user, which is the same path the Windows
      // implementation takes for resources nobody registered a credential for; a Windows host
      // installs WindowsImpersonationService (MediaPortal.Common.Windows) during startup.
      logger.Debug("ApplicationCore: Registering IImpersonationService");
      ServiceRegistration.Set<IImpersonationService>(new CurrentUserImpersonationService());

      // Guarded, unlike every other registration here: the Windows hosts create their state service
      // before calling this method (they need it early, for the Initializing transition), so
      // overwriting here would replace the one they hold a reference to. This only fills the gap for
      // a host that has no Windows power backend.
      if (ServiceRegistration.Get<ISystemStateService>(false) == null)
      {
        logger.Debug("ApplicationCore: Registering ISystemStateService");
        ServiceRegistration.Set<ISystemStateService>(new Services.Runtime.PortableSystemStateService());
      }

      // The wake-up timer factory is the same seam one level down: the core hands out a timer that
      // fires at a due time, and a Windows host replaces it with one whose expiry also resumes the
      // machine. Unconditional (unlike the state service above) because nothing holds the instance -
      // the task scheduler asks for the factory the first time it wants to wake anything, which is
      // long after the host has had its say.
      logger.Debug("ApplicationCore: Registering ITaskWakeUpTimerFactory");
      ServiceRegistration.Set<ITaskWakeUpTimerFactory>(new Services.TaskScheduler.PortableTaskWakeUpTimerFactory());

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

      // Which assemblies MVC searches for API controllers is a decision of the code that composes the
      // server - it should not be a side effect of which of those assemblies happen to also register HTTP
      // middleware. The core's own controllers live in this assembly and are declared here, so that they
      // stay reachable even if the assembly stops registering ResourceAccessModule. (The controllers
      // themselves only exist where ASP.NET Core does.)
#if NET5_0_OR_GREATER
      ServiceRegistration.Get<IResourceServer>().AddHttpControllerAssembly(typeof(ServerInfoController));
#endif

      // Mounting resources into the local file system is done by a file system driver, which is a
      // per-platform technology (Dokan on Windows, FUSE on Linux, nothing on Android). The portable
      // core can only say "not here"; a Windows host replaces this with the Dokan-backed
      // implementation from MediaPortal.Common.Windows, next to GdiPlusImageTransformer.Register().
      logger.Debug("ApplicationCore: Registering IResourceMountingService");
      ServiceRegistration.Set<IResourceMountingService>(new UnsupportedResourceMountingService());

      logger.Debug("ApplicationCore: Registering IRemoteResourceInformationService");
      ServiceRegistration.Set<IRemoteResourceInformationService>(new RemoteResourceInformationService());

      logger.Debug("ApplicationCore: Registering IThumbnailGenerator service");
      ServiceRegistration.Set<IThumbnailGenerator>(new ThumbnailGenerator());

      logger.Debug("ApplicationCore: Registering IGenreConverter service");
      ServiceRegistration.Set<IGenreConverter>(new GenreConverter());

      logger.Debug("ApplicationCore: Registering IFanArtCache service");
      ServiceRegistration.Set<IFanArtCache>(new FanArtCache());

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

    /// <summary>
    /// Brings the core services down.
    /// </summary>
    /// <remarks>
    /// Each service is resolved optionally. This runs on the way out of a startup that may have failed
    /// partway - measured in the Linux container, where the HTTP server could not start at all - and a
    /// strict <see cref="ServiceRegistration.Get{T}()"/> here turned teardown into a second
    /// <c>FATAL</c> sitting below the failure that mattered. The launcher that calls this resolves its
    /// own services the same way, for the same reason.
    /// </remarks>
    public static void StopCoreServices()
    {
      ResourcePath.ClearResourceCache();
      ServiceRegistration.Get<IRemoteResourceInformationService>(false)?.Shutdown();
      ServiceRegistration.Get<IResourceMountingService>(false)?.Shutdown();
      ServiceRegistration.Get<IResourceServer>(false)?.Shutdown();
      ServiceRegistration.Get<ITaskScheduler>(false)?.Shutdown();
      ServiceRegistration.Get<IThreadPool>(false)?.Shutdown();
    }

    public static async System.Threading.Tasks.Task RegisterDefaultMediaItemAspectTypes()
    {
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();

      var knownAspects = new List<MediaItemAspectMetadata>
      {
        ProviderResourceAspect.Metadata,
        ImporterAspect.Metadata,
        DirectoryAspect.Metadata,
        MediaAspect.Metadata,
        VideoAspect.Metadata,
        GenreAspect.Metadata,
        VideoStreamAspect.Metadata,
        VideoAudioStreamAspect.Metadata,
        SubtitleAspect.Metadata,
        AudioAspect.Metadata,
        AudioAlbumAspect.Metadata,
        ImageAspect.Metadata,
        EpisodeAspect.Metadata,
        SeasonAspect.Metadata,
        SeriesAspect.Metadata,
        MovieAspect.Metadata,
        MovieCollectionAspect.Metadata,
        CompanyAspect.Metadata,
        PersonAspect.Metadata,
        CharacterAspect.Metadata,
        ThumbnailLargeAspect.Metadata,
        ExternalIdentifierAspect.Metadata,
        RelationshipAspect.Metadata,
        StubAspect.Metadata,
        ReimportAspect.Metadata
      };
      await miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(knownAspects);
    }

    public static void DisposeCoreServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("ApplicationCore: Removing IThumbnailGenerator service");
      ServiceRegistration.RemoveAndDispose<IThumbnailGenerator>();

      logger.Debug("ApplicationCore: Removing IGenreConverter service");
      ServiceRegistration.RemoveAndDispose<IGenreConverter>();

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
