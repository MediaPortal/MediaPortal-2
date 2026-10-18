#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.BackendServer;
using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Backend.Services.UserProfileDataManagement;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaManagement;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Backend
{
  public class BackendExtension
  {
    public static void RegisterBackendServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("BackendExtension: Registering ISystemResolver service");
      ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

      logger.Debug("BackendExtension: Registering IDatabaseManager service");
      ServiceRegistration.Set<IDatabaseManager>(new DatabaseManager());

      logger.Debug("BackendExtension: Registering IMediaLibrary service");
      ServiceRegistration.Set<IMediaLibrary>(new Services.MediaLibrary.MediaLibrary());

      logger.Debug("BackendExtension: Registering IMediaItemAspectTypeRegistration service");
      ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(new MediaItemAspectTypeRegistration());

      logger.Debug("BackendExtension: Registering IRelationshipTypeRegistration service");
      ServiceRegistration.Set<IRelationshipTypeRegistration>(new RelationshipTypeRegistration());

      logger.Debug("BackendExtension: Registering IBackendServer service");
      ServiceRegistration.Set<IBackendServer>(new Services.BackendServer.BackendServer());

      logger.Debug("BackendExtension: Registering IClientManager service");
      ServiceRegistration.Set<IClientManager>(new Services.ClientCommunication.ClientManager());

      logger.Debug("BackendExtension: Registering IUserProfileDataManagement service");
      ServiceRegistration.Set<IUserProfileDataManagement>(new UserProfileDataManagement());
    }

    /// <summary>
    /// To be called when the database service is present.
    /// </summary>
    public static void StartupBackendServices()
    {
      ServiceRegistration.Get<IDatabaseManager>().Startup();
      ServiceRegistration.Get<IDatabaseManager>().UpgradeDatabase();
      ServiceRegistration.Get<IMediaLibrary>().Startup();
      ApplicationCore.RegisterDefaultMediaItemAspectTypes().Wait(); // Must be done before a client starts doing searches or exceptions can happen for missing Aspects
      ((UserProfileDataManagement)ServiceRegistration.Get<IUserProfileDataManagement>()).Startup();
      ServiceRegistration.Get<IClientManager>().Startup();
      ServiceRegistration.Get<IBackendServer>().Startup();
    }

    /// <summary>
    /// To be called when the database service is present.
    /// </summary>
    public static void MigrateDatabaseData()
    {
      ServiceRegistration.Get<IDatabaseManager>().MigrateDatabaseData();
    }

    /// <summary>
    /// To be called after all media item aspects are present.
    /// </summary>
    public static void ActivateImporterWorker()
    {
      ServiceRegistration.Get<IMediaLibrary>().ActivateImporterWorker();
    }

    /// <summary>
    /// Shuts the backend services down.
    /// </summary>
    /// <remarks>
    /// A service that is not registered is skipped instead of throwing. Startup registers these
    /// one by one, so when an early one fails - the database could not be opened, for instance - the
    /// later ones were never built, and asking for them here threw a <c>ServiceNotFoundException</c>
    /// that reached the launcher as a second <c>FATAL</c>. That buried the actual failure two lines
    /// above it, which is exactly what a shutdown path must not do. Measured in the Linux container
    /// run (ROADMAP items i and j): the database failing to open produced this exception on top.
    /// </remarks>
    public static void ShutdownBackendServices()
    {
      // The logger is optional here. On the failure path this runs after the core services have been
      // taken apart, and in the container's failed startup the ILogger service was already gone - so
      // asking for it strictly replaced one shutdown exception with another. Measured.
      ILogger logger = ServiceRegistration.Get<ILogger>(false) ?? new NoLogger();

      // Ordered as before: user profiles first, then clients, then the library that uses them, then
      // the server that hosts them.
      var userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>(false) as UserProfileDataManagement;
      if (userProfileDataManagement != null)
        userProfileDataManagement.Shutdown();
      else
        logger.Debug("BackendExtension: Skipping shutdown of IUserProfileDataManagement, the service is not registered");

      var clientManager = ServiceRegistration.Get<IClientManager>(false);
      if (clientManager != null)
        clientManager.Shutdown();
      else
        logger.Debug("BackendExtension: Skipping shutdown of IClientManager, the service is not registered");

      var mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary != null)
        mediaLibrary.Shutdown();
      else
        logger.Debug("BackendExtension: Skipping shutdown of IMediaLibrary, the service is not registered");

      var backendServer = ServiceRegistration.Get<IBackendServer>(false);
      if (backendServer != null)
        backendServer.Shutdown();
      else
        logger.Debug("BackendExtension: Skipping shutdown of IBackendServer, the service is not registered");
    }

    public static void DisposeBackendServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("BackendExtension: Removing IUserProfileDataManagement service");
      ServiceRegistration.RemoveAndDispose<IUserProfileDataManagement>();

      logger.Debug("BackendExtension: Removing IClientManager service");
      ServiceRegistration.RemoveAndDispose<IClientManager>();

      logger.Debug("BackendExtension: Removing IBackendServer service");
      ServiceRegistration.RemoveAndDispose<IBackendServer>();

      logger.Debug("BackendExtension: Removing IMediaItemAspectTypeRegistration service");
      ServiceRegistration.RemoveAndDispose<IMediaItemAspectTypeRegistration>();

      logger.Debug("BackendExtension: Removing IMediaLibrary service");
      ServiceRegistration.RemoveAndDispose<IMediaLibrary>();

      logger.Debug("BackendExtension: Removing IDatabaseManager service");
      ServiceRegistration.RemoveAndDispose<IDatabaseManager>();

      logger.Debug("BackendExtension: Removing ISystemResolver service");
      ServiceRegistration.RemoveAndDispose<ISystemResolver>();
    }
  }
}
