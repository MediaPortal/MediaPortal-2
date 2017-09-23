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
using System.IO;
using System.Reflection;

namespace MP2BootstrapperApp.ChainPackages
{
  public abstract class MP2Msi
  {
    private readonly IPackageChecker _packageChecker;

    internal MP2Msi(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    internal bool IsInstalled(string regValue, string executableName)
    {
      string regKey = "SOFTWARE\\Team MediaPortal\\MediaPortal 2";

      string installDir = _packageChecker.GetDataFromLocalMachineRegistry(regKey, regValue);
      if (string.IsNullOrEmpty(installDir))
      {
        return false;
      }

      Version installerVersion = GetInstallerVersion();
      return _packageChecker.IsEqualOrHigherVersion(Path.Combine(installDir, executableName), installerVersion);
    }

    private Version GetInstallerVersion()
    {
      Assembly bootstrapperAppAssembly = Assembly.GetExecutingAssembly();
      int installerMajorPart = _packageChecker.GetFileMajorVersion(bootstrapperAppAssembly.Location);
      int installerMinorPart = _packageChecker.GetFileMinorVersion(bootstrapperAppAssembly.Location);
      int installerBuildPart = _packageChecker.GetFileBuildVersion(bootstrapperAppAssembly.Location);
      int installerPrivatePart = _packageChecker.GetFilePrivateVersion(bootstrapperAppAssembly.Location);

      return new Version(installerMajorPart, installerMinorPart, installerBuildPart, installerPrivatePart);
    }
  }
}
