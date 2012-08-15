using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using CommandLine;

namespace TransifexHelper
{
  class Program
  {
    #region struct transifex project

    private class TransifexRessource
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

    private static readonly List<TransifexRessource> TransifexRessources = new List<TransifexRessource>();
    private static IniFile transifexIni = new IniFile();
    private static string targetDir = string.Empty;

    #endregion

    static void Main(string[] args)
    {
      Thread.CurrentThread.Name = "Main";

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, mpArgs, Console.Out))
        Environment.Exit(1);

      targetDir = mpArgs.TargetDir;

      // always run verification first
      if (!Verify())
        Environment.Exit(2);

      if (mpArgs.Verify)
        Environment.Exit(0);

      if (mpArgs.ToCache)
      {
        UpdateTransifexConfig();
        CopyToCache();
      }

      if (mpArgs.Push)
        ExecutePush();

      if (mpArgs.Pull)
        ExecutePull();

      if (mpArgs.Fix)
        FixEncodings();

      if (mpArgs.FromCache)
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

        if (!TransifexRessources.Exists(res => res.GetProjectAndResourceCombined() == section.Name))
        {
          if (result)
            Console.WriteLine(
              "A value in TransifexConfig can't be found in folder structure." + Environment.NewLine +
              "Please check and fix the following projects folders:");

          Console.WriteLine("   " + section.Name);
          result = false;
        }
      }

      foreach (var res in TransifexRessources)
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

      // if current dir is a valid language dir, read resource
      if (currentDir.Name.ToLower() == "language" &&
        File.Exists(currentDir.FullName + "\\strings_en.xml"))
      {
        ReadRessource(currentDir);
      }
      else
      {
        foreach (DirectoryInfo subDirectory in currentDir.GetDirectories())
          SearchLangDirs(subDirectory);
      }
    }

    private static void ReadRessource(DirectoryInfo languageDir)
    {
      TransifexRessource res = new TransifexRessource();
      res.LanguageDirectory = languageDir.FullName;

      // if no strings_en.xml, stop reading resource
      if (!File.Exists(res.LanguageDirectory + "\\strings_en.xml")) return;

      // try to read ressource infos from xml file
      res = ReadRessourceInfosFromXml(res);

      // if ressource name not found in xml, take parent directory of language dir
      if (String.IsNullOrEmpty(res.Name))
      {
        res.Name = languageDir.Parent.Name;
        res.ProjectSlug = "MP2";
      }

      Console.WriteLine("ressource [{0,-40}] found in language directory: {1}",
        res.GetProjectAndResourceCombined(),
        res.LanguageDirectory.Replace(targetDir, string.Empty));

      TransifexRessources.Add(res);
    }

    private static TransifexRessource ReadRessourceInfosFromXml(TransifexRessource oldRes)
    {
      TransifexRessource newRes = oldRes;
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
      foreach (var res in TransifexRessources)
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
    /// Adds ressource information to <see cref="transifexIni">Transifex config</see>
    /// for each item in <see cref="languageDirectories">list of languageDirectories</see>.
    /// </summary>
    private static void UpdateTransifexConfig()
    {
      Console.WriteLine("Updating Transifex project file...");
      foreach (var res in TransifexRessources)
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
      foreach (var res in TransifexRessources)
      {
        foreach (FileInfo langFile in new DirectoryInfo(res.GetCacheFullDirectory()).GetFiles())
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
      foreach (var res in TransifexRessources)
      {
        string inputDir = res.GetCacheFullDirectory();

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
            content = content.Replace("\\'", "'");
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

    #endregion
  }
}
