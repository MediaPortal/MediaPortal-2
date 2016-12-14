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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using MergeMSI.Properties;

namespace MergeMSI
{
  class Program
  {
    static void Main(string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args, mpOptions, () => Environment.Exit(1));

      if (String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("wix")))
      {
        Console.Write("WiX environment variable could not be found. Please reinstall WiX.");
        Environment.Exit(2);
      }

      if (!CreateTransforms(mpOptions))
        Environment.Exit(3);

      if (!MergeTransforms(mpOptions))
        Environment.Exit(4);
    }

    private static bool CreateTransforms(CommandLineOptions mpArgs)
    {
      try
      {
        foreach (string file in Directory.GetFiles(mpArgs.TargetDir))
          File.Delete(file);

        foreach (string file in Directory.GetFiles(Path.Combine(mpArgs.TargetDir, "en-us")))
          File.Copy(file, Path.Combine(mpArgs.TargetDir, Path.GetFileName(file)), true);

        foreach (DirectoryInfo dirInfo in new DirectoryInfo(mpArgs.TargetDir).GetDirectories())
        {
          if (dirInfo.Name.Equals("en-us")) continue;
          
          ProcessStartInfo processStartInfo = new ProcessStartInfo();
          processStartInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("wix") + "bin\\torch.exe");
          processStartInfo.Arguments = String.Format(
            " -t language \"{0}\" \"{1}\" -out \"{2}\"",
            Path.Combine(mpArgs.TargetDir, "en-us\\", mpArgs.TargetFileName),
            Path.Combine(mpArgs.TargetDir, dirInfo.Name + "\\", mpArgs.TargetFileName),
            Path.Combine(mpArgs.TargetDir, dirInfo.Name + ".mst"));

          Process process = Process.Start(processStartInfo);
          process.WaitForExit();
          
          if (process.ExitCode != 0) return false;
        }
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return false;
      }

      return true;
    }

    private static bool MergeTransforms(CommandLineOptions mpArgs)
    {
      try
      {
        string availableLCIDs = "1033"; // english

        ProcessStartInfo processStartInfo;
        Process process;

        string pathWiSubStg = Path.Combine(Path.GetTempPath(), "WiSubStg.vbs");
        string pathWiLangId = Path.Combine(Path.GetTempPath(), "WiLangId.vbs");
        File.WriteAllText(pathWiSubStg, Resources.WiSubStg);
        File.WriteAllText(pathWiLangId, Resources.WiLangId);

        foreach (string file in Directory.GetFiles(mpArgs.TargetDir, "*.mst"))
        {
          // merging the transforms back into the msi
          CultureInfo ci = new CultureInfo(Path.GetFileNameWithoutExtension(file));
          
          processStartInfo = new ProcessStartInfo();
          processStartInfo.UseShellExecute = true;
          processStartInfo.FileName = pathWiSubStg;
          processStartInfo.Arguments = String.Format(
            " \"{0}\" \"{1}\" {2}",
            Path.Combine(mpArgs.TargetDir, mpArgs.TargetFileName),
            file, ci.LCID);

          process = Process.Start(processStartInfo);
          process.WaitForExit();
          
          if (process.ExitCode != 0) return false;

          availableLCIDs += "," + ci.LCID;
        }

        // setting the msi-available language ids
        processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;
        processStartInfo.FileName = pathWiLangId;
        processStartInfo.Arguments = String.Format(
          " \"{0}\" Package {1}",
            Path.Combine(mpArgs.TargetDir, mpArgs.TargetFileName),
            availableLCIDs);

        process = Process.Start(processStartInfo);
        process.WaitForExit();

        if (process.ExitCode != 0) return false;
      }
      catch (Exception e)
      {
        Console.Write(e.Message);
        return false;
      }

      return true;
    }
  }
}
