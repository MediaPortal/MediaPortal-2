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
  public class MP2Common : IPackage 
  {
    private readonly IPackageChecker _packageChecker;
    
    public MP2Common(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      const string mp2RegKey = "SOFTWARE\\Team MediaPortal\\MediaPortal 2";
      const string mp2SrvMonitorRegValue = "INSTALLDIR_SERVICE_MONITOR";
      string mp2SrvMonitorInstallDir = _packageChecker.GetDataFromLocalMachineRegistry(mp2RegKey, mp2SrvMonitorRegValue);

      if (string.IsNullOrEmpty(mp2SrvMonitorInstallDir))
      {
        return new Version();
      }
      const string mp2SrvMonitorExe = "MP2-ServiceMonitor.exe";
      string pathToMp2SrvMonitorExe = Path.Combine(mp2SrvMonitorInstallDir, mp2SrvMonitorExe);
      int majorVersion = _packageChecker.GetFileMajorVersion(pathToMp2SrvMonitorExe);
      int minorVersion = _packageChecker.GetFileMinorVersion(pathToMp2SrvMonitorExe);
      int buildVersion = _packageChecker.GetFileBuildVersion(pathToMp2SrvMonitorExe);
      int revision = _packageChecker.GetFilePrivateVersion(pathToMp2SrvMonitorExe);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}
