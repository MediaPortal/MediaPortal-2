#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;

namespace MediaPortal.Package.UpdateService
{
  internal static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    private static void Main(string[] args)
    {
      try
      {
        var options = new CommandLineOptions();
        var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
        parser.ParseArgumentsStrict(args, options, () => Environment.Exit(1));

        //if (options.RunAsConsoleApp)
        //	new ApplicationLauncher(options.DataDirectory).RunAsConsole();
        //else
        //{
        //	ServiceBase[] servicesToRun = new ServiceBase[] { new WindowsService() };
        //	ServiceBase.Run(servicesToRun);
        //}				
      }
      catch (Exception ex)
      {
        var log = new ConsoleLogger(LogLevel.All, false);
        log.Error(ex);
      }
    }
  }
}