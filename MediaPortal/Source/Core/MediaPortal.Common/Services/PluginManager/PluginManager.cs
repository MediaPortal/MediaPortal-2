#region Copyright (C) 2007-2015 Team MediaPortal
/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Attributes;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Activation;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.Services.PluginManager
{
  /// <summary>
  /// Implementation of the <see cref="IPluginManager"/> interface that reads plugins from plugin directories, with
  /// plugin.xml descriptor files.
  /// </summary>
  /// <remarks>
  /// <para>
  /// We store a collection of all plugins which are available in the system. Each of these plugins can be
  /// enabled, or in a running state, or can be disabled, either explicitly by the user or because of other
  /// conflicting plugins.
  /// </para>
  /// <para>
  /// Responsibilities regarding plugin state and item management are split up to the classes
  /// <see cref="PluginManager"/> and <see cref="PluginRuntime"/>. See the docs of
  /// <see cref="PluginRuntime"/> for more info.
  /// </para>
  /// <seealso cref="PluginState"/>.
  /// </remarks>

  // Implementation hints (multithreading strategy):
  // - C# locks are held shortly only to protect internal data structures against data corruption
  // - Protection of plugin states against concurrent modification during state changes or during item request/revocation is
  //   done by the use of explicit, non-blocking reader/writer locks implemented in PluginRuntime class
  // - It is possible to lock a plugin state dependency (reader lock) or a state lock for write (writer lock). Also a reader
  //   lock can be turned into a writer lock.
  // - With this implementation, we shouldn't get too many problems with concurrent state modifications; the only thing to
  //   care about is the startup and shutdown phase, where potentially multiple concurrent services try to enable plugins.
  public class PluginManager : IPluginManager, IStatus
  {
    #region Protected fields
    private readonly PluginRepository _repository = new PluginRepository();
    private readonly PluginRegistry _registry = new PluginRegistry();
    private readonly PluginBuilderManager _builderManager = new PluginBuilderManager();
    private readonly PluginActivator _activator;
    #endregion

    #region Ctor
    public PluginManager()
    {
      _activator = new PluginActivator( _repository, _builderManager );
    }
    #endregion

    #region IPluginManager implementation
    public PluginManagerState State
    {
      get { return _activator.State; }
    }

    public IDictionary<string, CoreAPIAttribute> CoreComponents
    {
      get { return _repository.CoreComponents; }
    }

    public IDictionary<Guid, PluginMetadata> AvailablePlugins
    {
      get { return _repository.Models; }
    }

    public bool MaintenanceMode
    {
      get { return _activator.MaintenanceMode; }
    }

    public void Initialize()
    {
      _activator.Initialize();
    }

    public void Startup( bool maintenanceMode )
    {
      _activator.Startup( maintenanceMode );
    }

    public void Shutdown()
    {
      _activator.Shutdown();
    }

    public PluginRuntime AddPlugin( IPluginMetadata pluginMetadata )
    {
      var metadata = pluginMetadata as PluginMetadata;
      if( metadata == null )
        throw new ArgumentException( "We cannot change this interface, but callers are expected to pass an instance of the PluginMetadata class.");
      return _activator.AddPlugin( metadata );
    }

    public bool TryStartPlugin( Guid pluginId, bool activate )
    {
      return _activator.TryStartPlugin( pluginId, activate );
    }

    public bool TryStopPlugin( Guid pluginId )
    {
      return _activator.TryStopPlugin( pluginId );
    }

    public void RegisterSystemPluginItemBuilder( string builderName, IPluginItemBuilder builderInstance )
    {
      _builderManager.RegisterSystemPluginItemBuilder( builderName, builderInstance );
    }

    public PluginItemMetadata GetPluginItemMetadata( string location, string id )
    {
      return _registry.GetPluginItemMetadata( location, id );
    }

    public ICollection<PluginItemMetadata> GetAllPluginItemMetadata( string location )
    {
      return _registry.GetAllPluginItemMetadata( location );
    }

    public ICollection<string> GetAvailableChildLocations( string location )
    {
      return _registry.GetAvailableChildLocations( location );
    }

    public T RequestPluginItem<T>( string location, string id, IPluginItemStateTracker stateTracker ) where T : class
    {
      return (T)RequestPluginItem( location, id, typeof(T), stateTracker );
    }

    public object RequestPluginItem( string location, string id, Type type, IPluginItemStateTracker stateTracker )
    {
      return _registry.RequestPluginItem( location, id, type, stateTracker );
    }

    public ICollection<T> RequestAllPluginItems<T>( string location, IPluginItemStateTracker stateTracker ) where T : class
    {
      return _registry.RequestAllPluginItems<T>( location, stateTracker );
    }

    public ICollection RequestAllPluginItems( string location, Type type, IPluginItemStateTracker stateTracker )
    {
      return _registry.RequestAllPluginItems( location, type, stateTracker );
    }

    public void RevokePluginItem( string location, string id, IPluginItemStateTracker stateTracker )
    {
      _registry.RevokePluginItem( location, id, stateTracker );
    }

    public void RevokeAllPluginItems( string location, IPluginItemStateTracker stateTracker )
    {
      _registry.RevokeAllPluginItems( location, stateTracker );
    }

    public void AddItemRegistrationChangeListener( string location, IItemRegistrationChangeListener listener )
    {
      _registry.AddItemRegistrationChangeListener( location, listener );
    }

    public void RemoveItemRegistrationChangeListener( string location, IItemRegistrationChangeListener listener )
    {
      _registry.RemoveItemRegistrationChangeListener( location, listener );
    }
    #endregion

    #region Public methods
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
      return _activator.TryEnable( plugin, doAutoActivate );
    }

    /// <summary>
    /// Tries to activate the specified <paramref name="plugin"/>. This method first tries to enable the plugin.
    /// </summary>
    /// <param name="plugin">Plugin to activate.</param>
    /// <returns><c>true</c>, if the plugin could be activated or was already active, else <c>false</c>.</returns>
    public bool TryActivate( PluginRuntime plugin )
    {
      return _activator.TryActivate( plugin );
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
      return _activator.TryDisable( plugin );
    }
    #endregion

    #region IStatus Implementation
    public IList<string> GetStatus()
    {
      IList<string> result = new List<string> { "=== PlugInManager" };
      foreach( PluginRuntime plugin in _activator.AvailablePlugins.Values )
      {
        result.Add( string.Format( "  Plugin '{0}': {1}", plugin.Metadata.Name, plugin.State ) );
      }
      return result;
    }
    #endregion
  }
}
