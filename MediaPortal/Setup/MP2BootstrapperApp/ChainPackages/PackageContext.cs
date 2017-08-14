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

using System.Collections.Generic;

namespace MP2BootstrapperApp.ChainPackages
{
  public class PackageContext
  {
    private readonly Dictionary<PackageId, IPackage> _packages = new Dictionary<PackageId, IPackage>();
    private readonly IPackageChecker _packageChecker = new PackageChecker();

    public PackageContext()
    {
      _packages.Add(PackageId.VC2008SP1_x86, new Vc2008Sp1(_packageChecker));
      _packages.Add(PackageId.VC2010_x86, new Vc2010(_packageChecker));
      _packages.Add(PackageId.VC2013_x86, new Vc2013(_packageChecker));
      _packages.Add(PackageId.VC2015_x86, new Vc2015(_packageChecker));
      _packages.Add(PackageId.directx9, new DirectX(_packageChecker));
      _packages.Add(PackageId.LAVFilters, new LavFilters(_packageChecker));
      _packages.Add(PackageId.dokan, new Dokan(_packageChecker));
      _packages.Add(PackageId.MP2Client, new MP2Client(_packageChecker));
      _packages.Add(PackageId.MP2Server, new MP2Server(_packageChecker));
      _packages.Add(PackageId.MP2Common, new MP2Common(_packageChecker));
    }

    public bool CheckInstallState(PackageId id)
    {
      return _packages[id].IsInstalled();
    }
  }
}
