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
using System.Threading;
using MediaPortal.Attributes;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.PluginManager.Validation;
using MediaPortal.Common.Services.PluginManager;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.PluginManager.Discovery
{
  /// <summary>
  /// Class responsible for providing plugin metadata. It uses helper classes to perform discovery 
  /// of installed plugins and core components, as well as for validation and dependency checking.
  /// </summary>
  public class PluginRepository
  {
    #region Fields
    private readonly ConcurrentDictionary<string, CoreAPIAttribute> _coreComponents = new ConcurrentDictionary<string, CoreAPIAttribute>();
    private readonly ConcurrentDictionary<Guid, PluginMetadata> _models = new ConcurrentDictionary<Guid, PluginMetadata>();
    private readonly ConcurrentHashSet<Guid> _disabledPlugins = new ConcurrentHashSet<Guid>();
    private readonly ConcurrentHashSet<Guid> _validatedPlugins = new ConcurrentHashSet<Guid>();
    private long _initialized;
    #endregion

    #region Ctor
    internal PluginRepository()
    {
    }
    #endregion

    #region Initialization
    public bool IsInitialized
    {
      get { return Interlocked.Read( ref _initialized ) == 1; }
    }

    public void Initialize()
    {
      // only run initialize once
      if( Interlocked.CompareExchange( ref _initialized, 1, 0 ) == 0 )
      {
        Log.Debug( "PluginRepository: Initializing" );
        DiscoverCoreComponents();
        DiscoverInstalledPlugins();
        DiscoverDisabledPlugins();
        Log.Debug( "PluginRepository: Initialized" );
      }
    }

    private void DiscoverCoreComponents()
    {
      Log.Info( "PluginRepository: Discovering core components" );
      var assemblyScanner = new AssemblyScanner();
      var coreComponents = assemblyScanner.PerformDiscovery();
      foreach( var coreComponent in coreComponents )
      {
        _coreComponents.TryAdd( coreComponent.Key, coreComponent.Value );
      }
    }

    private void DiscoverInstalledPlugins()
    {
      Log.Debug( "PluginRepository: Discovering plugins" );
      string pluginsPath = PathManager.GetPath( "<PLUGINS>" );
      var scanner = new DirectoryScanner( pluginsPath );
      var plugins = scanner.PerformDiscovery();
      foreach( var pm in plugins.Values )
      {
        if( !_models.TryAdd( pm.PluginId, pm ) )
          Log.Error( "PluginRepository: Plugin {0} could not be registered because of a duplicate identifier.", pm.LogId );
      }
    }

    private void DiscoverDisabledPlugins()
    {
      var settings = SettingsManager.Load<PluginManagerSettings>();
      settings.UserDisabledPlugins.ForEach( pid => _disabledPlugins.Add( pid ) );
    }
    #endregion

    #region Properties
    public IDictionary<string, CoreAPIAttribute> CoreComponents
    {
      get
      {
        ThrowIfNotInitialized();
        return _coreComponents;
      }
    }

    public IDictionary<Guid, PluginMetadata> Models
    {
      get
      {
        ThrowIfNotInitialized();
        return _models;
      }
    }
    #endregion

    #region Disabled Plugins Management
    public bool IsDisabled( Guid pluginId )
    {
      ThrowIfNotInitialized();
      return _disabledPlugins.Contains( pluginId );
    }

    public void NotifyPluginDisabled( Guid pluginId )
    {
      ThrowIfNotInitialized();
      PluginMetadata plugin;
      if( !_models.TryGetValue( pluginId, out plugin ) )
        throw new ArgumentException( string.Format( "Plugin with id '{0}' not found", pluginId ) );

      // update local cache
      _disabledPlugins.Add( pluginId );

      // update system settings
      var settings = SettingsManager.Load<PluginManagerSettings>();
      settings.AddUserDisabledPlugin( pluginId );
      SettingsManager.Save( settings );
    }

    public void NotifyPluginEnabled( Guid pluginId )
    {
      ThrowIfNotInitialized();
      PluginMetadata plugin;
      if( !_models.TryGetValue( pluginId, out plugin ) )
        throw new ArgumentException( string.Format( "Plugin with id '{0}' not found", pluginId ) );

      // update local cache
      _disabledPlugins.Remove( pluginId );

      // update system settings
      var settings = SettingsManager.Load<PluginManagerSettings>();
      settings.RemoveUserDisabledPlugin( pluginId );
      SettingsManager.Save( settings );
    }
    #endregion

    #region Validation
    public bool IsCompatible( PluginMetadata plugin )
    {
      ThrowIfNotInitialized();
      if( _validatedPlugins.Contains( plugin.PluginId ) )
        return true;
      var validator = new Validator( _models, _disabledPlugins, _coreComponents );
      PluginMetadata metadata;
      if( !_models.TryGetValue( plugin.PluginId, out metadata ) )
        return false;
      var result = validator.Validate( metadata );

      if( !result.IsComplete )
      {
        result.MissingDependencies.Select( id => _models[id] )
          .ForEach( d => Log.Warn( "PluginRepository: Plugin {0} is missing dependency {1}", metadata.LogName, d.LogName ) );
        return false;
      }
      if( !result.CanEnable )
      {
        result.ConflictsWith.Select( id => _models[id] )
          .ForEach( d => Log.Warn( "PluginRepository: Plugin {0} is in conflict with {1}", metadata.LogName, d.LogName ) );
        result.IncompatibleWith.Select( id => _models[id] )
          .ForEach( d => Log.Warn( "PluginRepository: Plugin {0} is incompatible with {1}", metadata.LogInfo, d.LogInfo ) );
        return false;
      }
      _validatedPlugins.Add( plugin.PluginId );
      return true;
    }
    #endregion

    #region Metadata Lookup and Dependency Information
    public PluginMetadata GetPlugin( Guid pluginId )
    {
      ThrowIfNotInitialized();
      PluginMetadata result;
      return _models.TryGetValue( pluginId, out result ) ? result : null;
    }

    public IList<PluginMetadata> GetPluginAndDependencies( Guid pluginId, PluginSortOrder sortOrder )
    {
      ThrowIfNotInitialized();
      try
      {
        var models = Models; // use IDictionary to simplify lookup code; causes exceptions for missing lookups, but we don't expect any misses
        var plugin = models[ pluginId ];
        var result = new List<PluginMetadata>();
        var resultSet = new HashSet<Guid>();
        var stack = new Stack<PluginMetadata>( new [] { plugin } );
        while( stack.Count > 0 )
        {
          plugin = stack.Pop();
          if( resultSet.Contains( plugin.PluginId ) ) 
            continue;
          var insertAtIndex = result.Count;
          resultSet.Add( plugin.PluginId );
          if( plugin.DependencyInfo != null && plugin.DependencyInfo.DependsOn.Count > 0 )
          {
            // we check the resultSet to handle cyclic dependencies that would otherwise cause an infinite loop
            var dependencies = plugin.DependencyInfo.DependsOn.Where( d => !d.IsCoreDependency ).Select( d => models[ d.PluginId ] );
            foreach( var dependency in dependencies )
            {
              if( !resultSet.Contains( dependency.PluginId ) )
              {
                stack.Push( dependency );
              }
              else // insert plugin before dependencies in result
              {
                insertAtIndex = Math.Min( insertAtIndex, result.IndexOf( dependency ) );
              }
            }
          }
          result.Insert( insertAtIndex, plugin );
        }
        if( sortOrder == PluginSortOrder.DependenciesFirst )
          result.Reverse();
        return result;
      }
      catch( KeyNotFoundException knf )
      {
        var plugin = Models[ pluginId ];
        var msg = string.Format( "PluginRepository: Plugin {0} or one of its dependencies is not available/installed.", plugin.LogId );
        Log.Error( msg, knf );
        throw new PluginMissingDependencyException( msg, knf );
      }
    }

    /// <summary>
    /// Returns a list of plugins that depend on the specified plugin.
    /// </summary>
    /// <remarks>This method only returns direct dependencies and does not recurse in any way.</remarks>
    /// <param name="pluginId">The plugin identifier to search for in dependency declarations.</param>
    /// <returns>A list of plugins that depend on the specified plugin.</returns>
    public IEnumerable<PluginMetadata> GetPluginsDependingOn( Guid pluginId )
    {
      ThrowIfNotInitialized();
      return _models.Values.Where( pm => pm.DependencyInfo.DependsOn.Any( dep => dep.PluginId == pluginId ) );
    }
    #endregion

    #region Private Helpers
    private void ThrowIfNotInitialized()
    {
      if( !IsInitialized )
        throw new InvalidOperationException("PluginRepository can only be used after initialization is complete.");
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
