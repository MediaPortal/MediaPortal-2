#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.PackageCore.Package
{
  /// <summary>
  /// PackageRoot parses a MP2 plugin package, checks if it valid and provides functionality to extract, pack and install them.
  /// </summary>
  public class PackageRoot
  {
    #region constants

    public const string PACKAGE_INFO_FILE_NAME = "PackageInfo.xml";
    public const string PLUGIN_INFO_FILE_NAME = "PluginInfo.xml";
    public const string RELEASE_INFO_FILE_NAME = "ReleaseInfo.xml";

    #endregion

    #region parsing methods

    /// <summary>
    /// Extracts and parses a MP2 package file.
    /// </summary>
    /// <param name="log">Logger to use.</param>
    /// <param name="path">Full or relative path tho the package file.</param>
    /// <param name="targetDirectory">Full or relative path to the target directory into which the package is extracted.
    /// If <c>null</c> is specified the package is extracted to a temporary folder.
    /// This folder must be deleted manually by calling the <see cref="DeletePackage"/> method.</param>
    /// <returns>Returns the parsed package object.</returns>
    public static PackageRoot ExtractPackage(ILogger log, string path, string targetDirectory = null)
    {
      if (path == null) throw new ArgumentNullException("path");

      bool mightExistAlready = false;
      if (targetDirectory == null)
      {
        targetDirectory = Path.GetTempPath();
        mightExistAlready = true;
      }

      var packageName = Path.GetFileNameWithoutExtension(path);
      if (String.IsNullOrEmpty(packageName))
        throw new ArgumentException("The package file name is invalid");

      var targetPath = Path.Combine(targetDirectory, packageName);
      if (mightExistAlready)
      {
        int n = 0;
        while (Directory.Exists(targetPath))
        {
          ++n;
          targetPath = Path.Combine(targetDirectory, packageName + "-" + n);
        }
      }
      ZipFile.ExtractToDirectory(path, targetPath);
      return ParsePackage(log, targetPath);
    }

    /// <summary>
    /// Parses an extracted package
    /// </summary>
    /// <param name="log">Logger to use.</param>
    /// <param name="path">Path to the root directory of the package.</param>
    /// <param name="createPackageInfoFile"><c>true</c> if the PackageInfo.xml file should be create, <c>false</c> if it should be parsed.</param>
    /// <returns>Returns an instance of the package.</returns>
    public static PackageRoot ParsePackage(ILogger log, string path, bool createPackageInfoFile = false)
    {
      if (path == null) throw new ArgumentNullException("path");

      var package = new PackageRoot(log);
      package.Parse(path, createPackageInfoFile);
      return package;
    }

    private void Parse(string path, bool createPackageInfoFile)
    {
      if (path == null) throw new ArgumentNullException("path");
      if (String.IsNullOrEmpty(path)) throw new ArgumentException("path must not be empty", "path");

      if (!Directory.Exists(path))
        throw new DirectoryNotFoundException(String.Format("The package root directory {0} does not exist", path));

      PackagePath = path;

      PackageInfo = createPackageInfoFile ? PackageMetaData.DefaultInfo : ParsePackageInfoFile(PackageInfoPath);

      // parse root directories 1st, so anyone can mark directories as used
      RootDirectories = ReadRootDirectories(path);

      PluginMetaData = ParsePluginInfoFile(PluginInfoPath);

      ReleaseMetaData = ParseReleaseInfoFile(ReleaseInfoPath);

      ContainedPluginMetadatas = new Collection<PluginMetadata>();

      // parse actual plugin meta data
      // plugins can normally be found in the sub folders of the auto copy root directory named "Plugins"
      var pluginsDirectory = FindRootDirectory("Plugins", true);
      if (pluginsDirectory != null)
      {
        foreach (var pluginPath in Directory.GetDirectories(pluginsDirectory.FullPath))
        {
          PluginMetadata pluginMetadata;
          if (!pluginPath.TryParsePluginDefinition(out pluginMetadata))
            throw new PluginInvalidMetadataException("Unable to parse the plugin definition file.");

          // TODO additional verification steps here
          // check package conventions (folder names and content types)
          // anything else we can think of?

          ContainedPluginMetadatas.Add(pluginMetadata);
        }
      }

      // currently a package con only contain 1 actual plugin, which meta data must match the package plugin info, if it is provided.
      MainPluginMetadata = ContainedPluginMetadatas.FirstOrDefault();
      if (MainPluginMetadata == null)
      {
        throw new PackageParseException("The package must contain at least one plugin");
      }
      if (ContainedPluginMetadatas.Count > 1)
      {
        throw new PackageParseException("Packages support only one plugin at the moment.");
      }

      // copy some meta data info out of the package
      PluginMetaData.FillMissingMetadata(MainPluginMetadata);
      PluginMetaData.CheckMetadataMismatch(MainPluginMetadata);
      ReleaseMetaData.FillMissingMetadata(MainPluginMetadata);
      ReleaseMetaData.CheckMetadataMismatch(MainPluginMetadata);

      foreach (var rootDirectory in RootDirectories)
      {
        if (!rootDirectory.IsUsed)
        {
          throw new PackageParseException(String.Format("The root directory {0} is not used", rootDirectory.RealName));
        }
      }

      if (createPackageInfoFile)
      {
        PackageInfo.Save(PackageInfoPath);
      }
    }

    /// <summary>
    /// Removes the plugin.
    /// </summary>
    /// <param name="log">Logger to use.</param>
    /// <param name="pluginName">Name of the plugin (must match plugin folder name).</param>
    /// <param name="registredPaths">Dictionary with registered path.</param>
    public static void RemovePlugin(ILogger log, string pluginName, IDictionary<string, string> registredPaths)
    {
      log.Info("Removing plugin {0} ...", pluginName);

      string pluginsPath;
      if (!registredPaths.TryGetValue("Plugins", out pluginsPath))
      {
        log.Error("Plugins directory is not registered");
        return;
      }
      var pluginDir = Path.Combine(pluginsPath, pluginName);
      if (!Directory.Exists(pluginDir))
      {
        log.Error("Plugin directory '{0}' does not exist.", pluginDir);
        return;
      }
      var releaseInfoPath = Path.Combine(pluginDir, RELEASE_INFO_FILE_NAME);
      //TODO: need to find a way how I can do a good remove without having the whole package, may be instead of just copying the ReleaseInfo.xml, 
      //TODO: the whole package structure could be dumped into a single XML file into the plugin folder
      if (true)//!File.Exists(releaseInfoPath))
      {
        log.Warn("Release info file '{0}' not found, deleting plugin directory.", releaseInfoPath);
        Directory.Delete(pluginDir, true);
      }
      else
      {
        /*var package = new PackageRoot();
        // parse release info
        package.ReleaseMetaData = ParseReleaseInfoFile(null, releaseInfoPath);
        var context = new PackageActionContext(package, PackageInstallType.Remove, printCallback, registredPaths);
        package.InstallPackage(context);*/
      }
    }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    public ILogger Log { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="log">Logger to use.</param>
    private PackageRoot(ILogger log)
    {
      Log = log;
    }

    /// <summary>
    /// Parses a package info file
    /// </summary>
    /// <param name="path">Path to the package info file.</param>
    /// <returns>Returns the package meta data.</returns>
    public PackageMetaData ParsePackageInfoFile(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException(String.Format("The package info file {0} does not exist", path));

      var xDoc = XDocument.Load(path);
      if (xDoc.Root == null || !xDoc.Root.Name.LocalName.Equals("PackageInfo"))
        throw new PackageParseException(String.Format("The file {0} is not an valid package info file", path));

      var packageInfo = new PackageMetaData(xDoc.Root);
      string message;
      if (!packageInfo.CheckValid(this, out message))
      {
        throw new PackageParseException(String.Format("The package info file {0} is invalid: {1}", path, message));
      }
      return packageInfo;
    }

    /// <summary>
    /// Parses a package plugin info file.
    /// </summary>
    /// <param name="path">Path to the package plugin info file.</param>
    /// <returns>Returns the plugin meta data.</returns>
    public PackagePluginMetaData ParsePluginInfoFile(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException(String.Format("The plugin info file {0} does not exist", path));

      var xDoc = XDocument.Load(path);
      if (xDoc.Root == null || !xDoc.Root.Name.LocalName.Equals("PluginInfo"))
        throw new PackageParseException(String.Format("The file {0} is not an valid plugin info file", path));

      try
      {
        var pluginInfo = new PackagePluginMetaData(xDoc.Root);
        string message;
        if (!pluginInfo.CheckValid(this, out message))
        {
          throw new PackageParseException(String.Format("The plugin info file {0} is invalid: {1}", path, message));
        }
        return pluginInfo;
      }
      catch (PackageParseException ex)
      {
        if (String.IsNullOrEmpty(ex.FilePath))
        {
          ex.FilePath = path;
        }
        throw;
      }
    }

    /// <summary>
    /// Parses the package release info file.
    /// </summary>
    /// <param name="path">Path to the package release info file.</param>
    /// <returns>Return the package release meta data.</returns>
    public PackageReleaseMetaData ParseReleaseInfoFile(string path)
    {
      try
      {
        if (!File.Exists(path))
          throw new FileNotFoundException(String.Format("The release info file {0} does not exist", path));

        var xDoc = XDocument.Load(path);
        if (xDoc.Root == null || !xDoc.Root.Name.LocalName.Equals("ReleaseInfo"))
          throw new PackageParseException("The file is not an valid release info file", path);

        var releaseInfo = new PackageReleaseMetaData(xDoc.Root);
        string message;
        if (!releaseInfo.CheckValid(this, out message))
        {
          throw new PackageParseException(String.Format("The release info file is invalid: {0}", message), path);
        }

        return releaseInfo;
      }
      catch (PackageParseException ex)
      {
        if (String.IsNullOrEmpty(ex.FilePath))
        {
          ex.FilePath = path;
        }
        throw;
      }
    }

    /// <summary>
    /// Reads all directories from the package root path.
    /// </summary>
    /// <param name="path">Package root path.</param>
    /// <returns>Returns a collection with all directories in the package root path.</returns>
    public static ICollection<PackageRootDirectory> ReadRootDirectories(string path)
    {
      return Directory.GetDirectories(path).Select(directory => new PackageRootDirectory(directory)).ToList();
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the package root directory path.
    /// </summary>
    public string PackagePath { get; private set; }

    /// <summary>
    /// Gets the full path of the package info file.
    /// </summary>
    public string PackageInfoPath
    {
      get { return Path.Combine(PackagePath, PACKAGE_INFO_FILE_NAME); }
    }

    /// <summary>
    /// Gets the package info meta data.
    /// </summary>
    public PackageMetaData PackageInfo { get; private set; }

    /// <summary>
    /// Gets the full path of the plugin info file.
    /// </summary>
    public string PluginInfoPath
    {
      get { return Path.Combine(PackagePath, PLUGIN_INFO_FILE_NAME); }
    }

    /// <summary>
    /// Gets the plugin meta data.
    /// </summary>
    public PackagePluginMetaData PluginMetaData { get; private set; }

    /// <summary>
    /// Gets the full path of the release info file.
    /// </summary>
    public string ReleaseInfoPath
    {
      get { return Path.Combine(PackagePath, RELEASE_INFO_FILE_NAME); }
    }

    /// <summary>
    /// Gets the release meta data.
    /// </summary>
    public PackageReleaseMetaData ReleaseMetaData { get; private set; }

    /// <summary>
    /// gets the collection with the directories in the package root directory.
    /// </summary>
    public ICollection<PackageRootDirectory> RootDirectories { get; private set; }

    /// <summary>
    /// Gets the collection with the contained plugin meta data.
    /// </summary>
    public ICollection<PluginMetadata> ContainedPluginMetadatas { get; private set; }

    /// <summary>
    /// Gets the main plugin meta data.
    /// </summary>
    public PluginMetadata MainPluginMetadata { get; private set; }

    #endregion

    /// <summary>
    /// Sets the root directory which contains the directory or file given by <param name="path"> to used.</param>
    /// </summary>
    /// <param name="path">Path for which the root directory should be set as used.</param>
    public void SetRootDirectoryUsed(string path)
    {
      // get the 1st directory name, look it up in the root directory list and set it as used
      int n = path.LastIndexOfAny(new[] { '\\', '/' });
      if (n == 0) // rooted path
        return;
      var directory = n < 0 ? path : path.Substring(0, n);
      var rootDirectory = FindRootDirectory(directory);
      if (rootDirectory != null)
      {
        rootDirectory.SetUsed();
      }
    }

    /// <summary>
    /// Finds a root directory by it's real name.
    /// </summary>
    /// <param name="realName">Real name of the directory.</param>
    /// <returns>Returns the <see cref="PackageRootDirectory"/> or <c>null</c> if not matching directory was found.</returns>
    /// <remarks>
    /// The real name contains the prefix and suffix for auto copy directories.
    /// </remarks>
    public PackageRootDirectory FindRootDirectory(string realName)
    {
      return RootDirectories.FirstOrDefault(rootDirectory => rootDirectory.RealName.Equals(realName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a root directory by it's name.
    /// </summary>
    /// <param name="name">Name of the directory.</param>
    /// <param name="isAutoCopyDirectory"><c>true</c> if a auto copy directory is to be found; else <c>false</c>.</param>
    /// <returns>Returns the <see cref="PackageRootDirectory"/> or <c>null</c> if not matching directory was found.</returns>
    public PackageRootDirectory FindRootDirectory(string name, bool isAutoCopyDirectory)
    {
      return RootDirectories.FirstOrDefault(rootDirectory => 
        rootDirectory.IsAutoCopyDirectory == isAutoCopyDirectory && rootDirectory.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates the package file.
    /// </summary>
    /// <param name="packageFilePath">File path to the target file.</param>
    /// <remarks>
    /// A package file is a ZIP file without base directory and the file extension .mp2x.
    /// </remarks>
    public void CreatePackage(string packageFilePath)
    {
      // do not include base directory, since it might have any name
      ZipFile.CreateFromDirectory(PackagePath, packageFilePath, CompressionLevel.Optimal, false);
    }

    /// <summary>
    /// Deletes the package path.
    /// </summary>
    /// <remarks>This method can be used to delete a temporary target directory where a package file was extracted to.</remarks>
    public void DeletePackage()
    {
      if (Directory.Exists(PackagePath))
      {
        Directory.Delete(PackagePath, true);
      }
    }

    /// <summary>
    /// Installs a package.
    /// </summary>
    /// <param name="installType">Type of install.</param>
    /// <param name="registredPaths">Dictionary with registered path.</param>
    /// <remarks>
    /// This overload does not make any outputs during the installation process.
    /// </remarks>
    public void InstallPackage(PackageInstallType installType, IDictionary<string, string> registredPaths)
    {
      var context = new PackageActionContext(this, installType, registredPaths);
      InstallPackage(context);
    }

    /// <summary>
    /// Install a package.
    /// </summary>
    /// <param name="context">Context describing the install environment and parameters.</param>
    /// <remarks>Use this overload to have maximum flexibility.</remarks>
    public void InstallPackage(PackageActionContext context)
    {
      // get the action list
      PackageActionCollection actions;
      switch (context.InstallType)
      {
        case PackageInstallType.Install:
          actions = ReleaseMetaData.InstallActions;
          break;
        case PackageInstallType.Update:
          actions = ReleaseMetaData.UpdateActions;
          break;
        case PackageInstallType.Remove:
          actions = ReleaseMetaData.RemoveActions;
          break;
        default:
          throw new InvalidOperationException("Invalid install type");
      }

      // execute actions one after the other.
      foreach (var action in actions)
      {
        action.Execute(context);
      }

      if (context.InstallType != PackageInstallType.Remove)
      {
        // copy the release info file, which contains the remove actions to the plugin folder
        var pluginTargetDir = Path.Combine(context.GetPath("Plugins"), PluginMetaData.Name);
        if (Directory.Exists(pluginTargetDir))
        {
          // ReSharper disable once AssignNullToNotNullAttribute
          File.Copy(ReleaseInfoPath, Path.Combine(pluginTargetDir, Path.GetFileName(ReleaseInfoPath)), true);
        }
      }
    }
  }
}