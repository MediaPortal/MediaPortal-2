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
using MediaPortal.Control.InputManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.UserManagement;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Presentation;
using MediaPortal.Presentation.Workflow;
using MediaPortal.Services.InputManager;
using MediaPortal.Services.Logging; // Needed for Release build configuration
using MediaPortal.Services.ThumbnailGenerator;
using MediaPortal.Services.UserManagement;
using MediaPortal.Services.Workflow;
using MediaPortal.Thumbnails;
using MediaPortal.Utilities.CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Presentation.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.DeviceManager;
using MediaPortal.Services.Localization;
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
      System.Threading.Thread.CurrentThread.Name = "MediaPortal Client Main Thread";
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
        ApplicationCore.RegisterCoreServices();

        IPathManager pathManager = ServiceScope.Get<IPathManager>();

        // Check if user wants to override the default Application Data location.
        if (mpArgs.IsOption(CommandLineOptions.Option.Data))
          pathManager.ReplacePath("DATA", (string)mpArgs.GetOption(CommandLineOptions.Option.Data));

        //Check whether the user wants to log method names in the logger
        //This adds an extra 10 to 40 milliseconds to the log call, depending on the length of the stack trace
        bool logMethods = mpArgs.IsOption(CommandLineOptions.Option.LogMethods);
        LogLevel level = LogLevel.All;
        if (mpArgs.IsOption(CommandLineOptions.Option.LogLevel))
        {
          level = (LogLevel)mpArgs.GetOption(CommandLineOptions.Option.LogLevel);
        }

        ILogger logger = ServiceScope.Get<ILogger>();
        logger.Level = level;
        logger.LogMethodNames = logMethods;

        logger.Info("ApplicationLauncher: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);

        logger.Debug("ApplicationLauncher: Create MediaManager service");
        MediaManager mediaManager = new MediaManager();
        ServiceScope.Add<MediaManager>(mediaManager);

        logger.Debug("ApplicationLauncher: Create IInputMapper service");
        InputMapper inputMapper = new InputMapper();
        ServiceScope.Add<IInputMapper>(inputMapper);

        logger.Debug("ApplicationLauncher: Create IWorkflowManager service");
        WorkflowManager workflowManager = new WorkflowManager();
        ServiceScope.Add<IWorkflowManager>(workflowManager);

        logger.Debug("ApplicationLauncher: Create UserService service");
        UserService userservice = new UserService();
        ServiceScope.Add<IUserService>(userservice);

        logger.Debug("ApplicationLauncher: Create StringManager");
        ServiceScope.Add<ILocalization>(new StringManager());

        logger.Debug("ApplicationLauncher: Create ThumbnailGenerator");
        ServiceScope.Add<IAsyncThumbnailGenerator>(new ThumbnailGenerator());

        logger.Debug("ApplicationLauncher: Create BurnManager");
        ServiceScope.Add<IBurnManager>(new BurnManager());
        EventHelper.Init(); // only for quick test simulating a plugin

#if !DEBUG
        // Not in Debug mode (ie Release) then catch all Exceptions
        // In Debug mode these will be left unhandled.
      try
      {
#endif
        // Start the core
        logger.Debug("ApplicationLauncher: Starting core");

        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        pluginManager.Initialize();
        pluginManager.Startup(false);

        ISkinEngine skinEngine = ServiceScope.Get<ISkinEngine>();

        // We have to handle some dependencies here in the start order:
        // 1) After all plugins are loaded, the SkinEngine can initialize (=load all skin resources)
        // 2) After the skin resources are loaded, the workflow manager can initialize (=load its states and actions)
        // 3) After the workflow states and actions are loaded, the startup screen can be shown
        mediaManager.Initialize(); // Independent from skin engine/skin resources
        skinEngine.Initialize(); // 1)
        workflowManager.Startup(); // 2)
        skinEngine.Startup(); // 3)

        Application.Run();
        pluginManager.Shutdown();
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
