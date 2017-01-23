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
      ServiceRegistration.Get<IMediaLibrary>().Startup();
      ServiceRegistration.Get<IClientManager>().Startup();
      ServiceRegistration.Get<IBackendServer>().Startup();
      ((UserProfileDataManagement)ServiceRegistration.Get<IUserProfileDataManagement>()).Startup();
    }

    /// <summary>
    /// To be called after all media item aspects are present.
    /// </summary>
    public static void ActivateImporterWorker()
    {
      ServiceRegistration.Get<IMediaLibrary>().ActivateImporterWorker();
    }

    public static void ShutdownBackendServices()
    {
      ((UserProfileDataManagement)ServiceRegistration.Get<IUserProfileDataManagement>()).Shutdown();
      ServiceRegistration.Get<IClientManager>().Shutdown();
      ServiceRegistration.Get<IMediaLibrary>().Shutdown();
      ServiceRegistration.Get<IBackendServer>().Shutdown();
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
