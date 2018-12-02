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

using CommandLine;
using CommandLine.Text;

namespace MediaPortal.ServiceMonitor
{
  public class CommandLineOptions
  {
    [Option('m', "minimized", Required = false, HelpText = "Starts the application minimized.")]
    public bool IsMinimized { get; set; }

    [Option('c', "command", Required = false, HelpText = "Defines the command which should be executed on startup. Example: command=RestartService")]
    public string Command { get; set; }

    [HelpOption]
    public string GetUsage()
    {
      return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
    }
  }
}