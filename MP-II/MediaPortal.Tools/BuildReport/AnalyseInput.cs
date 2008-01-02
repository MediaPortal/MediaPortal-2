using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaPortal.Tootls.BuildReport
{
  class AnalyseInput
  {
    Solution _solution;

    public AnalyseInput()
    {
    }

    public Solution Solution
    {
      get { return _solution; }
    }

    public void Parse(string input)
    {
      Regex rebuild = new Regex("------ [^:]+: [^:]+: (?<name>[^,]+)");
      Regex complete = new Regex("Compile complete --[^0-9]+(?<errors>[0-9]+)[^0-9]+(?<warnings>[0-9]+)");
      Regex summary = new Regex("========== [^0-9]+(?<succeeded>[0-9]+)[^0-9]+(?<failed>[0-9]+)[^0-9]+(?<skipped>[0-9]+)");

      _solution = new Solution();
      _solution.Name = "Unknown";

      Match slnSummary = summary.Match(input);
      if (slnSummary.Success)
      {
        _solution.Succeeded = Int32.Parse(slnSummary.Groups["succeeded"].Value);
        _solution.Failed = Int32.Parse(slnSummary.Groups["failed"].Value);
        _solution.Skipped = Int32.Parse(slnSummary.Groups["skipped"].Value);
      }

      MatchCollection projStart = rebuild.Matches(input);
      MatchCollection projComplete = complete.Matches(input);

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

        _solution.AddProject(proj);
      }
    }
  }
}
