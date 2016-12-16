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
#if !DEBUG
using System.IO;
using MediaPortal.Common.PathManager;
#endif
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.Common.SystemResolver;
using MediaPortal.ServiceMonitor.UPNP;
using MediaPortal.ServiceMonitor.ViewModel;
using MediaPortal.Utilities.Process;
using Localization = MediaPortal.ServiceMonitor.Utilities.Localization;

namespace MediaPortal.ServiceMonitor
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App
  {
    #region Consts

    // Unique id for global mutex - Global prefix means it is global to the entire machine
    private const string MUTEX_ID = @"Global\{721780D6-6905-4CFA-A69B-5A1594C928D0}";

    #endregion

    #region Static fields

    private static Mutex _mutex = null;

    #endregion

    #region fileds
    private static IpcServer _ipcServer;
    #endregion

    #region OnStartUp

    /// <summary>
    /// Either shows the application's main window or inits the application in the system tray.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs args)
    {
      Thread.CurrentThread.Name = "Main";
      Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args.Args, mpOptions, () => Environment.Exit(1));

      // Check if another instance is already running
      // If new instance was created by UacHelper previous one, assume that previous one is already closed.
      if (SingleInstanceHelper.IsAlreadyRunning(MUTEX_ID, out _mutex))
      {
        _mutex = null;
        // Set focus on previously running app
        SingleInstanceHelper.SwitchToCurrentInstance(SingleInstanceHelper.SHOW_MP2_SERVICEMONITOR_MESSAGE );
        // Stop current instance
        Console.Out.WriteLine("Application already running.");
        Environment.Exit(2);
      }

      // Make sure we're properly handling exceptions
      DispatcherUnhandledException += OnUnhandledException;
      AppDomain.CurrentDomain.UnhandledException += LauncherExceptionHandling.CurrentDomain_UnhandledException;

      var systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(systemStateService);
      systemStateService.SwitchSystemState(SystemState.Initializing, false);

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-ServiceMonitor\Log");
#endif

      try
      {
        ILogger logger = null;
        try
        {
          ApplicationCore.RegisterVitalCoreServices(true);
          ApplicationCore.RegisterCoreServices();
          logger = ServiceRegistration.Get<ILogger>();

          logger.Debug("Starting Localization");
          Localization localization = new Localization();
          ServiceRegistration.Set<ILocalization>(localization);
          localization.Startup();

          //ApplicationCore.StartCoreServices();

          logger.Debug("UiExtension: Registering ISystemResolver service");
          ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

          logger.Debug("UiExtension: Registering IServerConnectionManager service");
          ServiceRegistration.Set<IServerConnectionManager>(new ServerConnectionManager());

#if !DEBUG
          logPath = ServiceRegistration.Get<IPathManager>().GetPath("<LOG>");
#endif
        }
        catch (Exception e)
        {
          if (logger != null)
            logger.Critical("Error starting application", e);

          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;

          ApplicationCore.DisposeCoreServices();

          throw;
        }

        InitIpc();

        var appController = new AppController();
        ServiceRegistration.Set<IAppController>(appController);

        // Start the application
        logger.Debug("Starting application");
        try
        {
          ServiceRegistration.Get<IServerConnectionManager>().Startup();
          appController.StartUp(mpOptions);
        }
        catch (Exception e)
        {
          logger.Critical("Error executing application", e);
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;
        }
      }
      catch (Exception ex)
      {
#if DEBUG
        var log = new ConsoleLogger(LogLevel.All, false);
        log.Error(ex);
#else
        ServerCrashLogger crash = new ServerCrashLogger(logPath);
        crash.CreateLog(ex);
#endif
        systemStateService.SwitchSystemState(SystemState.Ending, false);
        Current.Shutdown();
      }
    }

    #endregion

    #region OnExit

    private void OnExit(object sender, ExitEventArgs e)
    {
      var systemStateService = (SystemStateService) ServiceRegistration.Get<ISystemStateService>();

      systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
      ServiceRegistration.IsShuttingDown = true;
      ServiceRegistration.Get<IServerConnectionManager>().Shutdown();
      //ApplicationCore.StopCoreServices();
      ApplicationCore.DisposeCoreServices();
      systemStateService.SwitchSystemState(SystemState.Ending, false);

      CloseIpc();

      if (_mutex != null)
        _mutex.ReleaseMutex();
    }

    #endregion

    #region IPC
    private void InitIpc()
    {
      if (_ipcServer != null)
        return;
      ServiceRegistration.Get<ILogger>().Debug("Initializing IPC");
      try
      {
        _ipcServer = new IpcServer("ServiceMonitor");
        _ipcServer.CustomShutdownCallback = () =>
        {
          Dispatcher.BeginInvoke(new Action(Shutdown));
          return true;
        };
        _ipcServer.Open();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex);
      }
    }

    private void CloseIpc()
    {
      if (_ipcServer == null)
        return;
      try
      {
        _ipcServer.Close();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex);
      }
      _ipcServer = null;
    }
    #endregion

    #region Unhandled Exceptions

    /// <summary>
    /// Gets any unhandled exceptions.
    /// </summary>
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      if (!e.Dispatcher.CheckAccess())
      {
        // We are not running in the dispatcher thread, schedule execution in dispatcher thread
        e.Dispatcher.Invoke(new Action(() => HandleUnhandledException(e)), DispatcherPriority.Normal, null);
        return;
      }
      HandleUnhandledException(e);
    }

    private static void HandleUnhandledException(DispatcherUnhandledExceptionEventArgs e)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Critical("An Unhandled Exception occured", e.Exception);

      MessageBox.Show(e.Exception.ToString(), "Unhandled Exception");
    }

    #endregion
  }
}