using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommandLine;

namespace TransifexHelper
{
  class Program
  {
    #region Members & Constants

    private static Dictionary<string, DirectoryInfo> languageDirectories = new Dictionary<string, DirectoryInfo>();
    private static IniFile transifexIni = new IniFile();
    private static string targetDir = string.Empty;

    private const string ProjectSlug = "MP2";
    private const string CacheDir = "Cache";

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

    private static string TransifexCacheFolder
    {
      get { return TransifexRootFolder + "\\" + CacheDir; }
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
      transifexIni.Load(TransifexConfigFile);

      foreach (IniFile.IniSection section in transifexIni.Sections)
      {
        if (section.Name == "main") continue;

        if (!languageDirectories.ContainsKey(section.Name.Split('.')[1]))
        {
          if (result)
            Console.WriteLine(
              "A value in TransifexConfig can't be found in folder structure." + Environment.NewLine +
              "Please check and fix the following projects folders:");

          Console.WriteLine("   " + section.Name);
          result = false;
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
    /// <param name="parentDirectory">The </param>
    private static void SearchLangDirs(DirectoryInfo parentDirectory)
    {
      if (parentDirectory.Name.ToLower() == "bin") return;

      List<DirectoryInfo> subDirectories = new List<DirectoryInfo>(parentDirectory.GetDirectories());
      bool langDirFound = false;

      // check if current parentDirectory contains a "language\strings_en.xml"-subdirectory
      foreach (DirectoryInfo subDirectory in subDirectories)
      {
        if (subDirectory.Name.ToLower() != "language")
          continue;

        if (!File.Exists(subDirectory.FullName + "\\strings_en.xml"))
          break;

        Console.WriteLine("Language directory found: {0}",
          subDirectory.FullName.Replace(targetDir, string.Empty));

        languageDirectories.Add(subDirectory.Parent.Name, subDirectory);
        langDirFound = true;
      }

      // search all subdirectories, if no "language\strings_en.xml"-subdirectory was found
      if (!langDirFound)
        foreach (DirectoryInfo subDirectory in subDirectories)
          SearchLangDirs(subDirectory);
    }

    /// <summary>
    /// Copies all English language files (strings_en.xml) from language directories to Transifex cache.
    /// </summary>
    private static void CopyToCache()
    {
      Console.WriteLine("Copying English language files to Transifex cache...");
      foreach (KeyValuePair<string, DirectoryInfo> pair in languageDirectories)
      {
        Console.WriteLine("   " + pair.Value.FullName.Replace(targetDir, string.Empty));
        string outputDir = TransifexCacheFolder + "\\" + pair.Key;

        if (!Directory.Exists(outputDir))
          Directory.CreateDirectory(outputDir);

        foreach (FileInfo langFile in pair.Value.GetFiles())
        {
          if (!langFile.Name.ToLower().Equals("strings_en.xml"))
            continue;

          langFile.CopyTo(outputDir + @"\" + langFile.Name, true);
        }
      }
    }

    /// <summary>
    /// Adds ressource information to <see cref="transifexIni">Transifex config</see>
    /// for each item in <see cref="languageDirectories">list of languageDirectories</see>.
    /// </summary>
    private static void UpdateTransifexConfig()
    {
      Console.WriteLine("Updating Transifex project file...");
      foreach (KeyValuePair<string, DirectoryInfo> pair in languageDirectories)
      {
        //[MP2.SlimTvClient]
        string section = ProjectSlug + "." + pair.Key;

        //file_filter = SlimTvClient\strings_<lang>.xml
        transifexIni.AddSection(section).AddKey("file_filter").Value = CacheDir + "\\" + pair.Key + @"\strings_<lang>.xml";
        //source_file = SlimTvClient\strings_en.xml
        transifexIni.AddSection(section).AddKey("source_file").Value = CacheDir + "\\" + pair.Key + @"\strings_en.xml";
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
      foreach (KeyValuePair<string, DirectoryInfo> pair in languageDirectories)
      {
        string inputDir = TransifexCacheFolder + "\\" + pair.Key;

        foreach (FileInfo langFile in new DirectoryInfo(inputDir).GetFiles())
        {
          if (langFile.Name.ToLower().Equals("strings_en.xml"))
            continue;

          Console.WriteLine("   " + langFile.FullName.Replace(targetDir, string.Empty));
          langFile.CopyTo(pair.Value.FullName + "\\" + langFile.Name);
        }
      }
    }

    #endregion
  }
}
