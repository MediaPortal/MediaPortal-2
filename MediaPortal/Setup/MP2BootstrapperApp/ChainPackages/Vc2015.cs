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
  public class Vc2015 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2015(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public bool IsInstalled()
    {
      string vc2015Path = Path.Combine(Environment.SystemDirectory, "mfc140.dll");

      if (!_packageChecker.Exists(vc2015Path))
      {
        return false;
      }

      return _packageChecker.IsEqualOrHigherVersion(vc2015Path, new Version(14, 0, 24215, 0));
    }
  }
}
