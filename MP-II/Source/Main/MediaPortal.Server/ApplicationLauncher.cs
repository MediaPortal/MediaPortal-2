#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Backend;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.PluginManager;
#if !DEBUG
using MediaPortal.Backend.Services.Logging;
using System.IO;
#endif
using MediaPortal.Core.Services.Runtime;
using CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Runtime;
using MediaPortal.Core.Logging;

[assembly: CLSCompliant(true)]

namespace MediaPortal
{
  internal static class ApplicationLauncher
  {
    /// <summary>
    /// The main entry point for the MP-II server application.
    /// </summary>
    private static void Main(params string[] args)
    {
      Thread.CurrentThread.Name = "Main Thread";

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, mpArgs, Console.Out))
        Environment.Exit(1);

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP-II-Server\Log");
#endif

      using (new ServiceScope(true)) // Create the servicescope
      {
        SystemStateService systemStateService = new SystemStateService();
        ServiceScope.Add<ISystemStateService>(systemStateService);
        systemStateService.SwitchSystemState(SystemState.Initializing, false);

        try
        {
          ILogger logger = null;
          try
          {
            ApplicationCore.RegisterCoreServices(mpArgs.LogLevel, mpArgs.LogMethods, mpArgs.FlushLog);
            logger = ServiceScope.Get<ILogger>();

            IPathManager pathManager = ServiceScope.Get<IPathManager>();

            // Check if user wants to override the default Application Data location.
            if (!string.IsNullOrEmpty(mpArgs.DataDirectory))
              pathManager.SetPath("DATA", mpArgs.DataDirectory);

#if !DEBUG
            logPath = pathManager.GetPath("<LOG>");
#endif

            BackendExtension.RegisterBackendServices();
          }
          catch (Exception e)
          {
            if (logger != null)
              logger.Critical("Error starting application", e);
            systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
            ServiceScope.IsShuttingDown = true;

            BackendExtension.DisposeBackendServices();
            ApplicationCore.DisposeCoreServices();

            throw;
          }

          // Start the core
          logger.Debug("ApplicationLauncher: Starting core");

          try
          {
            IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
            IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
            pluginManager.Initialize();
            pluginManager.Startup(false);
            ApplicationCore.StartCoreServices();

            BackendExtension.StartupBackendServices();
            ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after backend services are running

            mediaAccessor.Initialize();

            systemStateService.SwitchSystemState(SystemState.Started, true);

            Application.Run(new MainForm());

            systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
            ServiceScope.IsShuttingDown = true; // Block ServiceScope from trying to load new services in shutdown phase

            mediaAccessor.Shutdown();

            pluginManager.Shutdown();

            BackendExtension.ShutdownBackendServices();
            ApplicationCore.StopCoreServices();
          }
          catch (Exception e)
          {
            logger.Critical("Error executing application", e);
            systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
            ServiceScope.IsShuttingDown = true;
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
#if !DEBUG
          ServerCrashLogger crash = new ServerCrashLogger(logPath);
          crash.CreateLog(ex);
#endif

          systemStateService.SwitchSystemState(SystemState.Ending, false);
          Application.Exit();
        }
      }
    }
  }
}
