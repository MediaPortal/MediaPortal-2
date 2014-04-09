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
    #region Fields
    private readonly ConcurrentDictionary<Guid, PluginMetadata> _availablePlugins;
    #endregion

    #region Ctor
    public DependencyPresenceValidator( ConcurrentDictionary<Guid, PluginMetadata> availablePlugins )
    {
      _availablePlugins = availablePlugins;
    } 
    #endregion

    #region IValidate
    /// <summary>
    /// Validates the given <paramref name="plugin"/> by checking for missing dependencies,
    /// both those declared directly by the plugin and those indirectly references through
    /// the dependencies. Currently disabled plugins are considered during validation, and
    /// the fact that they are disabled does not make them count as a missing dependency.
    /// </summary>
    /// <param name="plugin">The plugin to validate.</param>
    /// <returns>A set of plugin ids found to be missing, or an empty set if no missing
    /// dependencies were found.</returns>
    public HashSet<Guid> Validate( PluginMetadata plugin )
    {
      return FindMissingDependencies( plugin, new HashSet<Guid>() );
    }
    #endregion

    #region Validation Implementation (FindMissingDependencies)
    private HashSet<Guid> FindMissingDependencies( PluginMetadata plugin, HashSet<Guid> verifiedPlugins )
    {
      var result = new HashSet<Guid>();
      verifiedPlugins.Add( plugin.PluginId );
      foreach( PluginDependency dependency in plugin.DependencyInfo.DependsOn
        .Where( d => !d.IsCoreDependency && !verifiedPlugins.Contains( d.PluginId ) ) )
      {
        PluginMetadata pluginDependency;
        if( !_availablePlugins.TryGetValue( dependency.PluginId, out pluginDependency ) )
          result.Add( dependency.PluginId );
        else // TODO we could optimize performance by adopting the stack-based algorithm from PluginActivator.TryChangePluginState
          result.UnionWith( FindMissingDependencies( pluginDependency, verifiedPlugins ) );
      }
      return result;
    }
    #endregion
  }
}
