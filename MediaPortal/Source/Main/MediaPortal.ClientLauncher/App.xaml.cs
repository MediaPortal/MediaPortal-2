using Hardcodet.Wpf.TaskbarNotification;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Process;
using System;
using System.Threading;
using System.Windows;

namespace MediaPortal.Client.Launcher
{
  /// <summary>
  /// Simple application. Check the XAML for comments.
  /// </summary>
  public partial class App : Application
  {
    private TaskbarIcon _notifyIcon;
    private Mutex _mutex = null;

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
      _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");


      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(e.Args, mpOptions, () => Environment.Exit(1));

      // Check if another instance is already running
      if (SingleInstanceHelper.IsAlreadyRunning(ApplicationLauncher.MUTEX_ID, out _mutex))
      {
        _mutex = null;
        // Stop current instance
        Console.Out.WriteLine("Application already running.");
        Environment.Exit(2);
      }

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                    @"Team MediaPortal", "MP2-Client", "Log");
#endif

      //// todo: Make sure we're properly handling exceptions
      //DispatcherUnhandledException += OnUnhandledException;
      //AppDomain.CurrentDomain.UnhandledException += LauncherExceptionHandling.CurrentDomain_UnhandledException;

      // Start core services
      ILogger logger = null;
      try
      {
        //FormDpiAwarenessExtension.TryEnableDPIAwareness();

        // Check if user wants to override the default Application Data location.
        ApplicationCore.RegisterVitalCoreServices(true, mpOptions.DataDirectory);

        logger = ServiceRegistration.Get<ILogger>();

#if !DEBUG
        logPath = ServiceRegistration.Get<IPathManager>().GetPath("<LOG>");
#endif
      }
      catch (Exception ex)
      {
        if (logger != null)
          logger.Critical("Error starting application", ex);

        ApplicationCore.DisposeCoreServices();

        throw;
      }

      // Start application
      logger.Info("Starting application");

      try
      {
        if (ApplicationLauncher.TerminateProcess("ehtray"))
          logger.Info("Terminating running instance(s) of ehtray.exe");

        ApplicationLauncher.InitIpc();
        ApplicationLauncher.InitMsgHandler();
      }
      catch (Exception ex)
      {
        logger.Critical("Error executing application", ex);
      }
    }

    protected override void OnExit(ExitEventArgs e)
    {
      _notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner

      var logger = ServiceRegistration.Get<ILogger>();
      logger.Info("Exiting...");

      ApplicationLauncher.CloseMsgHandler();
      ApplicationLauncher.CloseIpc();

      // Release mutex for single instance
      if (_mutex != null)
        _mutex.ReleaseMutex();

      base.OnExit(e);
    }
  }
}
