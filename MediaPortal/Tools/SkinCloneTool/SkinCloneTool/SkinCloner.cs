#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkinCloneTool
{
  public class SkinCloner
  {
    /// <summary>
    /// Helper class to store all applied changes
    /// </summary>
    private class ProjectInfos
    {
      public string OldRootNameSpace { get; set; }
      public string NewRootNameSpace { get; set; }

      public object OldAssemblyName { get; set; }
      public string NewAssemblyName { get; set; }

      public string OldSkinDir { get; set; }
      public string NewSkinDir { get; set; }

      public string NewPluginId { get; set; }
      public string OldSkinName { get; set; }

      public Dictionary<string, string> ModelIdTranslations = new Dictionary<string, string>();
    }

    public static void Process(SkinCloneArguments args)
    {
      string sourcePath = Path.Combine(args.PluginFolder, args.SourceSkin);
      string targetPath = Path.Combine(args.PluginFolder, args.TargetSkin);
      if (!Directory.Exists(sourcePath))
        throw new ArgumentException(string.Format("Source folder does not exists: {0}", sourcePath));
      if (Directory.Exists(targetPath) && !args.Overwrite)
        throw new ArgumentException(string.Format("Target folder already not exists: {0}", targetPath));

      if (Directory.Exists(targetPath) && args.Overwrite)
        Directory.Delete(targetPath, true);

      Directory.CreateDirectory(targetPath);
      var source = new DirectoryInfo(sourcePath);
      var target = new DirectoryInfo(targetPath);
      CopyFilesRecursively(source, target);

      // Process project file
      var infos = new ProjectInfos();
      ProcessProject(target, args, infos);
      ProcessSkinFolder(target, args, infos);
      ProcessSkinXml(target, args, infos);
      ProcessPluginXml(target, args, infos);
      ProcessNameSpacesAndModels(target, args, infos);
      ProcessLanguage(target, args, infos);
    }

    private static void ProcessProject(DirectoryInfo target, SkinCloneArguments args, ProjectInfos infos)
    {
      var projectFile = target.GetFiles("*.csproj").First();
      string content = File.ReadAllText(projectFile.FullName);
      // <ProjectGuid>{874401F8-1283-4C20-8EFA-FD0EE7AD53A8}</ProjectGuid>
      Regex reProjectId = new Regex("<ProjectGuid>.*</ProjectGuid>");
      content = reProjectId.Replace(content, string.Format("<ProjectGuid>{0}</ProjectGuid>", Guid.NewGuid().ToString("B").ToUpperInvariant()));

      // <RootNamespace>MediaPortal.UiComponents.WMCSkin</RootNamespace>
      Regex reRootNs = new Regex("(<RootNamespace>)(.*)(</RootNamespace>)");
      string ns = reRootNs.Match(content).Groups[2].Value;
      var parts = ns.Split('.');
      var newNs = string.Join(".", parts.Take(parts.Length - 1).Concat(new List<string> { args.TargetSkin }).ToArray());

      infos.OldRootNameSpace = ns;
      infos.NewRootNameSpace = newNs;

      content = reRootNs.Replace(content, string.Format("$1{0}$3", newNs));

      // <AssemblyName>WMCSkin</AssemblyName>
      Regex reAssName = new Regex("(<AssemblyName>)(.*)(</AssemblyName>)");
      infos.OldAssemblyName = reAssName.Match(content).Groups[2].Value;
      content = reAssName.Replace(content, string.Format("$1{0}$3", args.TargetSkin));
      infos.NewAssemblyName = args.TargetSkin;

      // Rename all project references
      Regex reProjectFiles = new Regex(@"(Skin\\)([^\\]*)(.*)");
      content = reProjectFiles.Replace(content, string.Format("$1{0}$3", args.TargetSkin));

      string targetProjectFile = Path.Combine(target.FullName, args.TargetSkin + ".csproj");
      File.WriteAllText(targetProjectFile, content);

      projectFile.Delete();
    }

    private static void ProcessSkinFolder(DirectoryInfo target, SkinCloneArguments args, ProjectInfos infos)
    {
      var skinDir = new DirectoryInfo(Path.Combine(target.FullName, "Skin"));
      var skinNameDir = skinDir.GetDirectories().First();

      var newSkinDir = Path.Combine(skinNameDir.FullName, "..", args.TargetSkin);
      Directory.Move(skinNameDir.FullName, newSkinDir);
      infos.OldSkinDir = skinNameDir.FullName;
      infos.NewSkinDir = newSkinDir;
    }

    private static void ProcessSkinXml(DirectoryInfo target, SkinCloneArguments args, ProjectInfos infos)
    {
      var skinXml = Path.Combine(infos.NewSkinDir, "skin.xml");
      var content = File.ReadAllText(skinXml);

      //<Skin Name="WMCSkin" Version="1.0">
      Regex reName = new Regex("(Skin Name=\")([^\"]*)(.*)");
      content = reName.Replace(content, string.Format("$1{0}$3", args.TargetSkin));

      //  <ShortDescription>WMC</ShortDescription>
      Regex reDesc = new Regex("(<ShortDescription>)(.*)(</ShortDescription>)");
      content = reDesc.Replace(content, string.Format("$1{0}$3", args.TargetSkin));

      File.WriteAllText(skinXml, content);
    }

    private static void ProcessPluginXml(DirectoryInfo target, SkinCloneArguments args, ProjectInfos infos)
    {
      var pluginXml = Path.Combine(target.FullName, "plugin.xml");
      var content = File.ReadAllText(pluginXml);

      //<Plugin
      //    DescriptorVersion="1.0"
      //    Name="WMC"
      //    PluginId="{874401F8-1283-4C20-8EFA-FD0EE7AD53A8}"
      //    Author="Morpheus_xx, ge2301, Brownard"
      //    Copyright="GPL"
      //    AutoActivate="true"
      //    Description="WMC skin for MP2.">
      Regex reName = new Regex("(<Plugin[^>]*Name=\")([^\"]*)(\"[^>]*PluginId=\")([^\"]*)([^>]*)", RegexOptions.Singleline);
      string newPluginId = Guid.NewGuid().ToString("B").ToUpperInvariant();
      infos.OldSkinName = reName.Match(content).Groups[2].Value;
      content = reName.Replace(content, string.Format("$1{0}$3{1}$5", args.TargetSkin, newPluginId));
      infos.NewPluginId = newPluginId;

      // <Assembly FileName="WMCSkin.dll"/>
      Regex reAssembly = new Regex("(<Assembly FileName=\")([^\"]*)(\\.dll\"/>)");
      content = reAssembly.Replace(content, string.Format("$1{0}$3", infos.NewAssemblyName));

      // <Resource Id="WMCLanguage" Directory="Language" Type="Language"/>
      Regex reLang = new Regex("(<Resource Id=\")([^\"]*)([^>]*Type=\"Language\"/>)");
      content = reLang.Replace(content, string.Format("$1{0}$3", args.TargetSkin + "Language"));

      // <Resource Id="WMCkin" Directory="Skin" Type="Skin"/>
      Regex reSkinId = new Regex("(<Resource Id=\")([^\"]*)([^>]*Type=\"Skin\"/>)");
      content = reSkinId.Replace(content, string.Format("$1{0}$3", args.TargetSkin + "Skin"));

      // (ClassName=")([^"]*)([^>]*)
      Regex reClassNames = new Regex("(ClassName=\")(" + infos.OldRootNameSpace + ")([^\"]*)");
      content = reClassNames.Replace(content, string.Format("$1{0}$3", infos.NewRootNameSpace));

      // Create new model Guids and remember the old->new replacements
      Regex reModelIds = new Regex("(<Model Id=\")([^\"]*)\"([^>]*)");
      var matches = reModelIds.Matches(content);
      foreach (Match match in matches)
      {
        string oldModelId = Guid.Parse(match.Groups[2].Value).ToString("D").ToUpperInvariant();
        string newModelId = Guid.NewGuid().ToString("D").ToUpperInvariant();
        infos.ModelIdTranslations[oldModelId] = newModelId;
      }

      Regex reSkinSettings = new Regex("(<SkinSettings Id=\")([^\"]*)\"([^>]*)");
      matches = reSkinSettings.Matches(content);
      foreach (Match match in matches)
      {
        string oldModelId = Guid.Parse(match.Groups[2].Value).ToString("D").ToUpperInvariant();
        string newModelId = Guid.NewGuid().ToString("D").ToUpperInvariant();
        infos.ModelIdTranslations[oldModelId] = newModelId;
      }
      foreach (var translation in infos.ModelIdTranslations)
      {
        var reKvp = new Regex(translation.Key, RegexOptions.IgnoreCase);
        content = reKvp.Replace(content, translation.Value);
      }

      // Create unique skin setting IDs
      Regex reSettingIds = new Regex("<Register Location=\"/Configuration/Settings/Appearance/Skin/SkinSettings\">*(?:[^<]*<ConfigSetting[^<]*Id=\"([^\"]*)\"[^/>]*/>)+\\s+</Register>");

      var matchResult = reSettingIds.Match(content);
      {
        foreach (Capture capture in matchResult.Groups[1].Captures)
        {
          var oldId = capture.Value;
          if (oldId.StartsWith(args.SourceSkin))
            oldId = oldId.Substring(args.SourceSkin.Length);
          if (oldId.StartsWith(infos.OldSkinName))
            oldId = oldId.Substring(infos.OldSkinName.Length);

          content = content.Replace(string.Format("Id=\"{0}\"", capture.Value), string.Format("Id=\"{1}{0}\"", oldId, args.TargetSkin));
        }
      }

      // Replace all Name="WMC" (old skin name)
      content = content.Replace("Name=\"" + infos.OldSkinName + "\"", "Name=\"" + args.TargetSkin + "\"");

      // Replace all localized labels, starting with old name i.e. [WMC.Enable...]
      content = content.Replace("[" + infos.OldSkinName + ".", "[" + args.TargetSkin + ".");

      File.WriteAllText(pluginXml, content);
    }

    private static void ProcessNameSpacesAndModels(DirectoryInfo target, SkinCloneArguments args, ProjectInfos infos)
    {
      foreach (FileInfo file in target.GetFiles().FilterCodeFiles())
        ProcessNameSpacesAndModels(Path.Combine(target.FullName, file.Name), args, infos);

      foreach (DirectoryInfo dir in target.GetDirectories().FilterDirectories())
        ProcessNameSpacesAndModels(dir, args, infos);
    }

    private static void ProcessNameSpacesAndModels(string fileName, SkinCloneArguments args, ProjectInfos infos)
    {
      var content = File.ReadAllText(fileName);

      // Replace namespaces
      var reNs = new Regex(infos.OldRootNameSpace);
      content = reNs.Replace(content, infos.NewRootNameSpace);

      // Replace Guid references
      foreach (var translation in infos.ModelIdTranslations)
      {
        var reKvp = new Regex(translation.Key, RegexOptions.IgnoreCase);
        content = reKvp.Replace(content, translation.Value);
      }

      // Replace all localized labels, starting with old name i.e. [WMC.Enable...]
      content = content.Replace("[" + infos.OldSkinName + ".", "[" + args.TargetSkin + ".");

      // Replace all: IsVisible="{Binding Source={StaticResource SkinSettingsModel}, Path=[WMC].EnableFanart}">
      content = content.Replace("Path=[" + infos.OldSkinName + "].", "Path=[" + args.TargetSkin + "].");

      // Converter references
      //    xmlns:wmc="clr-namespace:MediaPortal.UiComponents.New1.Controls;assembly=WMCSkin"
      content = content.Replace(";assembly=" + infos.OldAssemblyName, ";assembly=" + infos.NewAssemblyName);

      // Replace all localized labels, starting with old name i.e. [WMC.Enable...]
      content = content.Replace("SKIN_NAME = \"" + infos.OldAssemblyName + "\"", "SKIN_NAME = \"" + args.TargetSkin + "\"");
      content = content.Replace("SKIN_NAME = \"" + infos.OldSkinName + "\"", "SKIN_NAME = \"" + args.TargetSkin + "\"");

      File.WriteAllText(fileName, content);
    }

    private static void ProcessLanguage(DirectoryInfo target, SkinCloneArguments args, ProjectInfos infos)
    {
      var filename = Path.Combine(target.FullName, "Language", "strings_en.xml");
      if (!File.Exists(filename))
        return;
      var content = File.ReadAllText(filename);
      content = content.Replace("name=\"" + infos.OldSkinName + ".", "name=\"" + args.TargetSkin + ".");

      File.WriteAllText(filename, content);
    }

    public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
      foreach (DirectoryInfo dir in source.GetDirectories().FilterDirectories())
        CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
      foreach (FileInfo file in source.GetFiles().FilterFiles(source))
        file.CopyTo(Path.Combine(target.FullName, file.Name));
    }
  }

  public static class DirectoryExtensions
  {
    public static IEnumerable<DirectoryInfo> FilterDirectories(this IEnumerable<DirectoryInfo> directoryInfos)
    {
      HashSet<string> filters = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
      {
        "bin",
        "obj",
      };
      foreach (var directoryInfo in directoryInfos)
      {
        if (filters.Contains(directoryInfo.Name))
          continue;
        yield return directoryInfo;
      }
    }

    public static IEnumerable<FileInfo> FilterFiles(this IEnumerable<FileInfo> fileInfos, DirectoryInfo directoryInfo)
    {
      foreach (var fileInfo in fileInfos)
      {
        // Ignore all non-english language files
        if (directoryInfo.Name.Equals("Language", StringComparison.InvariantCultureIgnoreCase) && !fileInfo.Name.Equals("strings_en.xml", StringComparison.InvariantCultureIgnoreCase))
          continue;
        yield return fileInfo;
      }
    }

    public static IEnumerable<FileInfo> FilterCodeFiles(this IEnumerable<FileInfo> fileInfos)
    {
      foreach (var fileInfo in fileInfos)
      {
        if (!fileInfo.Name.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase) && !fileInfo.Name.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase))
          continue;
        yield return fileInfo;
      }
    }
  }
}
