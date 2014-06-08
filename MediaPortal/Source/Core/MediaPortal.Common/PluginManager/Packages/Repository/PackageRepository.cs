#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.PluginManager.Packages.Interfaces;

namespace MediaPortal.Common.PluginManager.Packages.Repository
{
  /// <summary>
  /// 
  /// </summary>
  public class PackageRepository : IPackageRepository
  {
    private readonly OnlineRepositoryProvider _provider = new OnlineRepositoryProvider();

    public IList<PackageInfo> GetPackages()
    {
      return null;
    }

    public IList<PackageInfo> GetPackages(Predicate<PackageInfo> filter)
    {
      return null;
    }

    public IList<PackageInfo> GetUpdatedPackages(IEnumerable<PackageInfo> installedPlugins)
    {
      return GetPackages(FilterFactory.NewerVersionsOfInstalledPlugins(installedPlugins));
    }

    public PluginSocialInfo GetSocialMetadata(Guid plugin)
    {
      return null;
    }
  }
}