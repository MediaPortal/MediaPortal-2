#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Windows.Forms;


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
