using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BuildReport
{
  class BuildReport
  {
    static void Main(string[] args)
    {
      // Check for any command line args
      if (args.Length > 1)
      {
        string svn = "";
        if (args.Length > 2)
          svn = args[2];
          
        if (File.Exists(args[0]))
        {
          StreamReader input = File.OpenText(args[0]);
          string buildOutput = input.ReadToEnd();
          input.Close();

          AnalyseInput analyser = new AnalyseInput();

          analyser.Parse(buildOutput);

          WriteReport report = new WriteReport(args[1], svn);

          report.WriteSolution(analyser.GetSolution());

          for (int i = 0; i < analyser.ProjectCount(); i++)
          {
            report.WriteProject(analyser.GetProject(i));
          }

          report.WriteBuildReport(buildOutput);
        }
      }
      else
      {
        Console.WriteLine("BuildReport Input Output [svn]");
      }
    }
  }
}
