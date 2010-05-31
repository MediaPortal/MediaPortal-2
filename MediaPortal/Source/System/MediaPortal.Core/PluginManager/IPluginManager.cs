#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core.PluginManager.Exceptions;

namespace MediaPortal.Core.PluginManager
{
  public enum PluginManagerState
  {
    Uninitialized,
    Initializing,
    Starting,
    Running,
    ShuttingDown
  }

  /// <summary>
  /// Interface to access the MediaPortal plugin manager. It exposes the globally available methods
  /// to be called from throughout the application.
  /// </summary>
  /// <remarks>
  /// The plugin manager is responsible for managing all installed plugins. It resolves plugin conflicts,
  /// defines the load order and manages the lifecycle of the plugins.
  /// Its external interface provides methods to access items by their registration location and id.
  /// Plugin items will be lazily built. At the time an item is requested, it will be built if it was not
  /// built yet.
  /// TODO: Document multithreading.
  /// We need to make all methods able to be called while holding locks, e.g. requesting/revoking plugin items.
  /// So if we let all methods be called while other locks are held, we mustn't call other components in a synchronous
  /// way (e.g. callbacks for removing plugin items), so we must make them asynchronous.
  /// So, methods like start/stop plugin must be made asynchronous.
  /// </remarks>
  public interface IPluginManager
  {
    /// <summary>
    /// Denotes the current state of the plugin manager.
    /// </summary>
    PluginManagerState State { get; }

    /// <summary>
    /// Returns a dictionary containing the names of all plugins known by the plugin manager,
    /// mapped to their plugin runtime descriptors.
    /// </summary>
    IDictionary<Guid, PluginRuntime> AvailablePlugins { get; }

    /// <summary>
    /// Returns the information whether the plugin manager is in maintenance mode. This mode is for
    /// using the plugin manager without having the managed plugins started automatically.
    /// You can start the plugin manager in maintenance mode by using its <see cref="Startup"/>
    /// method with the <c>maintenanceMode</c> parameter set to <c>true</c>.
    /// </summary>
    bool MaintenanceMode { get; }

    /// <summary>
    /// Initializes the plugin manager. This will initialize internal structures, load the list
    /// of available plugins and initializes the plugins.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Starts the plugin manager. This will handle the plugin's auto activations, if
    /// the <paramref name="maintenanceMode"/> parameter is set to <c>false</c>.
    /// </summary>
    /// <param name="maintenanceMode">If set to <c>false</c> (normal case), this method will automatically
    /// activate those plugins with the <see cref="IPluginMetadata.AutoActivate"/> flag set. If set to
    /// <c>true</c> (maintainance mode), the plugins won't be started. This can be used for management
    /// application, which need the plugins be enabled but not activated.</param>
    void Startup(bool maintenanceMode);

    /// <summary>
    /// Called on system shutdown. The plugin manager will notify all plugins about the ongoing shutdown.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Adds a plugin to the <see cref="AvailablePlugins"/> collection during runtime.
    /// </summary>
    /// <param name="pluginMetadata">The new plugin's metadata instance.</param>
    PluginRuntime AddPlugin(IPluginMetadata pluginMetadata);

    /// <summary>
    /// Tries to enable the plugin with the specified <paramref name="pluginId"/>. The plugin will be
    /// enabled if it doesn't stand in conflict with already loaded plugins and if all plugins the
    /// specified plugin is dependent on can be enabled. The plugin will be activated automatically if its
    /// <see cref="IPluginMetadata.AutoActivate"/> property or the <paramref name="activate"/>
    /// parameter are set.
    /// </summary>
    /// <param name="pluginId">The id of the plugin to be enabled.</param>
    /// <param name="activate">If set to <c>true</c>, the plugin will be activated at once.</param>
    /// <returns><c>true</c>, if the plugin could be enabled, else <c>false</c>. If this plugin
    /// is already enabled or activated, the return value will be <c>true</c>.</returns>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    bool TryStartPlugin(Guid pluginId, bool activate);

