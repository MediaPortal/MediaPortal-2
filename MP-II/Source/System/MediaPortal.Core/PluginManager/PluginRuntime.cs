#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Core.Registry;

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Plugin runtime management class. Provides access to the plugin metadata, exposes the
  /// plugin's state and provides methods to switch between states.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Responsibility of <see cref="PluginRuntime"/>:</b>
  /// <list type="bullet">
  /// <item>Storing plugin runtime data like the <see cref="State"/>, the <see cref="StateTracker"/>,
  /// the <see cref="LoadedAssemblies"/> and the <see cref="DependentPlugins"/></item>
  /// <item>Registration of items in the plugin tree/registry and managing item location change listeners</item>
  /// <item>Instantiation of classes stored in assemblies of this plugin</item>
  /// </list>
  /// </para>
  /// <para>
  /// <b>Responsibility of the <see cref="IPluginManager"/> service:</b>
  /// <list type="bullet">
  /// <item>Management of the plugin state</item>
  /// <item>Management of the item usages</item>
  /// <item>Instantiation of the <see cref="StateTracker"/></item>
  /// <item>Management of plugin builder usages</item>
  /// </list>
  /// </para>
  /// The reason why all the plugin state management and instantiating of objects is done in the
  /// plugin manager is because those jobs require also the management of dependent plugins and so
  /// they encompass also other <see cref="PluginRuntime"/> instances.
  /// </remarks>
  public class PluginRuntime
  {
    #region Structs

    public class ObjectReference
    {
      protected int _refCounter = 0;
      protected object _object = null;

      public int RefCounter
      {
        get { return _refCounter; }
        set { _refCounter = value; }
      }

      public object Object
      {
        get { return _object; }
        set { _object = value; }
      }
    }

    #endregion

    #region Protected fields

    protected const string ITEMCHANGELISTENER_ID = "PLUGIN_ITEM_CHANGE_LISTENERS";

    protected IPluginMetadata _pluginMetadata;
    protected PluginState _state;

    protected IDictionary<PluginItemMetadata, PluginItemRegistration> _itemRegistrations =
        new Dictionary<PluginItemMetadata, PluginItemRegistration>();

    protected IPluginStateTracker _stateTracker = null;
    protected ICollection<Assembly> _loadedAssemblies = null; // Lazy initialized
    protected IDictionary<string, ObjectReference> _instantiatedObjects = null; // Lazy initialized
    protected ICollection<PluginRuntime> _dependentPlugins = null; // Lazy initialized

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new plugin runtime data structure for the specified plugin metadata
    /// instance.
    /// </summary>
    /// <param name="metaData">The metadata of the plugin to create this runtime structure for.</param>
    internal PluginRuntime(IPluginMetadata metaData)
    {
      _pluginMetadata = metaData;
      _state = PluginState.Available;
    }

    #endregion

    #region Public properties & methods

    /// <summary>
    /// Returns the plugin's metadata.
    /// </summary>
    public IPluginMetadata Metadata
    {
      get { return _pluginMetadata; }
    }

    /// <summary>
    /// Gets or sets the current runtime state of this plugin.
    /// </summary>
    public PluginState State
    {
      get { return _state; }
      set { _state = value; }
    }

    /// <summary>
    /// Returns the registration structures of all items of this plugin.
    /// </summary>
    public IDictionary<PluginItemMetadata, PluginItemRegistration> ItemRegistrations
    {
      get { return _itemRegistrations; }
    }

    /// <summary>
    /// Gets or sets the plugin's state tracker instance.
    /// </summary>
    public IPluginStateTracker StateTracker
    {
      get { return _stateTracker; }
      set { _stateTracker = value; }
    }

    /// <summary>
    /// Gets all assemblies which were already loaded for this plugin.
    /// </summary>
    public ICollection<Assembly> LoadedAssemblies
    {
      get { return _loadedAssemblies; }
    }

    /// <summary>
    /// Returns a collection of plugins, which depend directly on this plugin.
    /// This property builds up a plugin dependency tree. Note that there might be
    /// multiple dependency trees.
    /// </summary>
    public ICollection<PluginRuntime> DependentPlugins
    {
      get { return _dependentPlugins; }
    }

    public Type GetPluginType(string typeName)
    {
      LoadAssemblies();
      foreach (Assembly assembly in _loadedAssemblies)
      {
        Type type = assembly.GetType(typeName, false);
        if (type != null)
          return type;
      }
      return null;
    }

    /// <summary>
    /// Instantiates classes from this plugin's assemblies.
    /// </summary>
    public object InstanciatePluginObject(string typeName)
    {
      LoadAssemblies();
      if (_instantiatedObjects == null)
        _instantiatedObjects = new Dictionary<string, ObjectReference>();
      ObjectReference reference;
      if (_instantiatedObjects.ContainsKey(typeName))
        reference = _instantiatedObjects[typeName];
      else
      {
        Type type = GetPluginType(typeName);
        if (type == null)
          return null;
        reference = _instantiatedObjects[typeName] = new ObjectReference();
        reference.Object = Activator.CreateInstance(type);
      }
      reference.RefCounter++;
      return reference.Object;
    }

    /// <summary>
    /// Revokes the specified object from this plugin.
    /// </summary>
    public void RevokePluginObject(string typeName)
    {
      ObjectReference reference;
      if (!_instantiatedObjects.TryGetValue(typeName, out reference))
        return;
      if (--reference.RefCounter == 0)
      {
        IDisposable d = reference.Object as IDisposable;
        if (d != null)
          d.Dispose();
        _instantiatedObjects.Remove(typeName);
      }
    }

    #endregion

    #region Methods to be called from PluginManager

    /// <summary>
    /// Registers a plugin to be dependent from this plugin. This is necessary to build up a
    /// plugin dependency tree.
    /// </summary>
    /// <param name="plugin">The plugin which is dependent on this plugin.</param>
    internal void AddDependentPlugin(PluginRuntime plugin)
    {
      if (_dependentPlugins == null)
        _dependentPlugins = new List<PluginRuntime>();
      _dependentPlugins.Add(plugin);
    }

    /// <summary>
    /// Loads all plugin assemblies named in the plugin's metadata.
    /// </summary>
    internal void LoadAssemblies()
    {
      if (_loadedAssemblies != null)
        return;
      _loadedAssemblies = new List<Assembly>();
      foreach (string assemblyFilePath in _pluginMetadata.AssemblyFilePaths)
      {
        Assembly assembly = Assembly.LoadFrom(assemblyFilePath);
        _loadedAssemblies.Add(assembly);
      }
    }

    /// <summary>
    /// Registers all items of this plugin in the plugin tree and notifies the change listeners
    /// for all changed locations. This method should be called when the plugin gets
    /// enabled.
    /// </summary>
    internal void RegisterItems()
    {
      IDictionary<string, ICollection<PluginItemMetadata>> changedLocations =
          new Dictionary<string, ICollection<PluginItemMetadata>>();
      foreach (PluginItemMetadata itemMetadata in _pluginMetadata.PluginItemsMetadata)
      {
        if (!RegisterItem(itemMetadata))
          continue;
        // Prepare data for change listener calls
        ICollection<PluginItemMetadata> changedMetadataInLocation;
        if (changedLocations.ContainsKey(itemMetadata.RegistrationLocation))
          changedMetadataInLocation = changedLocations[itemMetadata.RegistrationLocation];
        else
          changedMetadataInLocation = changedLocations[itemMetadata.RegistrationLocation] = new List<PluginItemMetadata>();
        changedMetadataInLocation.Add(itemMetadata);
      }
      // Call change listeners
      foreach (KeyValuePair<string, ICollection<PluginItemMetadata>> changedLocation in changedLocations)
      {
        ICollection<IItemRegistrationChangeListener> listeners = GetListenersForLocation(changedLocation.Key, false);
        if (listeners == null)
          continue;
        foreach (IItemRegistrationChangeListener listener in listeners)
          listener.ItemsWereAdded(changedLocation.Key, changedLocation.Value);
      }
    }

    /// <summary>
    /// Unregisters all items of this plugin from the plugin tree and notifies the change listeners for
    /// all changed locations.
    /// </summary>
    internal void UnregisterItems()
    {
      // Collect data for listener calls
      IDictionary<string, ICollection<PluginItemMetadata>> changedLocations =
          new Dictionary<string, ICollection<PluginItemMetadata>>();
      foreach (PluginItemMetadata itemMetadata in _itemRegistrations.Keys)
      {
        ICollection<PluginItemMetadata> changedMetadataInLocation;
        if (changedLocations.ContainsKey(itemMetadata.RegistrationLocation))
          changedMetadataInLocation = changedLocations[itemMetadata.RegistrationLocation];
        else
          changedMetadataInLocation = changedLocations[itemMetadata.RegistrationLocation] = new List<PluginItemMetadata>();
        changedMetadataInLocation.Add(itemMetadata);
      }
      // Unregistration of items
      foreach (PluginItemMetadata itemMetadata in _pluginMetadata.PluginItemsMetadata)
        UnregisterItem(itemMetadata);
      // Call change listeners
      foreach (KeyValuePair<string, ICollection<PluginItemMetadata>> changedLocation in changedLocations)
      {
        ICollection<IItemRegistrationChangeListener> listeners = GetListenersForLocation(changedLocation.Key, false);
        if (listeners == null)
          continue;
        foreach (IItemRegistrationChangeListener listener in listeners)
          listener.ItemsWereRemoved(changedLocation.Key, changedLocation.Value);
      }
    }

    /// <summary>
    /// Returns the item registration instance for the specified <paramref name="location"/> and the specified
    /// <paramref name="id"/>.
    /// </summary>
    /// <param name="location">Registration location of the requested item registration instance.</param>
    /// <param name="id">Id of the requested item registration instance.</param>
    /// <returns>Requested item registration instance, if it exists.</returns>
    internal static PluginItemRegistration GetItemRegistration(string location, string id)
    {
      IRegistryNode node = GetRegistryNode(location, false);
      if (node != null && node.Items != null && node.Items.ContainsKey(id))
        return (PluginItemRegistration) node.Items[id];
      return null;
    }

    /// <summary>
    /// Returns all item registration instances for the specified <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Registration location of the requested item registration instances.</param>
    /// <returns>Collection of item registration instances at the specified location.</returns>
    internal static ICollection<PluginItemRegistration> GetItemRegistrations(string location)
    {
      IRegistryNode node = GetRegistryNode(location, false);
      ICollection<PluginItemRegistration> result = new List<PluginItemRegistration>();
      if (node != null && node.Items != null)
        foreach (object item in node.Items.Values)
          if (item is PluginItemRegistration)
            result.Add((PluginItemRegistration) item);
      return result;
    }

    /// <summary>
    /// Returns a collection of available child locations of the given <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Location for that the child locations should be returned.</param>
    /// <returns>Collection of child locations of the given parent <paramref name="location"/>.</returns>
    internal static ICollection<string> GetAvailableChildLocations(string location)
    {
      IRegistryNode node = GetRegistryNode(location, false);
      ICollection<string> result = new List<string>();
      if (node != null && node.SubNodes != null)
        foreach (string childName in node.SubNodes.Keys)
          result.Add(RegistryHelper.ConcatenatePaths(location, childName));
      return result;
    }

    /// <summary>
    /// Adds the specified item registration change <paramref name="listener"/> which will be notified
    /// when items are registered at the specified <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Location to add the listener to. The added <paramref name="listener"/> will
    /// be called when items are added to or removed from this location in the plugin tree.</param>
    /// <param name="listener">The listener to add.</param>
    internal static void AddItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
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
    internal static void RemoveItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      ICollection<IItemRegistrationChangeListener> listeners = GetListenersForLocation(location, false);
      if (listeners == null)
        return;
      listeners.Remove(listener);
    }

    #endregion

    #region Protected/private/internal methods to be called only from this class

    protected static ICollection<IItemRegistrationChangeListener> GetListenersForLocation(string location, bool createOnNotExist)
    {
      IRegistryNode node = GetRegistryNode(location, createOnNotExist);
      if (node == null)
        return null;
      object resultObj;
      if (node.Items != null && node.Items.TryGetValue(ITEMCHANGELISTENER_ID, out resultObj))
        return (ICollection<IItemRegistrationChangeListener>) resultObj;
      if (createOnNotExist)
      {
        ICollection<IItemRegistrationChangeListener> result = new List<IItemRegistrationChangeListener>();
        node.AddItem(ITEMCHANGELISTENER_ID, result);
        return result;
      }
      return null;
    }

    protected static IRegistryNode GetRegistryNode(string path, bool createOnNotExist)
    {
      IRegistry registry = ServiceScope.Get<IRegistry>();
      return registry.GetRegistryNode(path, createOnNotExist);
    }

    /// <summary>
    /// Registers the specified plugin item in the plugin tree.
    /// </summary>
    /// <param name="itemMetadata">Metadata structure of the item to register.</param>
    /// <returns><c>true</c>, if the plugin item could be registered, <c>false</c>,
    /// if the item already existed and <see cref="PluginItemMetadata.IsRedundant"/> is specified.</returns>
    /// <exception cref="ArgumentException">If there is already an item registered at the registration
    /// location and the <see cref="PluginItemMetadata.IsRedundant"/> flag is not set.</exception>
    internal bool RegisterItem(PluginItemMetadata itemMetadata)
    {
      IRegistryNode node = GetRegistryNode(itemMetadata.RegistrationLocation, true);
      itemMetadata.PluginRuntime = this;
      if (node.Items != null && node.Items.ContainsKey(itemMetadata.Id))
        if (itemMetadata.IsRedundant)
        {
          PluginItemRegistration itemRegistration = (PluginItemRegistration) node.Items[itemMetadata.Id];
          itemRegistration.AdditionalRedundantItemsMetadata.Add(itemMetadata);
          return false;
        }
        else
          throw new ArgumentException(string.Format("At location '{0}', a plugin item with id '{1}' is already registered",
              itemMetadata.RegistrationLocation, itemMetadata.Id));
      PluginItemRegistration resultItemRegistration = new PluginItemRegistration(itemMetadata);
      node.AddItem(itemMetadata.Id, resultItemRegistration);
      _itemRegistrations.Add(itemMetadata, resultItemRegistration);
      return true;
    }

    /// <summary>
    /// Unregisters the specified plugin item from the plugin tree.
    /// </summary>
    /// <param name="itemMetadata">Meta data structure of the item to unregister.</param>
    /// <returns>Plugin item registration structure of the item to be unregistered.</returns>
    internal void UnregisterItem(PluginItemMetadata itemMetadata)
    {
      try
      {
        IRegistryNode node = GetRegistryNode(itemMetadata.RegistrationLocation, false);
        if (node == null || node.Items == null)
          return;
        if (node.Items.ContainsKey(itemMetadata.Id))
        {
          PluginItemRegistration itemRegistration = (PluginItemRegistration) node.Items[itemMetadata.Id];
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
          if (newItemMetadata != null)
            newItemMetadata.PluginRuntime.RegisterItem(newItemMetadata);
        }
      }
      finally
      {
        _itemRegistrations.Remove(itemMetadata);
      }
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}: {1}", _pluginMetadata.Name, _state);
    }

    #endregion
  }
}
