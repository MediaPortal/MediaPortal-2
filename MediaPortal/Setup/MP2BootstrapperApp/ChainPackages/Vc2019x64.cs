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
  public class Vc2019x64 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2019x64(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      // ToDO: We should probably block this x64 package entirely earlier
      // on in the installation if this is a 32 bit OS.
      if (!Environment.Is64BitOperatingSystem)
        return new Version();

      const string mfc140Dll = "mfc140.dll";
      string systemDirectory;
      // Installer is x64, system directory will point to the correct x64 system directory for the x64 version of the dll. 
      if (Environment.Is64BitProcess)
        systemDirectory = Environment.SystemDirectory;
      else //Installer is x86, so we need to use the special sysnative path to get the x64 system directory
        systemDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");

      string vc2019Path = Path.Combine(systemDirectory, mfc140Dll);

      if (!_packageChecker.Exists(vc2019Path))
      {
        return new Version();
      }
      int majorVersion = _packageChecker.GetFileMajorVersion(vc2019Path);
      int minorVersion = _packageChecker.GetFileMinorVersion(vc2019Path);
      int buildVersion = _packageChecker.GetFileBuildVersion(vc2019Path);
      int revision = _packageChecker.GetFilePrivateVersion(vc2019Path);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}
