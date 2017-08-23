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

using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.PluginItemBuilders;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.FrontendServer;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.RemovableMedia;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.SystemResolver;
using MediaPortal.UI.Services.UiNotifications;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UI.Shares;
using MediaPortal.UI.Services.Players;
using MediaPortal.UI.Services.ServerCommunication;
using MediaPortal.UI.Services.Shares;
using MediaPortal.UI.Services.Workflow;
using MediaPortal.UI.Services.MediaManagement;

namespace MediaPortal.UI
{
  public class UiExtension
  {
    public static void RegisterUiServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("UiExtension: Registering INotificationService service");
      ServiceRegistration.Set<INotificationService>(new NotificationService());

      logger.Debug("UiExtension: Registering ISystemResolver service");
      ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

      logger.Debug("UiExtension: Registering IWorkflowManager service");
      ServiceRegistration.Set<IWorkflowManager>(new WorkflowManager());

      logger.Debug("UiExtension: Registering IPlayerManager service");
      ServiceRegistration.Set<IPlayerManager>(new PlayerManager());

      logger.Debug("UiExtension: Registering IPlayerContextManager service");
      ServiceRegistration.Set<IPlayerContextManager>(new PlayerContextManager());

      logger.Debug("UiExtension: Registering ILocalSharesManagement service");
      ServiceRegistration.Set<ILocalSharesManagement>(new LocalSharesManagement());

      logger.Debug("UiExtension: Registering IServerConnectionManager service");
      ServiceRegistration.Set<IServerConnectionManager>(new ServerConnectionManager());

      logger.Debug("UiExtension: Registering IUserManagement service");
      ServiceRegistration.Set<IUserManagement>(new UserManagement());

      logger.Debug("UiExtension: Registering IMediaItemAspectTypeRegistration service");
      ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(new MediaItemAspectTypeRegistration());

      logger.Debug("UiExtension: Registering IFrontendServer service");
      ServiceRegistration.Set<IFrontendServer>(new Services.FrontendServer.FrontendServer());

      logger.Debug("UiExtension: Registering IRemovableMediaTracker service");
      ServiceRegistration.Set<IRemovableMediaTracker>(new Services.RemovableMedia.RemovableMediaTracker());

      AdditionalPluginItemBuilders.Register();
    }

    public static void Startup()
    {
      RegisterDefaultCommandShortcuts();
      ServiceRegistration.Get<IServerConnectionManager>().Startup();
      ServiceRegistration.Get<IFrontendServer>().Startup();
      ServiceRegistration.Get<IRemovableMediaTracker>().Startup();
      ServiceRegistration.Get<INotificationService>().Startup();
    }

    public static void StopUiServices()
    {
      ServiceRegistration.Get<INotificationService>().Shutdown();
      ServiceRegistration.Get<IRemovableMediaTracker>().Shutdown();
      ServiceRegistration.Get<IServerConnectionManager>().Shutdown();
      ServiceRegistration.Get<IPlayerContextManager>().Shutdown();
      ServiceRegistration.Get<IPlayerManager>().CloseAllSlots();
    }

    public static void DisposeUiServices()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      // Reverse order than method RegisterUiServices()

      logger.Debug("UiExtension: Removing IRemovableMediaTracker service");
      ServiceRegistration.RemoveAndDispose<IRemovableMediaTracker>();

      logger.Debug("UiExtension: Removing IFrontendServer service");
      ServiceRegistration.RemoveAndDispose<IFrontendServer>();

      logger.Debug("UiExtension: Removing IMediaItemAspectTypeRegistration service");
      ServiceRegistration.RemoveAndDispose<IMediaItemAspectTypeRegistration>();

      logger.Debug("UiExtension: Removing IUserManagement service");
      ServiceRegistration.RemoveAndDispose<IUserManagement>();

      logger.Debug("UiExtension: Removing IServerConnectionManager service");
      ServiceRegistration.RemoveAndDispose<IServerConnectionManager>();

      logger.Debug("UiExtension: Removing ILocalSharesManagement service");
      ServiceRegistration.RemoveAndDispose<ILocalSharesManagement>();

      logger.Debug("UiExtension: Removing IPlayerContextManager service");
      ServiceRegistration.RemoveAndDispose<IPlayerContextManager>();

      logger.Debug("UiExtension: Removing IPlayerManager service");
      ServiceRegistration.RemoveAndDispose<IPlayerManager>();

      logger.Debug("UiExtension: Removing IWorkflowManager service");
      ServiceRegistration.RemoveAndDispose<IWorkflowManager>();

      logger.Debug("UiExtension: Removing ISystemResolver service");
      ServiceRegistration.RemoveAndDispose<ISystemResolver>();

      logger.Debug("UiExtension: Removing INotificationService service");
      ServiceRegistration.RemoveAndDispose<INotificationService>();
    }

    /// <summary>
    /// Registers default command shortcuts at the input manager.
    /// </summary>
    protected static void RegisterDefaultCommandShortcuts()
    {
      //TODO: Shortcut to handle the "Power" key, further shortcuts
    }
  }
}