    /// <summary>
    /// Tries to disable the specified plugin. The plugin will be deactivated and disabled if
    /// its plugin state tracker and every item user agree with the deactivation,
    /// and every plugin which depends on this plugin can be stopped too.
    /// </summary>
    /// <param name="pluginId">The id of the plugin to be stopped.</param>
    /// <returns><c>true</c>, if the plugin could be disabled, else <c>false</c>. If this plugin
    /// is already disabled, the return value will be <c>true</c>.</returns>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    bool TryStopPlugin(Guid pluginId);

    /// <summary>
    /// Adds a new plugin item builder, which is provided by a system component, to the plugin manager.
    /// This method mustn't be called from plugins, it should only be called by system components.
    /// </summary>
    /// <param name="builderName">The builder name. This is the name of the plugin item element which is handled
    /// by the builder to register.</param>
    /// <param name="builderInstance">The plugin item builder to register.</param>
    void RegisterSystemPluginItemBuilder(string builderName, IPluginItemBuilder builderInstance);

    /// <summary>
    /// Returns the ids of all plugins conflicting with the specified <paramref name="plugin"/>.
    /// </summary>
    /// <param name="plugin">The metadata of the plugin to check.</param>
    /// <returns>Collection of ids of the conflicting plugins.</returns>
    /// <exception cref="PluginMissingDependencyException">If the specified plugin depends on
    /// a missing plugin.</exception>
    ICollection<Guid> FindConflicts(IPluginMetadata plugin);

    /// <summary>
    /// Returns the ids of all plugins, from which the specified <paramref name="plugin"/> is dependent
    /// and which are not known by the plugin manager yet.
    /// </summary>
    /// <param name="plugin">The plugin whose dependencies should be checked.</param>
    /// <returns>Collection of plugin ids.</returns>
    ICollection<Guid> FindMissingDependencies(IPluginMetadata plugin);

    /// <summary>
    /// Returns the metadata of a registered item at the specified <paramref name="location"/> with
    /// the specified <paramref name="id"/>. This metadata contains all registration information of
    /// the specified item and can be evaluated before the item usage is requested. This method doesn't
    /// request the specified item yet, and thus doesn't load the item's plugin.
    /// </summary>
    /// <param name="location">Registration location of the requested item in the plugin tree.</param>
    /// <param name="id">Id which was used to register the requested item.</param>
    /// <returns>Item metadata structure, or <c>null</c>, if the specified item was not registered.</returns>
    PluginItemMetadata GetPluginItemMetadata(string location, string id);

    /// <summary>
    /// Returns the metadata of all registered items at the specified <paramref name="location"/>.
    /// The metadata structures contain all registration information of the specified items and can be
    /// evaluated before the item usages are requested. This method doesn't request the items' plugins
    /// yet and thus doesn't load the items' plugins.
    /// </summary>
    /// <param name="location">Registration location of the requested items in the plugin tree.</param>
    /// <returns>Collection of item metadata structures at the specified registration location.</returns>
    ICollection<PluginItemMetadata> GetAllPluginItemMetadata(string location);

    /// <summary>
    /// Returns a collection of available child locations of the given <paramref name="location"/>.
    /// </summary>
    /// <param name="location">Location for that the child locations should be returned.</param>
    /// <returns>Collection of child locations of the given parent <paramref name="location"/>.</returns>
    ICollection<string> GetAvailableChildLocations(string location);

    /// <summary>
    /// Returns a single plugin item registered at the given <paramref name="location"/> with the
    /// given <paramref name="id"/> and the specified type <typeparamref name="T"/> (or any subtype).
    /// </summary>
    /// <typeparam name="T">Class of the requested item.</typeparam>
    /// <param name="location">Registration location of the requested item in the plugin tree.</param>
    /// <param name="id">Id which was used to register the requested item.</param>
    /// <param name="stateTracker">Instance used to track the item's state.</param>
    /// <returns>The plugin item instance or <c>null</c>, if an item with the specified
    /// criteria was not registered.</returns>
    /// <remarks>
    /// This method will build the item if it was not built yet. If the usage of the item needs
    /// the plugin to be activated, the plugin activation will be triggered automatically by this method.
    /// If the requested item doesn't have type <see cref="T"/>, the item will be loaded anyhow,
    /// but in this case, the <paramref name="stateTracker"/> won't be registered at the item.
    /// </remarks>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    /// <seealso cref="RequestAllPluginItems{T}"/>
    T RequestPluginItem<T>(string location, string id, IPluginItemStateTracker stateTracker) where T : class;

