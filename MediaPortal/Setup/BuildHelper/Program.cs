using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using CommandLine;

namespace BuildHelper
{
  class Program
  {
    static void Main(string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, mpArgs, Console.Out))
        Environment.Exit(1);

      if (String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("wix")))
      {
        Console.Write("WiX environment variable could not be found. Please reinstall WiX.");
        Environment.Exit(2);
      }

      if (!CreateTransforms(mpArgs))
        Environment.Exit(3);

      if (!MergeTransforms(mpArgs))
        Environment.Exit(4);
    }

    private static bool CreateTransforms(CommandLineOptions mpArgs)
    {
      try
      {
        foreach (string file in Directory.GetFiles(mpArgs.TargetDir))
          File.Delete(file);

        foreach (string file in Directory.GetFiles(Path.Combine(mpArgs.TargetDir, "\\en-us")))
          File.Copy(file, Path.Combine(mpArgs.TargetDir, "\\" + Path.GetFileName(file)), true);

        foreach (DirectoryInfo dirInfo in new DirectoryInfo(mpArgs.TargetDir).GetDirectories())
        {
          if (dirInfo.Name.Equals("en-us")) continue;
          
          ProcessStartInfo processStartInfo = new ProcessStartInfo();
          processStartInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("wix") + "\\bin\\torch.exe");
          processStartInfo.Arguments = String.Format(
            " -t language \"{0}\" \"{1}\" -out \"{2}\"",
            Path.Combine(mpArgs.TargetDir, "\\en-us\\", mpArgs.TargetFileName),
            Path.Combine(mpArgs.TargetDir, "\\" + dirInfo.Name + "\\", mpArgs.TargetFileName),
            Path.Combine(mpArgs.TargetDir, "\\" + dirInfo.Name + ".mst"));

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

        foreach (string file in Directory.GetFiles(mpArgs.TargetDir, "*.mst"))
        {
          // merging the transforms back into the msi
          CultureInfo ci = new CultureInfo(Path.GetFileNameWithoutExtension(file));

          processStartInfo = new ProcessStartInfo();
          processStartInfo.UseShellExecute = true;
          processStartInfo.FileName = Directory.GetParent(Assembly.GetCallingAssembly().FullName) + "\\WiSubStg.vbs";
          processStartInfo.Arguments = String.Format(
            "\"{0}\" \"{1}\" {2}",
            Path.Combine(mpArgs.TargetDir, "\\" + mpArgs.TargetFileName),
            file, ci.LCID);

          process = Process.Start(processStartInfo);
          process.WaitForExit();
          
          if (process.ExitCode != 0) return false;

          availableLCIDs += "," + ci.LCID;
        }

        // setting the msi-available language ids
        processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;
        processStartInfo.FileName = Directory.GetParent(Assembly.GetCallingAssembly().FullName) + "\\WiLangId.vbs";
        processStartInfo.Arguments = String.Format(
          "\"{0}\" Package {1}",
            Path.Combine(mpArgs.TargetDir, "\\" + mpArgs.TargetFileName),
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
