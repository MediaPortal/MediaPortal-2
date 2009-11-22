#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Windows.Forms;
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.Services.Logging; // Needed for Release build configuration
using MediaPortal.Utilities.CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.UI.Presentation.Localisation;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Services.Localisation;
using MediaPortal.UI.Configuration;

namespace MediaPortal.Manager
{
  static class Manager
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(params string[] args)
    {
      System.Threading.Thread.CurrentThread.Name = "Manager";
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

        logger.Debug("Manager: Registering Strings Manager");
        ServiceScope.Add<ILocalisation>(new StringManager());

#if !DEBUG
        // Not in Debug mode (ie Release) then catch all Exceptions
        // In Debug mode these will be left unhandled.
      try
      {
#endif
        // Start the system
        logger.Debug("ApplicationLauncher: Starting MediaPortal manager");

        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        pluginManager.Initialize();
        pluginManager.Startup(true);
        Application.Run(new MainWindow());
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
    }
  }
}