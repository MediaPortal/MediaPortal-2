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
using MediaPortal.Attributes;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Validation
{
  /// <summary>
  /// Validation helper class responsible for version compatibility checking.
  /// </summary>
  public class CompatibilityValidator : IValidator
  {
    private readonly ConcurrentDictionary<Guid, PluginMetadata> _availablePlugins;
    private readonly IDictionary<string, CoreAPIAttribute> _coreComponents;

    public CompatibilityValidator( ConcurrentDictionary<Guid, PluginMetadata> availablePlugins, IDictionary<string, CoreAPIAttribute> coreComponents )
    {
      _availablePlugins = availablePlugins;
      _coreComponents = coreComponents;
    }

    public HashSet<Guid> Validate( PluginMetadata plugin )
    {
      return FindIncompatible( plugin, new HashSet<Guid>() );
    }

    /// <summary>
    /// Conflicts are searched recursive, but plugins might be referenced multiple times in the hierarchy.
    /// So in order to speed up this process and prevent a StackOverflowException we pass a list of already checked plugin Ids.
    /// </summary>
    /// <param name="plugin"></param>
    /// <param name="alreadyCheckedPlugins"></param>
    /// <returns></returns>
    private HashSet<Guid> FindIncompatible( IPluginMetadata plugin, HashSet<Guid> alreadyCheckedPlugins )
    {
      var result = new HashSet<Guid>();
      if( alreadyCheckedPlugins.Contains( plugin.PluginId ) )
        return result;
      alreadyCheckedPlugins.Add( plugin.PluginId );

      foreach( PluginDependency dependency in plugin.DependencyInfo.DependsOn )
      {
        if( dependency.IsCoreDependency )
        {
          CoreAPIAttribute api;
          if( !_coreComponents.TryGetValue( dependency.CoreDependencyName, out api ) )
            throw new PluginMissingDependencyException( "Plugin dependency '{0}' is not available", dependency.CoreDependencyName );
          if( api.MinCompatibleAPI > dependency.CompatibleApi || api.CurrentAPI < dependency.CompatibleApi )
            throw new PluginIncompatibleException( "Dependency '{0}' requires API level ({1}) and available is [min compatible ({2}) -> ({3}) current]", dependency.CoreDependencyName, dependency.CompatibleApi, api.MinCompatibleAPI, api.CurrentAPI );
        }
        else
        {
          PluginMetadata dependencyMetadata;
          if( !_availablePlugins.TryGetValue( dependency.PluginId, out dependencyMetadata ) )
            throw new PluginMissingDependencyException( "Plugin dependency '{0}' is not available", dependency.PluginId );
          if( dependencyMetadata.DependencyInfo.MinCompatibleApi > dependency.CompatibleApi ||
              dependencyMetadata.DependencyInfo.CurrentApi < dependency.CompatibleApi )
            throw new PluginIncompatibleException( "Dependency '{0}' requires API level ({1}) and available is [min compatible ({2}) -> ({3}) current]",
              dependencyMetadata.Name, dependency.CompatibleApi, dependencyMetadata.DependencyInfo.MinCompatibleApi, dependencyMetadata.DependencyInfo.CurrentApi );
          result.UnionWith( FindIncompatible( dependencyMetadata, alreadyCheckedPlugins ) );
        }
      }
      return result;
    }
  }
}
