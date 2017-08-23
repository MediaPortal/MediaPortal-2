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
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Backend;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement;
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
using MediaPortal.Utilities.Process;

[assembly: CLSCompliant(true)]
namespace MediaPortal.Server
{
  public class ApplicationLauncher
  {
    protected static WindowsService _windowsService;
    protected SystemStateService _systemStateService = null;
    protected string _dataDirectory = null;
    protected IpcServer _ipcServer;

    public ApplicationLauncher(string dataDirectory)
    {
      _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// The main entry point for the MP2 server application.
    /// </summary>
    public static void Main(params string[] args)
    {
      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args, mpOptions, () => Environment.Exit(1));

      if (mpOptions.RunAsConsoleApp)
      {
        new ApplicationLauncher(mpOptions.DataDirectory).RunAsConsole();
      }
      else
      {
        _windowsService = new WindowsService();
        ServiceBase[] servicesToRun = new ServiceBase[] { _windowsService };
        ServiceBase.Run(servicesToRun);
      }
    }

    public void Start()
    {
      Thread.CurrentThread.Name = "Main";

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-Server\Log");
#endif

      _systemStateService = new SystemStateService();
      ServiceRegistration.Set<ISystemStateService>(_systemStateService);
      _systemStateService.SwitchSystemState(SystemState.Initializing, false);

      try
      {
        ILogger logger = null;
        try
        {
          // Check if user wants to override the default Application Data location.
          ApplicationCore.RegisterVitalCoreServices(true, _dataDirectory);
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
          _systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
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
          InitIpc();
          BackendExtension.StartupBackendServices();
          ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after backend services are running

          mediaAccessor.Initialize();

          logger.Info("Switching to running state");
          _systemStateService.SwitchSystemState(SystemState.Running, true);
          BackendExtension.ActivateImporterWorker();
            // To be done after default media item aspect types are present and when the system is running (other plugins might also install media item aspect types)
        }
        catch (Exception e)
        {
          logger.Critical("Error starting application", e);
          _systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
          ServiceRegistration.IsShuttingDown = true;
          BackendExtension.DisposeBackendServices();
          ApplicationCore.DisposeCoreServices();
          _systemStateService.SwitchSystemState(SystemState.Ending, false);
          throw; // needed to cancel OnStart of the Service
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
        _systemStateService.SwitchSystemState(SystemState.Ending, false);
        throw; // needed to cancel OnStart of the Service
      }
    }


    public void Stop()
    {
      try
      {
        _systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
        ServiceRegistration.IsShuttingDown = true; // Block ServiceRegistration from trying to load new services in shutdown phase

        ServiceRegistration.Get<IImporterWorker>().Shutdown(); // needs to be shut down before MediaAccessor and plugins
        ServiceRegistration.Get<IMediaAccessor>().Shutdown();
        ServiceRegistration.Get<IPluginManager>().Shutdown();
        BackendExtension.ShutdownBackendServices();
        CloseIpc();
        ApplicationCore.StopCoreServices();
      }
      catch (Exception ex)
      {
        //ServiceRegistration.Get<ILogger.Critical("Error stopping application", e);
#if DEBUG
        ConsoleLogger log = new ConsoleLogger(LogLevel.All, false);
        log.Error(ex);
#else
        var pathManager = ServiceRegistration.Get<IPathManager>();
        var logPath = pathManager.GetPath("<LOG>");
        ServerCrashLogger crash = new ServerCrashLogger(logPath);
        crash.CreateLog(ex);
#endif
        _systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
        ServiceRegistration.IsShuttingDown = true;
      }
      finally
      {
        BackendExtension.DisposeBackendServices();
        ApplicationCore.DisposeCoreServices();
        _systemStateService.SwitchSystemState(SystemState.Ending, false);
        _systemStateService.Dispose();
      }
    }

    protected void RunAsConsole()
    {
      Application.ThreadException += LauncherExceptionHandling.Application_ThreadException;
      AppDomain.CurrentDomain.UnhandledException += LauncherExceptionHandling.CurrentDomain_UnhandledException;

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
      Application.ThreadException -= LauncherExceptionHandling.Application_ThreadException;
      AppDomain.CurrentDomain.UnhandledException -= LauncherExceptionHandling.CurrentDomain_UnhandledException;
    }

    private void InitIpc()
    {
      if (_ipcServer != null)
        return;
      ServiceRegistration.Get<ILogger>().Debug("Initializing IPC");
      try
      {
        if (_windowsService == null)
        {
          _ipcServer = new IpcServer("ServerConsole");
          _ipcServer.CustomShutdownCallback = () =>
          {
            Application.Exit();
            return true;
          };
        }
        else
        {
          _ipcServer = new IpcServer("ServerService");
          _ipcServer.CustomShutdownCallback = () =>
          {
            // invoke service shutdown asynchronous with a little delay
            ThreadPool.QueueUserWorkItem(_ =>
            {
              Thread.Sleep(500);
              try
              {
                _windowsService.Stop();
              }
              catch
              {
                // ignored
              }
            });
            return true;
          };
        }
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
  }
}