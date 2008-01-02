using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildReport
{
  class AnalyseInput
  {
    List<Project> _projects;
    Solution _solution;

    public AnalyseInput()
    {
    }


    public Solution GetSolution()
    {
      return _solution;
    }

    public Project GetProject(int index)
    {
      return _projects[index];
    }

    public int ProjectCount()
    {
      return _projects.Count;
    }

    public void Parse(string input)
    {
      Regex rebuild = new Regex("------ [^:]+: [^:]+: (?<name>[^,]+)");
      Regex complete = new Regex("Compile complete --[^0-9]+(?<errors>[0-9]+)[^0-9]+(?<warnings>[0-9]+)");
      Regex summary = new Regex("========== [^0-9]+(?<succeeded>[0-9]+)[^0-9]+(?<failed>[0-9]+)[^0-9]+(?<skipped>[0-9]+)");
      Regex name = new Regex("[^ ]+.sln");

      _solution = new Solution();
      Match slnName = name.Match(input);
      if (slnName.Success)
        _solution.name = slnName.Value;
      else
        _solution.name = "Unknown";

      Match slnSummary = summary.Match(input);
      if (slnSummary.Success)
      {
        _solution.succeeded = Int32.Parse(slnSummary.Groups["succeeded"].Value);
        _solution.failed = Int32.Parse(slnSummary.Groups["failed"].Value);
        _solution.skipped = Int32.Parse(slnSummary.Groups["skipped"].Value);
      }



      MatchCollection projStart = rebuild.Matches(input);
      MatchCollection projComplete = complete.Matches(input);

      _projects = new List<Project>();

      int c = 0;
      for (int i = 0; i < projStart.Count; i++)
      {
        Project proj = new Project();

        proj.name = projStart[i].Groups["name"].Value;

        int pos = projStart[i].Index;
        bool skipped = false;

        if (projComplete[c].Index < pos)
          skipped = true;

        if (i + 1 < projStart.Count && projComplete[c].Index > projStart[i + 1].Index)
          skipped = true;

        if (skipped)
        {
          proj.build = "Skipped";
          proj.errors = 0;
          proj.warnings = 0;
        }
        else
        {
          proj.build = "Completed";
          proj.errors = Int32.Parse(projComplete[c].Groups["errors"].Value);
          proj.warnings = Int32.Parse(projComplete[c].Groups["warnings"].Value);
          if (proj.errors > 0)
            proj.build = "Failed";
        }

        if (c + 1 < projComplete.Count)
          c++;

        _projects.Add(proj);
      }
    }
  }
}
