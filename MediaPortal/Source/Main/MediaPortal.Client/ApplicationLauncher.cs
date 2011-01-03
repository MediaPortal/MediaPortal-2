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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Runtime;
using MediaPortal.UI;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Workflow;
#if DEBUG
using MediaPortal.Core.Services.Logging;
#else
using MediaPortal.UI.Services.Logging;
#endif
using MediaPortal.UI.Shares;
using CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Services.Runtime;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.Screens;

[assembly: CLSCompliant(true)]

namespace MediaPortal.Client
{
  internal static class ApplicationLauncher
  {
    private static SplashScreen CreateSplashScreen()
    {
      SplashScreen result = SplashScreen.Current;

      result.FadeInDuration = TimeSpan.FromMilliseconds(300);
      result.FadeOutDuration = TimeSpan.FromMilliseconds(200);
      result.SplashBackgroundImage = Image.FromFile(Path.Combine(
          Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]),
          "MP2 Client Splashscreen.jpg"));
      return result;
    }

    /// <summary>
    /// The main entry point for the MP 2 client application.
    /// </summary>
    private static void Main(params string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      SplashScreen splashScreen = CreateSplashScreen();
      splashScreen.ShowSplashScreen();

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, mpArgs, Console.Out))
        Environment.Exit(1);

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-Client\Log");
#endif

      SystemStateService systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(systemStateService);
      systemStateService.SwitchSystemState(SystemState.Initializing, false);

      try
      {
        ILogger logger = null;
        try
        {

          ApplicationCore.RegisterCoreServices();

          logger = ServiceRegistration.Get<ILogger>();

          IPathManager pathManager = ServiceRegistration.Get<IPathManager>();

          // Check if user wants to override the default Application Data location.
          if (!string.IsNullOrEmpty(mpArgs.DataDirectory))
            pathManager.SetPath("DATA", mpArgs.DataDirectory);

#if !DEBUG
          logPath = pathManager.GetPath("<LOG>");
#endif

          UiExtension.RegisterUiServices();
        }
        catch (Exception e)
        {
          if (logger != null)
            logger.Critical("Error starting application", e);
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;

          UiExtension.DisposeUiServices();
          ApplicationCore.DisposeCoreServices();

          throw;
        }

        // Start the core
        logger.Debug("ApplicationLauncher: Starting application");

        try
        {
          IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
          pluginManager.Initialize();
          pluginManager.Startup(false);
          ApplicationCore.StartCoreServices();

          ISkinEngine skinEngine = ServiceRegistration.Get<ISkinEngine>();
          IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
          IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();

          // We have to handle some dependencies here in the start order:
          // 1) After all plugins are loaded, the SkinEngine can initialize (=load all skin resources)
          // 2) After the skin resources are loaded, the workflow manager can initialize (=load its states and actions)
          // 3) Before the main window is shown, the splash screen should be hidden
          // 4) After the workflow states and actions are loaded, the main window can be shown
          // 5) After the skinengine triggers the first workflow state/startup screen, the default shortcuts can be registered
          mediaAccessor.Initialize(); // Independent from other services
          localSharesManagement.Initialize(); // After media accessor was initialized
          skinEngine.Initialize(); // 1)
          workflowManager.Initialize(); // 2)

          splashScreen.CloseSplashScreen(); // 3)

          skinEngine.Startup(); // 4)
          UiExtension.Startup(); // 5)

          ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after UI services are running

          systemStateService.SwitchSystemState(SystemState.Running, true);

          Application.Run();

          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true; // Block ServiceRegistration from trying to load new services in shutdown phase

          // 1) Stop UI extensions (Releases all active players, must be done before shutting down SE)
          // 2) Shutdown SkinEngine (Closes all screens, uninstalls background manager, stops render thread)
          // 3) Shutdown WorkflowManager (Disposes all models)
          // 4) Shutdown PluginManager (Shuts down all plugins)
          // 5) Remove all services
          UiExtension.StopUiServices();
          skinEngine.Shutdown();
          workflowManager.Shutdown();
          pluginManager.Shutdown();
          mediaAccessor.Shutdown();
          localSharesManagement.Shutdown();
          ApplicationCore.StopCoreServices();
        }
        catch (Exception e)
        {
          logger.Critical("Error executing application", e);
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;
        }
        finally
        {
          UiExtension.DisposeUiServices();
          ApplicationCore.DisposeCoreServices();

          systemStateService.SwitchSystemState(SystemState.Ending, false);
        }
      }
      catch (Exception ex)
      {
#if DEBUG
        ConsoleLogger log = new ConsoleLogger(LogLevel.All, false);
        log.Error(ex);
#else
        UiCrashLogger crash = new UiCrashLogger(logPath);
        crash.CreateLog(ex);
#endif
        systemStateService.SwitchSystemState(SystemState.Ending, false);
        Application.Exit();
      }
    }
  }
}