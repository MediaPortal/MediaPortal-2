#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Windows.Forms;
using MediaPortal.Utilities.CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.TaskScheduler;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;
using MediaPortal.Core.DeviceManager;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Services.PathManager;
using MediaPortal.Services.Threading;
using MediaPortal.Services.TaskScheduler;
using MediaPortal.Services.Localisation;
using MediaPortal.Services.Logging;
using MediaPortal.Services.Settings;
using MediaPortal.Services.Messaging;
using MediaPortal.Services.ExtensionManager;
using MediaPortal.Services.Burning;

[assembly: CLSCompliant(true)]

namespace MediaPortal
{
  internal static class ApplicationLauncher
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    //[STAThread]
    private static void Main(params string[] args)
    {
      System.Threading.Thread.CurrentThread.Name = "Main";
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      try
      {
        CommandLine.Parse(args, mpArgs);
      }
      catch (ArgumentException)
      {
        mpArgs.DisplayOptions();
        return;
      }

      using (new ServiceScope(true)) //This is the first servicescope
      {
        // Create PathManager
        PathManager pathManager = new PathManager();

        // Check if user wants to override the default Application Data location.
        if (mpArgs.IsOption(CommandLineOptions.Option.Data))
          pathManager.ReplacePath("DATA", (string)mpArgs.GetOption(CommandLineOptions.Option.Data));

        // Register PathManager
        ServiceScope.Add<IPathManager>(pathManager);

        //Check whether the user wants to log method names in the logger
        //This adds an extra 10 to 40 milliseconds to the log call, depending on the length of the stack trace
        bool logMethods = mpArgs.IsOption(CommandLineOptions.Option.LogMethods);
        LogLevel level = LogLevel.All;
        if (mpArgs.IsOption(CommandLineOptions.Option.LogLevel))
        {
          level = (LogLevel)mpArgs.GetOption(CommandLineOptions.Option.LogLevel);
        }
#if DEBUG
        ILogger logger = new ConsoleLogger(level, logMethods);
#else
        ILogger logger = FileLogger.CreateFileLogger(pathManager.GetPath(@"<LOG>\MediaPortal.log"), level, logMethods);
#endif
        ServiceScope.Add(logger);
        logger.Info("ApplicationLauncher: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);
        //Debug.Assert(AppDomain.CurrentDomain.FriendlyName == "MPApplication",
        //             "Some code change has caused MP2 to load in the wrong AppDomain.  Crash recovery will fail now...");

        //register core service implementations
        logger.Debug("ApplicationLauncher: Registering ThreadPool");
        MediaPortal.Services.Threading.ThreadPool pool = new MediaPortal.Services.Threading.ThreadPool();
        pool.ErrorLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Error);
        pool.WarnLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Warn);
        pool.InfoLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Info);
        pool.DebugLog += new LoggerDelegate(ServiceScope.Get<ILogger>().Debug);
        ServiceScope.Add<MediaPortal.Core.Threading.IThreadPool>(pool);

        logger.Debug("ApplicationLauncher: Registering Message Broker");
        ServiceScope.Add<IMessageBroker>(new MessageBroker());

        logger.Debug("ApplicationLauncher: Registering Plugin Manager");
        ServiceScope.Add<IPluginManager>(new PluginManager());

        logger.Debug("ApplicationLauncher: Registering Settings Manager");
        ServiceScope.Add<ISettingsManager>(new SettingsManager());

        logger.Debug("ApplicationLauncher: Registering Strings Manager");
        ServiceScope.Add<ILocalisation>(new StringManager());

        logger.Debug("ApplicationLauncher: Registering TaskScheduler");
        ServiceScope.Add<ITaskScheduler>(new TaskScheduler());

        logger.Debug("ApplicationLauncher: Registering BurnManager");
        ServiceScope.Add<IBurnManager>(new BurnManager());
        EventHelper.Init(); // only for quick test simulating a plugin

        //MPInstaller - for testing only 
        logger.Debug("ApplicationLauncher: Executing ExtensionInstaller");
        ExtensionInstaller Installer = new ExtensionInstaller();
        ServiceScope.Add<IExtensionInstaller>(Installer);
        Installer.LoadQueue();
        Installer.ExecuteQueue(false);

#if !DEBUG
        // Not in Debug mode (ie Release) then catch all Exceptions
        // In Debug mode these will be left unhandled.
      try
      {
#endif
        // Start the core
        logger.Debug("ApplicationLauncher: Starting core");
        ApplicationCore core = new ApplicationCore();
        core.Start();
        //throw new ArgumentException("Test Execption");
#if !DEBUG
        }
        catch (Exception ex)
        {
          CrashLogger crash = new CrashLogger(pathManager.GetPath("<LOG>"));
          crash.CreateLog(ex);
          //Form frm =
          //  new YesNoDialogScreen("MediaPortal II", "Unrecoverable Error",
          //                        "MediaPortal has encountered an unrecoverable error\r\nDetails have been logged\r\n\r\nRestart?",
          //                        BaseScreen.Image.bug);
          //restart = frm.ShowDialog() == DialogResult.Yes;
        }
#endif
      }

      // Running in a new AppDomain didn't bring any extra protection for catching exceptions
      // reverting code.
      //// MP2 is now started in a new AppDomain.
      //// if this AppDomain crashes or stops unexpectedly, a notification is shown to the user
      //// and the user is asked whether he wants to restart MP2
      //AppDomain mpDomain = null;
      //bool restart = true;
      //while (restart) //while restart is wanted
      //{
      //  try
      //  {
      //    //Create the AppDomain
      //    mpDomain = AppDomain.CreateDomain("MPApplication");  
      //    //Create the MP2 application instance
      //    mpDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "MPApplication");

      //    //Run the application
      //    restart = MPApplication.Run(mpArgs);
      //  }
      //    // Catch and log all exceptions - optionally restart
      //  catch (Exception ex)
      //  {
      //    //TODO: log exception
      //    FileLogger logger = new FileLogger(@"log\MediaPortal.log", LogLevel.All, false);
      //    logger.Error(ex);
      //  }
      //  if (mpDomain != null)
      //  {
      //    try
      //    {
      //      AppDomain.Unload(mpDomain);  //unload the AppDomain.  All assemblies that were loaded will be unloaded...
      //    }
      //    catch {}
      //    mpDomain = null;
      //    GC.Collect();  //causes the garbage collector to release all unneeded objects
      //  }
      //  if (restart) //do not ask to restart if the program terminated normally (i.e. restart = false)
      //  {
      //    Form frm =
      //      new YesNoDialogScreen("MediaPortal II", "Unrecoverable Error",
      //                            "MediaPortal has encountered an unrecoverable error\r\nDetails have been logged\r\n\r\nRestart?",
      //                            BaseScreen.Image.bug);
      //    restart = frm.ShowDialog() == DialogResult.Yes;
      //  }
      //}
    }
  }
}
