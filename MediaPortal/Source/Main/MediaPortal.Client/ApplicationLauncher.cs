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
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Logging;
using MediaPortal.UI;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Workflow;
#if !DEBUG
using System.Drawing;
using System.IO;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Settings;
#endif
using MediaPortal.UI.Shares;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Common;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Events;
using MediaPortal.Utilities.Process;
using MediaPortal.Utilities.Screens;

[assembly: CLSCompliant(true)]

namespace MediaPortal.Client
{
  /// <summary>
  /// The main class for the MediaPortal 2 client.
  /// </summary>
  internal static class ApplicationLauncher
  {
    #region Consts

    // Unique id for global mutex - Global prefix means it is global to the entire machine
    private const string MUTEX_ID = @"Global\{6CE68D44-7173-47C6-A5F2-C01D73B2F903}";

    #endregion

    #region Static fields

    private static Mutex _mutex = null;
    private static DelayedEvent _focusTimer = null;
    private static DelayedEvent _deactivatedEvent = null;
    private static IpcServer _ipcServer;

    #endregion

#if !DEBUG
    private static SplashScreen CreateSplashScreen()
    {
      StartupSettings startupSettings = ServiceRegistration.Get<ISettingsManager>().Load<StartupSettings>();

      string startupPath = Path.GetDirectoryName(Application.ExecutablePath);
      List<string> testFileNames = new List<string>();
      if (!string.IsNullOrEmpty(startupSettings.AlternativeSplashScreen))
        testFileNames.Add(startupSettings.AlternativeSplashScreen);

      testFileNames.Add("MP2 Client Splashscreen.jpg");

      Image image = null;
      foreach (string testFileName in testFileNames)
      {
        try
        {
          string fileName = Path.Combine(startupPath, testFileName);
          image = Image.FromFile(fileName);
          break;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("SplashScreen: Error loading startup image '{0}'", ex, testFileName);
        }
      }

      SplashScreen result = new SplashScreen
          {
            StartupScreen = startupSettings.StartupScreenNum,
            ScaleToFullscreen = true,
            FadeInDuration = TimeSpan.FromMilliseconds(300),
            FadeOutDuration = TimeSpan.FromMilliseconds(200),
            SplashBackgroundImage = image
          };
      return result;
    }
#endif

    /// <summary>
    /// The main entry point for the MP2 client application.
    /// </summary>
    private static void Main(params string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args, mpOptions, () => Environment.Exit(1));

      // Check if another instance is already running
      if (SingleInstanceHelper.IsAlreadyRunning(MUTEX_ID, out _mutex))
      {
        _mutex = null;
        // Set focus on previously running app
        SingleInstanceHelper.SwitchToCurrentInstance(SingleInstanceHelper.SHOW_MP2_CLIENT_MESSAGE);
        // Stop current instance
        Console.Out.WriteLine("Application already running.");
        Environment.Exit(2);
      }

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-Client\Log");
#endif

      Application.ThreadException += LauncherExceptionHandling.Application_ThreadException;
      AppDomain.CurrentDomain.UnhandledException += LauncherExceptionHandling.CurrentDomain_UnhandledException;

      SystemStateService systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(systemStateService);
      systemStateService.SwitchSystemState(SystemState.Initializing, false);

      try
      {
#if !DEBUG
        SplashScreen splashScreen = null;
#endif
        ILogger logger = null;
        try
        {
          // Check if user wants to override the default Application Data location.
          ApplicationCore.RegisterVitalCoreServices(true, mpOptions.DataDirectory);

#if !DEBUG
          splashScreen = CreateSplashScreen();
          splashScreen.ShowSplashScreen();
#endif

          ApplicationCore.RegisterCoreServices();

          logger = ServiceRegistration.Get<ILogger>();

#if !DEBUG
          IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
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

#if !DEBUG
          splashScreen.CloseSplashScreen(); // 3)
#endif

          skinEngine.Startup(); // 4)
          UiExtension.Startup(); // 5)

          ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after UI services are running

          _ipcServer = new IpcServer("Client");
          _ipcServer.CustomShutdownCallback = () =>
          {
            ServiceRegistration.Get<IScreenControl>().Shutdown();
            return true;
          };
          try
          {
            _ipcServer.Open();
          }
          catch (Exception ipcEx)
          {
            logger.Error(ipcEx);
          }
          systemStateService.SwitchSystemState(SystemState.Running, true);

          if (mpOptions.AutoStart)
            StartFocusKeeper();

          Application.Run();
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true; // Block ServiceRegistration from trying to load new services in shutdown phase

          // 1) Stop UI extensions (Releases all active players, must be done before shutting down SE)
          // 2) Shutdown SkinEngine (Closes all screens, uninstalls background manager, stops render thread)
          // 3) Shutdown WorkflowManager (Disposes all models)
          // 4) Shutdown ImporterWorker
          // 5) Shutdown PluginManager (Shuts down all plugins)
          // 6) Remove all services
          UiExtension.StopUiServices();
          skinEngine.Shutdown();
          workflowManager.Shutdown();
          ServiceRegistration.Get<IImporterWorker>().Shutdown();
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
          if (_ipcServer != null)
          {
            _ipcServer.Close();
          }
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

        // Release mutex for single instance
        if (_mutex != null)
          _mutex.ReleaseMutex();

        Application.Exit();
      }
    }

    #region Startup focus workaround

    /// <summary>
    /// Helper method to prevent stealing focus of main window by other processes during auto start phase of Windows.
    /// </summary>
    private static void StartFocusKeeper()
    {
      Form form = ServiceRegistration.Get<IScreenControl>() as Form;
      if (form != null)
      {
        ServiceRegistration.Get<ILogger>().Info("ApplicationLauncher: Autostart by Windows. Starting Focus Keeper.");
        // Max. seconds after startup of MainForm.
        _focusTimer = new DelayedEvent(10000);
        _focusTimer.OnEventHandler += StopFocusKeeper;
        _focusTimer.EnqueueEvent(null, EventArgs.Empty);

        // Collects all deactivations and executes the handler after 500 ms.
        // This is especially required, because in deactivate event handler it's not possible to reactivate the form.
        _deactivatedEvent = new DelayedEvent(500);
        _deactivatedEvent.OnEventHandler += PreventDeactivate;
        form.Deactivate += EnqueueDeactivationEvent;
      }
    }

    private static void EnqueueDeactivationEvent(object sender, EventArgs e)
    {
      _deactivatedEvent.EnqueueEvent(sender, e);
    }

    private static void StopFocusKeeper(object sender, EventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Info("ApplicationLauncher: Stopping Focus Keeper.");

      Form form = ServiceRegistration.Get<IScreenControl>() as Form;
      if (form != null)
        form.Deactivate -= EnqueueDeactivationEvent;

      _deactivatedEvent.Dispose();
      _deactivatedEvent = null;
      _focusTimer.Dispose();
      _focusTimer = null;
    }

    private static void PreventDeactivate(object sender, EventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Info("ApplicationLauncher: Window got deactivated. Reactivate it again.");
      Form form = ServiceRegistration.Get<IScreenControl>() as Form;
      if (form != null)
        form.SafeActivate();
    }

    #endregion
  }
}
