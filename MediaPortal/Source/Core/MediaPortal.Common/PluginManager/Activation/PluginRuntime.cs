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
using System.Linq;
using System.Reflection;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Activation
{
  /// <summary>
  /// Plugin runtime management class. Every installed plugin will have its own instance of this class,
  /// which manages <see cref="State"/> transitions and resources allocated by the plugin. Metadata for 
  /// the plugin can be accessed through the <see cref="Metadata"/> property. The class is thread-safe.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Responsibilities of <see cref="PluginRuntime"/>:</b>
  /// <list type="bullet">
  /// <item>Storing plugin runtime data like the <see cref="State"/>, the <see cref="StateTracker"/>
  /// and the loaded assemblies.</item>
  /// <item>Instantiation of classes stored in assemblies of this plugin.</item>
  /// <item>Registration of items in the plugin tree/registry and managing item location change listeners.
  /// This is done by delegating to an instance of the <see cref="PluginItemManager"/> class owned by the
  /// <see cref="PluginRuntime"/>. Builders are managed by the <see cref="PluginBuilderManager"/> class,
  /// which is shared between all plugins.</item>
  /// </list>
  /// </para>
  /// </remarks>
  public class PluginRuntime
  {
    #region Nested Classes and Structs
    private class ObjectReference
    {
      public int RefCounter { get; set; }
      public object Object { get; set; }
    }
    #endregion

    #region Fields
    // readonly fields
    private readonly object _syncObj = new object();
    private readonly PluginMetadata _pluginMetadata;
    private readonly PluginItemManager _itemManager;
    private readonly PluginBuilderManager _builderManager;
    private readonly PluginActivator _activator;
    // fields
    private PluginState _state;
    private IPluginStateTracker _stateTracker = null;
    private ICollection<Assembly> _loadedAssemblies = null; // Lazy initialized
    private IDictionary<string, ObjectReference> _instantiatedObjects = null; // Lazy initialized
    #endregion

    #region Ctor
    /// <summary>
    /// Creates a new plugin runtime data structure for the specified plugin metadata
    /// instance.
    /// </summary>
    /// <param name="metaData">The metadata of the plugin to create this runtime structure for.</param>
    /// <param name="builderManager">The builder manager where this plugin can register and find 
    /// builder instances.</param>
    /// <param name="activator">The PluginActivator instance creating this object.</param>
    internal PluginRuntime( PluginMetadata metaData, PluginBuilderManager builderManager, PluginActivator activator )
    {
      _pluginMetadata = metaData;
      _builderManager = builderManager;
      _activator = activator;
      _state = PluginState.Available;
      _itemManager = new PluginItemManager( this );
    }
    #endregion

    #region Properties
    /// <summary>
    /// Returns the plugin's metadata.
    /// </summary>
    public IPluginMetadata Metadata
    {
      get
      {
        lock( _syncObj )
          return _pluginMetadata;
      }
    }

    /// <summary>
    /// Gets or sets the current runtime state of this plugin.
    /// </summary>
    public PluginState State
    {
      get
      {
        lock( _syncObj )
          return _state;
      }
      private set
      {
        lock( _syncObj )
        {
          _state = value;
          if( _loadedAssemblies == null && value == PluginState.Active)
            LoadAssemblies();
        }
      }
    }

    /// <summary>
    /// Gets or sets the plugin's state tracker instance.
    /// </summary>
    public IPluginStateTracker StateTracker
    {
      get
      {
        lock( _syncObj )
          return _stateTracker;
      }
      internal set
      {
        lock( _syncObj )
          _stateTracker = value;
      }
    }

    /// <summary>
    /// Gets all assemblies which were already loaded for this plugin.
    /// </summary>
    public ICollection<Assembly> LoadedAssemblies
    {
      get
      {
        lock( _syncObj )
        {
          // NOTE we could return _loadedAssemblies directly if property was declared as IEnumerable
          return _loadedAssemblies.ToList().AsReadOnly();
        }
      }
    }
    #endregion

    #region Enable/Activate/Disable/Shutdown
    internal bool Disable()
    {
      lock( _syncObj )
      {
        switch( State )
        {
          case PluginState.Disabled:
            return true;
          case PluginState.Available:
            State = PluginState.Disabled;
            return true;
          case PluginState.Active:
          case PluginState.Enabled:
            if( State == PluginState.Active )
            {
              State = PluginState.EndRequest;
              if( _stateTracker != null && !_stateTracker.RequestEnd() )
              {
                Log.Debug( "PluginRuntime: Cannot disable plugin {0} because its state tracker doesn't want to be disabled", LogName );
                State = PluginState.Active;
                return false;
              }
              IDictionary<PluginItemRegistration, ICollection<IPluginItemStateTracker>> endRequestsToClose;
              ICollection<IPluginItemStateTracker> failedStateTrackers;
              _itemManager.PerformEndRequests( out endRequestsToClose, out failedStateTrackers );
              if( failedStateTrackers.Count == 0 )
              {
                State = PluginState.Stopping;
                if( _stateTracker != null )
                {
                  try
                  {
                    _stateTracker.Stop();
                  }
                  catch( Exception e )
                  {
                    Log.Warn( "PluginRuntime: Error stopping plugin state tracker '{0}' in plugin {1}", e,
                      _stateTracker, LogId );
                  }
                  RevokePluginObject( _stateTracker.GetType().FullName );
                  _stateTracker = null;
                }
                _itemManager.StopOpenEndRequests( endRequestsToClose );
                _itemManager.UnregisterItems();
                foreach( string builderName in Metadata.ActivationInfo.Builders.Keys )
                {
                  object builder = _builderManager.GetBuilder( builderName );
                  RevokePluginObject( builder.GetType().FullName );
                  _builderManager.RemoveBuilder( builderName );
                }
              }
              else
              {
                Log.Debug( "PluginRuntime: Cannot disable plugin {0}, because it is still in use by '{1}'",
                  LogName, failedStateTrackers.Select( failedStateTracker => failedStateTracker.UsageDescription ) );
                if( _stateTracker != null )
                  _stateTracker.Continue();
                _itemManager.ContinueOpenEndRequests( endRequestsToClose );
                return false;
              }
            }
            State = PluginState.Disabled;
            return true;
          default: // invalid current state for requested state change
            ThrowInvalidStateException( "disable" );
            return false;
        }
      }
    }

    internal bool Enable()
    {
      lock( _syncObj )
      {
        switch( State )
        {
          case PluginState.Active:
          case PluginState.Enabled:
            return true;
          case PluginState.Available:
          case PluginState.Disabled:
            var success = _itemManager.RegisterItems() && _builderManager.CreateBuilderRegistrations( this );
            if( success )
              State = PluginState.Enabled;
            return success;
          default: // invalid current state for requested state change
            ThrowInvalidStateException( "enable" );
            return false;
        }
      }
    }

    internal bool Activate( bool maintenanceMode )
    {
      lock( _syncObj )
      {
        switch( State )
        {
          case PluginState.Active:
            return true;
          case PluginState.Available:
          case PluginState.Disabled:
            return Enable() && Activate( maintenanceMode );
          case PluginState.Enabled:
            if( Metadata.ActivationInfo.StateTrackerClassName != null && !maintenanceMode )
            {
              try
              {
                object obj = InstantiatePluginObject( Metadata.ActivationInfo.StateTrackerClassName );
                var stateTracker = obj as IPluginStateTracker;
                if( obj == null )
                  Log.Error( "PluginRuntime: Couldn't instantiate plugin state tracker class '{0}' for plugin {1}",
                    Metadata.ActivationInfo.StateTrackerClassName, LogId );
                else if( stateTracker != null )
                {
                  _stateTracker = stateTracker;
                  try
                  {
                    stateTracker.Activated( this );
                  }
                  catch( Exception e )
                  {
                    Log.Warn( "PluginRuntime: Error activating plugin state tracker '{0}' in plugin {1}", e, stateTracker, LogId );
                  }
                }
                else
                {
                  Log.Error( "PluginRuntime: Plugin state tracker class '{0}' of plugin {1} doesn't implement interface {3}",
                    Metadata.ActivationInfo.StateTrackerClassName, LogName, typeof(IPluginStateTracker).Name );
                  RevokePluginObject( Metadata.ActivationInfo.StateTrackerClassName );
                }
              }
              catch( Exception e )
              {
                Log.Error( "PluginRuntime: Error instantiating plugin state tracker class '{0}' for plugin {1}",
                  e, Metadata.ActivationInfo.StateTrackerClassName, LogId );
              }
            }
            State = PluginState.Active;
            return true;
          default:
            ThrowInvalidStateException( "activate" );
            return false;
        }
      }
    }

    internal void Shutdown()
    {
      lock( _syncObj )
      {
        if( _stateTracker != null )
        {
          try
          {
            _stateTracker.Shutdown();
          }
          catch( Exception e )
          {
            Log.Warn( "PluginRuntime: Error shutting down plugin state tracker '{0}' for plugin {1}", e, _stateTracker, LogId );
          }
        }
      }
    }

    private void ThrowInvalidStateException( string action )
    {
      var msg = String.Format( "PluginRuntime: Cannot {0} plugin {1} while it is in state {2}.", action, LogName, State );
      Log.Error( msg );
      throw new PluginInvalidStateException( msg );
    }
    #endregion

    #region Internal Metadata Formatters (Logging Helpers)
    /// <summary>
    /// Returns a string with the plugins name, version, author and id.
    /// </summary>
    internal string LogInfo
    {
      get { return _pluginMetadata.LogInfo; }
    }

    /// <summary>
    /// Returns a string with the plugins name and id.
    /// </summary>
    internal string LogId
    {
      get { return _pluginMetadata.LogId; }
    }

    /// <summary>
    /// Returns a string with the plugins name.
    /// </summary>
    internal string LogName
    {
      get { return _pluginMetadata.LogName; }
    }
    #endregion

    #region Object Access (GetPluginType, Instantiate/Revoke PluginObject)
    /// <summary>
    /// Returns instances of types which are implemented in this plugin's assemblies.
    /// </summary>
    /// <param name="typeName">Fully qualified name of the requested type.</param>
    /// <returns>Type of the specified name or <c>null</c>, if the type wasn't found.</returns>
    public Type GetPluginType( string typeName )
    {
      lock( _syncObj )
      {
        LoadAssemblies();
        foreach( Assembly assembly in _loadedAssemblies )
        {
          Type type = assembly.GetType( typeName, false );
          if( type != null )
            return type;
        }
      }
      return null;
    }

    /// <summary>
    /// Instantiates classes from this plugin's assemblies.
    /// </summary>
    public object InstantiatePluginObject( string typeName )
    {
      ObjectReference reference;
      Type type = null; // Set to a type if we need to create instance
      lock( _syncObj )
      {
        LoadAssemblies();
        if( _instantiatedObjects == null )
          _instantiatedObjects = new Dictionary<string, ObjectReference>();
        if( _instantiatedObjects.ContainsKey( typeName ) )
          reference = _instantiatedObjects[ typeName ];
        else
        {
          type = GetPluginType( typeName );
          if( type == null )
            return null;
          reference = _instantiatedObjects[ typeName ] = new ObjectReference();
        }
        reference.RefCounter++;
      }
      if( type != null )
      {
        object obj = Activator.CreateInstance( type ); // Must be done outside the lock because we are calling foreign code
        lock( _syncObj )
          reference.Object = obj;
      }
      return reference.Object;
    }

    /// <summary>
    /// Revokes the specified object from this plugin.
    /// </summary>
    public void RevokePluginObject( string typeName )
    {
      IDisposable d = null;
      lock( _syncObj )
      {
        ObjectReference reference;
        if( !_instantiatedObjects.TryGetValue( typeName, out reference ) )
          return;
        if( --reference.RefCounter == 0 )
        {
          d = reference.Object as IDisposable;
          _instantiatedObjects.Remove( typeName );
        }
      }
      if( d != null )
      {
        try
        {
          d.Dispose(); // Must be done outside the lock because we are calling foreign code
        }
        catch( Exception e )
        {
          Log.Warn( "Error disposing plugin object '{0}' in plugin '{1}' (id '{2}')", e,
            typeName, _pluginMetadata.Name, _pluginMetadata.PluginId );
          throw;
        }
      }
    }
    #endregion

    #region Initialization Helpers
    /// <summary>
    /// Loads all plugin assemblies named in the plugin's metadata. The caller must hold the
    /// synchronization lock when calling this method.
    /// </summary>
    private void LoadAssemblies()
    {
      if( _loadedAssemblies != null )
        return;
      _loadedAssemblies = new List<Assembly>();
      foreach( string assemblyFilePath in _pluginMetadata.ActivationInfo.Assemblies )
      {
        Assembly assembly = Assembly.LoadFrom( assemblyFilePath );
        _loadedAssemblies.Add( assembly );
      }
    }
    #endregion

    #region Builder Methods
    public IPluginItemBuilder GetOrCreateBuilder( string builderName )
    {
      lock( _syncObj )
      {
        if( _state != PluginState.Active && _state != PluginState.Enabled )
          throw new PluginInvalidStateException( "PluginRuntime: GetOrCreateBuilder can only be called for enabled or active plugins." );
        return _builderManager.GetOrCreateBuilder( builderName, _state );
      }
    }
    #endregion

    #region Item Access
    internal object RequestItem( PluginItemRegistration itemRegistration, Type type, IPluginItemStateTracker stateTracker )
    {
      lock( _syncObj )
      {
        return _itemManager.RequestItem( itemRegistration, type, stateTracker, _activator );
      }
    }

    internal bool RegisterItem( PluginItemMetadata itemMetadata )
    {
      lock( _syncObj )
      {
        return _itemManager.RegisterItem( itemMetadata );
      }
    }

    public void RevokeItem( PluginItemRegistration itemRegistration )
    {
      lock( _syncObj )
      {
        IPluginItemBuilder builder = GetOrCreateBuilder( itemRegistration.Metadata.BuilderName );
        try
        {
          builder.RevokeItem( itemRegistration.Item, itemRegistration.Metadata, this );
        }
        catch( Exception e )
        {
          Log.Error( "PluginRuntime: Error revoking usage of item '{0}' at location '{1}' (item builder is '{2}')", e,
            itemRegistration.Metadata.Id, itemRegistration.Metadata.RegistrationLocation, itemRegistration.Metadata.BuilderName );
        }
      }
    }
    #endregion

    #region Static Helpers
    private static ILogger Log
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    #region Base overrides
    public override string ToString()
    {
      lock( _syncObj )
      {
        return string.Format( "{0}: {1}", _pluginMetadata.Name, _state );
      }
    }
    #endregion
  }
}
