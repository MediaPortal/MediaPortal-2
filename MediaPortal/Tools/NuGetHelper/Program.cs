#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NuGetHelper
{
  class Program
  {
    #region Members & Constants

    private static string targetDir = null;
    private static string nuGetExePath = null;

    #endregion

    static void Main(string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      Console.WriteLine("NuGetHelper");
      Console.WriteLine("===========");
      Console.WriteLine();

      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args, mpOptions, () => Environment.Exit(1));

      // Search repository root
      targetDir = mpOptions.TargetDir;
      if (string.IsNullOrEmpty(targetDir))
      {
        Console.WriteLine("Target directory not specified as parameter. Searching in parant directories for NuGet.config ...");

        targetDir = Assembly.GetEntryAssembly().Location;
        try
        {
          do
          {
            string nugetConfigFile = Path.Combine(targetDir, "NuGet.config");
            if (File.Exists(nugetConfigFile))
              break;
            else
              targetDir = Directory.GetParent(targetDir).FullName;

          } while (Directory.Exists(targetDir));
        }
        catch (Exception)
        {
          Console.WriteLine("NuGet.config and root / target directory not found in parent directories.");
          Environment.Exit(1);
          throw;
        }
      }

      if (!Directory.Exists(targetDir))
      {
        Console.WriteLine("Target directory does not exist: {0}", targetDir);
        Environment.Exit(2);
      }
      Console.WriteLine("Target directory is: {0}", targetDir);

      // Search NuGet.exe
      nuGetExePath = Assembly.GetEntryAssembly().Location;
      do
      {
        string nugetDir = Path.Combine(nuGetExePath, ".nuget");
        if (Directory.Exists(nugetDir))
        {
          nuGetExePath = Path.Combine(nugetDir, "NuGet.exe");
          break;
        }
        else
          nuGetExePath = Directory.GetParent(nuGetExePath).FullName;

      } while (Directory.Exists(nuGetExePath));

      if (!File.Exists(nuGetExePath))
      {
        Console.WriteLine("NuGet.exe not found.");
        Environment.Exit(3);
      }
      Console.WriteLine("NuGet.exe is: {0}", nuGetExePath);

      SearchForPackageConfigs(targetDir);
    }

    /// <summary>
    /// Searches for <c>packages.config</c>-files in the given <para>targetDirectory</para>.
    /// </summary>
    private static void SearchForPackageConfigs(string targetDirectory)
    {
      var files = Directory.GetFiles(targetDirectory, "packages.config", SearchOption.AllDirectories);
      foreach (string file in files)
        InstallPackages(file);
    }

    /// <summary>
    /// Calls a <c>NuGet.exe</c> to install packages from the given <para>pathToPackagesConfig</para>-file.
    /// </summary>
    private static void InstallPackages(string pathToPackagesConfig)
    {
      string prettyPath = pathToPackagesConfig.Remove(0, targetDir.Length);
      Console.WriteLine();
      Console.WriteLine(prettyPath);

      ProcessStartInfo processStartInfo = new ProcessStartInfo();
      processStartInfo.FileName = nuGetExePath;
      processStartInfo.Arguments = String.Format("install \"{0}\"", pathToPackagesConfig);
      processStartInfo.WorkingDirectory = Directory.GetParent(nuGetExePath).FullName;
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;

      Process process = Process.Start(processStartInfo);
      Console.Write(process.StandardOutput.ReadToEnd());
      process.WaitForExit();
    }
  }
}
