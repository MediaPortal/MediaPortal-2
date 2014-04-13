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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Activation;
using MediaPortal.Common.PluginManager.Builders;
using MediaPortal.Common.PluginManager.Exceptions;

namespace MediaPortal.Common.PluginManager.Items
{
  /// <summary>
  /// This class is responsible for managing item registrations. It is an internal helper
  /// class intended only for use by the PluginRuntime class.
  /// </summary>
  internal class PluginItemManager
  {
    #region Fields
    private readonly PluginRuntime _pluginRuntime;
    private readonly IDictionary<PluginItemMetadata, PluginItemRegistration> _itemRegistrations =
      new Dictionary<PluginItemMetadata, PluginItemRegistration>();
    private readonly PluginRegistry _pluginRegistry = new PluginRegistry();
    #endregion

    #region Ctor
    internal PluginItemManager( PluginRuntime pluginRuntime )
    {
      _pluginRuntime = pluginRuntime;
    }
    #endregion

    #region Item Requests
    internal bool IsActivationRequiredForItemRequests( PluginItemRegistration itemRegistration )
    {
      var plugin = itemRegistration.Metadata.PluginRuntime;
      var state = plugin.State;
      var isActive = state == PluginState.Active;
      var isEnabled = state == PluginState.Enabled;
      if( !(isActive || isEnabled) )
        throw new PluginInvalidStateException( "Plugin {0} is neither enabled nor active; cannot request items in this state", plugin.LogId );

      PluginItemMetadata itemMetadata = itemRegistration.Metadata;
      IPluginItemBuilder builder = plugin.GetOrCreateBuilder( itemMetadata.BuilderName );
      bool mustBuild = itemRegistration.Item == null;
      bool mustBeActive = builder.NeedsPluginActive( itemMetadata, plugin );
      return mustBuild && mustBeActive && !isActive;
    }

    internal object RequestItem( PluginItemRegistration itemRegistration, Type type, IPluginItemStateTracker stateTracker )
    {
      PluginRuntime plugin = itemRegistration.Metadata.PluginRuntime;
      if( IsActivationRequiredForItemRequests( itemRegistration ) )
      {
        throw new PluginInvalidStateException(
          string.Format( "Plugin {0} must be in active state for item requests. It is currently in state '{1}'.",
            plugin.LogId, plugin.State ) );
      }
        
      try
      {
        PluginItemMetadata itemMetadata = itemRegistration.Metadata;
        IPluginItemBuilder builder = plugin.GetOrCreateBuilder( itemMetadata.BuilderName );

        // TODO REVIEW
        if( _pluginRuntime != itemRegistration.Metadata.PluginRuntime )
          throw new ArgumentException("Mismatch between PluginRuntime references");
        // this code below can be simplified, assuming that _pluginRuntime == itemRegistration.Metadata.PluginRuntime, 
        // since then we know that we have the runtime instance lock: building twice should not be possible.
        object item = builder.BuildItem( itemMetadata, plugin );
        if( item == null )
          return null;
        object releaseItem = null; // Fail safety - if we built the item twice by accident, release it at the end
        try
        {
          if( itemRegistration.Item == null )
            itemRegistration.Item = item;
          else
            releaseItem = item;
          object result = itemRegistration.Item;
          if( type.IsInstanceOfType( result ) )
          {
            if( !itemRegistration.StateTrackers.Contains( stateTracker ) )
              itemRegistration.StateTrackers.Add( stateTracker );
            return result;
          }
        }
        finally
        {
          // In case we built the item concurrently twice, release item
          if( releaseItem != null )
            builder.RevokeItem( releaseItem, itemMetadata, plugin );
        }
      }
      catch( Exception e )
      {
        Log.Error( "PluginItemManager: Error building plugin item '{0}' at location '{1}'",
          e, itemRegistration.Metadata.Id, itemRegistration.Metadata.RegistrationLocation );
      }
      // Requested item isn't of type T - revoke usage again
      if( itemRegistration.Item != null )
        _pluginRegistry.RevokeItemUsage( itemRegistration, null );
      return null;
    }
    #endregion

    #region Register/Unregister Items
    /// <summary>
    /// Registers all items of this plugin in the plugin tree and notifies the change listeners
    /// for all changed locations. This method should be called when the plugin gets enabled.
    /// </summary>
    internal bool RegisterItems()
    {
      try
      {
        var changedLocations = new Dictionary<string, ICollection<PluginItemMetadata>>();
        foreach( PluginItemMetadata itemMetadata in _pluginRuntime.Metadata.ActivationInfo.Items )
        {
          itemMetadata.PluginRuntime = _pluginRuntime;
          if( !RegisterItem( itemMetadata ) )
            continue;
          // save changed locations so we can notify listeners when done
          if( !changedLocations.ContainsKey( itemMetadata.RegistrationLocation ) )
            changedLocations[ itemMetadata.RegistrationLocation ] = new List<PluginItemMetadata>();
          changedLocations[ itemMetadata.RegistrationLocation ].Add( itemMetadata );
        }
        _pluginRegistry.NotifyItemsChanged( changedLocations, added: true );
        return true;
      }
      catch( Exception e )
      {
        Log.Error( "PluginItemManager: Error registering plugin items for plugin {0}", e, _pluginRuntime.LogId );
        UnregisterItems();
        return false;
      }
    }

