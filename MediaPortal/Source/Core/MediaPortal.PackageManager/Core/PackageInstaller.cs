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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Configuration;
using System.Net.Http;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Packages;
using MediaPortal.Common.PluginManager.Packages.DataContracts.UserAdmin;
using MediaPortal.Common.Services.SystemResolver;
using MediaPortal.PackageManager.Options.Shared;
using MediaPortal.PackageManager.Options.Users;

namespace MediaPortal.PackageManager.Core
{
  internal class PackageInstaller : Requestor
  {
    private readonly ProcessManager _processManager;

    public PackageInstaller(ILogger log, ProcessManager processManager) : base(log)
    {
      _processManager = processManager;
    }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      var manager = new PackageInstaller(log, new ProcessManager(log));
      switch (operation)
      {
        case Operation.List:
          return manager.List(options as ListOptions);
        case Operation.Install:
          return manager.Install(options as InstallOptions);
        case Operation.Update:
          return manager.Update(options as UpdateOptions);
        case Operation.Remove:
          return manager.Remove(options as RemoveOptions);
        default:
          return false;
      }
    }

    #region List
    
    public bool List( ListOptions options )
    {
      VerifyOptions( options );

      var proxy = new RequestExecutionHelper();
      ICollection<CoreComponent> localSystemCoreComponents = null; // TODO
      var model = new PackageListQuery
      {
        PackageType = options.PackageType,
        PartialAuthor = options.AuthorText,
        PartialPackageName = options.PackageName,
        SearchDescriptions = options.SearchDescriptions,
        CategoryTags = options.CategoryTags,
        CoreComponents = options.All ? null : localSystemCoreComponents
      };
      var response = proxy.ExecuteRequest( PackageServerApi.Packages.List, model );

      if( !IsSuccess( response, null, HttpStatusCode.OK ) )
        return false;

      var packages = proxy.GetResponseContent<IList<PackageInfo>>( response );
      _log.Info( "{0,80}", "-" );
      packages.ForEach( p => _log.Info( p.ToString() ) );
      _log.Info( "{0,80}", "-" );
      return true;
    } 

    #endregion

    #region Install

    public bool Install(InstallOptions options)
    {
      VerifyOptions(options);

      var proxy = new RequestExecutionHelper();
      var model = new PackageReleaseQuery(options.PackageName, options.PackageVersion);
      var response = proxy.ExecuteRequest(PackageServerApi.Packages.FindRelease, model);

      if (!IsSuccess(response, null, HttpStatusCode.OK))
        return false;

      var releaseInfo = proxy.GetResponseContent<ReleaseInfo>(response);
      return TryInstall(options.PackageName, releaseInfo, options.PluginRootPath, startStopProcesses: true);
    }

    private bool TryInstall(string packageName, ReleaseInfo releaseInfo, string packageRootPath, bool startStopProcesses)
    {
      var proxy = new RequestExecutionHelper();

      // get the file
      var response = proxy.ExecuteRequest(HttpMethod.Get, releaseInfo.DownloadUrl);
      if (!IsSuccess(response, null, HttpStatusCode.OK))
        return false;

      // got the file, save it
      var tempFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
      File.WriteAllBytes(tempFile, response.Content.ReadAsByteArrayAsync().Result);
      try
      {
        // sanity checks
        var fs = new FileInfo(tempFile);
        if (fs.Length != releaseInfo.PackageSize)
        {
          _log.Error("Downloaded release package had size of '{0}' bytes (expected '{1}' bytes).", fs.Length, releaseInfo.PackageSize);
          return false;
        }

        var isClientPackage = releaseInfo.PackageType == "Client";
        // ensure target folder does not exist
        var targetRootPath = packageRootPath ?? AutoDetectInstallationTarget(isClientPackage);
        if (targetRootPath == null || !Directory.Exists(targetRootPath))
        {
          _log.Error("The installation target directory '{0}' does not exist (if you specified it manually, try omitting the option to use auto-detection).", targetRootPath);
          return false;
        }

        var targetFolder = Path.Combine(targetRootPath, packageName);
        if (Directory.Exists(targetFolder) && targetFolder.IsPluginDirectory())
        {
          if (startStopProcesses)
            _processManager.Stop(isClientPackage);
          Directory.Delete(targetFolder, recursive: true);
        }
        // all good, time to extract to target folder
        ZipFile.ExtractToDirectory(tempFile, targetRootPath);
        if (startStopProcesses)
          _processManager.Start(isClientPackage);
      }
      finally
      {
        File.Delete(tempFile);
      }
      return true;
    }

