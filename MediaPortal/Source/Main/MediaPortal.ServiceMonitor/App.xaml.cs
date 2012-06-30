#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.ServiceMonitor.UPNP;
using MediaPortal.ServiceMonitor.ViewModel;

namespace MediaPortal.ServiceMonitor
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {

    #region OnStartUp
    /// <summary>
    /// Either shows the application's main window or
    /// inits the application in the system tray.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse Command Line options
      var mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args.Args, mpArgs, Console.Out))
        Environment.Exit(1);

      //make sure we're properly handling exceptions
      DispatcherUnhandledException += OnUnhandledException;
      AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

      var systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(systemStateService);
      systemStateService.SwitchSystemState(SystemState.Initializing, false);

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-ServiveMonitor\Log");
#endif
      try
      {
        ILogger logger = null;
        try
        {
          ApplicationCore.RegisterCoreServices();
          logger = ServiceRegistration.Get<ILogger>();
          //ApplicationCore.StartCoreServices();

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

        var appController = new AppController();
        ServiceRegistration.Set<IAppController>(appController);

        // Start the core
        logger.Debug("Starting application");
        try
        {
          ServiceRegistration.Get<IServerConnectionManager>().Startup();
          if (mpArgs.IsMinimized)
          {
            appController.MinimizeToTray();
          }
          else
          {
            appController.ShowMainWindow();
          }
          
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
        //we are infact running on a dispatcher thread, but better safe than sorry
        e.Dispatcher.Invoke(new Action(() => OnUnhandledException(sender, e)), DispatcherPriority.Normal, null);
        return;
      }

      MessageBox.Show(e.Exception.ToString(), "Unhandled Exception");
      // here you can log the exception ...

      if (ServiceRegistration.IsRegistered<ILogger>())
      {
        ServiceRegistration.Get<ILogger>().Critical("An Unhandled Exception occured", e.Exception);
      }
    }

    /// <summary>
    /// Gets any unhandled exceptions.
    /// </summary>
    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      MessageBox.Show(((Exception) e.ExceptionObject).ToString(), "Unhandled UI Exception");
      // here you can log the exception ...
      if (ServiceRegistration.IsRegistered<ILogger>())
      {
        ServiceRegistration.Get<ILogger>().Critical("An Unhandled Cuurent Domain Exception occured", (Exception)e.ExceptionObject);
      }
    }

    #endregion
  }
}
