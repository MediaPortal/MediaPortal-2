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
using System.Text.RegularExpressions;

namespace MediaPortal.Tools.BuildReport
{
  class AnalyseVS2005Input : IAnalyseInput
  {
    Solution _solution;

    public AnalyseVS2005Input()
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

      //Match slnSummary = summary.Match(input);
      //if (slnSummary.Success)
      //{
      //  _solution.Succeeded = Int32.Parse(slnSummary.Groups["succeeded"].Value);
      //  _solution.Failed = Int32.Parse(slnSummary.Groups["failed"].Value);
      //  _solution.Skipped = Int32.Parse(slnSummary.Groups["skipped"].Value);
      //}

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
          proj.build = Solution.Compile.Skipped;
          proj.errors = 0;
          proj.warnings = 0;
        }
        else
        {
          proj.build = Solution.Compile.Success;
          proj.errors = Int32.Parse(projComplete[c].Groups["errors"].Value);
          proj.warnings = Int32.Parse(projComplete[c].Groups["warnings"].Value);
          if (proj.errors > 0)
            proj.build = Solution.Compile.Failed;
        }

        if (c + 1 < projComplete.Count)
          c++;

        _solution.AddProject(proj);
      }

      _solution.Projects.Sort();
    }
  }
}
