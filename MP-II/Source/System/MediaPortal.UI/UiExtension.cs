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

using MediaPortal.Builders;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;
using MediaPortal.Services.Localization;
using MediaPortal.Services.Players;
using MediaPortal.Services.ThumbnailGenerator;
using MediaPortal.Services.UserManagement;
using MediaPortal.Services.Workflow;
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

      logger.Debug("UiExtension: Registering UserService service");
      ServiceScope.Add<IUserService>(new UserService());

      logger.Debug("UiExtension: Registering StringManager");
      ServiceScope.Add<ILocalization>(new StringManager());

      logger.Debug("UiExtension: Registering ThumbnailGenerator");
      ServiceScope.Add<IAsyncThumbnailGenerator>(new ThumbnailGenerator());

      AdditionalUiBuilders.Register();
    }

    public static void StopAll()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseAllSlots();
    }

    public static void DisposeUiServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("UiExtension: Removing IWorkflowManager service");
      ServiceScope.RemoveAndDispose<IWorkflowManager>();

      logger.Debug("UiExtension: Removing IPlayerContextManager service");
      ServiceScope.RemoveAndDispose<IPlayerContextManager>();

      logger.Debug("UiExtension: Removing IPlayerManager service");
      ServiceScope.RemoveAndDispose<IPlayerManager>();

      logger.Debug("UiExtension: Removing UserService service");
      ServiceScope.RemoveAndDispose<IUserService>();

      logger.Debug("UiExtension: Removing StringManager");
      ServiceScope.RemoveAndDispose<ILocalization>();

      logger.Debug("UiExtension: Removing ThumbnailGenerator");
      ServiceScope.RemoveAndDispose<IAsyncThumbnailGenerator>();
    }

    /// <summary>
    /// Registers default command shortcuts at the workflow manager. This must be done AFTER the workflow manager
    /// switched to its initial state.
    /// </summary>
    public static void RegisterDefaultCommandShortcuts()
    {
      //TODO: Shortcut to handle the "Power" key, further shortcuts
    }
  }
}