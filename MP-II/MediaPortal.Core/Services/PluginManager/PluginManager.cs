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
using MediaPortal.Core.Exceptions;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
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
  public class PluginManager : IPluginManager
  {
    #region Protected fields

    protected IDictionary<string, PluginRuntime> _availablePlugins = new Dictionary<string, PluginRuntime>();

    protected IDictionary<string, PluginBuilderRegistration> _builders =
        new Dictionary<string, PluginBuilderRegistration>();

    protected IDictionary<string, PluginState> _pendingPlugins = new Dictionary<string, PluginState>();

    protected PluginManagerState _state = PluginManagerState.Uninitialized;

    #endregion

    #region Ctor

    public PluginManager()
    {
      CollectionUtils.AddAll(_builders, CreateDefaultBuilderRegistrations());
    }

    #endregion

    #region IPluginManager implementation

    public PluginManagerState State
    {
      get { return _state; }
    }

    public IDictionary<string, PluginRuntime> AvailablePlugins
    {
      get { return _availablePlugins; }
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

    public void Startup()
    {
      ServiceScope.Get<ILogger>().Info("PluginManager: Startup");
      _state = PluginManagerState.Starting;
      SendPluginManagerMessage(PluginManagerMessaging.NotificationType.Startup);
      PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
      ICollection<string> disabledPlugins = settings.UserDisabledPlugins;
      ServiceScope.Get<ILogger>().Debug("PluginManager: Checking dependencies");
      foreach (PluginRuntime plugin in _availablePlugins.Values)
      {
        if (disabledPlugins.Contains(plugin.Metadata.Name))
          plugin.State = PluginState.Disabled;
        else
          TryEnable(plugin, true);
      }
      SendPluginManagerMessage(PluginManagerMessaging.NotificationType.PluginsInitialized);
      _state = PluginManagerState.Running;
      ServiceScope.Get<ILogger>().Debug("PluginManager: Ready");
    }

    public void Shutdown()
    {
      ServiceScope.Get<ILogger>().Info("PluginManager: Shutdown");
      _state = PluginManagerState.ShuttingDown;
      SendPluginManagerMessage(PluginManagerMessaging.NotificationType.Shutdown);
      foreach (PluginRuntime plugin in _availablePlugins.Values)
      {
        if (plugin.StateTracker != null)
          plugin.StateTracker.Shutdown();
      }
    }

    public bool TryStartPlugin(IPluginMetadata plugin, bool activate)
    {
      PluginRuntime pr;
      if (!_availablePlugins.TryGetValue(plugin.Name, out pr))
        pr = AddPlugin(plugin);
      bool result = activate ? TryActivate(pr) : TryEnable(pr, true);
      if (result)
      {
        PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
        settings.RemoveUserDisabledPlugin(plugin.Name);
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
      return result;
    }

    public bool TryStopPlugin(IPluginMetadata plugin)
    {
      PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
      settings.AddUserDisabledPlugin(plugin.Name);
      ServiceScope.Get<ISettingsManager>().Save(settings);

      PluginRuntime pr;
      if (_availablePlugins.TryGetValue(plugin.Name, out pr))
        return TryDisable(pr);
      return true;
    }

    public ICollection<IPluginMetadata> FindConflicts(IPluginMetadata plugin)
    {
      ICollection<IPluginMetadata> result = new List<IPluginMetadata>();
      // Conflicts declared by plugin
      ICollection<string> conflictingPlugins = CollectionUtils.Intersection(plugin.ConflictsWith, _availablePlugins.Keys);
      foreach (string conflictName in conflictingPlugins)
      {
        PluginRuntime conflict = _availablePlugins[conflictName];
        if (conflict.State != PluginState.Disabled)
          // Found a conflict
          result.Add(conflict.Metadata);
      }
      // Conflicts declared by other plugins
      foreach (PluginRuntime pr in _availablePlugins.Values)
      {
        if (pr.State != PluginState.Disabled && pr.Metadata.ConflictsWith.Contains(plugin.Name))
          // Plugin pr conflicts with plugin
          result.Add(pr.Metadata);
      }
      foreach (string dependencyName in plugin.DependsOn)
      {
        PluginRuntime pr;
        if (!_availablePlugins.TryGetValue(dependencyName, out pr))
          throw new PluginMissingDependencyException("Plugin dependency '{0}' is not available", dependencyName);
        CollectionUtils.AddAll(result, FindConflicts(pr.Metadata));
      }
      return result;
    }

    public ICollection<string> FindMissingDependencies(IPluginMetadata plugin)
    {
      ICollection<string> result = new List<string>();
      foreach (string dependencyName in plugin.DependsOn)
      {
        PluginRuntime pr;
        if (!_availablePlugins.TryGetValue(dependencyName, out pr))
          result.Add(dependencyName);
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
        throw new PluginInvalidStateException("Plugin '{0}' neither is enabled nor active, although it has registered items. Something is wrong.", itemRegistration.Metadata.Id);
      // TODO: As of the specification (see PluginState.EndRequest), we have to make the current
      // thread sleep until the stopping procedure is finished or cancelled. This means we have to
      // implement a notification mechanism in the stop request methods to re-awake this thread.
      // By now, we simply let the item request fail (which is not the specified behavior!)
      if (!itemRegistration.StateTrackers.Contains(stateTracker))
        itemRegistration.StateTrackers.Add(stateTracker);
      if (itemRegistration.Item == null)
        itemRegistration.Item = BuildItem(itemRegistration.Metadata, pluginRuntime);
      if (itemRegistration.Item is T)
        return (T) itemRegistration.Item;
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

    internal static KeyValuePair<string, PluginBuilderRegistration> CreateDefaultBuilderRegistration(
        string builderName, IPluginItemBuilder builderInstance)
    {
      KeyValuePair<string, PluginBuilderRegistration> result =
          new KeyValuePair<string, PluginBuilderRegistration>(
              builderName,
              new PluginBuilderRegistration(builderName, builderInstance.GetType().FullName, null));
      result.Value.Builder = builderInstance;
      return result;
    }

    internal static IDictionary<string, PluginBuilderRegistration> CreateDefaultBuilderRegistrations()
    {
      IDictionary<string, PluginBuilderRegistration> result = new Dictionary<string, PluginBuilderRegistration>();
      result.Add(CreateDefaultBuilderRegistration("Instance", new InstanceBuilder()));
      result.Add(CreateDefaultBuilderRegistration("Resource", new ResourceBuilder()));
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
      if (builder.NeedsPluginActive && !TryActivate(plugin))
        throw new PluginInvalidStateException("Plugin '{0}' cannot be activated, although it has registered items. Something is wrong.");
      return builder.BuildItem(metadata, plugin);
    }

    #endregion

    #region Private & protected methods

    protected static void SendPluginManagerMessage(PluginManagerMessaging.NotificationType notificationType)
    {
      // Send Startup Finished Message.
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.Queue);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[PluginManagerMessaging.Notification] = notificationType;
      queue.Send(msg);
    }

    protected static bool IsEnabledOrActive(PluginRuntime pr)
    {
      return pr.State == PluginState.Enabled || pr.State == PluginState.Active;
    }

    protected static bool IsInStoppingProcess(PluginRuntime pr)
    {
      return pr.State == PluginState.EndRequest || pr.State == PluginState.Stopping;
    }

    protected PluginRuntime AddPlugin(IPluginMetadata pm)
    {
      PluginRuntime result = new PluginRuntime(pm);
      _availablePlugins.Add(pm.Name, result);
      return result;
    }

    private static void ContinueOpenEndRequests(
        IEnumerable<KeyValuePair<PluginItemMetadata, ICollection<IPluginItemStateTracker>>>
        endRequestsToClose)
    {
      foreach (KeyValuePair<PluginItemMetadata, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose)
        foreach (IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value)
          stateTracker.Continue(itemStateTrackersToFinish.Key);
    }

    private static void StopOpenEndRequests(
        IEnumerable<KeyValuePair<PluginItemMetadata, ICollection<IPluginItemStateTracker>>>
        endRequestsToClose)
    {
      foreach (KeyValuePair<PluginItemMetadata, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose)
        foreach (IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value)
          stateTracker.Stop(itemStateTrackersToFinish.Key);
    }

    private static bool AllEndRequestsSucceed(IEnumerable<PluginItemRegistration> items,
        out IDictionary<PluginItemMetadata, ICollection<IPluginItemStateTracker>>
        endRequestsToClose)
    {
      endRequestsToClose = new Dictionary<PluginItemMetadata, ICollection<IPluginItemStateTracker>>();
      foreach (PluginItemRegistration itemRegistration in items)
      {
        ICollection<IPluginItemStateTracker> stateTrackersToFinish = new List<IPluginItemStateTracker>();
        endRequestsToClose.Add(itemRegistration.Metadata, stateTrackersToFinish);
        foreach (IPluginItemStateTracker stateTracker in itemRegistration.StateTrackers)
        {
          if (stateTracker.RequestEnd(itemRegistration.Metadata))
            stateTrackersToFinish.Add(stateTracker);
          else
            return false;
        }
      }
      return true;
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
      string pluginName = plugin.Metadata.Name;
      if (_pendingPlugins.ContainsKey(pluginName))
        throw new PluginRecursiveStateChangeException(
          "Plugin '{0}' is already changing its state to '{1}'", pluginName, newState);
      _pendingPlugins.Add(pluginName, newState);
    }

    /// <summary>
    /// Method to finish a state change. This will enable the plugin for other state changes again.
    /// </summary>
    /// <param name="plugin">The plugin for that method <see cref="StateChangeStart"/> was called
    /// before.</param>
    protected void StateChangeFinish(PluginRuntime plugin)
    {
      _pendingPlugins.Remove(plugin.Metadata.Name);
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
        logger.Debug("PluginManager: Trying to enable plugin '{0}'", pluginName);
        if (FindConflicts(plugin.Metadata).Count > 0)
        {
          TryDisable(plugin);
          return false;
        }

        // Handle dependencies
        ICollection<PluginRuntime> pendingChildRegistrations = new List<PluginRuntime>();
        foreach (string parent in plugin.Metadata.DependsOn)
        {
          PluginRuntime parentRuntime;
          if (!_availablePlugins.TryGetValue(parent, out parentRuntime))
          {
            logger.Warn("Plugin '{0}': Dependency '{1}' is not available", pluginName, parent);
            return false;
          }
          if (!TryEnable(parentRuntime, doAutoActivate))
          {
            logger.Warn("Plugin '{0}': Dependency '{1}' cannot be enabled", pluginName, parent);
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
          if (!plugin.Metadata.Builders.Keys.Contains(builderName))
          {
            PluginBuilderRegistration builderRegistration;
            if (!_builders.TryGetValue(builderName, out builderRegistration))
            {
              logger.Warn("Plugin '{0}': Builder '{1}' is not available", pluginName, builderName);
              return false;
            }
            if (builderRegistration.PluginRuntime != null) // If builder is no default builder
              if (!plugin.Metadata.DependsOn.Contains(builderRegistration.PluginRuntime.Metadata.Name))
              {
                logger.Warn(
                    "Plugin '{0}': Builder '{1}' is added to the system by plugin '{2}', but this is not specified in the plugin dependencies",
                    pluginName, builderName, builderRegistration.PluginRuntime.Metadata.Name);
                return false;
              }
          }
        }
        plugin.RegisterItems();
        CollectionUtils.AddAll(_builders, CreateBuilderRegistrations(plugin));
        foreach (PluginRuntime parent in pendingChildRegistrations)
          parent.AddDependentPlugin(plugin);
        plugin.State = PluginState.Enabled;
        logger.Info("PluginManager: Plugin '{0}' enabled.", pluginName);
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
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Debug("PluginManager: Trying to activate plugin '{0}'", pluginName);
      StateChangeStart(plugin, PluginState.Active);
      try
      {
        // Load assemblies of parent plugins
        foreach (string parentName in plugin.Metadata.DependsOn)
        {
          PluginRuntime parent = _availablePlugins[parentName];
          logger.Debug("PluginManager: Processing dependency '{0}' for plugin '{1}'", parentName, pluginName);
          if (!TryActivate(parent))
            return false;
        }
        // Activate plugin
        plugin.LoadAssemblies();
        plugin.State = PluginState.Active;
        if (plugin.Metadata.StateTrackerClassName != null)
        {
          try
          {
            object obj = plugin.InstanciatePluginObject(plugin.Metadata.StateTrackerClassName);
            if (obj == null)
              logger.Warn("PluginManager: Couldn't instantiate plugin state tracker class '{0}' for plugin '{1}'",
                  plugin.Metadata.StateTrackerClassName, plugin.Metadata.Name);
            else if (obj is IPluginStateTracker)
            {
              plugin.StateTracker = obj as IPluginStateTracker;
              plugin.StateTracker.Activated();
            }
            else
              logger.Warn("PluginManager: Plugin state tracker class '{0}' of plugin '{1}' doesn't implement interface {2}",
                  plugin.Metadata.StateTrackerClassName, plugin.Metadata.Name, typeof(IPluginStateTracker).Name);
          }
          catch (Exception e)
          {
            logger.Error("PluginManager: Error instantiating plugin state tracker class '{0}' for plugin '{1}'",
                e, plugin.Metadata.StateTrackerClassName, plugin.Metadata.Name);
          }
        }
        logger.Info("PluginManager: Plugin '{0}' activated.", pluginName);
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
        // Handle dependencies
        ICollection<PluginRuntime> dependencies = plugin.DependentPlugins;
        if (dependencies != null)
          foreach (PluginRuntime child in dependencies)
            if (!TryDisable(child))
              return false;
        if (plugin.State == PluginState.Active)
        {
          plugin.State = PluginState.EndRequest;
          if (plugin.StateTracker != null)
            if (!plugin.StateTracker.RequestEnd())
            {
              plugin.State = PluginState.Active;
              return false;
            }
          IDictionary<PluginItemMetadata, ICollection<IPluginItemStateTracker>> endRequestsToClose;
          if (AllEndRequestsSucceed(plugin.ItemRegistrations.Values, out endRequestsToClose))
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
            if (plugin.StateTracker != null)
              plugin.StateTracker.Continue();
            ContinueOpenEndRequests(endRequestsToClose);
            return false;
          }
        }
        plugin.State = PluginState.Disabled;
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
        try
        {
          IPluginMetadata pm = new PluginDirectoryDescriptor(pluginDirectoryPath);
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
