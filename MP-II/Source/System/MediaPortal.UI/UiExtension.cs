using System;
using MediaPortal.Builders;
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;
using MediaPortal.Services.InputManager;
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

      logger.Debug("UiExtension: Registering IInputMapper service");
      ServiceScope.Add<IInputMapper>(new InputMapper());

      logger.Debug("UiExtension: Registering IWorkflowManager service");
      ServiceScope.Add<IWorkflowManager>(new WorkflowManager());

      logger.Debug("UiExtension: Registering IPlayerManager service");
      ServiceScope.Add<IPlayerManager>(new PlayerManager());

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
      playerManager.ReleaseAllPlayers();
    }

    public static void DisposeUiServices()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("UiExtension: Removing IInputMapper service");
      ServiceScope.RemoveAndDispose<IInputMapper>();

      logger.Debug("UiExtension: Removing IWorkflowManager service");
      ServiceScope.RemoveAndDispose<IWorkflowManager>();

      logger.Debug("UiExtension: Removing IPlayerManager service");
      ServiceScope.RemoveAndDispose<IPlayerManager>();

      logger.Debug("UiExtension: Removing UserService service");
      ServiceScope.RemoveAndDispose<IUserService>();

      logger.Debug("UiExtension: Removing StringManager");
      ServiceScope.RemoveAndDispose<ILocalization>();

      logger.Debug("UiExtension: Removing ThumbnailGenerator");
      ServiceScope.RemoveAndDispose<IAsyncThumbnailGenerator>();
    }
  }
}