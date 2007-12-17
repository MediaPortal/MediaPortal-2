#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Reflection;
using System.Windows.Forms;
using MediaPortal.Utilities.CommandLine;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.Screens;
using MediaPortal.Services.Logging;

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

          
      
      // MP2 is now started in a new AppDomain.
      // if this AppDomain crashes or stops unexpectedly, a notification is shown to the user
      // and the user is asked whether he wants to restart MP2
      AppDomain mpDomain = null;
      bool restart = true;
      while (restart) //while restart is wanted
      {
        try
        {
          //Create the AppDomain
          mpDomain = AppDomain.CreateDomain("MPApplication");  
          //Create the MP2 application instance
          mpDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "MPApplication");

          //Run the application
          restart = MPApplication.Run(mpArgs);
        }
          // Catch and log all exceptions - optionally restart
        catch (Exception ex)
        {
          //TODO: log exception
          FileLogger logger = new FileLogger(@"log\MediaPortal.log", LogLevel.All, false);
          logger.Error(ex);
        }
        if (mpDomain != null)
        {
          try
          {
            AppDomain.Unload(mpDomain);  //unload the AppDomain.  All assemblies that were loaded will be unloaded...
          }
          catch {}
          mpDomain = null;
          GC.Collect();  //causes the garbage collector to release all unneeded objects
        }
        if (restart) //do not ask to restart if the program terminated normally (i.e. restart = false)
        {
          Form frm =
            new YesNoDialogScreen("MediaPortal II", "Unrecoverable Error",
                                  "MediaPortal has encountered an unrecoverable error\r\nDetails have been logged\r\n\r\nRestart?",
                                  BaseScreen.Image.bug);
          restart = frm.ShowDialog() == DialogResult.Yes;
        }
      }
    }
  }
}