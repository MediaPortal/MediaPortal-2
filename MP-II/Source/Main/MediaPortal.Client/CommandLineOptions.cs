#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Logging;
using CommandLine;

namespace MediaPortal
{
  public class CommandLineOptions
  {
    [Option("l", "loglevel", Required = false,
        HelpText = "Sets the lowest level for log output. Log output of a lower level will be discarded.")]
    public LogLevel LogLevel = LogLevel.All;

    [Option("m", "logmethods", Required = false,
        HelpText = "Instructs the logger to also log the name of its calling method.")]
    public bool LogMethods = false;

    [Option("f", "flushlog", Required = false,
        HelpText = "Makes the logger flush its output buffer to the log file after each log output.")]
    public bool FlushLog = false;

    [Option("d", "data", Required = false,
        HelpText = "Overrides the default application data directory.")]
    public string DataDirectory = null;
  }
}
