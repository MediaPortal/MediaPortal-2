#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore.Package.Action;


namespace MediaPortal.PackageCore.Package.Root
{
  /// <summary>
  /// The PackageModel represents the root element of an MP2 package.xml file.
  /// </summary>
  /// <remarks>
  /// This class also provides all methods to manage a package.
  /// </remarks>
  [DebuggerDisplay("Package({Name}-{Version}-{Channel})")]
  partial class PackageModel : ICheckable
  {
    #region const

    public const string PACKAGE_MANAGER_DATA_PATH_LABEL = "PackageManagerData";

    #endregion

    #region private fileds

    [XmlIgnore]
    private ZipArchive _archive;

    #endregion


    #region public properties

    [XmlIgnore]
    public string PackageDirectory { get; private set; }

    [XmlIgnore]
    public bool ExtractOnDemand
    {
       get { return _archive != null; }
    }

    #endregion

    #region static methods

    public static PackageModel Load(string path, ILogger log)
    {
      var serializer = new XmlSerializer(typeof(PackageModel));
      serializer.UnknownAttribute += (sender, args) => log.Error("Unknown attribute: {0} ({1},{2})", args.Attr, args.LineNumber, args.LinePosition);
      serializer.UnknownElement += (sender, args) => log.Error("Unknown element: {0} ({1},{2})", args.Element, args.LineNumber, args.LinePosition);
      serializer.UnknownNode += (sender, args) => log.Error("Unknown node: {0} ({1},{2})", args.Name, args.LineNumber, args.LinePosition);
      serializer.UnreferencedObject += (sender, args) => log.Error("Unreferenced object: {0} ({1})", args.UnreferencedId, args.UnreferencedObject);
      using (var stream = new StreamReader(path))
      {
        return (PackageModel) serializer.Deserialize(stream);
      }
    }

    /// <summary>
    /// Parses an extracted package
    /// </summary>
    /// <param name="archive">Zip archive to extract files from.</param>
    /// <param name="directory">Path to the root directory of the package.</param>
    /// <param name="log">Logger to use.</param>
    /// <returns>Returns an instance of the package.</returns>
    public static PackageModel ParsePackage(ZipArchive archive, string directory, ILogger log)
    {
      log.Info("Parsing package directory");

      if (directory == null) throw new ArgumentNullException("directory");
      if (String.IsNullOrEmpty(directory)) throw new ArgumentException("directory must not be empty", "directory");

      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var packageFilePath = Path.Combine(directory, "Package.xml");
      if (!File.Exists(packageFilePath))
      {
        var entry = archive.GetEntry("Package.xml");
        if (entry != null)
        {
          entry.ExtractToFile(packageFilePath);
        }
        else
        {
          throw new DirectoryNotFoundException(String.Format("The package root directory {0} does contain a Package.xml file", directory));
        }
      }

      log.Info("Reading Package.xml file");
      var package = Load(packageFilePath, log);
      package.Initialize(archive, directory, log);

      return package;
    }

    /// <summary>
    /// Parses an extracted package
    /// </summary>
    /// <param name="directory">Path to the root directory of the package.</param>
    /// <param name="log">Logger to use.</param>
    /// <returns>Returns an instance of the package.</returns>
    public static PackageModel ParsePackage(string directory, ILogger log)
    {
      log.Info("Parsing package directory");

      if (directory == null) throw new ArgumentNullException("directory");
      if (String.IsNullOrEmpty(directory)) throw new ArgumentException("directory must not be empty", "directory");

      if (!Directory.Exists(directory))
        throw new DirectoryNotFoundException(String.Format("The package root directory {0} does not exist", directory));

      var packageFilePath = Path.Combine(directory, "Package.xml");
      if (!File.Exists(packageFilePath))
        throw new DirectoryNotFoundException(String.Format("The package root directory {0} does contain a Package.xml file", directory));

      log.Info("Reading Package.xml file");
      var package = Load(packageFilePath, log);
      package.Initialize(directory, log);

      return package;
    }

