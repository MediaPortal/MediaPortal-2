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
  public class Vc2010 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2010(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      const string mfc100Dll = "mfc100.dll";
      string vc2010Path = Path.Combine(Environment.SystemDirectory, mfc100Dll);

      if (!_packageChecker.Exists(vc2010Path))
      {
        return new Version();
      }
      int majorVersion = _packageChecker.GetFileMajorVersion(vc2010Path);
      int minorVersion = _packageChecker.GetFileMinorVersion(vc2010Path);
      int buildVersion = _packageChecker.GetFileBuildVersion(vc2010Path);
      int revision = _packageChecker.GetFilePrivateVersion(vc2010Path);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}
