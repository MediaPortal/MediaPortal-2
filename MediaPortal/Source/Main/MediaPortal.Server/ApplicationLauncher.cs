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
using System.Windows.Forms;
using MediaPortal.Backend;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.PluginManager;
#if DEBUG
using MediaPortal.Common.Services.Logging;
#else
using MediaPortal.Common.PathManager;
using MediaPortal.Backend.Services.Logging;
using System.IO;
#endif
using MediaPortal.Common.Services.Runtime;
using CommandLine;
using MediaPortal.Common;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Logging;

[assembly: CLSCompliant(true)]

namespace MediaPortal.Server
{
  internal static class ApplicationLauncher
  {
    /// <summary>
    /// The main entry point for the MP 2 server application.
    /// </summary>
    private static void Main(params string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, mpArgs, Console.Out))
        Environment.Exit(1);

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-Server\Log");
#endif

      Application.ThreadException += Application_ThreadException;
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


      SystemStateService systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(systemStateService);
      systemStateService.SwitchSystemState(SystemState.Initializing, false);

      try
      {
        ILogger logger = null;
        try
        {
          // Check if user wants to override the default Application Data location.
          ApplicationCore.RegisterCoreServices(mpArgs.DataDirectory);
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
          IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
          pluginManager.Initialize();
          pluginManager.Startup(false);
          ApplicationCore.StartCoreServices();

          BackendExtension.StartupBackendServices();
          ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after backend services are running

          mediaAccessor.Initialize();

          systemStateService.SwitchSystemState(SystemState.Running, true);
          BackendExtension.ActivateImporterWorker(); // To be done after default media item aspect types are present and when the system is running (other plugins might also install media item aspect types)

          Application.Run(new MainForm());

          systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true; // Block ServiceRegistration from trying to load new services in shutdown phase

          mediaAccessor.Shutdown();

          pluginManager.Shutdown();

          BackendExtension.ShutdownBackendServices();
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
          BackendExtension.DisposeBackendServices();
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
        ServerCrashLogger crash = new ServerCrashLogger(logPath);
        crash.CreateLog(ex);
#endif
        systemStateService.SwitchSystemState(SystemState.Ending, false);
        Application.Exit();
      }
    }

    static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
      MessageBox.Show(e.Exception.ToString(), "Unhandled Thread Exception");
      // here you can log the exception ...
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      MessageBox.Show((e.ExceptionObject as Exception).ToString(), "Unhandled UI Exception");
      // here you can log the exception ...
    }
  }
}