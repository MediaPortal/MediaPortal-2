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
using System.IO;
using System.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.PluginManager.Packages.Repository
{
  /// <summary>
  /// 
  /// </summary>
  public class LocalCacheProvider
  {
    #region Fields

    private const string CACHE_FILE_NAME = "packages.xml";
    private readonly string _cacheFilePath;
    private List<PackageInfo> _packages;

    #endregion

    #region Ctor

    public LocalCacheProvider()
    {
      var dataDirectory = PathManager.GetPath("<PLUGINS>");
      _cacheFilePath = Path.Combine(dataDirectory, CACHE_FILE_NAME);
    }

    #endregion

    #region Properties

    public DateTime LastSeenChange { get; private set; }

    #endregion

    #region Get/Add Packages, ClearCache

    public IList<PackageInfo> GetPackages()
    {
      if (_packages == null)
      {
        _packages = new List<PackageInfo>();
        if (File.Exists(_cacheFilePath))
        {
          var data = File.ReadAllText(_cacheFilePath);
          // TODO parse/deserialize and populate _packages
        }
      }
      return _packages;
    }

    public void AddPackages(IList<PackageInfo> packages)
    {
      // prefer packages received as input (in case an existing package has updated information)
      // TODO package model class must implement IEquatable for Distinct to work
      _packages = packages.Concat(_packages).Distinct().ToList();
      //LastSeenChange = _packages.Any() ? _packages.Select( p => p.LastModified ).Max() : DateTime.MinValue;
    }

    public void ClearCache()
    {
      if (File.Exists(_cacheFilePath))
        File.Delete(_cacheFilePath);
      _packages = null;
      LastSeenChange = DateTime.MinValue;
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