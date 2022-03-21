#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
  public class Vc2019 : AbstractPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2019(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public override Version GetInstalledVersion()
    {
      const string mfc140Dll = "mfc140.dll";

      // Explicitly check the x86 system path for the x86 version of the dll. The installer is currently always run
      // as x86 so this is not technically necessary as the OS will automatically redirect to the x86 path, but in
      // case we change to x64 in future this will ensure we are looking in the x86 directory.
      string vc2019Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), mfc140Dll);

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
