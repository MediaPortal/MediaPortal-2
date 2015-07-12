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

using System.Collections.Generic;
using System.IO;
using System.Net;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Packages;
using MediaPortal.PackageCore.Package;

namespace MediaPortal.PackageCore
{
  public class PackageInstaller : Requestor
  {
    public PackageInstaller(ILogger log) :
      base(log)
    { }

    protected bool List(PackageType packageType, string authorText, string packageName, bool searchDescriptions, ICollection<string> categoryTags, bool all)
    {
      var proxy = new RequestExecutionHelper();
      ICollection<CoreComponent> localSystemCoreComponents = null; // TODO
      var model = new PackageListQuery
      {
        PackageType = packageType,
        PartialAuthor = authorText,
        PartialPackageName = packageName,
        SearchDescriptions = searchDescriptions,
        CategoryTags = categoryTags,
        CoreComponents = all ? null : localSystemCoreComponents
      };
      var response = proxy.ExecuteRequest(PackageServerApi.Packages.List, model);

      if (!IsSuccess(response, null, HttpStatusCode.OK))
        return false;

      var packages = proxy.GetResponseContent<IList<PackageInfo>>(response);
      Log.Info("{0,80}", "-");
      packages.ForEach(p => Log.Info(p.ToString()));
      Log.Info("{0,80}", "-");
      return true;
    }

    public void InstallFromFile(string packageFilePath, bool update, IDictionary<string, string> installPaths)
    {
      PackageRoot package;
      bool delete;
      if (Directory.Exists(packageFilePath))
      {
        package = PackageRoot.ParsePackage(Log, packageFilePath);
        delete = false;
      }
      else
      {
        Log.Info("Extracting package '{0}' ...", Path.GetFileNameWithoutExtension(packageFilePath));
        package = PackageRoot.ExtractPackage(Log, packageFilePath);
        delete = true;
      }
      try
      {
        Log.Info("{0} package {1} V{2} ...", update ? "Updating" : "Installing", package.PluginMetaData.Name, package.ReleaseMetaData.Version);
        package.InstallPackage(update ? PackageInstallType.Update : PackageInstallType.Install, installPaths);
      }
      finally
      {
        if (delete)
        {
          // delete temporary extracted package
          Log.Info("Deleting temporary files ...");
          package.DeletePackage();
        }
      }
    }

    public void RemovePlugin(string pluginName, IDictionary<string, string> installPaths)
    {
      PackageRoot.RemovePlugin(Log, pluginName, installPaths);
    }
  }
}