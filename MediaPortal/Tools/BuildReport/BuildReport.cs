#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.IO;
using CommandLine;

namespace MediaPortal.Tools.BuildReport
{
  class BuildReport
  {
    static void Main(string[] args)
    {
      // Parse Command Line options
      CommandLineOptions brArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(false, Console.Error));
      if (!parser.ParseArguments(args, brArgs, Console.Out))
        Environment.Exit(1);

      if (!File.Exists(brArgs.Input))
      {
        Console.WriteLine("Input file does not exist: " + brArgs.Input);
        return;
      }

      StreamReader input = File.OpenText(brArgs.Input);
      string buildOutput = input.ReadToEnd();
      input.Close();

      IAnalyseInput analyser = brArgs.VS2010 ? (IAnalyseInput) new AnalyseVSInput() : new AnalyseMsbuildInput();

      analyser.Parse(buildOutput);

      Solution solution = analyser.Solution;

      if (!string.IsNullOrEmpty(brArgs.Solution))
        solution.Name = brArgs.Solution;

      string reportTitle;
      if (string.IsNullOrEmpty(brArgs.Title))
        reportTitle = "Build Results";
      else
        reportTitle = brArgs.Title;

      WriteReport report = new WriteReport(brArgs.Output, reportTitle);

      report.WriteSolution(solution);
      report.WriteBuildReport(buildOutput);
    }
  }
}
