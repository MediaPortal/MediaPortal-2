#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Runtime;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Server.Headless
{
  /// <summary>
  /// Runs the MP2 server as a plain console process, on any platform the core supports.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The sequence is <c>MediaPortal.Server</c>'s, in the same order, because the order is load
  /// bearing: vital services (paths, settings, logging) before anything logs, core services before
  /// backend services, plugins initialised before the core starts and the media accessor
  /// initialised after the backend is up, and the importer worker activated last so that aspect
  /// types installed by other plugins already exist. What is deliberately absent is everything that
  /// needs a desktop or a service control manager: <c>MainForm</c>, <c>ServiceBase.Run</c>,
  /// <c>Application.Exit</c>, and the <c>MediaPortal.Common.Windows</c> registrations that make
  /// <c>MP2-Server</c> able to power-cycle the machine.
  /// </para>
  /// <para>
  /// So this host runs the degraded path on purpose. <see cref="PortableSystemStateService"/> keeps
  /// the application's own state machine and says out loud that it cannot touch the machine's power
  /// state; the core's other fallbacks (no mounting, current-user impersonation, no image
  /// transformer) are installed by <see cref="ApplicationCore.RegisterCoreServices"/> and stay. On
  /// Windows, <c>MP2-Server</c> upgrades all of them. That asymmetry is the value of this file: the
  /// fallbacks stop being code nobody runs.
  /// </para>
  /// <para>
  /// It is also the only executable the Linux container can run, which is what makes it the gate for
  /// everything in Phase 1 that currently only proves "compiles".
  /// </para>
  /// </remarks>
  /// <remarks>
  /// CA1001 is suppressed below because this type is not the kind of disposable the rule is
  /// written for: one instance exists per process and its fields' lifetimes are the process's
  /// lifetime. Everything with an external resource (the IPC endpoint, the state service, the core
  /// and backend services, and the exit event) is released by <see cref="Stop"/> in a finally
  /// block, so nothing depends on a finalizer or on a caller remembering to dispose.
  /// </remarks>
#pragma warning disable CA1001
  public class HeadlessServerLauncher
#pragma warning restore CA1001
  {
    #region Fields

    private readonly string _dataDirectory;
    private readonly ManualResetEventSlim _exitRequested = new ManualResetEventSlim(false);
    // The concrete type, not ISystemStateService: SwitchSystemState() is not on the interface -
    // which is why MP2-Server and ServiceMonitor both hold their WindowsSystemStateService
    // instance too. The interface only exposes the machine actions (shutdown, suspend, ...) and
    // CurrentState, while the application's own state transitions are driven through the class.
    private readonly PortableSystemStateService _systemStateService;
    private IpcServer _ipcServer;
    private ILogger _logger;
    private bool _coreStarted;
    private bool _coreServicesRegistered;
    private bool _backendRegistered;

    // 0 while the teardown sequence has not run, 1 once it has. An int so the check-and-set is atomic:
    // an IPC shutdown can race with the one Main() performs.
    private int _stopped;
    private bool _servicesDisposed;

    #endregion

    #region Construction

    public HeadlessServerLauncher(string dataDirectory)
    {
      _dataDirectory = dataDirectory;

      // Registered before RegisterCoreServices() and unconditionally: this host has no message
      // window to receive WM_POWERBROADCAST and, off Windows, no API to drive the machine's power
      // state at all. PortableSystemStateService still sends the SystemMessaging transitions that
      // the rest of the server reacts to, which is the half that matters here.
      _systemStateService = new PortableSystemStateService();
      ServiceRegistration.Set<ISystemStateService>(_systemStateService);
      _systemStateService.SwitchSystemState(SystemState.Initializing, false);
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Brings the server up to the <see cref="SystemState.Running"/> state.
    /// </summary>
    /// <exception cref="Exception">Any failure during startup, after the services involved have been disposed.</exception>
    public void Start()
    {
      Thread.CurrentThread.Name = "Main";

      ILogger logger = null;
      try
      {
        // The first argument asks for the path manager, the second is an override of <DATA>.
        ApplicationCore.RegisterVitalCoreServices(true, _dataDirectory);
        ApplicationCore.RegisterCoreServices();
        _coreServicesRegistered = true;
        logger = _logger = ServiceRegistration.Get<ILogger>();
        logger.Info("HeadlessServerLauncher: no window, no service controller, no platform upgrades");
        BackendExtension.RegisterBackendServices();
        _backendRegistered = true;
      }
      catch (Exception e)
      {
        ReportFatal(logger, "error during service registration", e);
        DisposeServices();
        throw;
      }

      try
      {
        _logger.Debug("HeadlessServerLauncher: Starting core");
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        pluginManager.Initialize();
        pluginManager.Startup(false);
        ApplicationCore.StartCoreServices();
        InitIpc();
        BackendExtension.StartupBackendServices();

        ServiceRegistration.Get<IMediaAccessor>().Initialize();

        _logger.Info("HeadlessServerLauncher: Switching to running state");
        _systemStateService.SwitchSystemState(SystemState.Running, true);

        // Both of these need the system to be running: the migration reads aspect types, and the
        // importer worker is what other plugins may depend on being able to query.
        BackendExtension.MigrateDatabaseData();
        BackendExtension.ActivateImporterWorker();
        _coreStarted = true;
      }
      catch (Exception e)
      {
        ReportFatal(_logger, "error while starting the core", e);
        try
        {
          Stop();
        }
        catch (Exception stopException)
        {
          _logger.Error("HeadlessServerLauncher: shutdown after a failed startup also failed", stopException);
        }
        throw;
      }
    }

    /// <summary>
    /// Returns when shutdown was requested, by Ctrl+C, SIGTERM or the IPC shutdown command.
    /// </summary>
    public void WaitForExitRequest()
    {
      _exitRequested.Wait();
    }

    /// <summary>
    /// Waits for shutdown to be requested, but not longer than <paramref name="timeout"/>.
    /// </summary>
    /// <returns><c>true</c> if shutdown was requested before the timeout ran out.</returns>
    public bool WaitForExitRequest(TimeSpan timeout)
    {
      return _exitRequested.Wait(timeout);
    }

    public void RequestExit()
    {
      try
      {
        _exitRequested.Set();
      }
      catch (ObjectDisposedException)
      {
        // A signal that arrives while Stop() is already running. The process is going down anyway.
      }
    }

    /// <summary>
    /// Brings the server down. Safe to call once, and safe to call after a failed <see cref="Start"/>.
    /// </summary>
    public void Stop()
    {
      // Stop() runs twice on the failed-startup path: Start() calls it from its catch, and Main() calls
      // it again in its finally block. DisposeServices() has guarded against that since it was written;
      // the teardown sequence below had not, so the second pass asked for the services the first pass
      // had already removed and reported "error while shutting down" as a second FATAL directly below
      // the startup failure that mattered. Measured in the Linux container run (ROADMAP item i/j), where
      // the service named in that second FATAL changed every time a caller was made tolerant - because
      // the caller was never the problem.
      if (Interlocked.Exchange(ref _stopped, 1) != 0)
        return;

      try
      {
        _systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
        // After this, ServiceRegistration refuses to resolve new services, which is what keeps
        // plugins that are still running from pulling in ones already torn down.
        ServiceRegistration.IsShuttingDown = true;

        if (_coreStarted)
        {
          // Order matters: the importer worker uses the media library, which uses the plugins.
          ServiceRegistration.Get<IImporterWorker>(false)?.Shutdown();
          ServiceRegistration.Get<IMediaAccessor>(false)?.Shutdown();
          ServiceRegistration.Get<IPluginManager>(false)?.Shutdown();
        }

        // BackendExtension.Shutdown/DisposeBackendServices resolve their services without checking,
        // so they may only run if RegisterBackendServices actually completed. A host that failed
        // during registration has to be able to shut down too - that is how the failure reaches the
        // log instead of turning into a second exception on the way out.
        if (_backendRegistered)
          BackendExtension.ShutdownBackendServices();
        CloseIpc();
        // Same reason as the backend gate above: StopCoreServices resolves the services it stops.
        if (_coreServicesRegistered)
          ApplicationCore.StopCoreServices();
      }
      catch (Exception e)
      {
        if (_logger != null)
          _logger.Critical("HeadlessServerLauncher: error while shutting down", e);
        else
          Console.Error.WriteLine("HeadlessServerLauncher: error while shutting down: " + e);
      }
      finally
      {
        DisposeServices();
      }
    }

    private void DisposeServices()
    {
      // Stop() can run twice - Start() calls it on a failed startup and Main() calls it again in
      // its finally block - and none of the Dispose() implementations here are written to be
      // idempotent.
      if (_servicesDisposed)
        return;
      _servicesDisposed = true;

      if (_backendRegistered)
        BackendExtension.DisposeBackendServices();
      ApplicationCore.DisposeCoreServices();
      _systemStateService.SwitchSystemState(SystemState.Ending, false);
      _systemStateService.Dispose();
      _exitRequested.Dispose();
    }

    private void ReportFatal(ILogger logger, string what, Exception e)
    {
      if (logger != null)
        logger.Critical("HeadlessServerLauncher: " + what, e);
      else
        Console.Error.WriteLine("HeadlessServerLauncher: " + what + ": " + e);
    }

    #endregion

    #region IPC

    /// <summary>
    /// Opens the IPC endpoint so that other MP2 processes can ask this server to shut down.
    /// </summary>
    /// <remarks>
    /// The name is <c>ServerConsole</c>, the one <c>MP2-Server</c> uses when it is not running as a
    /// service, so the existing tooling reaches this host the same way. <see cref="IpcServer"/>
    /// creates a named pipe on Windows and a byte-mode unix socket elsewhere; the canary exercises
    /// both round trips on Linux. Failing to open it must not stop the server - IPC is a convenience
    /// channel, not a dependency.
    /// </remarks>
    private void InitIpc()
    {
      if (_ipcServer != null)
        return;
      _logger.Debug("HeadlessServerLauncher: Initializing IPC");
      try
      {
        _ipcServer = new IpcServer("ServerConsole")
        {
          CustomShutdownCallback = () =>
          {
            RequestExit();
            return true;
          }
        };
        _ipcServer.Open();
      }
      catch (Exception e)
      {
        _logger.Error("HeadlessServerLauncher: IPC endpoint unavailable, shutdown by IPC is not possible", e);
        _ipcServer = null;
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
      catch (Exception e)
      {
        if (_logger != null)
          _logger.Error(e);
      }
      _ipcServer = null;
    }

    #endregion

    #region Entry point

    /// <summary>
    /// Runs the server until the process is asked to stop.
    /// </summary>
    /// <param name="args"><c>--data &lt;path&gt;</c> puts the media database and configuration somewhere specific; <c>--check</c> shuts down again as soon as the server is up; <c>--duration &lt;seconds&gt;</c> stays up for a fixed time first.</param>
    /// <returns>0 on a clean run, 1 if startup failed.</returns>
    public static int Main(string[] args)
    {
      string dataDirectory = null;
      int durationSeconds = 0;
      for (int i = 0; i < args.Length - 1; i++)
      {
        if (args[i] == "--data" || args[i] == "-d")
          dataDirectory = args[i + 1];
        else if (args[i] == "--duration")
          int.TryParse(args[i + 1], out durationSeconds);
      }
      bool checkOnly = false;
      foreach (string arg in args)
      {
        if (arg == "--check")
          checkOnly = true;
      }

      // LastResortReporter already writes to stderr, which is the right last resort for a process
      // with no window; the two handlers are the ones MP2's other hosts install as well.
      AppDomain.CurrentDomain.UnhandledException += LauncherExceptionHandling.CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException += LauncherExceptionHandling.TaskScheduler_UnobservedTaskException;

      HeadlessServerLauncher launcher = new HeadlessServerLauncher(dataDirectory);

      Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
      {
        e.Cancel = true; // otherwise the runtime kills us mid-shutdown
        launcher.RequestExit();
      };

      // A container stops a process with SIGTERM, and the default for that signal is to terminate
      // immediately - mid-transaction. Cancel=true keeps the process alive long enough to run Stop().
      // Windows gets SIGINT/SIGBREAK through Console.CancelKeyPress above.
      using (PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
      {
        context.Cancel = true;
        launcher.RequestExit();
      }))
      {
        try
        {
          launcher.Start();
          // --check exists for the CI containers: bring the whole server up on a platform where
          // nobody has run it, then come down cleanly. A non-zero exit is the failure signal.
          if (checkOnly)
            launcher.RequestExit();
          else if (durationSeconds > 0 && !launcher.WaitForExitRequest(TimeSpan.FromSeconds(durationSeconds)))
            // --duration keeps the server reachable for a fixed time, so that a test can send it traffic
            // (an SSDP search, an HTTP request) and still get the clean shutdown that --check produces.
            launcher.RequestExit();
          launcher.WaitForExitRequest();
          return 0;
        }
        catch (Exception e)
        {
          Console.Error.WriteLine("MP2-Server-Headless: fatal error, see the log for details: " + e.Message);
          LauncherExceptionHandling.HandleException("MP2-Server-Headless", "The server stopped with an error.", e);
          return 1;
        }
        finally
        {
          launcher.Stop();
          AppDomain.CurrentDomain.UnhandledException -= LauncherExceptionHandling.CurrentDomain_UnhandledException;
          TaskScheduler.UnobservedTaskException -= LauncherExceptionHandling.TaskScheduler_UnobservedTaskException;
        }
      }
    }

    #endregion
  }
}
