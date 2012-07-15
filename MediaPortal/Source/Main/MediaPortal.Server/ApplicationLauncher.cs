#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Windows.Forms;
using CommandLine;
using MediaPortal.Backend;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
#if !DEBUG
using MediaPortal.Common.PathManager;
using System.IO;
#endif
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.Common;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Logging;

[assembly: CLSCompliant(true)]

namespace MediaPortal.Server
{
  public class ApplicationLauncher
  {
    protected static CommandLineOptions MpArgs = new CommandLineOptions();
    /// <summary>
    /// The main entry point for the MP2 server application.
    /// </summary>
    public static void Main(params string[] args)
    {
      // Parse Command Line options
      var parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, MpArgs, Console.Out))
        Environment.Exit(1);

      if (MpArgs.RunAsConsoleApp)
      {
        RunAsConsole();
      }
      else
      {
        var servicesToRun = new System.ServiceProcess.ServiceBase[] { new WindowsService() };
        System.ServiceProcess.ServiceBase.Run(servicesToRun);
      }
    }


    public static void Start()
    {
      Thread.CurrentThread.Name = "Main";

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-Server\Log");
#endif

      var systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(systemStateService);
      systemStateService.SwitchSystemState(SystemState.Initializing, false);

      try
      {
        ILogger logger = null;
        try
        {
          // Check if user wants to override the default Application Data location.
          ApplicationCore.RegisterVitalCoreServices(MpArgs.DataDirectory);
          ApplicationCore.RegisterCoreServices();
          logger = ServiceRegistration.Get<ILogger>();

#if !DEBUG
          IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
          logPath = pathManager.GetPath("<LOG>");
#endif

          BackendExtension.RegisterBackendServices();
        }
        catch (Exception e)
        {
          if (logger != null)
            logger.Critical("Error starting application", e);
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;

          BackendExtension.DisposeBackendServices();
          ApplicationCore.DisposeCoreServices();

          throw;
        }

        // Start the core
        logger.Debug("ApplicationLauncher: Starting core");

        try
        {
          var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          var pluginManager = ServiceRegistration.Get<IPluginManager>();
          pluginManager.Initialize();
          pluginManager.Startup(false);
          ApplicationCore.StartCoreServices();

          BackendExtension.StartupBackendServices();
          ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after backend services are running

          mediaAccessor.Initialize();

          systemStateService.SwitchSystemState(SystemState.Running, true);
          BackendExtension.ActivateImporterWorker();
            // To be done after default media item aspect types are present and when the system is running (other plugins might also install media item aspect types)
        }
        catch (Exception e)
        {
          logger.Critical("Error starting application", e);
          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;
          BackendExtension.DisposeBackendServices();
          ApplicationCore.DisposeCoreServices();
          systemStateService.SwitchSystemState(SystemState.Ending, false);
          throw; // needed to cancel OnStart of the Service
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
        throw; // needed to cancel OnStart of the Service
      }
    }


    public static void Stop()
    {
      var systemStateService = ServiceRegistration.Get<ISystemStateService>() as SystemStateService;
      try
      {
        systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
        ServiceRegistration.IsShuttingDown = true;
        // Block ServiceRegistration from trying to load new services in shutdown phase

        ServiceRegistration.Get<IMediaAccessor>().Shutdown();
        ServiceRegistration.Get<IPluginManager>().Shutdown();
        BackendExtension.ShutdownBackendServices();
        ApplicationCore.StopCoreServices();
      }
      catch (Exception ex)
      {
        //ServiceRegistration.Get<ILogger.Critical("Error stopping application", e);
#if DEBUG
        var log = new ConsoleLogger(LogLevel.All, false);
        log.Error(ex);
#else
        var pathManager = ServiceRegistration.Get<IPathManager>();
        var logPath = pathManager.GetPath("<LOG>");
        ServerCrashLogger crash = new ServerCrashLogger(logPath);
        crash.CreateLog(ex);
#endif
        systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
        ServiceRegistration.IsShuttingDown = true;
      }
      finally
      {
        BackendExtension.DisposeBackendServices();
        ApplicationCore.DisposeCoreServices();
        systemStateService.SwitchSystemState(SystemState.Ending, false);
      }
    }

    static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
    {
      MessageBox.Show(e.Exception.ToString(), "Unhandled Thread Exception");
      // here you can log the exception ...
    }

    static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      MessageBox.Show((e.ExceptionObject as Exception).ToString(), "Unhandled UI Exception");
      // here you can log the exception ...
    }

    protected static void RunAsConsole()
    {
      // 
      Application.ThreadException += new ThreadExceptionEventHandler(ApplicationThreadException);
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomainUnhandledException);  
      Start();

      try
      {
        Application.Run(new MainForm());
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("Error executing application", e);
      }

      Stop();
      Application.ThreadException -= new ThreadExceptionEventHandler(ApplicationThreadException);
      AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(CurrentDomainUnhandledException);

    }
  }
}