    /// <summary>
    /// Unregisters all items of this plugin from the plugin tree and notifies the change listeners for
    /// all changed locations.
    /// </summary>
    internal void UnregisterItems()
    {
      // collect data on changed locations so we can notify listeners when done
      var changedLocations = new Dictionary<string, ICollection<PluginItemMetadata>>();
      foreach( PluginItemMetadata itemMetadata in _itemRegistrations.Keys )
      {
        if( !changedLocations.ContainsKey( itemMetadata.RegistrationLocation ) )
          changedLocations[ itemMetadata.RegistrationLocation ] = new List<PluginItemMetadata>();
        changedLocations[ itemMetadata.RegistrationLocation ].Add( itemMetadata );
      }
      foreach( PluginItemMetadata itemMetadata in _pluginRuntime.Metadata.ActivationInfo.Items )
      {
        UnregisterItem( itemMetadata );
      }
      _pluginRegistry.NotifyItemsChanged( changedLocations, added: false );
    }
    #endregion

    #region Register/Unregister Item
    /// <summary>
    /// Registers the specified plugin item in the plugin tree.
    /// </summary>
    /// <param name="itemMetadata">Metadata structure of the item to register.</param>
    /// <returns><c>true</c>, if the plugin item could be registered, <c>false</c>,
    /// if the item already existed and <see cref="PluginItemMetadata.IsRedundant"/> is specified.</returns>
    /// <exception cref="ArgumentException">If there is already an item registered at the registration
    /// location and the <see cref="PluginItemMetadata.IsRedundant"/> flag is not set.</exception>
    internal bool RegisterItem( PluginItemMetadata itemMetadata )
    {
      PluginItemRegistration itemRegistration;
      if( !_pluginRegistry.RegisterItem( itemMetadata, out itemRegistration ) )
        return false;
      _itemRegistrations.Add( itemMetadata, itemRegistration );
      return true;
    }

    /// <summary>
    /// Unregisters the specified plugin item from the plugin tree.
    /// </summary>
    /// <param name="itemMetadata">Meta data structure of the item to unregister.</param>
    /// <returns>Plugin item registration structure of the item to be unregistered.</returns>
    internal void UnregisterItem( PluginItemMetadata itemMetadata )
    {
      try
      {
        _pluginRegistry.UnregisterItem( itemMetadata );
      }
      finally
      {
        _itemRegistrations.Remove( itemMetadata );
      }
    }
    #endregion

    #region StateTracker Management
    private static bool StateTrackerRequestEnd( IPluginItemStateTracker stateTracker, PluginItemRegistration itemRegistration )
    {
      try
      {
        return stateTracker.RequestEnd( itemRegistration );
      }
      catch( Exception e )
      {
        Log.Error( "Error calling method 'RequestEnd' at plugin item state tracker '{0}'", e, stateTracker );
        return true;
      }
    }

    private static void StateTrackerContinue( IPluginItemStateTracker stateTracker, PluginItemRegistration itemRegistration )
    {
      try
      {
        stateTracker.Continue( itemRegistration );
      }
      catch( Exception e )
      {
        Log.Error( "Error calling method 'Continue' at plugin item state tracker '{0}'", e, stateTracker );
      }
    }

    private static void StateTrackerStop( IPluginItemStateTracker stateTracker, PluginItemRegistration itemRegistration )
    {
      try
      {
        stateTracker.Stop( itemRegistration );
      }
      catch( Exception e )
      {
        Log.Error( "Error calling method 'Stop' at plugin item state tracker '{0}'", e, stateTracker );
      }
    }

    internal void ContinueOpenEndRequests(
      IEnumerable<KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>> endRequestsToClose )
    {
      foreach( KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose )
      {
        foreach( IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value )
        {
          StateTrackerContinue( stateTracker, itemStateTrackersToFinish.Key );
        }
      }
    }

    internal void StopOpenEndRequests(
      IEnumerable<KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>> endRequestsToClose )
    {
      foreach( KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose )
      {
        foreach( IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value )
        {
          StateTrackerStop( stateTracker, itemStateTrackersToFinish.Key );
        }
      }
    }

    internal void PerformEndRequests(
      out IDictionary<PluginItemRegistration, ICollection<IPluginItemStateTracker>> succeededEndRequests,
      out ICollection<IPluginItemStateTracker> failedStateTrackers )
    {
      succeededEndRequests = new Dictionary<PluginItemRegistration, ICollection<IPluginItemStateTracker>>();
      failedStateTrackers = new List<IPluginItemStateTracker>();
      foreach( PluginItemRegistration itemRegistration in _itemRegistrations.Values )
      {
        ICollection<IPluginItemStateTracker> succeededStataTrackers = new List<IPluginItemStateTracker>();
        succeededEndRequests.Add( itemRegistration, succeededStataTrackers );
        foreach( IPluginItemStateTracker stateTracker in itemRegistration.StateTrackers )
        {
          if( StateTrackerRequestEnd( stateTracker, itemRegistration ) )
            succeededStataTrackers.Add( stateTracker );
          else
            failedStateTrackers.Add( stateTracker );
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
  }
}
