using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaPortal.Tools.BuildReport
{
  class AnalyseMsbuildInput : IAnalyseInput
  {
    Solution _solution;

    public AnalyseMsbuildInput()
    {
    }

    public Solution Solution
    {
      get { return _solution; }
    }

    public void Parse(string input)
    {
      _solution = new Solution();

      Regex projectSearch = new Regex("\"(?<solution>[^\"]+)\" is building \"(?<project>[^\"]+)\"");
      Regex targetSearch = new Regex("Target (?<target>[^:]+):Rebuild:");

      List<string> lines = GetLines(input);

      FileInfo solutionFile = null;
      Project project = new Project();

      foreach (string line in lines)
      {
        switch (IndentDepth(line))
        {
          case 0: // header + footer
            if (line.IndexOf("Build") == 0 && (line.IndexOf("succeeded") > 0 || line.IndexOf("FAILED") > 0))
            {
              // save last project
              _solution.AddProject(project);
            }
            break;
          case 4: // project start
            if (line.IndexOf(":Rebuild:") > 0)
            {
              if (project.name != string.Empty)
              {
                // save last project
                _solution.AddProject(project);
                // create new project
                project = new Project();
              }

              Match lineMatch = targetSearch.Match(line);
              if (lineMatch.Success)
                project.name = lineMatch.Groups["target"].Value.Replace('_', '.');
            }
            break;
          case 8: // project info
            if (line.IndexOf(".sln") > 0)
            {
              Match lineMatch = projectSearch.Match(line);

              if (lineMatch.Success)
              {
                if (solutionFile == null)
                {
                  string solutionName = lineMatch.Groups["solution"].Value;
                  solutionFile = new FileInfo(solutionName);
                  _solution.Name = solutionFile.Name;
                }

                string projectName = lineMatch.Groups["project"].Value;

                FileInfo projectFile = new FileInfo(projectName);

                project.filename = Path.Combine(GetProjectDirectory(projectFile.Directory, solutionFile.Directory), projectFile.Name);
                project.build = Solution.Compile.Success;
              }
              break;
            }

            if (line.IndexOf("Done building project") > 0 && line.IndexOf("FAILED") > 0)
              project.build = Solution.Compile.Failed;

            break;
          case 12: // details
            if (line.IndexOf(": warning CS") > 0)
            {
              project.warnings++;
              break;
            }

            if (line.IndexOf(": error CS") > 0)
            {
              project.errors++;
              break;
            }
            break;
          default:
            break;
        }
      }
      _solution.Projects.Sort();
    }

    private List<string> GetLines(string input)
    {
      List<string> stringList = new List<string>();

      int start = 0;
      int pos;
      while ((pos = input.IndexOf("\r\n", start)) > 0)
      {
        stringList.Add(input.Substring(start, pos - start));
        start = pos + 2;
      }

      return stringList;
    }

    private int IndentDepth(string line)
    {
      int depth = 0;

      while (depth < line.Length && line[depth] == ' ')
        depth++;

      return depth;
    }

    private string GetProjectDirectory(DirectoryInfo project, DirectoryInfo solution)
    {
      if (solution == null)
        return string.Empty;

      if (project.FullName == solution.FullName)
        return string.Empty;

      if (project.FullName.StartsWith(solution.FullName))
      {
        return project.FullName.Substring(solution.FullName.Length);
      }
      else
      {
        return GetProjectDirectory(project, solution.Parent);
      }
    }
  }
}
