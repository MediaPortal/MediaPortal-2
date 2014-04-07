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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Activation
{
  /// <summary>
  /// This class is responsible for orchestrating plugin state changes. It uses the
  /// <see cref="PluginRepository"/> to locate available plugins, and manages the overall
  /// intialization, startup and shutdown sequences for plugins. The <see cref="PluginActivator"/>
  /// class automatically performs appropriate actions for all plugin dependencies affected by 
  /// a state change operation. The class is thread-safe.
  /// The logic to perform state transitions for a single plugin is handled by the
  /// <see cref="PluginRuntime"/> class, which also maintains the plugins <see cref="PluginState"/>.
  /// </summary>
  internal class PluginActivator
  {
    #region Fields
    private readonly PluginRepository _repository;
    private readonly PluginBuilderManager _builderManager;
    private readonly ConcurrentDictionary<Guid, PluginRuntime> _runtimes = new ConcurrentDictionary<Guid, PluginRuntime>();
    // lock used to synchronize access to methods that change plugin states
    private readonly object _pluginStateChangeLock = new object();
    // using numeric fields allows us to use Interlocked to ensure thread safety for these
    private long _state = (long)PluginManagerState.Uninitialized;
    private long _maintenanceMode; // 0 = false, 1 = true
    #endregion

    #region Ctor
    internal PluginActivator( PluginRepository repository, PluginBuilderManager builderManager )
    {
      _repository = repository;
      _builderManager = builderManager;
    }
    #endregion

    #region Properties
    public PluginManagerState State
    {
      get { return (PluginManagerState)Interlocked.Read( ref _state ); }
      private set { Interlocked.Exchange( ref _state, (long)value ); }
    }

    public IDictionary<Guid, PluginRuntime> AvailablePlugins
    {
      get { return _runtimes; }
    }

    public bool MaintenanceMode
    {
      get { return Interlocked.Read( ref _maintenanceMode ) == 1; }
      private set { Interlocked.Exchange( ref _maintenanceMode, value ? 1 : 0 ); }
    }
    #endregion

    #region Initialize/Startup/Shutdown
    public void Initialize()
    {
      var stopWatch = Stopwatch.StartNew();
      _repository.Initialize();
      Log.Info( "PluginActivator: Initializing..." );
      State = PluginManagerState.Initializing;
      _repository.Models.Values.ForEach( pm => AddPlugin( pm ) );
      stopWatch.Stop();
      Log.Debug("PluginActivator: Initialized (in {0}ms)", stopWatch.ElapsedMilliseconds);
    }

    public void Startup( bool maintenanceMode )
    {
      var stopWatch = Stopwatch.StartNew();
      Log.Info( maintenanceMode ? "PluginActivator: Startup in maintenance mode..." : "PluginActivator: Startup..." );
      MaintenanceMode = maintenanceMode;
      State = PluginManagerState.Starting;
      PluginManagerMessaging.SendPluginManagerMessage( PluginManagerMessaging.MessageType.Startup );

      Log.Debug( "PluginActivator: Checking dependencies" );
      ICollection<PluginRuntime> availablePlugins = _runtimes.Values;
      foreach( PluginRuntime plugin in availablePlugins )
      {
        if( _repository.IsDisabled( plugin.Metadata.PluginId ) )
          plugin.Disable();
        else
          TryEnable( plugin, !MaintenanceMode );
      }

      PluginManagerMessaging.SendPluginManagerMessage( PluginManagerMessaging.MessageType.PluginsInitialized );
      State = PluginManagerState.Running;
      Log.Debug( maintenanceMode ? "PluginActivator: Running in maintenance mode" : "PluginActivator: Ready" );
      var time = stopWatch.ElapsedMilliseconds;
      ServiceRegistration.LoadServicesFromPlugins();
      // performance logging
      Log.Info("PluginActivator: Spent {0}ms in {1} ({2}ms before loading services)", stopWatch.ElapsedMilliseconds, "Startup", time);
    }

    public void Shutdown()
    {
      ServiceRegistration.RemoveAndDisposePluginServices();
      Log.Info( "PluginActivator: Shutdown" );
      ICollection<PluginRuntime> availablePlugins = _runtimes.Values;
      State = PluginManagerState.ShuttingDown;
      PluginManagerMessaging.SendPluginManagerMessage( PluginManagerMessaging.MessageType.Shutdown );

      foreach( PluginRuntime plugin in availablePlugins )
      {
        plugin.Shutdown();
      }
    }
    #endregion

    #region Add/Start/Stop Plugin
    // we should make this private.. but it's part of IPluginManager
    internal PluginRuntime AddPlugin( PluginMetadata pluginMetadata )
    {
      var result = new PluginRuntime( pluginMetadata, _builderManager, this );
      if( _runtimes.TryAdd( pluginMetadata.PluginId, result ) ) 
        return result;
      var msg = string.Format( "PluginActivator: Plugin {0} could not be registered because of a duplicate identifier.", pluginMetadata.LogId );
      Log.Error( msg );
      throw new PluginInvalidMetadataException( msg );
    }

    public bool TryStartPlugin( Guid pluginId, bool activate )
    {
      PluginRuntime plugin;
      if( !_runtimes.TryGetValue( pluginId, out plugin ) )
        throw new ArgumentException( string.Format( "Plugin with id '{0}' not found", pluginId ) );
      bool result = activate ? TryActivate( plugin ) : TryEnable( plugin, true );
      if( result )
        _repository.NotifyPluginEnabled( pluginId );
      return result;
    }

    public bool TryStopPlugin( Guid pluginId )
    {
      _repository.NotifyPluginDisabled( pluginId );
      PluginRuntime plugin;
      if( !_runtimes.TryGetValue( pluginId, out plugin ) )
        return true;
      return TryDisable( plugin );
    }
    #endregion

    #region TryEnable/TryActivate/TryDisable
    /// <summary>
    /// Tries to enable the specified <paramref name="plugin"/>.
    /// If the plugin has the <see cref="IPluginMetadata.AutoActivate"/> property set, the plugin will be
    /// activated in this method as well if it could be enabled.
    /// </summary>
    /// <param name="plugin">Plugin to enable.</param>
    /// <param name="doAutoActivate">If set to <c>true</c>, this method will automatically activate
    /// the plugin if its <see cref="IPluginMetadata.AutoActivate"/> property is set. Else, if set to
    /// <c>false</c>, the auto activation setting will be ignored.</param>
    /// <returns><c>true</c>, if the specified <paramref name="plugin"/> and all its dependencies could
    /// be enabled, else <c>false</c>.</returns>
    public bool TryEnable( PluginRuntime plugin, bool doAutoActivate )
    {
      return TryChangePluginState( plugin.Metadata.PluginId, PluginState.Enabled, doAutoActivate );
    }

    /// <summary>
    /// Tries to activate the specified <paramref name="plugin"/>. This method first tries to enable the plugin.
    /// </summary>
    /// <param name="plugin">Plugin to activate.</param>
    /// <returns><c>true</c>, if the plugin could be activated or was already active, else <c>false</c>.</returns>
    public bool TryActivate( PluginRuntime plugin )
    {
      return TryChangePluginState( plugin.Metadata.PluginId, PluginState.Active );
    }

    /// <summary>
    /// Tries to disable the specified <paramref name="plugin"/>. This will try to disable all
    /// dependent plugins, deactivate the specified plugin, stop all its item usages, remove
    /// registered builders and disable the plugin.
    /// </summary>
    /// <param name="plugin">The plugin to disable.</param>
    /// <returns><c>true</c>, if the plugin and all dependent plugins could be disabled and all
    /// items usages could be stopped, else <c>false</c>.</returns>
    public bool TryDisable( PluginRuntime plugin )
    {
      return TryChangePluginState( plugin.Metadata.PluginId, PluginState.Disabled );
    }

    #region TryChangePluginState
    private bool TryChangePluginState( Guid pluginId, PluginState targetState, bool autoActivateOnEnable = false )
    {
      var plugin = _repository.GetPlugin( pluginId );
      lock( _pluginStateChangeLock )
      {
        // make sure plugin and its dependencies are all available and compatible
        if( !_repository.IsCompatible( plugin ) )
          return false;

        // get list of plugin with dependencies from repository
        var sortOrder = targetState == PluginState.Disabled ? PluginSortOrder.DependenciesLast : PluginSortOrder.DependenciesFirst;
        var plugins = _repository.GetPluginAndDependencies( pluginId, sortOrder ).Select( pm => pm.PluginId ).ToList();

        try
        {
          // get corresponding list of plugin runtimes
          var pluginRuntimes = plugins.Select( id => _runtimes[ id ] ).ToList();
          // filter plugins to operate on: exclude those already in target state and avoid downgrading from active to enabled
          var runtimesToChange = pluginRuntimes.Where( r => r.State != targetState && !(r.State == PluginState.Active && targetState == PluginState.Enabled) ).ToList();
          foreach( var runtime in runtimesToChange )
          {
            Log.Debug( "PluginActivator: Trying to change plugin {0} to state '{1}'...", runtime.LogInfo, targetState.ToString().ToLower() );
            switch( targetState )
            {
              case PluginState.Enabled:
                if( !runtime.Enable() )
                {
                  Log.Error("PluginActivator: Plugin {0} could not be enabled!", runtime.LogName);
                  return false;
                }
                if( autoActivateOnEnable && runtime.Metadata.ActivationInfo.AutoActivate )
                {
                  Log.Debug( "PluginActivator: Auto-activating plugin {0} as part of enable.", runtime.LogName );
                  if( !runtime.Activate( MaintenanceMode ) )
                  {
                    Log.Error("PluginActivator: Plugin {0} could not be activated!", runtime.LogName);
                    return false;
                  }
                }
                break;
              case PluginState.Active:
                if( !runtime.Activate( MaintenanceMode ) )
                {
                  Log.Error("PluginActivator: Plugin {0} could not be activated!", runtime.LogName);
                  return false;
                }
                break;
              case PluginState.Disabled:
                if( !runtime.Disable() )
                {
                  Log.Error("PluginActivator: Plugin {0} could not be disabled!", runtime.LogName);
                  return false;
                }
                break;
            }
            Log.Info( "PluginActivator: Plugin {0} was changed to state '{1}'.", runtime.LogName, runtime.State.ToString().ToLower() );
          }
        }
        catch( PluginInvalidStateException )
        {
          // TODO message could be more informative
          Log.Error( "PluginActivator: encountered plugin with invalid state, aborting current operation." );
          return false;
        }
        return true;        
      }
    }
    #endregion
    #endregion

    #region Static Helpers
    private static ILogger Log
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    #region IStatus Implementation
    public IList<string> GetStatus()
    {
      IList<string> result = new List<string> { "=== PlugInManager" };
      foreach( PluginRuntime plugin in _runtimes.Values )
      {
        result.Add( string.Format( "  Plugin '{0}': {1}", plugin.Metadata.Name, plugin.State ) );
      }
      return result;
    }
    #endregion
  }
}
