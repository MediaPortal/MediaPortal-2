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
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.PackageCore;
using MediaPortal.PackageManager.Options.Shared;
using MediaPortal.PackageManager.Options.Users;

namespace MediaPortal.PackageManager.Core
{
  internal class PackageInstallerCmd : PackageInstaller
  {
    public PackageInstallerCmd(ILogger log)
      : base(log ?? new BasicConsoleLogger(LogLevel.All))
    { }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      var manager = new PackageInstallerCmd(log);
      switch (operation)
      {
        case Operation.List:
          return manager.List(options as ListOptions);

        case Operation.Install:
          return manager.Install(options as InstallOptions);

        default:
          return false;
      }
    }

    #region List

    public bool List(ListOptions options)
    {
      VerifyOptions(options);
      return base.List(options.PackageType, options.AuthorText, options.PackageName, options.SearchDescriptions, options.CategoryTags, options.All);
    }

    #endregion

    #region Install

    public bool Install(InstallOptions options)
    {
      VerifyOptions(options);

      foreach (var action in options.GetActions())
      {
        if (action.ActionType == InstallActionType.Remove)
        {
          RemovePlugin(action.PackageName, options.GetInstallPaths());
        }
        else if (action.IsLocalSource)
        {
          switch (action.ActionType)
          {
            case InstallActionType.Install:
              InstallFromFile(action.LocalPath, false, options.GetInstallPaths());
              break;

            case InstallActionType.Update:
              InstallFromFile(action.LocalPath, true, options.GetInstallPaths());
              break;
          }
        }
        else
        {
          // download from web
          /*var proxy = new RequestExecutionHelper();
        var model = new PackageReleaseQuery(options.PackageName, options.PackageVersion);
        var response = proxy.ExecuteRequest(PackageServerApi.Packages.FindRelease, model);

        if (!IsSuccess(response, null, HttpStatusCode.OK))
          return false;

        var releaseInfo = proxy.GetResponseContent<ReleaseInfo>(response);
        */
          return false;
          //return TryInstall(options.PackageName, releaseInfo, options.PluginRootPath, startStopProcesses: true);
        }
      }
      return true;
    }

    /*private bool TryInstall(string packageName, ReleaseInfo releaseInfo, string packageRootPath, bool startStopProcesses)
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

        var isClientPackage = releaseInfo.PackageType.HasFlag(PackageType.Client);
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
    }*/

    #endregion

    /*#region Update

    public bool Update(UpdateOptions options)
    {
      VerifyOptions(options);
      
      var isClientPackage = options.PackageType.HasFlag(PackageType.Client);

      // build list of packages to operate on
      var packages = FindPackagesWithNewerCompatibleVersionAvailable(isClientPackage, options.PackageName);

      _processManager.Stop(isClientPackage);
      var result = true;
      foreach (var package in packages)
      {
        //result &= TryRemove(package.Name, isClientPackage) && TryInstall(package.Name, package.CurrentRelease, options.PluginRootPath, startStopProcesses: false);
      }
      _processManager.Start(isClientPackage);
      return result;
      return false;
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

    #endregion*/

    /*#region Remove

    public bool Remove(RemoveOptions options)
    {
      VerifyOptions(options);
      
      var isClientPackage = options.PackageType.HasFlag(PackageType.Client);
      _processManager.Stop(isClientPackage);
      var result = true;//TryRemove(options.PackageName, isClientPackage, options.PluginRootPath);
      _processManager.Start(isClientPackage);
      return result;
      return false;
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

    #endregion*/

    #region Verify Options

    private void VerifyOptions(InstallOptions options)
    {
      if (options.InstallPaths.Length == 0)
      {
        throw new ArgumentException("There are no installation paths specified.");
      }
      foreach (var installPath in options.GetInstallPaths())
      {
        if (!Directory.Exists(installPath.Value))
        {
          throw new ArgumentException(String.Format("The installation path {0}:{1} does not exist.", installPath.Key, installPath.Value));
        }
      }

      foreach (var action in options.GetActions())
      {
        if (action.IsLocalSource &&
          !Directory.Exists(action.LocalPath) &&
          !File.Exists(action.LocalPath))
        {
          throw new ArgumentException(String.Format("The package file or directory {0} does not exist.", action.LocalPath));
        }
      }
    }

    private void VerifyOptions(ListOptions options)
    {
      if (!Enum.IsDefined(typeof(PackageType), options.PackageType))
      {
        throw new ArgumentException("Invalid package type (must be either Client or Server).");
      }
    }

    #endregion
  }
}
