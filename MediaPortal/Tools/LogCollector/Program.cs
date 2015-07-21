using System;
using System.Collections.Generic;
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
        "MP2-Client",
        "MP2-Server",
        "MP2-ClientLauncher",
        "MP2-ServiceMonitor"
      };

      string dataPath = !string.IsNullOrEmpty(mpOptions.DataDirectory) ?
        mpOptions.DataDirectory :
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

      CollectLogFiles(dataPath, products);
    }

    private static void CollectLogFiles(string dataPath, List<string> products)
    {
      string targetFile = string.Format("MediaPortal2-Logs-{0}.zip", DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss"));
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
    }
  }
}
