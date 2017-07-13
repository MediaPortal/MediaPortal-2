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
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

      try
      {
        string applicationEventLog = CollectEventLog(outputPath, "Application");
        archive.AddFile(applicationEventLog, "Windows Logs");
        string systemEventLog = CollectEventLog(outputPath, "System");
        archive.AddFile(systemEventLog, "Windows Logs");
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error collecting event log files {0}.", ex);
      }
    
      archive.Save();
      archive.Dispose();
      Console.WriteLine("Successful created log archive: {0}", targetFile);

      CleanupTempEventLogFiles(outputPath);

      Process.Start(outputPath); // Opens output folder
    }
    
    private static string CollectEventLog(string pathToFile, string logType)
    {
      string fileName = Path.Combine(pathToFile, logType + "_eventlog.csv");

      StringBuilder csv = new StringBuilder();
      csv.AppendLine("\"Level\";\"Date and Time\";\"Source\";\"Message\";\"EventID\"");
      EventLogReader logReader = new EventLogReader(logType, PathType.LogName);
      XNamespace eventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";

      for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
      {
        XDocument xDoc = XDocument.Parse(eventdetail.ToXml());

        XElement timeCreated = xDoc.Descendants(eventNamespace + "TimeCreated").FirstOrDefault();
        XAttribute systemTime = timeCreated?.Attribute("SystemTime");
        DateTime date = DateTime.Parse(systemTime?.Value);

        // collect only records for the last 14 days
        if (date < DateTime.Now.AddDays(-14))
        {
          continue;
        }

        string timeGenerated = date.ToString("dd.MM.yyyy HH:mm:ss");

        XElement provider = xDoc.Descendants(eventNamespace + "Provider").FirstOrDefault();
        XAttribute name = provider?.Attribute("Name");
        string source = name?.Value ?? string.Empty;

        XElement levelNode = xDoc.Descendants(eventNamespace + "Level").FirstOrDefault();
        LogLevel level = (LogLevel)Enum.Parse(typeof(LogLevel), levelNode.Value);

        XElement data = xDoc.Descendants(eventNamespace + "Data").FirstOrDefault();
        string message = data?.Value ?? string.Empty;

        XElement id = xDoc.Descendants(eventNamespace + "EventID").FirstOrDefault();
        string eventId = id?.Value ?? string.Empty;

        string newLine = $"{level}; {timeGenerated}; {source}; {message}; {eventId}";
        csv.AppendLine(newLine);
      }
      File.WriteAllText(fileName, csv.ToString());

      return fileName;
    }

    private static void CleanupTempEventLogFiles(string pathToLogs)
    {
      try
      {
        File.Delete(Path.Combine(pathToLogs, "Application_eventlog.csv"));
        File.Delete(Path.Combine(pathToLogs, "System_eventlog.csv"));
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error error cleaning up event log files {0}.", ex);
      }
    }

    enum LogLevel
    {
      Critical = 0,
      Error = 2,
      Information = 4,
      Undefined = 0,
      Verbose = 5,
      Warning = 3
    }
  }

}