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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.PluginManager.Items;
using MediaPortal.Common.Registry;
using MediaPortal.Common.Services.Registry;

namespace MediaPortal.Common.PluginManager.Activation
{
  /// <summary>
  /// This class is responsible for all registry operations for plugins. It contains no state
  /// and is thread-safe only to the extent provided by the accessed registry content.
  /// It relies on access to registry, including reading from <see cref="RegistryNode"/>
  /// properties (such as the Items or SubNodes dictionaries), being thread-safe.
  /// </summary>
  internal class PluginRegistry
  {
    #region Fields

    private const string ITEMCHANGELISTENER_ID = "PLUGIN_ITEM_CHANGE_LISTENERS";

    #endregion

    #region Item Metadata Lookup

    public PluginItemMetadata GetPluginItemMetadata(string location, string id)
    {
      PluginItemRegistration registration = GetItemRegistration(location, id);
      return registration == null ? null : registration.Metadata;
    }

    public ICollection<PluginItemMetadata> GetAllPluginItemMetadata(string location)
    {
      return GetItemRegistrations(location).Select(registration => registration.Metadata).ToList();
    }

    #endregion

    #region Item Requests

    public T RequestPluginItem<T>(string location, string id, IPluginItemStateTracker stateTracker) where T : class
    {
      return (T)RequestPluginItem(location, id, typeof(T), stateTracker);
    }

    public object RequestPluginItem(string location, string id, Type type, IPluginItemStateTracker stateTracker)
    {
      PluginItemRegistration itemRegistration = GetItemRegistration(location, id);
      return itemRegistration == null ? null : RequestItem(itemRegistration, type, stateTracker);
    }

    public ICollection<T> RequestAllPluginItems<T>(string location, IPluginItemStateTracker stateTracker) where T : class
    {
      return GetItemRegistrations(location).Select(itemRegistration =>
      {
        try
        {
          return (T)RequestItem(itemRegistration, typeof(T), stateTracker);
        }
        catch (PluginInvalidStateException e)
        {
          Log.Warn("Cannot request {0}", e, itemRegistration.Metadata);
        }
        return null;
      }).Where(item => item != null).ToList();
    }

    public ICollection RequestAllPluginItems(string location, Type type, IPluginItemStateTracker stateTracker)
    {
      IList result = new ArrayList();
      foreach (PluginItemRegistration itemRegistration in GetItemRegistrations(location))
      {
        try
        {
          object item = RequestItem(itemRegistration, type, stateTracker);
          if (item != null)
            result.Add(item);
        }
        catch (PluginInvalidStateException e)
        {
          Log.Warn("Cannot request {0}", e, itemRegistration.Metadata);
        }
      }
      return result;
    }

    private object RequestItem(PluginItemRegistration itemRegistration, Type type, IPluginItemStateTracker stateTracker)
    {
      PluginRuntime plugin = itemRegistration.Metadata.PluginRuntime;
      // go through plugin when requesting item to obtain lock on PluginRuntime
      return plugin.RequestItem(itemRegistration, type, stateTracker);
    }

    #endregion

    #region Item Revocation

    public void RevokePluginItem(string location, string id, IPluginItemStateTracker stateTracker)
    {
      PluginItemRegistration itemRegistration = GetItemRegistration(location, id);
      if (itemRegistration != null)
        RevokeItemUsage(itemRegistration, stateTracker);
    }

    public void RevokeAllPluginItems(string location, IPluginItemStateTracker stateTracker)
    {
      ICollection<PluginItemRegistration> registrations = GetItemRegistrations(location);
      foreach (PluginItemRegistration itemRegistration in registrations)
        RevokeItemUsage(itemRegistration, stateTracker);
    }

    /// <summary>
    /// Revokes the usage of the item which is identified by the specified item registration instance
    /// for the specified item <paramref name="stateTracker"/>.
    /// </summary>
    /// <param name="itemRegistration">
    /// Item registration instance which specifies the item to be
    /// revoked.
    /// </param>
    /// <param name="stateTracker">
    /// State tracker for which the usage of the item is revoked.
    /// The state tracker won't be called any more for any plugin state changes.
    /// </param>
    internal void RevokeItemUsage(PluginItemRegistration itemRegistration, IPluginItemStateTracker stateTracker)
    {
      PluginItemMetadata metadata = itemRegistration.Metadata;
      PluginRuntime plugin = metadata.PluginRuntime;
      if (stateTracker != null)
        itemRegistration.StateTrackers.Remove(stateTracker);
      if (itemRegistration.StateTrackers.Count > 0)
        return;
      // No more state trackers present, revoke item
      plugin.RevokeItem(itemRegistration);
      itemRegistration.Item = null;
      // If we wanted to automatically unload plugins whose items are not accessed any more, this
      // should be done here
    }

