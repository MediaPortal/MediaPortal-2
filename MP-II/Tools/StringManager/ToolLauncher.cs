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
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;

using MediaPortal.Utilities.CommandLine;
using MediaPortal.Utilities.Screens;
// using MediaPortal.Utilities.InfoScreen;


namespace MediaPortal.Tools.StringManager
{
  static class ToolLauncher
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(params string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      //// Parse Command Line options
      //ICommandLineOptions mpArgs = new CommandLineOptions();

      //try
      //{
      //  CommandLine.Parse(args, ref mpArgs);
      //}
      //catch (ArgumentException)
      //{
      //  mpArgs.DisplayOptions();
      //  return;
      //}

#if !DEBUG
      try
      {
#endif

      Application.Run(new StringManagerForm());

#if !DEBUG
      }
      // Catch and log all exceptions - fail cleanly
      catch (Exception ex)
      {
        Application.Run(new InfoScreen("StringManager", "Unrecoverable Error","MediaPortal has incountered an unrecoverable error\r\nDetails have been logged",InfoScreen.Image.bug));
      }
#endif
    }
  }
}
