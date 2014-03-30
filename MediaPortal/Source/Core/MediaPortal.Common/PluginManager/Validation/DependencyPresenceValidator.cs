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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Validation
{
  /// <summary>
  /// Validation helper class responsible for locating missing plugin dependencies.
  /// </summary>
  public class DependencyPresenceValidator : IValidator
  {
    private readonly ConcurrentDictionary<Guid, PluginMetadata> _availablePlugins;

    public DependencyPresenceValidator( ConcurrentDictionary<Guid, PluginMetadata> availablePlugins )
    {
      _availablePlugins = availablePlugins;
    }

    public HashSet<Guid> Validate( PluginMetadata plugin )
    {
      return FindMissingDependencies( plugin );
    }

    private HashSet<Guid> FindMissingDependencies( IPluginMetadata plugin )
    {
      var result = new HashSet<Guid>();
      foreach( PluginDependency dependency in plugin.DependencyInfo.DependsOn.Where( d => !d.IsCoreDependency ) )
      {
        PluginMetadata pluginDependency;
        if( !_availablePlugins.TryGetValue( dependency.PluginId, out pluginDependency ) )
          result.Add( dependency.PluginId );
        if( pluginDependency != null )
          result.UnionWith( FindMissingDependencies( pluginDependency ) );
      }
      return result;
    }
  }
}
