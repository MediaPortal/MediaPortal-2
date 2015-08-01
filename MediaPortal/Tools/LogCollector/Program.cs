using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ionic.Zip;

namespace MediaPortal.LogCollector
{
  class Program
  {
    static void Main(string[] args)
    {
      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args, mpOptions, () => Environment.Exit(1));

      List<string> products = new List<string>
      {
        "MediaPortal Setup TV", // Legacy folders for TVE3 support
        "MediaPortal TV Server", // Legacy folders for TVE3 support
        "MP2-Client",
        "MP2-Server",
        "MP2-ClientLauncher",
        "MP2-ServiceMonitor"
      };

      string dataPath = !string.IsNullOrEmpty(mpOptions.DataDirectory) ?
        mpOptions.DataDirectory :
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

      string outputPath = !string.IsNullOrEmpty(mpOptions.OutputDirectory) ?
        mpOptions.OutputDirectory :
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MediaPortal2-Logs");

      try
      {
        CollectLogFiles(dataPath, outputPath, products);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Exception while collecting log files: {0}", ex);
      }
    }

    private static void CollectLogFiles(string dataPath, string outputPath, List<string> products)
    {
      if (!Directory.Exists(outputPath))
        Directory.CreateDirectory(outputPath);

      string targetFile = string.Format("MediaPortal2-Logs-{0}.zip", DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss"));
      targetFile = Path.Combine(outputPath, targetFile);
      if (File.Exists(targetFile))
        File.Delete(targetFile);

      ZipFile archive = new ZipFile(targetFile);
      foreach (var product in products)
      {
        string sourceFolder = Path.Combine(dataPath, @"Team MediaPortal", product, "Log");
        if (!Directory.Exists(sourceFolder))
        {
          Console.WriteLine("Skipping non-existant folder {0}", sourceFolder);
          continue;
        }
        Console.WriteLine("Adding folder {0}", sourceFolder);
        try
        {
          archive.AddDirectory(sourceFolder, product);
        }
        catch (Exception ex)
        {
          Console.WriteLine("Error adding folder {0}: {1}", sourceFolder, ex);
        }
      }
      archive.Save();
      archive.Dispose();
      Console.WriteLine("Successful created log archive: {0}", targetFile);

      Process.Start(outputPath); // Opens output folder
    }
  }
}