    /// <summary>
    /// Non-generic form of <see cref="RequestPluginItem{T}<>"/>.
    /// </summary>
    /// <param name="location">Registration location of the requested item in the plugin tree.</param>
    /// <param name="id">Id which was used to register the requested item.</param>
    /// <param name="type">Class of the requested item.</param>
    /// <param name="stateTracker">Instance used to track the item's state.</param>
    /// <returns>The plugin item instance or <c>null</c>, if an item with the specified
    /// criteria was not registered.</returns>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    object RequestPluginItem(string location, string id, Type type, IPluginItemStateTracker stateTracker);

    /// <summary>
    /// Returns all plugin items registered at the given location, which have the specified type
    /// <see cref="T"/> (or any subtype).
    /// </summary>
    /// <typeparam name="T">Class of the requested items.</typeparam>
    /// <param name="location">Registration location of the requested items in the plugin tree.</param>
    /// <param name="stateTracker">Instance used to manage the item's state.</param>
    /// <returns>Collection of plugin items registered at the specified location in the plugin tree.</returns>
    /// <remarks>
    /// This method will build the items if they were not built yet. If the usage of some items needs
    /// their plugins to be activated, the plugin activations will be triggered automatically by this
    /// method.
    /// All items at the specified <paramref name="location"/> will be loaded. For those items which don't have
    /// type <see cref="T"/>, the <paramref name="stateTracker"/> won't be registered.
    /// </remarks>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    ICollection<T> RequestAllPluginItems<T>(string location, IPluginItemStateTracker stateTracker) where T : class;

    /// <summary>
    /// Non-generic form of <see cref="RequestAllPluginItems{T}<>"/>.
    /// </summary>
    /// <param name="location">Registration location of the requested items in the plugin tree.</param>
    /// <param name="type">Class of the requested items.</typeparam>
    /// <param name="stateTracker">Instance used to manage the item's state.</param>
    /// <returns>Collection of plugin items registered at the specified location in the plugin tree.</returns>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    ICollection RequestAllPluginItems(string location, Type type, IPluginItemStateTracker stateTracker);

    /// <summary>
    /// Revokes the usage of the item with the specified <paramref name="location"/> and the specified
    /// <paramref name="id"/> for the specified <paramref name="stateTracker"/>.
    /// This is necessary to release an item which was requested before.
    /// </summary>
    /// <param name="location">Registration location of the item to revoke.</param>
    /// <param name="id">Id which was used to register the item to revoke.</param>
    /// <param name="stateTracker">State tracker instance which was registered by the call to
    /// <see cref="RequestPluginItem{T}"/> or <see cref="RequestAllPluginItems{T}"/> before.</param>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    void RevokePluginItem(string location, string id, IPluginItemStateTracker stateTracker);

    /// <summary>
    /// Revokes the usage of all items with the specified <paramref name="location"/> for the specified
    /// <paramref name="stateTracker"/>.
    /// This is necessary to release the items requested before.
    /// </summary>
    /// <param name="location">Registration location of the items to revoke.</param>
    /// <param name="stateTracker">State tracker instance which was registered by the call to
    /// <see cref="RequestAllPluginItems{T}"/> before.</param>
    /// <exception cref="PluginLockException">If the plugin is locked by an ongoing plugin action.</exception>
    void RevokeAllPluginItems(string location, IPluginItemStateTracker stateTracker);

    /// <summary>
    /// Adds the specified change <paramref name="listener"/> instance which will be notified when items
    /// are registered or unregistered at the specified <paramref name="location"/>.
    /// </summary>
    void AddItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener);

    /// <summary>
    /// Removes the specified change <paramref name="listener"/> instance from the specified
    /// <paramref name="location"/>.
    /// </summary>
    void RemoveItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener);
  }
}
