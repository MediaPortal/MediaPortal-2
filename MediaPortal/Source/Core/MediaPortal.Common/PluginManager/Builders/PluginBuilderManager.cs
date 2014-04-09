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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Activation;
using MediaPortal.Common.PluginManager.Builders.SystemBuilders;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.PluginManager.Builders
{
  /// <summary>
  /// This class is responsible for maintaining builder registrations, including creating builders. It is
  /// intended for use internally by the plugin subsystem only. The class is thread-safe.
  /// </summary>
  internal class PluginBuilderManager
  {
    #region Fields
    private readonly ConcurrentDictionary<string, PluginBuilderRegistration> _builders = new ConcurrentDictionary<string, PluginBuilderRegistration>();
    #endregion

    #region Ctor
    public PluginBuilderManager()
    {
      foreach( KeyValuePair<string, IPluginItemBuilder> builderRegistration in GetDefaultBuilders() )
      {
        RegisterSystemPluginItemBuilder( builderRegistration.Key, builderRegistration.Value );
      }
    }
    #endregion

    #region Registration (RegisterSystemPluginItemBuilder, CreateBuilderRegistrations)
    internal bool RegisterSystemPluginItemBuilder( string builderName, IPluginItemBuilder builderInstance )
    {
      var item = CreateSystemBuilderRegistration( builderName, builderInstance );
      return _builders.TryAdd( item.Key, item.Value );
    }

    /// <summary>
    /// Creates the builder registration instances for all builders in the specified
    /// <paramref name="plugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to take the builders from.</param>
    /// <returns>True if the builders were successfully registered, false otherwise.</returns>
    internal bool CreateBuilderRegistrations( PluginRuntime plugin )
    {
      try
      {
        var pluginMetadata = plugin.Metadata;
        foreach( var pair in pluginMetadata.ActivationInfo.Builders )
        {
          var builder = new PluginBuilderRegistration( pair.Key, pair.Value, plugin );
          if( !_builders.TryAdd( pair.Key, builder ) )
          {
            Log.Error( "PluginBuilderManager: Error registering builder '{0}' for plugin {1}.", pair.Key, plugin.LogName );
            return false;
          }
        }
        // check if builder dependencies are explicitly declared (has to be done after dependencies are 
        // loaded, as builders could be added by dependent plugins)
        return CheckDependencyDeclarations( plugin.Metadata as PluginMetadata );
      }
      catch( Exception ex )
      {
        Log.Error( "PluginBuilderManager: An unspecified error occurred while registering builders for plugin {0}.", ex, plugin.LogName );
        return false;
      }
    }
    #endregion

    #region Private Helpers (CreateSystemBuilderRegistration, GetDefaultBuilders)
    private KeyValuePair<string, PluginBuilderRegistration> CreateSystemBuilderRegistration(
      string builderName, IPluginItemBuilder builderInstance )
    {
      var result = new KeyValuePair<string, PluginBuilderRegistration>( builderName,
        new PluginBuilderRegistration( builderName, builderInstance.GetType().FullName, null ) );
      result.Value.Builder = builderInstance;
      return result;
    }

    private IDictionary<string, IPluginItemBuilder> GetDefaultBuilders()
    {
      IDictionary<string, IPluginItemBuilder> result = new Dictionary<string, IPluginItemBuilder>
      {
        { "Instance", new InstanceBuilder() },
        { "Resource", new ResourceBuilder() },
        { "Service", new ServiceBuilder() },
      };
      return result;
    }
    #endregion

    #region Builder Access (GetBuilder, GetOrCreateBuilder)
    internal IPluginItemBuilder GetBuilder( string builderName )
    {
      PluginBuilderRegistration builderRegistration;
      if( !_builders.TryGetValue( builderName, out builderRegistration ) )
        throw new FatalException( "Builder '{0}' cannot be found. Something is wrong.", builderName );
      return builderRegistration.IsInstantiated ? builderRegistration.Builder : null;
    }

    internal IPluginItemBuilder GetOrCreateBuilder( string builderName, PluginState currentPluginState )
    {
      PluginBuilderRegistration builderRegistration;
      if( !_builders.TryGetValue( builderName, out builderRegistration ) )
        throw new FatalException( "Builder '{0}' cannot be found. Something is wrong.", builderName );
      if( builderRegistration.IsInstantiated )
        return builderRegistration.Builder;

      PluginRuntime runtime = builderRegistration.PluginRuntime;
      object obj = null;
      try
      {
        obj = runtime.InstantiatePluginObject( builderRegistration.BuilderClassName );
      }
      catch( Exception e )
      {
        Log.Error( "PluginBuilderManager: Error instanciating plugin item builder '{0}' (id '{1}')", e,
          runtime.Metadata.Name, runtime.Metadata.PluginId );
      }
      if( obj == null )
        throw new PluginBuilderException( "Builder class '{0}' could not be instantiated",
          builderRegistration.BuilderClassName );
      builderRegistration.Builder = obj as IPluginItemBuilder;
      if( builderRegistration.Builder != null )
        return builderRegistration.Builder;
      // build creation failed, remove builder registration
      runtime.RevokePluginObject( builderRegistration.BuilderClassName );
      throw new PluginBuilderException( "Builder class '{0}' does not implement the plugin item builder interface '{1}'",
        builderRegistration.BuilderClassName, typeof(IPluginItemBuilder).Name );
    }
    #endregion

    #region Remove (RemoveBuilder)
    internal void RemoveBuilder( string builderName )
    {
      PluginBuilderRegistration builderRegistration;
      if( !_builders.TryRemove( builderName, out builderRegistration ) )
        Log.Warn( "Builder '{0}' was not found in a request to remove it.", builderName );
    }
    #endregion

    #region Validation
    /// <summary>
    /// Verifies that all builders required by <param name="plugin" /> are available. It also verifies
    /// that only builders from plugins declared as dependencies are used (without this check it would
    /// be possible for plugins to omit dependency declarations, which could result in errors if a
    /// dependency is later removed or disabled).
    /// </summary>
    /// <param name="plugin">The plugin to verify.</param>
    /// <returns>True if all checks pass and false otherwise.</returns>
    internal bool CheckDependencyDeclarations( PluginMetadata plugin )
    {
      foreach( string builderName in plugin.ActivationInfo.GetNecessaryBuilders() )
      {
        if( plugin.ActivationInfo.Builders.Keys.Contains( builderName ) ) // builder is provided by the plugin itself
          continue;

        PluginBuilderRegistration builderRegistration;
        if( !_builders.TryGetValue( builderName, out builderRegistration ) )
        {
          Log.Warn( "PluginBuilderManager: Plugin {0} requires builder '{1}', which is not available (the plugin cannot be enabled).", plugin.LogName, builderName );
          return false;
        }

        if( builderRegistration.PluginRuntime == null ) // builder is a default builder
          continue;

        if( !plugin.DependencyInfo.DependsOn.Any( d => !d.IsCoreDependency && d.PluginId == builderRegistration.PluginRuntime.Metadata.PluginId ) )
        {
          Log.Error( "PluginBuilderManager: Plugin {0} uses builder '{1}' from plugin {2}, but does not declare it as a dependency (this error will prevent the plugin from being enabled).",
            plugin.LogName, builderName, builderRegistration.PluginRuntime.LogName );
          return false;
        }
      }
      return true;
    }
    #endregion

    #region Static Helpers
    private static ILogger Log
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion
  }
}