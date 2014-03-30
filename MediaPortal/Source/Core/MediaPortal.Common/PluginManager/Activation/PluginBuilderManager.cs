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
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.Services.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.PluginManager.Activation
{
  /// <summary>
  /// This class is responsible for maintaining builder registrations, including creating builders. 
  /// </summary>
  internal class PluginBuilderManager
  {
    #region Fields
    private ConcurrentDictionary<string, PluginBuilderRegistration> _builders = new ConcurrentDictionary<string, PluginBuilderRegistration>();
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

    #region Registration
    public bool RegisterSystemPluginItemBuilder( string builderName, IPluginItemBuilder builderInstance )
    {
      var item = CreateSystemBuilderRegistration( builderName, builderInstance );
      return _builders.TryAdd( item.Key, item.Value );
    }

    /// <summary>
    /// Creates the builder registration instances for all builders in the specified
    /// <paramref name="plugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to take the builders from.</param>
    /// <returns>Dictionary of builder name to builder registration mappings.</returns>
    internal bool CreateBuilderRegistrations( PluginRuntime plugin )
    {
      try
      {
        IPluginMetadata md = plugin.Metadata;
        foreach( var pair in md.ActivationInfo.Builders )
        {
          var builder = new PluginBuilderRegistration( pair.Key, pair.Value, plugin );
          if( !_builders.TryAdd( pair.Key, builder ) )
            throw new Exception( "TODO" ); // TODO
        }
        // Check if builder dependencies are explicitly named (has to be done after dependencies are loaded - builders could be added by dependent plugins)
        return CheckDependencyDeclarations( plugin.Metadata as PluginMetadata );
      }
      catch( Exception ) // TODO
      {
        return false;
      }
    }

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

    #region Access
    public IPluginItemBuilder GetBuilder( string builderName )
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
        Log.Error( "Error instanciating plugin item builder '{0}' (id '{1}')", e,
          runtime.Metadata.Name, runtime.Metadata.PluginId );
      }
      if( obj == null )
        throw new PluginInternalException( "Builder class '{0}' could not be instantiated",
          builderRegistration.BuilderClassName );
      builderRegistration.Builder = obj as IPluginItemBuilder;
      if( builderRegistration.Builder != null )
        return builderRegistration.Builder;
      // build creation failed, remove builder registration
      runtime.RevokePluginObject( builderRegistration.BuilderClassName );
      throw new PluginInternalException( "Builder class '{0}' does not implement the plugin item builder interface '{1}'",
        builderRegistration.BuilderClassName, typeof(IPluginItemBuilder).Name );
    }
    #endregion

    #region Remove
    public void RemoveBuilder( string builderName )
    {
      PluginBuilderRegistration builderRegistration;
      if( !_builders.TryRemove( builderName, out builderRegistration ) )
        Log.Warn( "Builder '{0}' was not found in a request to remove it.", builderName );
    }
    #endregion

    #region Validation
    internal bool CheckDependencyDeclarations( PluginMetadata plugin )
    {
      foreach( string builderName in plugin.ActivationInfo.GetNecessaryBuilders() )
      {
        // Check if all plugins providing required builders are explicitly named as dependencies.
        // We require this check, because we want to ensure the plugin will run once it is enabled.
        // If we wouldn't force the plugin to place an explicit dependency on all its builder plugins,
        // some of the builder plugins could be removed and the new plugin would fail creating items.
        if( plugin.ActivationInfo.Builders.Keys.Contains( builderName ) )
          // Builder is provided by the plugin itself
          continue;

        PluginBuilderRegistration builderRegistration;
        if( !_builders.TryGetValue( builderName, out builderRegistration ) )
        {
          Log.Warn( "Plugin {0}: Builder '{1}' is not available - plugin won't be enabled", plugin.Name, builderName );
          return false;
        }

        // TODO may need something else here, as we're trying to uncouple metadata checks from runtime things
        if( builderRegistration.PluginRuntime == null )
          // Builder is a default builder
          continue;

        if( !plugin.DependencyInfo.DependsOn.Any( d => !d.IsCoreDependency && d.PluginId == builderRegistration.PluginRuntime.Metadata.PluginId ) )
        {
          Log.Error( "Plugin {0}: Builder '{1}' (implemented by plugin '{2}') is used, but this plugin dependency is not explicitly specified.",
            plugin.Name, builderName, builderRegistration.PluginRuntime.Metadata.Name );
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