    /// <summary>
    /// Opens and parses a MP2 package file.
    /// </summary>
    /// <param name="path">Full or relative path tho the package file.</param>
    /// <param name="fullyExtract"><c>true</c> if the package should be fully extracted, or <c>false</c> if only the actually needed files should be extracted.</param>
    /// <param name="targetDirectory">Full or relative path to the target directory into which the package is extracted.
    /// If <c>null</c> is specified the package is extracted to a temporary folder.
    /// This folder must be deleted manually by calling the <see cref="DeletePackage"/> method.</param>
    /// <param name="log">Logger to use.</param>
    /// <returns>Returns the parsed package object.</returns>
    public static PackageModel OpenPackage(string path, bool fullyExtract, string targetDirectory, ILogger log)
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
      if (fullyExtract)
      {
        ZipFile.ExtractToDirectory(path, targetPath);
        return ParsePackage(targetPath, log);
      }
      var archive = ZipFile.OpenRead(path);
      Directory.CreateDirectory(targetPath);
      return ParsePackage(archive, targetPath, log);
    }

    #endregion

    #region public methods

    public string GetArchivePath(string path)
    {
      path = Path.GetFullPath(path);
      if (path.StartsWith(PackageDirectory))
      {
        path = path.Substring(PackageDirectory.Length);
        if (path.StartsWith("\\", StringComparison.Ordinal))
        {
          path = PackageDirectory.Substring(1);
        }
      }
      return path;
    }

    public ZipArchiveEntry GetArchiveEntry(string path)
    {
      if (ExtractOnDemand)
      {
        path = GetArchivePath(path);
        _archive.GetEntry(path);
      }
      return null;
    }

    public void EnsureFileExtracted(string path)
    {
      if (ExtractOnDemand && !File.Exists(path))
      {
        var entry = GetArchiveEntry(path);
        entry.ExtractToFile(path);
      }
    }

    public void EnsureDirectoryExtracted(string path)
    {
      if (ExtractOnDemand)
      {
        var archivePath = GetArchivePath(path);
        foreach (var entry in _archive.Entries)
        {
          if (entry.FullName.StartsWith(archivePath))
          {
            var targetPath = Path.Combine(PackageDirectory, archivePath);
            if (!File.Exists(targetPath))
            {
              var targetDir = Path.GetDirectoryName(targetPath);
              if (targetDir != null && !Directory.Exists(targetDir))
              {
                Directory.CreateDirectory(targetDir);
              }
              entry.ExtractToFile(targetPath);
            }
          }
        }
      }
    }

    /// <summary>
    /// Creates the package file.
    /// </summary>
    /// <param name="packageFilePath">File path to the target file.</param>
    /// <param name="log">Logger to use.</param>
    /// <remarks>
    /// A package file is a ZIP file without base directory and the file extension .mp2x.
    /// </remarks>
    public void CreatePackage(string packageFilePath, ILogger log)
    {
      log.Info("Creating package file");
      // do not include base directory, since it might have any name
      ZipFile.CreateFromDirectory(PackageDirectory, packageFilePath, CompressionLevel.Optimal, false);
    }

    /// <summary>
    /// Deletes the package path.
    /// </summary>
    /// <remarks>This method can be used to delete a temporary target directory where a package file was extracted to.</remarks>
    public void DeletePackage()
    {
      if (Directory.Exists(PackageDirectory))
      {
        Directory.Delete(PackageDirectory, true);
      }
    }

    /// <summary>
    /// Installs a package.
    /// </summary>
    /// <param name="option">The option to install.</param>
    /// <param name="installType">Type of install.</param>
    /// <param name="registredPaths">Dictionary with registered path.</param>
    /// <remarks>
    /// This overload does not make any outputs during the installation process.
    /// </remarks>
    public void InstallPackage(InstallOptionModel option, InstallType installType, IDictionary<string, string> registredPaths, ILogger log)
    {
      var context = new InstallContext(this, installType, registredPaths, log);
      InstallPackage(option, context);
    }

