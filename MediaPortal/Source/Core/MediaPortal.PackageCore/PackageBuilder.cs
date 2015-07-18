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
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore.Package;

namespace MediaPortal.PackageCore
{
  public class PackageBuilder
  {
    private const string PACKAGE_EXTENSION = ".mp2x";

    public ILogger Log { get; private set; }

    public PackageBuilder(ILogger log)
    {
      Log = log;
    }
    
    public bool CreatePackage(string sourceFolder, string targetFolder, bool overwriteExistingTarget)
    {
      var packageRoot = PackageRoot.ParsePackage(Log, sourceFolder, true);

      // verify that output file doesn't exist
      var packageFileName = string.Format("{0}-{1}{2}", packageRoot.PluginMetaData.Name, packageRoot.ReleaseMetaData.Version, PACKAGE_EXTENSION);
      var packageFilePath = Path.Combine(targetFolder, packageFileName);
      if (File.Exists(packageFilePath))
      {
        if (overwriteExistingTarget)
          File.Delete(packageFilePath);
        else
          throw new InvalidOperationException(string.Format("The target directory already contains a package named '{0}'.", packageFileName));
      }

      // create package archive
      packageRoot.CreatePackage(packageFilePath);

      #region TODOs for the future

      // TODO we may want to support signing packages somehow to ensure authenticity

      // TODO we could offer to publish the package just created

      #endregion

      Log.Info("Package '{0}' created!", packageFileName);
      Log.Info("Hint: use the 'publish' command to upload it to the MediaPortal package server.");
      return true;
    }
  }
}