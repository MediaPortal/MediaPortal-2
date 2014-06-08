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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.PluginManager.Packages.Repository
{
  /// <summary>
  /// 
  /// </summary>
  public class OnlineRepositoryProvider
  {
    #region Fields

    private readonly LocalCacheProvider _cacheProvider = new LocalCacheProvider();
    private DateTime _lastOnline = DateTime.MinValue;

    #endregion

    #region GetPackages

    public IList<PackageInfo> GetPackages()
    {
      if (DateTime.Now.Subtract(_lastOnline) > TimeSpan.FromHours(1))
      {
        var lastSeenChange = _cacheProvider.LastSeenChange;
        // TODO query server for packages using lastSeen parameter
        var packages = new List<PackageInfo>();
        _cacheProvider.AddPackages(packages);
        _lastOnline = DateTime.Now;
      }
      return _cacheProvider.GetPackages();
    }

    #endregion

    #region Static Helpers

    private static ILogger Log
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    private static ISettingsManager SettingsManager
    {
      get { return ServiceRegistration.Get<ISettingsManager>(); }
    }

    private static IPathManager PathManager
    {
      get { return ServiceRegistration.Get<IPathManager>(); }
    }

    #endregion
  }
}