    #endregion

    #region GetItemRegistration(s) / GetAvailableChildLocations

    /// <summary>
    /// Returns the item registration instance for the specified <paramref name="location"/> and the specified
    /// <paramref name="id"/>.
    /// </summary>
    /// <param name="location">Registration location of the requested item registration instance.</param>
    /// <param name="id">Id of the requested item registration instance.</param>
    /// <returns>Requested item registration instance, if it exists.</returns>
    internal PluginItemRegistration GetItemRegistration(string location, string id)
    {
      IRegistryNode node = GetRegistryNode(location, false);
      if (node != null && node.Items != null && node.Items.ContainsKey(id))
        return (PluginItemRegistration)node.Items[id];
      return null;
    }

    /// <summary>
    /// Returns all item registration instances for the specified <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Registration location of the requested item registration instances.</param>
    /// <returns>Collection of item registration instances at the specified location.</returns>
    internal ICollection<PluginItemRegistration> GetItemRegistrations(string location)
    {
      IRegistryNode node = GetRegistryNode(location, false);
      var result = new List<PluginItemRegistration>();
      if (node != null && node.Items != null)
        result.AddRange(node.Items.Values.OfType<PluginItemRegistration>());
      return result;
    }

    /// <summary>
    /// Returns a collection of available child locations of the given <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Location for that the child locations should be returned.</param>
    /// <returns>Collection of child locations of the given parent <paramref name="location"/>.</returns>
    internal ICollection<string> GetAvailableChildLocations(string location)
    {
      IRegistryNode node = GetRegistryNode(location, false);
      var result = new List<string>();
      if (node != null && node.SubNodes != null)
        result.AddRange(node.SubNodes.Keys.Select(childName => RegistryHelper.ConcatenatePaths(location, childName)));
      return result;
    }

    #endregion

    #region Register/Unregister Item

    /// <summary>
    /// Registers the specified plugin item in the plugin tree.
    /// </summary>
    /// <param name="itemMetadata">Metadata structure of the item to register.</param>
    /// <param name="itemRegistration">The item registratiom for the just registered item.</param>
    /// <returns>
    /// <c>true</c>, if the plugin item could be registered, <c>false</c>,
    /// if the item already existed and <see cref="PluginItemMetadata.IsRedundant"/> is specified.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If there is already an item registered at the registration
    /// location and the <see cref="PluginItemMetadata.IsRedundant"/> flag is not set.
    /// </exception>
    internal bool RegisterItem(PluginItemMetadata itemMetadata, out PluginItemRegistration itemRegistration)
    {
      IRegistryNode node = GetRegistryNode(itemMetadata.RegistrationLocation, true);
      if (node.Items != null && node.Items.ContainsKey(itemMetadata.Id))
      {
        if (!itemMetadata.IsRedundant)
          throw new ArgumentException(string.Format("At location '{0}', a plugin item with id '{1}' is already registered",
            itemMetadata.RegistrationLocation, itemMetadata.Id));
        itemRegistration = (PluginItemRegistration)node.Items[itemMetadata.Id];
        itemRegistration.AdditionalRedundantItemsMetadata.Add(itemMetadata);
        return false;
      }
      itemRegistration = new PluginItemRegistration(itemMetadata);
      node.AddItem(itemMetadata.Id, itemRegistration);
      return true;
    }