    #endregion

    #region Update

    public bool Update(UpdateOptions options)
    {
      VerifyOptions(options);

      var isClientPackage = options.PackageType == "Client";

      // build list of packages to operate on
      var packages = FindPackagesWithNewerCompatibleVersionAvailable(isClientPackage, options.PackageName);

      _processManager.Stop(isClientPackage);
      var result = true;
      foreach (var package in packages)
      {
        result &= TryRemove(package.Name, isClientPackage) && TryInstall(package.Name, package.CurrentRelease, options.PluginRootPath, startStopProcesses: false);
      }
      _processManager.Start(isClientPackage);
      return result;
    }

    private List<PackageInfo> FindPackagesWithNewerCompatibleVersionAvailable(bool isClient, string packageName)
    {
      // determine whether to operate on single or multiple packages
      var singlePackage = !string.IsNullOrEmpty(packageName);

      // TODO get list of installed packages

      // TODO if singlePackage, make sure specified package is already installed

      // TODO query server for compatible updates
      //PackageServerApi.Packages.UpdateCheck

      return new List<PackageInfo>();
    }

    #endregion

    #region Remove

    public bool Remove(RemoveOptions options)
    {
      VerifyOptions(options);

      var isClientPackage = options.PackageType == "Client";
      _processManager.Stop(isClientPackage);
      var result = TryRemove(options.PackageName, isClientPackage, options.PluginRootPath);
      _processManager.Start(isClientPackage);
      return result;
    }

    private bool TryRemove(string packageName, bool isClientPackage, string packageRootPath = null)
    {
      var targetRootPath = packageRootPath ?? AutoDetectInstallationTarget(isClientPackage);
      var packagePath = Path.Combine(targetRootPath, packageName);

      try
      {
        Directory.Delete(packagePath, true);
        return true;
      }
      catch (IOException ex)
      {
        _log.Error("An unexpected error occurred while removing the directory '{0}'. {1}: {2}", packagePath, ex.GetType().Name, ex.Message);
        _log.Error("The package may have been partially removed. Complete manual removal before restarting MP2 is recommended.");
        return false;
      }
    }

    #endregion

    #region Verify Options

    private void VerifyOptions(PackageOptions options)
    {
      // ensure target folder does not exist
      var targetRootPath = options.PluginRootPath ?? "TODO use default MP2 plugin folder; we need PackageType in result then :/";
      if (!Directory.Exists(targetRootPath))
      {
        throw new ArgumentException("The installation target directory does not exist (note: specify the plugin root directory, not a directory below this).");
      }
    }

    private void VerifyOptions(ListOptions options)
    {
      if (!Enum.IsDefined(typeof(PackageType), options.PackageType))
      {
        throw new ArgumentException("Invalid package type (must be either Client or Server).");
      }
    }

    private void VerifyOptions(RemoveOptions options)
    {
      // ensure target folder does not exist
      var targetRootPath = options.PluginRootPath ?? AutoDetectInstallationTarget(options.PackageType == "Client");
      if (targetRootPath == null || !Directory.Exists(targetRootPath))
      {
        throw new ArgumentException("The plugin root path '{0}' does not exist (if you specified it manually, try omitting the option to use auto-detection).", targetRootPath);
      }

      var packagePath = Path.Combine(targetRootPath, options.PackageName);
      if (!Directory.Exists(packagePath))
      {
        throw new ArgumentException("The expected package installation path '{0}' does not exist.", packagePath);
      }

      if (!packagePath.IsPluginDirectory())
      {
        throw new ArgumentException("The package path '{0}' does not appear to be a plugin directory and therefore cannot be removed.", packagePath);
      }

      // sanity check to make sure we don't accidentally delete something we shouldn't
      if (!packagePath.Contains("Plugins"))
      {
        throw new ArgumentException("Unexpected package install path '{0}', unable to continue.", packagePath);
      }
    }

    #endregion

    #region Path Helpers

    private string AutoDetectInstallationTarget(bool isClientPackage)
    {
      const string defaultBasePath = @"c:\Program Files (x86)\Team MediaPortal\";
      const string defaultClientDirectory = defaultBasePath + @"MP2-Client";
      const string defaultServerDirectory = defaultBasePath + @"MP2-Server";

      // TODO determine plugin folder for MP2 client/server installations.. 

      var targetRoot = isClientPackage ? defaultClientDirectory : defaultServerDirectory;
      return targetRoot + @"\Plugins";
    }

    #endregion
  }
}
