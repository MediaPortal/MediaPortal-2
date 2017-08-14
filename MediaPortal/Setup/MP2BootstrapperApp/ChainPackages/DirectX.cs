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
  public class DirectX : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public DirectX(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public bool IsInstalled()
    {
      string dx41Path = Path.Combine(Environment.SystemDirectory, "D3DX9_41.dll");
      string dx43Path = Path.Combine(Environment.SystemDirectory, "D3DX9_43.dll");

      if (!_packageChecker.Exists(dx41Path) || !_packageChecker.Exists(dx43Path))
      {
        return false;
      }

      return _packageChecker.IsEqualOrHigherVersion(dx41Path, new Version(9, 26, 952, 2844)) && _packageChecker.IsEqualOrHigherVersion(dx43Path, new Version(9, 29, 952, 3111));
    }
  }
}
