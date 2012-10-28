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
#if !DEBUG
using System.IO;
using MediaPortal.Common.PathManager;
#endif
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.Common.SystemResolver;
using MediaPortal.ServiceMonitor.UPNP;
using MediaPortal.ServiceMonitor.Utilities;
using MediaPortal.ServiceMonitor.ViewModel;
using MediaPortal.Utilities.SystemAPI;
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
    private static bool _hasHandle = false;

    #endregion

    #region Ctor

    public App()
    {
      if (IsAlreadyRunning())
      {
        //set focus on previously running app
        SwitchToCurrentInstance();
        throw new ApplicationException("Application already running");
      }
    }

    #endregion

    #region Single Application

    /// <summary>
    /// check if our application is running or not
    /// </summary>
    /// <returns>returns true if already running</returns>
    private static bool IsAlreadyRunning()
    {
      // Allow only one instance
      _mutex = new Mutex(false, MUTEX_ID);

      var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
          MutexRights.FullControl, AccessControlType.Allow);
      var securitySettings = new MutexSecurity();
      securitySettings.AddAccessRule(allowEveryoneRule);
      _mutex.SetAccessControl(securitySettings);

      _hasHandle = false;
      try
      {
        // Check if we can start the application
        _hasHandle = _mutex.WaitOne(500, false);
      }
      catch (AbandonedMutexException)
      {
        // The mutex was abandoned in another process, it will still get aquired
        _hasHandle = true;
      }
      return !_hasHandle;
    }

    /// <summary>
    /// Switch To Current Instance of the Application
    /// </summary>
    private static void SwitchToCurrentInstance()
    {
      // send our Win32 message to make the currently running instance
      // jump on top of all the other windows
      WindowsAPI.PostMessage(
          (IntPtr) WinApi.HWND_BROADCAST,
          WinApi.MP2_SHOWME,
          IntPtr.Zero,
          IntPtr.Zero);
    }
    
    #endregion

    #region OnStartUp

    /// <summary>
    /// Either shows the application's main window or inits the application in the system tray.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs args)
    {
      Thread.CurrentThread.Name = "Main";
      Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

     // Parse Command Line options
      var mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args.Args, mpArgs, Console.Out))
        Environment.Exit(1);

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

        var appController = new AppController();
        ServiceRegistration.Set<IAppController>(appController);
        
        // Start the application
        logger.Debug("Starting application");
        try
        {
          ServiceRegistration.Get<IServerConnectionManager>().Startup();
          appController.StartUp(mpArgs.IsMinimized);
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

      if (_hasHandle)
        _mutex.ReleaseMutex();
    }

    #endregion
    
    #region Unhandled Exceptions

    /// <summary>
    /// Gets any unhandled exceptions.
    /// </summary>
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      if (!e.Dispatcher.CheckAccess())
      { // We are not running in the dispatcher thread, schedule execution in dispatcher thread
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
