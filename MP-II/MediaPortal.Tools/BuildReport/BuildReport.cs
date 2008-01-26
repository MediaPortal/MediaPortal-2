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
using System.IO;
using System.Text;
using MediaPortal.Utilities.CommandLine;

namespace MediaPortal.Tools.BuildReport
{
  class BuildReport
  {
    static void Main(string[] args)
    {
      // Check for any command line args
      CommandLineOptions brArgs = new CommandLineOptions();
      try
      {
        CommandLine.Parse(args, brArgs);
      }
      catch (ArgumentException)
      {
        brArgs.DisplayOptions();
        return;
      }

      if (!brArgs.IsOption(CommandLineOptions.Option.Input) && !brArgs.IsOption(CommandLineOptions.Option.Output))
      {
        brArgs.DisplayOptions();
        return;
      }

      if (!File.Exists((string)brArgs.GetOption(CommandLineOptions.Option.Input)))
      {
        Console.WriteLine("Input file does not exist: " + (string)brArgs.GetOption(CommandLineOptions.Option.Input));
        return;
      }

      StreamReader input = File.OpenText((string)brArgs.GetOption(CommandLineOptions.Option.Input));
      string buildOutput = input.ReadToEnd();
      input.Close();

      IAnalyseInput analyser = new AnalyseMsbuildInput();

      if (brArgs.IsOption(CommandLineOptions.Option.VS2005))
        analyser = new AnalyseVS2005Input();

      analyser.Parse(buildOutput);

      Solution solution = analyser.Solution;

      if (brArgs.IsOption(CommandLineOptions.Option.Solution))
        solution.Name = (string)brArgs.GetOption(CommandLineOptions.Option.Solution);

      string reportTitle;
      if (brArgs.IsOption(CommandLineOptions.Option.Title))
        reportTitle = (string)brArgs.GetOption(CommandLineOptions.Option.Title);
      else
        reportTitle = "Build Results";

      WriteReport report = new WriteReport((string)brArgs.GetOption(CommandLineOptions.Option.Output), reportTitle);

      report.WriteSolution(solution);
      report.WriteBuildReport(buildOutput);
    }
  }
}
