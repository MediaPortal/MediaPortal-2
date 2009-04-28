#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Services.PluginManager.Builders;
using MediaPortal.Core.PluginManager.Exceptions;
using MediaPortal.Core.Settings;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Services.PluginManager
{
  /// <summary>
  /// An <see cref="IPluginManager"/> implementation that reads plugins from plugin directories, with
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
  public class PluginManager : IPluginManager, IStatus
  {
    #region Protected fields

    protected IDictionary<Guid, PluginRuntime> _availablePlugins = new Dictionary<Guid, PluginRuntime>();

    protected IDictionary<string, PluginBuilderRegistration> _builders =
        new Dictionary<string, PluginBuilderRegistration>();

    protected IDictionary<Guid, PluginState> _pendingPlugins = new Dictionary<Guid, PluginState>();

    protected PluginManagerState _state = PluginManagerState.Uninitialized;

    protected bool _maintenanceMode = false;

    #endregion

    #region Ctor

    public PluginManager()
    {
      foreach (KeyValuePair<string, IPluginItemBuilder> builderRegistration in GetDefaultBuilders())
        RegisterSystemPluginItemBuilder(builderRegistration.Key, builderRegistration.Value);
    }

    #endregion

    #region IPluginManager implementation

    public PluginManagerState State
    {
      get { return _state; }
    }

    public IDictionary<Guid, PluginRuntime> AvailablePlugins
    {
      get { return _availablePlugins; }
    }

    public bool MaintenanceMode
    {
      get { return _maintenanceMode; }
    }

    public void Initialize()
    {
      ServiceScope.Get<ILogger>().Info("PluginManager: Initialize");
      _state = PluginManagerState.Initializing;
      ServiceScope.Get<ILogger>().Debug("PluginManager: Loading plugins");
      IDictionary<string, IPluginMetadata> loadedPlugins = LoadPluginsData();
      foreach (IPluginMetadata pm in loadedPlugins.Values)
        AddPlugin(pm);
      ServiceScope.Get<ILogger>().Debug("PluginManager: Initialized");
    }

    public void Startup(bool maintenanceMode)
    {
      if (maintenanceMode)
        ServiceScope.Get<ILogger>().Info("PluginManager: Startup in maintenance mode");
      else
        ServiceScope.Get<ILogger>().Info("PluginManager: Startup");
      _maintenanceMode = maintenanceMode;
      _state = PluginManagerState.Starting;
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.NotificationType.Startup);
      PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
      ICollection<Guid> disabledPlugins = settings.UserDisabledPlugins;
      ServiceScope.Get<ILogger>().Debug("PluginManager: Checking dependencies");
      foreach (PluginRuntime plugin in _availablePlugins.Values)
      {
        if (disabledPlugins.Contains(plugin.Metadata.PluginId))
          plugin.State = PluginState.Disabled;
        else
          TryEnable(plugin, !_maintenanceMode);
      }
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.NotificationType.PluginsInitialized);
      _state = PluginManagerState.Running;
      if (maintenanceMode)
        ServiceScope.Get<ILogger>().Debug("PluginManager: Running in maintenance mode");
      else
        ServiceScope.Get<ILogger>().Debug("PluginManager: Ready");
    }

    public void Shutdown()
    {
      ServiceScope.Get<ILogger>().Info("PluginManager: Shutdown");
      _state = PluginManagerState.ShuttingDown;
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.NotificationType.Shutdown);
      foreach (PluginRuntime plugin in _availablePlugins.Values)
      {
        if (plugin.StateTracker != null)
          plugin.StateTracker.Shutdown();
      }
    }

    public PluginRuntime AddPlugin(IPluginMetadata pluginMetadata)
    {
      PluginRuntime result = new PluginRuntime(pluginMetadata);
      _availablePlugins.Add(pluginMetadata.PluginId, result);
      return result;
    }

    public bool TryStartPlugin(Guid pluginId, bool activate)
    {
      PluginRuntime pr;
      if (!_availablePlugins.TryGetValue(pluginId, out pr))
        throw new ArgumentException(string.Format("Plugin with id '{0}' not found", pluginId));
      bool result = activate ? TryActivate(pr) : TryEnable(pr, true);
      if (result)
      {
        PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
        settings.RemoveUserDisabledPlugin(pluginId);
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
      return result;
    }

    public bool TryStopPlugin(Guid pluginId)
    {
      PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
      settings.AddUserDisabledPlugin(pluginId);
      ServiceScope.Get<ISettingsManager>().Save(settings);

      PluginRuntime pr;
      if (_availablePlugins.TryGetValue(pluginId, out pr))
        return TryDisable(pr);
      return true;
    }

    public void RegisterSystemPluginItemBuilder(string builderName, IPluginItemBuilder builderInstance)
    {
      _builders.Add(CreateSystemBuilderRegistration(builderName, builderInstance));
    }

    public ICollection<Guid> FindConflicts(IPluginMetadata plugin)
    {
      ICollection<Guid> result = new HashSet<Guid>();
      // Conflicts declared by plugin
      ICollection<Guid> conflictingPlugins = CollectionUtils.Intersection(plugin.ConflictsWith, _availablePlugins.Keys);
      foreach (Guid conflictId in conflictingPlugins)
      {
        PluginRuntime conflict = _availablePlugins[conflictId];
        if (conflict.State != PluginState.Disabled)
          // Found a conflict
          result.Add(conflictId);
      }
      // Conflicts declared by other plugins
      foreach (PluginRuntime pr in _availablePlugins.Values)
      {
        if (pr.State != PluginState.Disabled && pr.Metadata.ConflictsWith.Contains(plugin.PluginId))
          // Plugin pr conflicts with plugin
          result.Add(pr.Metadata.PluginId);
      }
      foreach (Guid dependencyId in plugin.DependsOn)
      {
        PluginRuntime pr;
        if (!_availablePlugins.TryGetValue(dependencyId, out pr))
          throw new PluginMissingDependencyException("Plugin dependency '{0}' is not available", dependencyId);
        CollectionUtils.AddAll(result, FindConflicts(pr.Metadata));
      }
      return result;
    }

    public ICollection<Guid> FindMissingDependencies(IPluginMetadata plugin)
    {
      ICollection<Guid> result = new HashSet<Guid>();
      foreach (Guid dependencyId in plugin.DependsOn)
      {
        PluginRuntime pr;
        if (!_availablePlugins.TryGetValue(dependencyId, out pr))
          result.Add(dependencyId);
        CollectionUtils.AddAll(result, FindMissingDependencies(pr.Metadata));
      }
      return result;
    }

    public PluginItemMetadata GetPluginItemMetadata(string location, string id)
    {
      PluginItemRegistration registration = PluginRuntime.GetItemRegistration(location, id);
      return registration == null ? null : registration.Metadata;
    }

    public ICollection<PluginItemMetadata> GetAllPluginItemMetadata(string location)
    {
      ICollection<PluginItemMetadata> result = new List<PluginItemMetadata>();
      foreach (PluginItemRegistration registration in PluginRuntime.GetItemRegistrations(location))
        result.Add(registration.Metadata);
      return result;
    }

    public ICollection<string> GetAvailableChildLocations(string location)
    {
      return PluginRuntime.GetAvailableChildLocations(location);
    }

    /// <summary>
    /// Returns the item registered at the specified <paramref name="location"/> with the specified
    /// <paramref name="id"/> and the specified type.
    /// </summary>
    /// <remarks>
    /// If the requested item doesn't have type <see cref="T"/>, the item will be loaded anyhow,
    /// but in this case, the <paramref name="stateTracker"/> won't be registered at the item.
    /// </remarks>
    /// <seealso cref="IPluginManager.RequestAllPluginItems{T}"/>
    public T RequestPluginItem<T>(string location, string id, IPluginItemStateTracker stateTracker) where T : class
    {
      PluginItemRegistration itemRegistration = PluginRuntime.GetItemRegistration(location, id);
      return itemRegistration == null ? null : RequestItem<T>(itemRegistration, stateTracker);
    }

    /// <summary>
    /// Returns all items registered at the specified <paramref name="location"/> with the specified type.
    /// </summary>
    /// <remarks>
    /// All items at the specified <paramref name="location"/> will be loaded. For those items which don't have
    /// type <see cref="T"/>, the <paramref name="stateTracker"/> won't be registered.
    /// </remarks>
    /// <seealso cref="IPluginManager.RequestAllPluginItems{T}"/>
    public ICollection<T> RequestAllPluginItems<T>(string location, IPluginItemStateTracker stateTracker) where T : class
    {
      ICollection<T> result = new List<T>();
      foreach (PluginItemRegistration itemRegistration in PluginRuntime.GetItemRegistrations(location))
      {
        T item = RequestItem<T>(itemRegistration, stateTracker);
        if (item != null)
          result.Add(item);
      }
      return result;
    }

    public void RevokePluginItem(string location, string id, IPluginItemStateTracker stateTracker)
    {
      PluginItemRegistration itemRegistration = PluginRuntime.GetItemRegistration(location, id);
      RevokeItemUsage(itemRegistration, stateTracker);
    }

    public void RevokeAllPluginItems(string location, IPluginItemStateTracker stateTracker)
    {
      foreach (PluginItemRegistration itemRegistration in PluginRuntime.GetItemRegistrations(location))
        RevokeItemUsage(itemRegistration, stateTracker);
    }

    public void AddItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      PluginRuntime.AddItemRegistrationChangeListener(location, listener);
    }

    public void RemoveItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      PluginRuntime.RemoveItemRegistrationChangeListener(location, listener);
    }

    #endregion

    #region Item&Builder management

    internal T RequestItem<T>(PluginItemRegistration itemRegistration, IPluginItemStateTracker stateTracker) where T : class
    {
      PluginRuntime pluginRuntime = itemRegistration.Metadata.PluginRuntime;
      if (pluginRuntime.State != PluginState.Enabled && pluginRuntime.State != PluginState.Active)
        throw new PluginInvalidStateException("Plugin '{0}' (id '{1}') neither is enabled nor active, although it has registered items. Something is wrong.",
            itemRegistration.Metadata.PluginRuntime.Metadata.Name,
            itemRegistration.Metadata.PluginRuntime.Metadata.PluginId);
      // TODO: As of the specification (see PluginState.EndRequest), we have to make the current
      // thread sleep until the stopping procedure is finished or cancelled. This means we have to
      // implement a notification mechanism in the stop request methods to re-awake this thread.
      // By now, we simply let the item request fail (which is not the specified behavior!)
      if (!itemRegistration.StateTrackers.Contains(stateTracker))
        itemRegistration.StateTrackers.Add(stateTracker);
      try
      {
        if (itemRegistration.Item == null)
          itemRegistration.Item = BuildItem(itemRegistration.Metadata, pluginRuntime);
        if (itemRegistration.Item is T)
          return (T) itemRegistration.Item;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("PluginManager: Error building plugin item '{0}' at location '{1}'",
            e, itemRegistration.Metadata.Id, itemRegistration.Metadata.RegistrationLocation);
      }
      // Requested item isn't of type T - revoke usage again
      RevokeItemUsage(itemRegistration, stateTracker);
      return null;
    }

    /// <summary>
    /// Revokes the usage of the item which is identified by the specified item registration instance
    /// for the specified item <paramref name="stateTracker"/>.
    /// </summary>
    /// <param name="itemRegistration">Item registration instance which specifies the item to be
    /// revoked.</param>
    /// <param name="stateTracker">State tracker for which the usage of the item is revoked.
    /// The state tracker won't be called any more for any plugin state changes.</param>
    internal static void RevokeItemUsage(PluginItemRegistration itemRegistration, IPluginItemStateTracker stateTracker)
    {
      itemRegistration.StateTrackers.Remove(stateTracker);
      if (itemRegistration.StateTrackers.Count == 0)
      {
        IDisposable d = itemRegistration.Item as IDisposable;
        if (d != null)
          d.Dispose();
        itemRegistration.Item = null;
      }
      // If we wanted to automatically unload plugins whose items are not accessed any more, this
      // should be done here
    }

    /// <summary>
    /// Creates the builder registration instances for all builders in the specified
    /// <paramref name="plugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to take the builders from.</param>
    /// <returns>Dictionary of builder name to builder registration mappings.</returns>
    internal static IDictionary<string, PluginBuilderRegistration> CreateBuilderRegistrations(
        PluginRuntime plugin)
    {
      IDictionary<string, PluginBuilderRegistration> result = new Dictionary<string, PluginBuilderRegistration>();
      IPluginMetadata md = plugin.Metadata;
      foreach (KeyValuePair<string, string> builder in md.Builders)
        result.Add(builder.Key, new PluginBuilderRegistration(builder.Key, builder.Value, plugin));
      return result;
    }

    internal static KeyValuePair<string, PluginBuilderRegistration> CreateSystemBuilderRegistration(
        string builderName, IPluginItemBuilder builderInstance)
    {
      KeyValuePair<string, PluginBuilderRegistration> result =
          new KeyValuePair<string, PluginBuilderRegistration>(
              builderName,
              new PluginBuilderRegistration(builderName, builderInstance.GetType().FullName, null));
      result.Value.Builder = builderInstance;
      return result;
    }

    internal static IDictionary<string, IPluginItemBuilder> GetDefaultBuilders()
    {
      IDictionary<string, IPluginItemBuilder> result = new Dictionary<string, IPluginItemBuilder>();
      result.Add("Instance", new InstanceBuilder());
      result.Add("Resource", new ResourceBuilder());
      return result;
    }

    internal IPluginItemBuilder GetOrCreateBuilder(string builderName)
    {
      PluginBuilderRegistration builderRegistration;
      if (!_builders.TryGetValue(builderName, out builderRegistration))
        throw new FatalException(
              "Builder '{0}' cannot be found. Something is wrong.", builderName);
      IPluginItemBuilder result;
      if (builderRegistration.IsInstantiated)
        result = builderRegistration.Builder;
      else
      {
        if (!TryActivate(builderRegistration.PluginRuntime))
          throw new PluginInvalidStateException(
              "Cannot activate plugin providing builder '{0}', which is necessary to build item");
        result = builderRegistration.PluginRuntime.InstanciatePluginObject(
              builderRegistration.BuilderClassName) as IPluginItemBuilder;
        if (result == null)
          throw new PluginInternalException("Builder class '{0}' could not be instantiated",
              builderRegistration.PluginRuntime.Metadata.Name);
      }
      return result;
    }

    internal object BuildItem(PluginItemMetadata metadata, PluginRuntime plugin)
    {
      IPluginItemBuilder builder = GetOrCreateBuilder(metadata.BuilderName);
      if (builder.NeedsPluginActive(metadata, plugin) && !TryActivate(plugin))
        throw new PluginInvalidStateException(string.Format(
            "Plugin '{0}' (id '{1}') cannot be activated, although it has registered items. Something is wrong.",
            metadata.PluginRuntime.Metadata.Name, metadata.PluginRuntime.Metadata.PluginId));
      return builder.BuildItem(metadata, plugin);
    }

    #endregion

    #region Private & protected methods

    protected static bool IsEnabledOrActive(PluginRuntime pr)
    {
      return pr.State == PluginState.Enabled || pr.State == PluginState.Active;
    }

    protected static bool IsInStoppingProcess(PluginRuntime pr)
    {
      return pr.State == PluginState.EndRequest || pr.State == PluginState.Stopping;
    }

    private static void ContinueOpenEndRequests(
        IEnumerable<KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>>
        endRequestsToClose)
    {
      foreach (KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose)
        foreach (IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value)
          stateTracker.Continue(itemStateTrackersToFinish.Key);
    }

    private static void StopOpenEndRequests(
        IEnumerable<KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>>
        endRequestsToClose)
    {
      foreach (KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose)
        foreach (IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value)
          stateTracker.Stop(itemStateTrackersToFinish.Key);
    }

    private static void PerformEndRequests(IEnumerable<PluginItemRegistration> items,
        out IDictionary<PluginItemRegistration, ICollection<IPluginItemStateTracker>> succeededEndRequests,
        out ICollection<IPluginItemStateTracker> failedStateTrackers)
    {
      succeededEndRequests = new Dictionary<PluginItemRegistration, ICollection<IPluginItemStateTracker>>();
      failedStateTrackers = new List<IPluginItemStateTracker>();
      foreach (PluginItemRegistration itemRegistration in items)
      {
        ICollection<IPluginItemStateTracker> succeededStataTrackers = new List<IPluginItemStateTracker>();
        succeededEndRequests.Add(itemRegistration, succeededStataTrackers);
        foreach (IPluginItemStateTracker stateTracker in itemRegistration.StateTrackers)
        {
          if (stateTracker.RequestEnd(itemRegistration))
            succeededStataTrackers.Add(stateTracker);
          else
            failedStateTrackers.Add(stateTracker);
        }
      }
    }

    /// <summary>
    /// Method to avoid the re-entering of state change methods for the same plugin.
    /// This might happen if a component of the outside tries to trigger a state change while
    /// it is called back from a state changing method.
    /// </summary>
    /// <param name="plugin">The plugin currently handled.</param>
    /// <param name="newState">New state which is to be set.</param>
    protected void StateChangeStart(PluginRuntime plugin, PluginState newState)
    {
      Guid pluginId = plugin.Metadata.PluginId;
      if (_pendingPlugins.ContainsKey(pluginId))
        throw new PluginRecursiveStateChangeException(
            "Plugin '{0}' (id: '{1}') is already changing its state to '{1}'",
            plugin.Metadata.Name, pluginId, newState);
      _pendingPlugins.Add(pluginId, newState);
    }

    /// <summary>
    /// Method to finish a state change. This will enable the plugin for other state changes again.
    /// </summary>
    /// <param name="plugin">The plugin for that method <see cref="StateChangeStart"/> was called
    /// before.</param>
    protected void StateChangeFinish(PluginRuntime plugin)
    {
      _pendingPlugins.Remove(plugin.Metadata.PluginId);
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
    public bool TryEnable(PluginRuntime plugin, bool doAutoActivate)
    {
      // Plugin is already available - check its current state
      if (IsEnabledOrActive(plugin))
        return true;
      if (IsInStoppingProcess(plugin))
        return false;
      StateChangeStart(plugin, PluginState.Enabled);
      try
      {
        ILogger logger = ServiceScope.Get<ILogger>();
        string pluginName = plugin.Metadata.Name;
        Guid pluginId = plugin.Metadata.PluginId;
        logger.Debug("PluginManager: Trying to enable plugin '{0}' (id '{1}')", pluginName, pluginId);
        if (FindConflicts(plugin.Metadata).Count > 0)
        {
          TryDisable(plugin);
          return false;
        }

        // Handle dependencies
        ICollection<PluginRuntime> pendingChildRegistrations = new List<PluginRuntime>();
        foreach (Guid parentId in plugin.Metadata.DependsOn)
        {
          PluginRuntime parentRuntime;
          if (!_availablePlugins.TryGetValue(parentId, out parentRuntime))
          {
            logger.Warn("Plugin '{0}': Dependency '{2}' is not available", pluginName, pluginId, parentId);
            return false;
          }
          if (!TryEnable(parentRuntime, doAutoActivate))
          {
            logger.Warn("Plugin '{0}': Dependency '{2}' cannot be enabled", pluginName, pluginId, parentId);
            return false;
          }
          pendingChildRegistrations.Add(parentRuntime); // Remember parent -> have to register return value as dependent plugin later
        }
        foreach (string builderName in plugin.Metadata.GetNecessaryBuilders())
        {
          // Check if all plugins providing required builders are explicitly named as dependencies.
          // We require this check, because we want to ensure the plugin will run once it is enabled.
          // If we wouldn't force the plugin to place an explicit dependency on all its builder plugins,
          // some of the builder plugins could be removed and the new plugin would fail creating items.
          if (plugin.Metadata.Builders.Keys.Contains(builderName))
            // Builder is provided by the plugin itself
            continue;
          PluginBuilderRegistration builderRegistration;
          if (!_builders.TryGetValue(builderName, out builderRegistration))
          {
            logger.Warn("Plugin '{0}': Builder '{2}' is not available - plugin won't be enabled",
              pluginName, pluginId, builderName);
            return false;
          }
          if (builderRegistration.PluginRuntime == null)
            // Builder is a default builder
            continue;
          if (!plugin.Metadata.DependsOn.Contains(builderRegistration.PluginRuntime.Metadata.PluginId))
          {
            logger.Error(
                "Plugin '{0}': Builder '{2}' (implemented by plugin '{3}') is used, but this plugin dependency is not explicitly specified - plugin won't be enabled",
                pluginName, pluginId, builderName, builderRegistration.PluginRuntime.Metadata.Name);
            return false;
          }
        }
        try
        {
          plugin.RegisterItems();
        }
        catch (Exception e)
        {
          logger.Error("Error registering plugin items for plugin '{0}' (id '{1}')", e, pluginName, pluginId);
          plugin.UnregisterItems();
          return false;
        }
        CollectionUtils.AddAll(_builders, CreateBuilderRegistrations(plugin));
        foreach (PluginRuntime parent in pendingChildRegistrations)
          parent.AddDependentPlugin(plugin);
        plugin.State = PluginState.Enabled;
        logger.Info("PluginManager: Plugin '{0}' (id '{1}') enabled.", pluginName, pluginId);
      }
      finally
      {
        StateChangeFinish(plugin);
      }
      if (doAutoActivate && plugin.Metadata.AutoActivate)
        TryActivate(plugin);
      return true;
    }

    /// <summary>
    /// Tries to activate the specified <paramref name="plugin"/>. This method first tries to enable the plugin.
    /// </summary>
    /// <param name="plugin">Plugin to activate.</param>
    /// <returns><c>true</c>, if the plugin could be activated or was already active, else <c>false</c>.</returns>
    public bool TryActivate(PluginRuntime plugin)
    {
      if (!TryEnable(plugin, false))
        return false;
      if (IsInStoppingProcess(plugin))
        return false;
      if (plugin.State == PluginState.Active)
        return true;
      string pluginName = plugin.Metadata.Name;
      Guid pluginId = plugin.Metadata.PluginId;
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Debug("PluginManager: Trying to activate plugin '{0}' (id '{1}')", pluginName, pluginId);
      StateChangeStart(plugin, PluginState.Active);
      try
      {
        // Load assemblies of parent plugins
        foreach (Guid parentId in plugin.Metadata.DependsOn)
        {
          PluginRuntime parent = _availablePlugins[parentId];
          logger.Debug("PluginManager: Checking activation of plugin dependency '{0}' for plugin '{1}'",
              parentId, pluginName);
          if (!TryActivate(parent))
          {
            logger.Debug("PluginManager: Dependent plugin '{0}' could not be activated. Activation of plugin '{1}' was not successful.",
                parentId, pluginName);
            return false;
          }
        }
        // Activate plugin
        plugin.LoadAssemblies();
        plugin.State = PluginState.Active;
        if (plugin.Metadata.StateTrackerClassName != null && !_maintenanceMode)
        {
          try
          {
            object obj = plugin.InstanciatePluginObject(plugin.Metadata.StateTrackerClassName);
            if (obj == null)
              logger.Error("PluginManager: Couldn't instantiate plugin state tracker class '{0}' for plugin '{1}'",
                  plugin.Metadata.StateTrackerClassName, pluginName);
            else if (obj is IPluginStateTracker)
            {
              plugin.StateTracker = obj as IPluginStateTracker;
              plugin.StateTracker.Activated(plugin);
            }
            else
              logger.Error("PluginManager: Plugin state tracker class '{0}' of plugin '{1}' doesn't implement interface {3}",
                  plugin.Metadata.StateTrackerClassName, pluginName, typeof(IPluginStateTracker).Name);
          }
          catch (Exception e)
          {
            logger.Error("PluginManager: Error instantiating plugin state tracker class '{0}' for plugin '{1}' (id '{2}')",
                e, plugin.Metadata.StateTrackerClassName, pluginName, pluginId);
          }
        }
        logger.Info("PluginManager: Plugin '{0}' (id '{1}') activated.", pluginName, pluginId);
        return true;
      }
      finally
      {
        StateChangeFinish(plugin);
      }
    }

    /// <summary>
    /// Tries to disable the specified <paramref name="plugin"/>. This will try to disable all
    /// dependent plugins, deactivate the specified plugin, stop all its item usages, remove
    /// registered builders and disable the plugin.
    /// </summary>
    /// <param name="plugin">The plugin to disable.</param>
    /// <returns><c>true</c>, if the plugin and all dependent plugins could be disabled and all
    /// items usages could be stopped, else <c>false</c>.</returns>
    public bool TryDisable(PluginRuntime plugin)
    {
      if (plugin.State == PluginState.Disabled)
        return true;
      if (IsInStoppingProcess(plugin))
        throw new PluginInvalidStateException("Plugin is in stopping process");
      StateChangeStart(plugin, PluginState.Disabled);
      try
      {
        ILogger logger = ServiceScope.Get<ILogger>();
        string pluginName = plugin.Metadata.Name;
        Guid pluginId = plugin.Metadata.PluginId;
        logger.Debug("PluginManager: Trying to disable plugin '{0}' (id '{1}')", pluginName, pluginId);
        // Handle dependencies
        ICollection<PluginRuntime> dependencies = plugin.DependentPlugins;
        if (dependencies != null)
          foreach (PluginRuntime child in dependencies)
            if (!TryDisable(child))
            {
              logger.Debug("PluginManager: Cannot disable plugin '{0}' because dependent plugin '{1}' cannot be disabled",
                  pluginName, child.Metadata.PluginId);
              return false;
            }
        if (plugin.State == PluginState.Active)
        {
          plugin.State = PluginState.EndRequest;
          if (plugin.StateTracker != null)
            if (!plugin.StateTracker.RequestEnd())
            {
              logger.Debug("PluginManager: Cannot disable plugin '{0}' because its state tracker doesn't want to be disabled",
                  pluginName);
              plugin.State = PluginState.Active;
              return false;
            }
          IDictionary<PluginItemRegistration, ICollection<IPluginItemStateTracker>> endRequestsToClose;
          ICollection<IPluginItemStateTracker> failedStateTrackers;
          PerformEndRequests(plugin.ItemRegistrations.Values, out endRequestsToClose, out failedStateTrackers);
          if (failedStateTrackers.Count == 0)
          {
            plugin.State = PluginState.Stopping;
            if (plugin.StateTracker != null)
              plugin.StateTracker.Stop();
            StopOpenEndRequests(endRequestsToClose);

            plugin.UnregisterItems();
            foreach (string builderName in plugin.Metadata.Builders.Keys)
              _builders.Remove(builderName);
          }
          else
          {
            logger.Debug("PluginManager: Cannot disable plugin '{0}', because it is still in use by '{1}'",
                pluginName, CollectionUtils.Transform(failedStateTrackers, new PluginItemStateTrackerToNameTransformer()));
            if (plugin.StateTracker != null)
              plugin.StateTracker.Continue();
            ContinueOpenEndRequests(endRequestsToClose);
            return false;
          }
        }
        plugin.State = PluginState.Disabled;
        logger.Info("PluginManager: Plugin '{0}' (id '{1}') disabled.", pluginName, pluginId);
        return true;
      }
      finally
      {
        StateChangeFinish(plugin);
      }
    }

    /// <summary>
    /// Loads all available plugin descriptors from all known plugin directories.
    /// </summary>
    /// <returns>Mapping of plugin names to metadata descriptors.</returns>
    public IDictionary<string, IPluginMetadata> LoadPluginsData()
    {
      IDictionary<string, IPluginMetadata> result = new Dictionary<string, IPluginMetadata>();
      String pluginsDirectoryPath = ServiceScope.Get<IPathManager>().GetPath("<PLUGINS>");
      foreach (string pluginDirectoryPath in Directory.GetDirectories(pluginsDirectoryPath))
      {
        if (Path.GetFileName(pluginDirectoryPath).StartsWith("."))
          continue;
        try
        {
          IPluginMetadata pm = new PluginDirectoryDescriptor(pluginDirectoryPath);
          if (result.ContainsKey(pm.Name))
            throw new ArgumentException(string.Format(
                "Duplicate plugin '{0}'", pm.Name));
          result.Add(pm.Name, pm);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error("Error loading plugin in directory '{0}'", e, pluginDirectoryPath);
        }
      }
      return result;
    }

    #endregion

    #region IStatus Implementation

    public IList<string> GetStatus()
    {
      IList<string> result = new List<string>();
      result.Add("=== PlugInManager");
      foreach (PluginRuntime plugin in _availablePlugins.Values)
        result.Add(string.Format("  Plugin '{0}': {1}", plugin.Metadata.Name, plugin.State));
      return result;
    }

    #endregion
  }
}
