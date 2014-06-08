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
using System.IO;
using System.IO.Compression;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.PackageManager.Options.Authors;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Core
{
  internal class PackageBuilder
  {
    private const string PACKAGE_EXTENSION = ".mp2x";
    private readonly ILogger _log;

    public PackageBuilder(ILogger log)
    {
      _log = log ?? new BasicConsoleLogger(LogLevel.All);
    }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      if (options == null)
        return false;

      var builder = new PackageBuilder(log);
      switch (operation)
      {
        case Operation.Create:
          return builder.CreatePackage(options as CreateOptions);
        default:
          return false;
      }
    }

    public bool CreatePackage(CreateOptions options)
    {
      VerifyOptions(options);

      // parse plugin definition file
      PluginMetadata pluginMetadata;
      if (!options.SourceFolder.TryParsePluginDefinition(out pluginMetadata))
        throw new PluginInvalidMetadataException("Unable to parse the plugin definition file.");

      // verify that output file doesn't exist
      var packageFileName = string.Format("{0}-{1}{2}", pluginMetadata.Name, pluginMetadata.PluginVersion, PACKAGE_EXTENSION);
      var packageFilePath = Path.Combine(options.TargetFolder, packageFileName);
      if (File.Exists(packageFilePath))
      {
        if (options.OverwriteExistingTarget)
          File.Delete(packageFilePath);
        else
          throw new InvalidOperationException(string.Format("The target directory already contains a package named '{0}'.", packageFileName));
      }

      // TODO additional verification steps here
      // check package conventions (folder names and content types)
      // anything else we can think of?

      // create package archive
      ZipFile.CreateFromDirectory(options.SourceFolder, packageFilePath, CompressionLevel.Optimal, includeBaseDirectory: true);

      #region TODOs for the future

      // TODO we may want to support signing packages somehow to ensure authenticity

      // TODO we could offer to publish the package just created

      #endregion

      _log.Info("Package '{0}' created!", packageFileName);
      _log.Info("Hint: use the 'publish' command to upload it to the MediaPortal package server.");
      return true;
    }

    private static void VerifyOptions(CreateOptions options)
    {
      if (options == null)
        throw new ArgumentNullException("options");

      // make sure source and target are specified with absolute paths
      if (!Path.IsPathRooted(options.SourceFolder))
        options.SourceFolder = Path.Combine(Environment.CurrentDirectory, options.SourceFolder);
      if (options.TargetFolder == null)
        options.TargetFolder = Environment.CurrentDirectory;
      else if (!Path.IsPathRooted(options.TargetFolder))
        options.TargetFolder = Path.Combine(Environment.CurrentDirectory, options.TargetFolder);

      // make sure source and target paths exist
      if (!Directory.Exists(options.SourceFolder))
        throw new ArgumentException("The specified source folder does not exist.");
      if (!Directory.Exists(options.TargetFolder))
        throw new ArgumentException("The specified target folder does not exist.");

      // make sure target is not inside source
      if (options.TargetFolder.StartsWith(options.SourceFolder))
        throw new ArgumentException("The target folder cannot be inside the source folder.");

      // make sure plugin descriptor file exists
      options.SourceFolder.VerifyIsPluginDirectory();
    }
  }
}