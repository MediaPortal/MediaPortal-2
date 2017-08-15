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
  public class LavFilters : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public LavFilters(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public bool IsInstalled()
    {
      // TODO: add registry check to find the installed path
      // TODO: does the LAV filters installer allows at all a custom install path?

      string lavFiltersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LAV Filters\\x86\\LAVSplitter.ax");

      if (!_packageChecker.Exists(lavFiltersPath))
      {
        return false;
      }

      return _packageChecker.IsEqualOrHigherVersion(lavFiltersPath, new Version(0, 70, 0, 0));
    }
  }
}
