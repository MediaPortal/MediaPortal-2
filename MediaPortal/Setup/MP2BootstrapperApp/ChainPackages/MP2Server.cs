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

namespace MP2BootstrapperApp.ChainPackages
{
  public class MP2Server : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public MP2Server(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      const string mp2RegKey = "SOFTWARE\\Team MediaPortal\\MediaPortal 2";
      const string mp2ServerRegValue = "INSTALLDIR_SERVER";
      string mp2ServerInstallDir = _packageChecker.GetDataFromLocalMachineRegistry(mp2RegKey, mp2ServerRegValue);

      if (string.IsNullOrEmpty(mp2ServerInstallDir))
      {
        return new Version();
      }
      const string mp2ServerExe = "MP2-Server.exe";
      string pathToMp2ServerExe = Path.Combine(mp2ServerInstallDir, mp2ServerExe);
      int majorVersion = _packageChecker.GetFileMajorVersion(pathToMp2ServerExe);
      int minorVersion = _packageChecker.GetFileMinorVersion(pathToMp2ServerExe);
      int buildVersion = _packageChecker.GetFileBuildVersion(pathToMp2ServerExe);
      int revision = _packageChecker.GetFilePrivateVersion(pathToMp2ServerExe);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}