    /// <summary>
    /// Unregisters the specified plugin item from the plugin tree.
    /// </summary>
    /// <param name="itemMetadata">Meta data structure of the item to unregister.</param>
    /// <returns>Plugin item registration structure of the item to be unregistered.</returns>
    internal void UnregisterItem(PluginItemMetadata itemMetadata)
    {
      IRegistryNode node = GetRegistryNode(itemMetadata.RegistrationLocation, false);
      if (node == null || node.Items == null)
        return;
      if (node.Items.ContainsKey(itemMetadata.Id))
      {
        var itemRegistration = (PluginItemRegistration)node.Items[itemMetadata.Id];
        // Check, if there are additional redundant items registered at this position. If yes, we'll use
        // the first of them instead of the old item to be unregistered.
        PluginItemMetadata newItemMetadata = null;
        IEnumerator<PluginItemMetadata> enumerator = itemRegistration.AdditionalRedundantItemsMetadata.GetEnumerator();
        if (enumerator.MoveNext())
        {
          newItemMetadata = enumerator.Current;
          itemRegistration.AdditionalRedundantItemsMetadata.Remove(newItemMetadata);
        }
        node.Items.Remove(itemMetadata.Id);

        // TODO NOTE FIXME looks like we're calling another PluginRuntime here - since they don't 
        // share locks, this could result in deadlocks. Additional thought required.
        if (newItemMetadata != null)
          newItemMetadata.PluginRuntime.RegisterItem(newItemMetadata);
      }
    }

    #endregion

    #region ItemRegistrationChangeListener Management

    /// <summary>
    /// Adds the specified item registration change <paramref name="listener"/> which will be notified
    /// when items are registered at the specified <paramref name="location"/>.
    /// </summary>
    /// <param name="location">
    /// Location to add the listener to. The added <paramref name="listener"/> will
    /// be called when items are added to or removed from this location in the plugin tree.
    /// </param>
    /// <param name="listener">The listener to add.</param>
    internal void AddItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      ICollection<IItemRegistrationChangeListener> listeners = GetListenersForLocation(location, true);
      listeners.Add(listener);
    }

    /// <summary>
    /// Removes the specified change <paramref name="listener"/> instance from the specified
    /// <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Location to remove the listener from.</param>
    /// <param name="listener">The listener to remove.</param>
    internal void RemoveItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      ICollection<IItemRegistrationChangeListener> listeners = GetListenersForLocation(location, false);
      if (listeners == null)
        return;
      listeners.Remove(listener);
    }

    /// <summary>
    /// Returns all plugin item change listeners for the given <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Location to return the listeners for.</param>
    /// <param name="createOnNotExist">If set to <c>true</c>, the plugin tree node will be created, if it doesn't exist.</param>
    private ICollection<IItemRegistrationChangeListener> GetListenersForLocation(string location, bool createOnNotExist)
    {
      IRegistryNode node = GetRegistryNode(location, createOnNotExist);
      if (node == null)
        return null;
      object resultObj;
      if (node.Items != null && node.Items.TryGetValue(ITEMCHANGELISTENER_ID, out resultObj))
        return (ICollection<IItemRegistrationChangeListener>)resultObj;
      if (createOnNotExist)
      {
        var result = new List<IItemRegistrationChangeListener>();
        node.AddItem(ITEMCHANGELISTENER_ID, result);
        return result;
      }
      return null;
    }

    /// <summary>
    /// Notifies registered listeners that items have been added or removed.
    /// </summary>
    /// <param name="changedLocations">The locations that have been modified.</param>
    /// <param name="added"><c>True</c> if items were added and <c>false</c> if items were removed.</param>
    internal void NotifyItemsChanged(Dictionary<string, ICollection<PluginItemMetadata>> changedLocations, bool added)
    {
      foreach (KeyValuePair<string, ICollection<PluginItemMetadata>> changedLocation in changedLocations)
      {
        ICollection<IItemRegistrationChangeListener> listeners = GetListenersForLocation(changedLocation.Key, false);
        if (listeners == null)
          continue;
        foreach (IItemRegistrationChangeListener listener in listeners)
        {
          if (added)
            listener.ItemsWereAdded(changedLocation.Key, changedLocation.Value);
          else
            listener.ItemsWereRemoved(changedLocation.Key, changedLocation.Value);
        }
      }
    }

    #endregion

    #region Static Helpers

    private static ILogger Log
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    private static IRegistryNode GetRegistryNode(string path, bool createOnNotExist)
    {
      var registry = ServiceRegistration.Get<IRegistry>();
      return registry.GetRegistryNode(path, createOnNotExist);
    }

    #endregion
  }
}