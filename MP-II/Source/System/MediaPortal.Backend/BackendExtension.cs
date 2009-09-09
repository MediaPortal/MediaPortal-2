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

using MediaPortal.BackendServer;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Database;
using MediaPortal.MediaLibrary;
using MediaPortal.Services.Database;

namespace MediaPortal
{
  public class BackendExtension
  {
    public static void RegisterBackendServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("BackendExtension: Registering DatabaseManager");
      ServiceScope.Add<IDatabaseManager>(new DatabaseManager());

      logger.Debug("BackendExtension: Registering MediaLibrary");
      ServiceScope.Add<IMediaLibrary>(new Services.MediaLibrary.MediaLibrary());

      logger.Debug("BackendExtension: Registering BackendServer");
      ServiceScope.Add<IBackendServer>(new Services.BackendServer.BackendServer());
    }

    /// <summary>
    /// To be called when the database service is present.
    /// </summary>
    public static void StartupBackendServices()
    {
      ServiceScope.Get<IDatabaseManager>().Startup();
      ServiceScope.Get<IMediaLibrary>().Startup();
      ServiceScope.Get<IBackendServer>().Startup();
    }

    public static void ShutdownBackendServices()
    {
      ServiceScope.Get<IMediaLibrary>().Shutdown();
      ServiceScope.Get<IBackendServer>().Shutdown();
    }

    public static void DisposeBackendServices()
    {
      ServiceScope.RemoveAndDispose<IBackendServer>();
      ServiceScope.RemoveAndDispose<IMediaLibrary>();
      ServiceScope.RemoveAndDispose<IDatabaseManager>();
    }
  }
}
