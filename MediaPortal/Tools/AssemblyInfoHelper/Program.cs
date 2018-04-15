#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace AssemblyInfoHelper
{
  class Program
  {
    private const string MAJOR_VERSION = "2.1"; // 2.1 relates to MIA rework
    private static readonly Regex RE_REPLACE_ADDITIONAL = new Regex("(AssemblyInformationalVersion\\(\").*(\")", RegexOptions.Multiline);
    private static readonly Regex RE_REPLACE_VERSION_NUMBER = new Regex("(Assembly.*Version\\(\")([^\"]*)(\")", RegexOptions.Multiline);
    private static readonly Regex RE_REPLACE_YEAR_COPY = new Regex("(AssemblyCopyright\\(\"Copyright © Team MediaPortal 2007 - )\\d{4}(\")", RegexOptions.Multiline);

    static void Main(string[] args)
    {
      UpdateAssemblyInfos();
    }

    private static void UpdateAssemblyInfos()
    {
      // Open current repository
      string path = FindRepoRoot();
      if (string.IsNullOrEmpty(path))
      {
        Console.WriteLine("Failed to find repository root, cannot continue. This program must be run from a subfolder of MediaPortal 2 repository.");
        return;
      }

      using (Repository repo = new Repository(path))
      {
        Branch branch = repo.Head.TrackedBranch ?? repo.Head;
        int commits = branch.Commits.Count();
        string name = branch.Name;
        foreach (var tag in repo.Tags)
        {
          if (tag.Target.Sha == branch.Tip.Sha)
          {
            name = tag.CanonicalName.Replace("refs/tags/", string.Empty);
            break;
          }
        }
        string versionInfo = string.Format("{0}-{1}", name, branch.Tip.Sha.Substring(0, 6));
        WriteToFile(path, versionInfo, commits);
      }
    }

    private static void WriteToFile(string path, string versionInfo, int commits)
    {
      string filePath = Path.Combine(path, @"MediaPortal\Source\Core\MediaPortal.Common\VersionInfo\VersionInfo.cs");
      string template = File.ReadAllText(filePath);

      DateTime now = DateTime.Now;
      string year2 = now.Year.ToString().Substring(2, 2);
      string year = now.Year.ToString();
      string month = now.Month.ToString().PadLeft(2, '0');
      string fullVersion = string.Format("{0}.{1}{2}.{3}", MAJOR_VERSION, year2, month, commits);
      template = RE_REPLACE_VERSION_NUMBER.Replace(template, string.Format("${{1}}{0}${{3}}", fullVersion));

      template = RE_REPLACE_YEAR_COPY.Replace(template, string.Format("${{1}}{0}${{2}}", year));

      template = RE_REPLACE_ADDITIONAL.Replace(template, string.Format("${{1}}{0}${{2}}", versionInfo));

      File.WriteAllText(filePath, template);
      Console.WriteLine("AssemblyInfoHelper successfully changed VersionInfo.cs! New branch: {0}, Build number: {1}", versionInfo, fullVersion);
    }

    private static string FindRepoRoot()
    {
      string current = Directory.GetCurrentDirectory();
      do
      {
        if (Directory.Exists(Path.Combine(current, ".git")))
          return current;
        current = Path.Combine(current, "..");
      } while (Directory.Exists(current));
      return null;
    }
  }
}
