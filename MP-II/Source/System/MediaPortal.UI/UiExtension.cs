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
using MediaPortal.Builders;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;
using MediaPortal.ServerConnection;
using MediaPortal.Services.Players;
using MediaPortal.Services.ServerConnection;
using MediaPortal.Services.Shares;
using MediaPortal.Services.ThumbnailGenerator;
using MediaPortal.Services.UserManagement;
using MediaPortal.Services.Workflow;
using MediaPortal.Shares;
using MediaPortal.Thumbnails;
using MediaPortal.UserManagement;

namespace MediaPortal
{
  public class UiExtension
  {
    public static void RegisterUiServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("UiExtension: Registering IWorkflowManager service");
      ServiceScope.Add<IWorkflowManager>(new WorkflowManager());

      logger.Debug("UiExtension: Registering IPlayerManager service");
      ServiceScope.Add<IPlayerManager>(new PlayerManager());

      logger.Debug("UiExtension: Registering IPlayerContextManager service");
      ServiceScope.Add<IPlayerContextManager>(new PlayerContextManager());

      logger.Debug("UiExtension: Registering IUserService service");
      ServiceScope.Add<IUserService>(new UserService());

      logger.Debug("UiExtension: Registering IAsyncThumbnailGenerator service");
      ServiceScope.Add<IAsyncThumbnailGenerator>(new ThumbnailGenerator());

      logger.Debug("UiExtension: Registering ILocalSharesManagement service");
      ServiceScope.Add<ILocalSharesManagement>(new LocalSharesManagement());

      logger.Debug("UiExtension: Registering IServerConnectionManager service");
      ServiceScope.Add<IServerConnectionManager>(new ServerConnectionManager());

      AdditionalUiBuilders.Register();
    }

    public static void StopAll()
    {
      IServerConnectionManager serverConnectionManager = ServiceScope.Get<IServerConnectionManager>();
      serverConnectionManager.Shutdown();

      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.Shutdown();

      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseAllSlots();
    }

    public static void DisposeUiServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      // Reverse order than method RegisterUiServices()

      logger.Debug("UiExtension: Removing IServerConnectionManager service");
      ServiceScope.RemoveAndDispose<IServerConnectionManager>();

      logger.Debug("UiExtension: Removing ILocalSharesManagement service");
      ServiceScope.RemoveAndDispose<ILocalSharesManagement>();

      logger.Debug("UiExtension: Removing IAsyncThumbnailGenerator service");
      ServiceScope.RemoveAndDispose<IAsyncThumbnailGenerator>();

      logger.Debug("UiExtension: Removing IUserService service");
      ServiceScope.RemoveAndDispose<IUserService>();

      logger.Debug("UiExtension: Removing IPlayerContextManager service");
      ServiceScope.RemoveAndDispose<IPlayerContextManager>();

      logger.Debug("UiExtension: Removing IPlayerManager service");
      ServiceScope.RemoveAndDispose<IPlayerManager>();

      logger.Debug("UiExtension: Removing IWorkflowManager service");
      ServiceScope.RemoveAndDispose<IWorkflowManager>();
    }

    /// <summary>
    /// Registers default command shortcuts at the input manager.
    /// </summary>
    protected static void RegisterDefaultCommandShortcuts()
    {
      //TODO: Shortcut to handle the "Power" key, further shortcuts
    }

    public static void Startup()
    {
      RegisterDefaultCommandShortcuts();
      ServiceScope.Get<IServerConnectionManager>().Startup();
    }
  }
}