    /// <summary>
    /// Install a package.
    /// </summary>
    /// <param name="option">The option to install.</param>
    /// <param name="context">Context describing the install environment and parameters.</param>
    /// <remarks>Use this overload to have maximum flexibility.</remarks>
    public void InstallPackage(InstallOptionModel option, InstallContext context)
    {
      option = option ?? InstallOptions.GetDefaultOption();
      WritePackageInstallState(context, PackageInstallState.Installing, option);
      try
      {
        foreach (var optionContent in option.Contents)
        {
          optionContent.ReferencedContent.Install(context);
        }
        WritePackageInstallState(context, PackageInstallState.Installed, option);
      }
      catch (Exception ex)
      {
        WritePackageInstallState(context, PackageInstallState.InstallFailed, option, ex);
      }
    }

    #endregion

    #region private/internal methods

    private void Initialize(ZipArchive archive, string directory, ILogger log)
    {
      _archive = archive;
      Initialize(directory, log);
    }

    private void Initialize(string directory, ILogger log)
    {
      PackageDirectory = directory;

      if (Images != null)
      {
        Images.Initialize(this, log);
      }

      if (Links != null)
      {
        Links.Initialize(this, log);
      }

      if (ReleaseInfo != null)
      {
        ReleaseInfo.Initialize(this, log);
      }

      if (Content != null)
      {
        Content.Initialize(this, log);
      }

      // InstallOptions must be initialized after Content!
      if (InstallOptions != null)
      {
        InstallOptions.Initialize(this, log);
      }
      else if (Content != null)
      {
        // create default install option "All"
        InstallOptions = new InstallOptionsModel();
        var option = new InstallOptionModel()
        {
          Name = "All",
          IsDefault = true
        };
        foreach (var content in Content.Contents)
        {
          option.Contents.Add(new ContentRefModel()
          {
            Name = content.Name
          });
        }
        InstallOptions.Options.Add(option);
        InstallOptions.Initialize(this, log);
      }
    }

    private void WritePackageInstallState(InstallContext context, PackageInstallState packageInstallState, InstallOptionModel option, Exception exception = null)
    {
      var dataPath = context.GetPath(PACKAGE_MANAGER_DATA_PATH_LABEL);
      if (String.IsNullOrEmpty(dataPath))
      {
        throw new ApplicationException("The %data% path must be specified to install packages");
      }
      if (!Directory.Exists(dataPath))
      {
        throw new ApplicationException("The %data% path must exist to install packages");
      }
      var pckMgrDir = Path.Combine(dataPath, "PackageManager");
      if (!Directory.Exists(pckMgrDir))
      {
        Directory.CreateDirectory(pckMgrDir);
      }
      var pckDir = Path.Combine(pckMgrDir, Name);
      if (!Directory.Exists(pckDir))
      {
        Directory.CreateDirectory(pckDir);
      }
      if (packageInstallState == PackageInstallState.Installing)
      {
        File.Copy(Path.Combine(PackageDirectory, "Package.xml"), Path.Combine(pckDir, "Package.xml"), true);
      }
      var xRoot = new XElement("MP2PackageInstallState",
        new XAttribute("State", packageInstallState),
        new XAttribute("Option", option.Name));
      if (exception != null)
      {
        xRoot.Add(new XElement("Exception",
          new XAttribute("Type", exception.GetType().FullName),
          exception.Message));
      }
      var xStateDoc = new XDocument(xRoot);
      xStateDoc.Save(Path.Combine(pckDir, "InstallState.xml"));
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public string ElementsName
    {
      get { return "Package"; }
    }

    public bool CheckElements(ILogger log)
    {
      if (log != null)
      {
        log.Info("Checking package content");
      }

      return this.CheckNotNullOrEmpty(Name, "Name", log) &&
             this.CheckNotNullOrEmpty(Version, "Version", log) &&
             this.CheckNotNullAndContent(InstallOptions, "InstallOptions", log) &&
             this.CheckNotNullAndContent(Images, "Images", log) &&
             this.CheckNotNullAndContent(Links, "Links", log) &&
             this.CheckNotNullAndContent(ReleaseInfo, "ReleaseInfo", log) &&
             this.CheckNotNullAndContent(Content, "Content", log);
    }

    #endregion
  }

  public enum PackageInstallState
  {
    Installing,
    Installed,
    InstallFailed
  }
}