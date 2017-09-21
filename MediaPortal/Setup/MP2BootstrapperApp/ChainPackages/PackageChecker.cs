#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MP2BootstrapperApp.ChainPackages
{
  public class PackageChecker : IPackageChecker
  {
    /// <inheritdoc />
    public int GetFileMajorVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FileMajorPart;
    }

    /// <inheritdoc />
    public int GetFileMinorVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FileMinorPart;
    }

    /// <inheritdoc />
    public int GetFileBuildVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FileBuildPart;
    }

    /// <inheritdoc />
    public int GetFilePrivateVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FilePrivatePart;
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
      return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    /// <inheritdoc />
    public bool IsEqualOrHigherVersion(string pathToFile, Version onlineVersion)
    {
      int majorPart = GetFileMajorVersion(pathToFile);
      int minorPart = GetFileMinorVersion(pathToFile);
      int buildPart = GetFileBuildVersion(pathToFile);
      int privatePart = GetFilePrivateVersion(pathToFile);
      Version localVersion = new Version(majorPart, minorPart, buildPart, privatePart);

      return localVersion >= onlineVersion;
    }

    /// <inheritdoc />
    public bool CheckInstallPresenceByMsiProductCode(string productCode)
    {
      return MsiQueryProductState(productCode) == MsiInstallState.InstallStateDefault;
    }

    /// <inheritdoc />
    public string GetDataFromLocalMachineRegistry(string registryKey, string registryValue)
    {
      string result = string.Empty;

      RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey);
      if (key != null)
      {
        result = key.GetValue(registryValue, string.Empty) as string;
      }
      return result;
    }

    [DllImport("msi.dll")]
    private static extern MsiInstallState MsiQueryProductState(string productCode);

    enum MsiInstallState
    {
      InstallStateNotused = -7,  // component disabled
      InstallStateBadconfig = -6,  // configuration data corrupt
      InstallStateIncomplete = -5,  // installation suspended or in progress
      InstallStateSourceabsent = -4,  // run from source, source is unavailable
      InstallStateMoredata = -3,  // return buffer overflow
      InstallStateInvalidarg = -2,  // invalid function argument
      InstallStateUnknown = -1,  // unrecognized product or feature
      InstallStateBroken = 0,  // broken
      InstallStateAdvertised = 1,  // advertised feature
      InstallStateRemoved = 1,  // component being removed (action state, not settable)
      InstallStateAbsent = 2,  // uninstalled (or action state absent but clients remain)
      InstallStateLocal = 3,  // installed on local drive
      InstallStateSource = 4,  // run from source, CD or net
      InstallStateDefault = 5,  // use default, local or source
    }
  }
}
