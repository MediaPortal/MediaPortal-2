#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace AssemblyInfoHelper
{
  class Program
  {
    private static readonly Regex RE_REPLACE_ADDITIONAL = new Regex("(AssemblyInformationalVersion\\(\").*(\")", RegexOptions.Multiline);
    private static readonly Regex RE_REPLACE_YEAR_MONTH = new Regex("(Assembly.*Version\\(\".*\\.)\\d{4}(\")", RegexOptions.Multiline);

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
        string versionInfo = string.Format("{0}-{1}", branch.Name, branch.Tip.Sha.Substring(0, 6));
        WriteToFile(path, versionInfo);
      }
    }

    private static void WriteToFile(string path, string versionInfo)
    {
      string filePath = Path.Combine(path, @"MediaPortal\Source\Core\MediaPortal.Common\VersionInfo\VersionInfo.cs");
      string template = File.ReadAllText(filePath);
      template = RE_REPLACE_ADDITIONAL.Replace(template, string.Format("${{1}}{0}${{2}}", versionInfo));

      DateTime now = DateTime.Now;
      string yearMonth = now.Year.ToString().Substring(2, 2) + now.Month.ToString().PadLeft(2, '0');
      template = RE_REPLACE_YEAR_MONTH.Replace(template, string.Format("${{1}}{0}${{2}}", yearMonth));

      File.WriteAllText(filePath, template);
      Console.WriteLine("AssemblyInfoHelper successfully changed VersionInfo.cs! New branch: {0}, Build month: {1}", versionInfo, yearMonth);
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
