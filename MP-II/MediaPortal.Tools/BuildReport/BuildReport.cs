using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaPortal.Utilities.CommandLine;

namespace MediaPortal.Tootls.BuildReport
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

      AnalyseInput analyser = new AnalyseInput();

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
