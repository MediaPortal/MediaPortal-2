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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace TransifexHelper
{
  class Program
  {
    #region struct transifex project

    private class TransifexResource
    {
      public string Name;
      public string ProjectSlug;
      public string LanguageDirectory;

      public string GetCacheSubDirectory()
      {
        return String.Format("Cache_{0}\\{1}", ProjectSlug, Name);
      }

      public string GetCacheFullDirectory()
      {
        return TransifexRootFolder + "\\" + GetCacheSubDirectory();
      }

      public string GetProjectAndResourceCombined()
      {
        return ProjectSlug + "." + Name;
      }
    }

    #endregion

    #region Members & Constants

    static readonly Regex REPLACE_EMPTY = new Regex("^.*(<string[^>]*></string>)\r\n", RegexOptions.Multiline);

    private static readonly List<TransifexResource> TransifexResources = new List<TransifexResource>();
    private static IniFile transifexIni = new IniFile();
    private static string targetDir = string.Empty;

    #endregion

    static void Main(string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse command line options
      var mpOptions = new CommandLineOptions();
      var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
      parser.ParseArgumentsStrict(args, mpOptions, () => Environment.Exit(1));

      targetDir = mpOptions.TargetDir;

      // always run verification first
      if (!Verify())
        Environment.Exit(2);

      if (mpOptions.Verify)
        Environment.Exit(0);

      if (mpOptions.ToCache)
      {
        UpdateTransifexConfig();
        CopyToCache();
      }

      if (mpOptions.Push)
        ExecutePush();

      if (mpOptions.Pull)
        ExecutePull();

      if (mpOptions.Fix)
        FixEncodings();

      if (mpOptions.FromCache)
        CopyFromCache();
    }

    #region Properties

    private static string TransifexRootFolder
    {
      get { return targetDir + @"\Tools\Transifex"; }
    }

    private static string TransifexConfigFile
    {
      get { return TransifexRootFolder + @"\.tx\config"; }
    }

    private static string TransifexClientExeFile
    {
      get { return TransifexRootFolder + @"\tx.exe"; }
    }

    #endregion

    #region Implementation

    /// <summary>
    /// Initializes <see cref="languageDirectories"/> and checks <see cref="TransifexConfigFile"/> for old/wrong references.
    /// </summary>
    /// <returns>true if <see cref="TransifexConfigFile"/> is OK.</returns>
    /// <returns>false if <see cref="TransifexConfigFile"/> contains old/wrong references.</returns>
    private static bool Verify()
    {
      Console.WriteLine();
      Console.WriteLine("Verifing the Transifex project file against the folder structure...");

      bool result = true;

      DirectoryInfo targetDirInfo = new DirectoryInfo(targetDir);
      Console.WriteLine("Searching language directories in: {0}", targetDirInfo.FullName);

      SearchLangDirs(targetDirInfo);

      if (File.Exists(TransifexConfigFile))
      {
        transifexIni.Load(TransifexConfigFile);
      }
      else
      {
        transifexIni = new IniFile();
        transifexIni.AddSection("main").AddKey("host").Value = "https://www.transifex.com";
      }

      foreach (IniFile.IniSection section in transifexIni.Sections)
      {
        if (section.Name == "main") continue;

        if (!TransifexResources.Exists(res => res.GetProjectAndResourceCombined() == section.Name))
        {
          if (result)
            Console.WriteLine(
              "A value in TransifexConfig can't be found in folder structure." + Environment.NewLine +
              "Please check and fix the following projects folders:");

          Console.WriteLine("   " + section.Name);
          result = false;
        }
      }

      foreach (var res in TransifexResources)
      {
        if (transifexIni.GetSection(res.GetProjectAndResourceCombined()) == null)
        {
          Console.WriteLine(
              "WARNING - A language directory for plugin {0} was found, but the entry does not exist in Transifex project file.", res.GetProjectAndResourceCombined());
        }
      }

      if (result)
        Console.WriteLine("Verifification done. No issues found.");
      return result;
    }

    /// <summary>
    /// Starts TransifexClient to push all English template files from Transifex Cache to Transifex.net.
    /// </summary>
    private static void ExecutePush()
    {
      ProcessStartInfo processStartInfo = new ProcessStartInfo();
      processStartInfo.FileName = TransifexClientExeFile;
      processStartInfo.Arguments = " push -s";
      processStartInfo.WorkingDirectory = TransifexRootFolder;
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;

      Process process = Process.Start(processStartInfo);
      Console.Write(process.StandardOutput.ReadToEnd());
      process.WaitForExit();
    }

    /// <summary>
    /// Starts TransifexClient to pull all translations from Transifex.net to Transifex Cache.
    /// </summary>
    private static void ExecutePull()
    {
      ProcessStartInfo processStartInfo = new ProcessStartInfo();
      processStartInfo.FileName = TransifexClientExeFile;
      processStartInfo.Arguments = " pull -f";
      processStartInfo.WorkingDirectory = TransifexRootFolder;
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;

      Process process = Process.Start(processStartInfo);
      Console.Write(process.StandardOutput.ReadToEnd());
      process.WaitForExit();
    }

    /// <summary>
    /// Searches all subdirectories of <param name="parentDirectory">parentDirectory</param> recursively
    /// for "language\string_en.xml"-folder structures and add them to <see cref="languageDirectories"/>-list.
    /// </summary>
    /// <param name="currentDir">The </param>
    private static void SearchLangDirs(DirectoryInfo currentDir)
    {
      if (currentDir.Name.ToLower() == "bin") return;
      if (currentDir.Name.ToLower() == "packages") return;

      // if current dir is a valid language dir, read resource
      if (currentDir.Name.ToLower() == "language" &&
        File.Exists(currentDir.FullName + "\\strings_en.xml"))
      {
        ReadResource(currentDir);
      }
      else
      {
        foreach (DirectoryInfo subDirectory in currentDir.GetDirectories())
          SearchLangDirs(subDirectory);
      }
    }

    private static void ReadResource(DirectoryInfo languageDir)
    {
      TransifexResource res = new TransifexResource();
      res.LanguageDirectory = languageDir.FullName;

      // if no strings_en.xml, stop reading resource
      if (!File.Exists(res.LanguageDirectory + "\\strings_en.xml")) return;

      // try to read resource infos from xml file
      res = ReadResourceInfosFromXml(res);

      // if resource name not found in xml, take parent directory of language dir
      if (String.IsNullOrEmpty(res.Name))
      {
        res.Name = languageDir.Parent.Name;
        res.ProjectSlug = "MP2";
      }

      Console.WriteLine("resource [{0,-40}] found in language directory: {1}",
        res.GetProjectAndResourceCombined(),
        res.LanguageDirectory.Replace(targetDir, string.Empty));

      TransifexResources.Add(res);
    }

    private static TransifexResource ReadResourceInfosFromXml(TransifexResource oldRes)
    {
      TransifexResource newRes = oldRes;
      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(newRes.LanguageDirectory + "\\strings_en.xml");
        XmlNodeList elemList = doc.GetElementsByTagName("resources");

        newRes.Name = elemList[0].Attributes["name"].Value;
        newRes.ProjectSlug = elemList[0].Attributes["project"].Value;
        return newRes;
      }
      catch (Exception)
      {
        return oldRes;
      }
    }

    /// <summary>
    /// Copies all English language files (strings_en.xml) from language directories to Transifex cache.
    /// </summary>
    private static void CopyToCache()
    {
      Console.WriteLine("Copying English language files to Transifex cache...");
      foreach (var res in TransifexResources)
      {
        Console.WriteLine("   " + res.LanguageDirectory.Replace(targetDir, string.Empty));
        string outputDir = res.GetCacheFullDirectory();

        if (!Directory.Exists(outputDir))
          Directory.CreateDirectory(outputDir);

        File.Copy(
          res.LanguageDirectory + "\\strings_en.xml",
          outputDir + "\\strings_en.xml", true);
      }
    }

    /// <summary>
    /// Adds resource information to <see cref="transifexIni">Transifex config</see>
    /// for each item in <see cref="languageDirectories">list of languageDirectories</see>.
    /// </summary>
    private static void UpdateTransifexConfig()
    {
      Console.WriteLine("Updating Transifex project file...");
      foreach (var res in TransifexResources)
      {
        //[MP2.SlimTvClient]
        string section = res.GetProjectAndResourceCombined();

        //file_filter = SlimTvClient\strings_<lang>.xml
        transifexIni.AddSection(section).AddKey("file_filter").Value = res.GetCacheSubDirectory() + @"\strings_<lang>.xml";
        //source_file = SlimTvClient\strings_en.xml
        transifexIni.AddSection(section).AddKey("source_file").Value = res.GetCacheSubDirectory() + @"\strings_en.xml";
        //source_lang = en
        transifexIni.AddSection(section).AddKey("source_lang").Value = "en";
        //type = ANDROID
        transifexIni.AddSection(section).AddKey("type").Value = "ANDROID";
      }

      transifexIni.Save(TransifexConfigFile);
    }

    /// <summary>
    /// Copies all non-English language files from Transifex cache to language pack plugin.
    /// </summary>
    private static void CopyFromCache()
    {
      Console.WriteLine("Copying non-English language files from Transifex cache...");
      foreach (var res in TransifexResources)
      {
        string inputDir = res.GetCacheFullDirectory();
        DirectoryInfo sourceDir = new DirectoryInfo(inputDir);
        if (!sourceDir.Exists)
          continue;

        foreach (FileInfo langFile in sourceDir.GetFiles())
        {
          if (langFile.Name.ToLower().Equals("strings_en.xml"))
            continue;

          Console.WriteLine("   " + langFile.FullName.Replace(targetDir, string.Empty));
          langFile.CopyTo(res.LanguageDirectory + "\\" + langFile.Name, true);
        }
      }
    }

    /// <summary>
    /// Temporary workaround: Open all language xml and replace &lt; and &gt; tags by valid xml encodings.
    /// </summary>
    private static void FixEncodings()
    {
      Console.WriteLine("Fixing encoding of language files...");
      foreach (var res in TransifexResources)
      {
        string inputDir = res.GetCacheFullDirectory();
        if (!Directory.Exists(inputDir))
          continue;

        foreach (FileInfo langFile in new DirectoryInfo(inputDir).GetFiles())
        {
          string content;
          bool changed = false;
          using (FileStream stream = new FileStream(langFile.FullName, FileMode.Open, FileAccess.Read))
          using (StreamReader streamReader = new StreamReader(stream))
          {
            content = streamReader.ReadToEnd();
            string orig = content;
            content = REPLACE_EMPTY.Replace(content, "");
            content = NormalizeLineBreaks(content);
            changed = (orig != content);
            streamReader.Close();
            stream.Close();
          }
          if (changed)
          {
            using (FileStream stream = new FileStream(langFile.FullName, FileMode.Create, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(stream))
            {
              streamWriter.Write(content);
              streamWriter.Close();
              stream.Close();
            }
            Console.WriteLine("Fixed file {0}!", langFile);
          }
        }
      }
    }

    /// <summary>
    /// Workaround to replace LF-linebreaks from Transifex by CR+LF-linebreaks.
    /// Replaced would be done by each git commit, so doing it before prevents having modified files, only because of the line breaks.
    /// </summary>
    /// <param name="input">String with inconsistent or no CR+LF-linebreaks.</param>
    /// <returns>String with normalized CR+LF-linebreaks.</returns>
    private static string NormalizeLineBreaks(string input)
    {
        // Allow 10% as a rough guess of how much the string may grow.
        // If we're wrong we'll either waste space or have extra copies -
        // it will still work
        StringBuilder builder = new StringBuilder((int) (input.Length * 1.1));

        bool lastWasCR = false;

        foreach (char c in input)
        {
            if (lastWasCR)
            {
                lastWasCR = false;
                if (c == '\n')
                {
                    continue; // Already written \r\n
                }
            }
            switch (c)
            {
                case '\r':
                    builder.Append("\r\n");
                    lastWasCR = true;
                    break;
                case '\n':
                    builder.Append("\r\n");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }
        return builder.ToString();
    }

    #endregion
  }
}
