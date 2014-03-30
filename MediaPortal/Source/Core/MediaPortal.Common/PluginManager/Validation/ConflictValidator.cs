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
using MediaPortal.Common.General;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Validation
{
  /// <summary>
  /// Validation helper class responsible for detecting plugin conflicts (by looking at
  /// dependency declarations). Installed but disabled plugins are not considered in conflict. 
  /// </summary>
  public class ConflictValidator : IValidator
  {
    private readonly ConcurrentDictionary<Guid, PluginMetadata> _availablePlugins;
    private readonly ConcurrentHashSet<Guid> _disabledPlugins;

    public ConflictValidator( ConcurrentDictionary<Guid, PluginMetadata> availablePlugins, ConcurrentHashSet<Guid> disabledPlugins )
    {
      _availablePlugins = availablePlugins;
      _disabledPlugins = disabledPlugins;
    }

    public HashSet<Guid> Validate( PluginMetadata plugin )
    {
      return FindConflicts( plugin, new HashSet<Guid>() );
    }

    /// <summary>
    /// Conflicts are searched recursive, but plugins might be referenced multiple times in the hierarchy.
    /// So in order to speed up this process and prevent a StackOverflowException we pass a list of already 
    /// checked plugin Ids.
    /// </summary>
    /// <param name="plugin"></param>
    /// <param name="alreadyCheckedPlugins"></param>
    /// <returns></returns>
    private HashSet<Guid> FindConflicts( IPluginMetadata plugin, HashSet<Guid> alreadyCheckedPlugins )
    {
      var result = new HashSet<Guid>();
      if( alreadyCheckedPlugins.Contains( plugin.PluginId ) )
        return result;
      alreadyCheckedPlugins.Add( plugin.PluginId );
      
      // find conflicts declared by plugin
      var conflictingPlugins = plugin.DependencyInfo.ConflictsWith
        .Intersect( _availablePlugins.Keys ) // disregard plugins not installed
        .Where( cp => !_disabledPlugins.Contains( cp ) ); // disregard disabled plugins
      conflictingPlugins.ForEach( cp => result.Add( cp ) );
      
      // find conflicts declared by other plugins
      conflictingPlugins = _availablePlugins.Values
        .Where( pm => pm.DependencyInfo.ConflictsWith.Contains( plugin.PluginId ) // only those referencing plugin
                      && !_disabledPlugins.Contains( pm.PluginId ) ) // disregard disabled plugins
        .Select( pm => pm.PluginId );
      conflictingPlugins.ForEach( cp => result.Add( cp ) );

      // check dependencies for any conflicts
      foreach( var dependency in plugin.DependencyInfo.DependsOn )
      {
        PluginMetadata dependencyMetadata;
        if( _availablePlugins.TryGetValue( dependency.PluginId, out dependencyMetadata ) )
        {
          result.UnionWith( FindConflicts( dependencyMetadata, alreadyCheckedPlugins ) );
        }
      }
      return result;
    }
  }
}
