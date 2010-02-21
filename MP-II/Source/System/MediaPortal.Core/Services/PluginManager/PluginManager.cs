#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections;
using System.Linq;

namespace MediaPortal.Core.Services.PluginManager
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
  /// 
  /// Implementation hints (multithreading strategy):
  /// - C# locks are held shortly only to protect internal data structures against data corruption
  /// - Protection of plugin states against concurrent modification during state changes or during item request/revocation is
  ///   done by the use of explicit, non-blocking reader/writer locks implemented in PluginRuntime class
  /// - It is possible to lock a plugin state dependency (reader lock) or a state lock for write (writer lock). Also a reader
  ///   lock can be turned into a writer lock.
  /// - With this implementation, we shouldn't get too many problems with concurrent state modifications; the only thing to
  ///   care about is the startup and shutdown phase, where potentially multiple concurrent services try to enable plugins.
  public class PluginManager : IPluginManager, IStatus
  {
    #region Protected fields

    protected object _syncObj = new object();

    protected IDictionary<Guid, PluginRuntime> _availablePlugins = new Dictionary<Guid, PluginRuntime>();

    protected IDictionary<string, PluginBuilderRegistration> _builders =
        new Dictionary<string, PluginBuilderRegistration>();

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
      lock (_syncObj)
      {
        ServiceScope.Get<ILogger>().Info("PluginManager: Initialize");
        _state = PluginManagerState.Initializing;
        ServiceScope.Get<ILogger>().Debug("PluginManager: Loading plugins");
        IDictionary<string, IPluginMetadata> loadedPlugins = LoadPluginsData();
        foreach (IPluginMetadata pm in loadedPlugins.Values)
          AddPlugin(pm);
        ServiceScope.Get<ILogger>().Debug("PluginManager: Initialized");
      }
    }

    public void Startup(bool maintenanceMode)
    {
      lock (_syncObj)
      {
        if (maintenanceMode)
          ServiceScope.Get<ILogger>().Info("PluginManager: Startup in maintenance mode");
        else
          ServiceScope.Get<ILogger>().Info("PluginManager: Startup");
        _maintenanceMode = maintenanceMode;
        _state = PluginManagerState.Starting;
      }
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.MessageType.Startup);
      ServiceScope.Get<ILogger>().Debug("PluginManager: Checking dependencies");
      ICollection<Guid> disabledPlugins;
      ICollection<PluginRuntime> availablePlugins;
      lock (_syncObj)
      {
        availablePlugins = new List<PluginRuntime>(_availablePlugins.Values);
        PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
        disabledPlugins = settings.UserDisabledPlugins;
      }
      foreach (PluginRuntime plugin in availablePlugins)
      {
        if (disabledPlugins.Contains(plugin.Metadata.PluginId))
          plugin.State = PluginState.Disabled;
        else
          TryEnable(plugin, !_maintenanceMode);
      }
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.MessageType.PluginsInitialized);
      lock (_syncObj)
        _state = PluginManagerState.Running;
      if (maintenanceMode)
        ServiceScope.Get<ILogger>().Debug("PluginManager: Running in maintenance mode");
      else
        ServiceScope.Get<ILogger>().Debug("PluginManager: Ready");
      ServiceScope.LoadServicesFromPlugins();
    }

    public void Shutdown()
    {
      ServiceScope.RemoveAndDisposePluginServices();
      ServiceScope.Get<ILogger>().Info("PluginManager: Shutdown");
      ICollection<PluginRuntime> availablePlugins;
      lock (_syncObj)
      {
        _state = PluginManagerState.ShuttingDown;
        availablePlugins = new List<PluginRuntime>(_availablePlugins.Values);
      }
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.MessageType.Shutdown);
      foreach (PluginRuntime plugin in availablePlugins)
      {
        IPluginStateTracker stateTracker = plugin.StateTracker; // Local variable to avoid necessity to lock
        if (stateTracker != null)
          try
          {
            stateTracker.Shutdown();
          }
          catch (Exception e)
          {
            ServiceScope.Get<ILogger>().Warn("Error shutting plugin state tracker '{0}' down in plugin '{1}' (id '{2})", e,
                stateTracker, plugin.Metadata.Name, plugin.Metadata.PluginId);
          }
      }
    }

    public PluginRuntime AddPlugin(IPluginMetadata pluginMetadata)
    {
      lock (_syncObj)
      {
        PluginRuntime result = new PluginRuntime(pluginMetadata, _syncObj);
        _availablePlugins.Add(pluginMetadata.PluginId, result);
        return result;
      }
    }

    public bool TryStartPlugin(Guid pluginId, bool activate)
    {
      PluginRuntime plugin;
      lock (_syncObj)
        if (!_availablePlugins.TryGetValue(pluginId, out plugin))
          throw new ArgumentException(string.Format("Plugin with id '{0}' not found", pluginId));
      bool result = activate ? TryActivate(plugin) : TryEnable(plugin, true);
      if (result)
        lock (_syncObj)
        {
          PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
          settings.RemoveUserDisabledPlugin(pluginId);
          ServiceScope.Get<ISettingsManager>().Save(settings);
        }
      return result;
    }

    public bool TryStopPlugin(Guid pluginId)
    {
      lock (_syncObj)
      {
        PluginManagerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PluginManagerSettings>();
        settings.AddUserDisabledPlugin(pluginId);
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }

      PluginRuntime plugin;
      lock (_syncObj)
        if (!_availablePlugins.TryGetValue(pluginId, out plugin))
          return true;
      return TryDisable(plugin);
    }

    public void RegisterSystemPluginItemBuilder(string builderName, IPluginItemBuilder builderInstance)
    {
      lock (_syncObj)
        _builders.Add(CreateSystemBuilderRegistration(builderName, builderInstance));
    }

    public ICollection<Guid> FindConflicts(IPluginMetadata plugin)
    {
      ICollection<Guid> result = new HashSet<Guid>();
      lock (_syncObj)
      {
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
      }
      return result;
    }

    public ICollection<Guid> FindMissingDependencies(IPluginMetadata plugin)
    {
      ICollection<Guid> result = new HashSet<Guid>();
      lock (_syncObj)
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
      lock (_syncObj)
        return PluginRuntime.GetAvailableChildLocations(location);
    }

    public T RequestPluginItem<T>(string location, string id, IPluginItemStateTracker stateTracker) where T : class
    {
      return (T) RequestPluginItem(location, id, typeof(T), stateTracker);
    }

    public object RequestPluginItem(string location, string id, Type type, IPluginItemStateTracker stateTracker)
    {
      PluginItemRegistration itemRegistration = PluginRuntime.GetItemRegistration(location, id);
      return itemRegistration == null ? null : RequestItem(itemRegistration, type, stateTracker);
    }

    public ICollection<T> RequestAllPluginItems<T>(string location, IPluginItemStateTracker stateTracker) where T : class
    {
      ICollection<T> result = new List<T>();
      foreach (PluginItemRegistration itemRegistration in PluginRuntime.GetItemRegistrations(location))
      {
        T item = (T) RequestItem(itemRegistration, typeof(T), stateTracker);
        if (item != null)
          result.Add(item);
      }
      return result;
    }

    public ICollection RequestAllPluginItems(string location, Type type, IPluginItemStateTracker stateTracker)
    {
      IList result = new ArrayList();
      foreach (PluginItemRegistration itemRegistration in PluginRuntime.GetItemRegistrations(location))
      {
        object item = RequestItem(itemRegistration, type, stateTracker);
        if (item != null)
          result.Add(item);
      }
      return result;
    }

    public void RevokePluginItem(string location, string id, IPluginItemStateTracker stateTracker)
    {
      PluginItemRegistration itemRegistration;
      lock (_syncObj) // Lock necessary (see docs of PluginRuntime.GetItemRegistration)
        itemRegistration = PluginRuntime.GetItemRegistration(location, id);
      if (itemRegistration != null)
        RevokeItemUsage(itemRegistration, stateTracker);
    }

    public void RevokeAllPluginItems(string location, IPluginItemStateTracker stateTracker)
    {
      ICollection<PluginItemRegistration> registrations;
      lock (_syncObj) // Lock necessary (see docs of PluginRuntime.GetItemRegistrations)
        registrations = PluginRuntime.GetItemRegistrations(location);
      foreach (PluginItemRegistration itemRegistration in registrations)
        RevokeItemUsage(itemRegistration, stateTracker);
    }

    public void AddItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      lock (_syncObj) // Lock necessary (see docs of PluginRuntime.AddItemRegistrationChangeListener)
        PluginRuntime.AddItemRegistrationChangeListener(location, listener);
    }

    public void RemoveItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
      lock (_syncObj) // Lock necessary (see docs of PluginRuntime.RemoveItemRegistrationChangeListener)
        PluginRuntime.RemoveItemRegistrationChangeListener(location, listener);
    }

    #endregion

    #region Item&Builder management

    internal object RequestItem(PluginItemRegistration itemRegistration, Type type, IPluginItemStateTracker stateTracker)
    {
      PluginRuntime plugin = itemRegistration.Metadata.PluginRuntime;
      // We don't change the plugin's state to Pending because in some code paths, we need to call TryActivate which itself
      // will check the state
      lock (_syncObj)
      {
        if (!IsEnabledOrActive(plugin))
          throw new PluginInvalidStateException("Plugin '{0}' (id '{1}') neither is enabled nor active; cannot request items in this state",
              plugin.Metadata.Name, plugin.Metadata.PluginId);
        // Hint: As of the specification (see PluginState.EndRequest), we would have to make the current
        // thread sleep until a possible ongoing stopping procedure is finished or cancelled. This means we have to
        // implement a notification mechanism in the stop request methods to re-awake this thread.
        // By now, we simply let the item request fail (which is not exactly the specified behavior!)
      }
      try
      {
        PluginItemMetadata itemMetadata = itemRegistration.Metadata;
        IPluginItemBuilder builder = GetOrCreateBuilder(itemMetadata.BuilderName);
        bool mustBuild = false;
        lock (_syncObj)
          if (itemRegistration.Item == null)
            mustBuild = true;
        // Escape lock because method TryActivate() is called
        bool mustBeActive = builder.NeedsPluginActive(itemMetadata, plugin);
        if (mustBuild && mustBeActive && !TryActivate(plugin))
          throw new PluginInvalidStateException(string.Format(
              "Plugin '{0}' (id '{1}') cannot be activated", plugin.Metadata.Name, plugin.Metadata.PluginId));
        if (mustBeActive)
          LockPluginStateDependency(plugin, PluginState.Active);
        else
          LockPluginStateDependency(plugin, PluginState.Active, PluginState.Enabled);
        try
        {
          object item = builder.BuildItem(itemMetadata, plugin);
          if (item == null)
            return null;
          object releaseItem = null; // Fail safety - if we built the item twice by accident, release it at the end
          try
          {
            lock (_syncObj)
            {
              if (itemRegistration.Item == null)
                itemRegistration.Item = item;
              else
                releaseItem = item;
              object result = itemRegistration.Item;
              if (result != null && type.IsAssignableFrom(result.GetType()))
              {
                if (!itemRegistration.StateTrackers.Contains(stateTracker))
                  itemRegistration.StateTrackers.Add(stateTracker);
                return result;
              }
            }
          }
          finally
          {
            // In case we built the item concurrently twice, release item
            if (releaseItem != null)
              builder.RevokeItem(releaseItem, itemMetadata, plugin);
          }
        }
        finally
        {
          UnlockPluginStateDependency(plugin);
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("PluginManager: Error building plugin item '{0}' at location '{1}'",
            e, itemRegistration.Metadata.Id, itemRegistration.Metadata.RegistrationLocation);
      }
      // Requested item isn't of type T - revoke usage again
      if (itemRegistration.Item != null)
        RevokeItemUsage(itemRegistration, null);
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
    internal void RevokeItemUsage(PluginItemRegistration itemRegistration, IPluginItemStateTracker stateTracker)
    {
      PluginItemMetadata metadata = itemRegistration.Metadata;
      PluginRuntime plugin = metadata.PluginRuntime;
      LockPluginStateDependency(plugin, PluginState.Active, PluginState.Enabled);
      try
      {
        lock (_syncObj)
        {
          if (stateTracker != null)
            itemRegistration.StateTrackers.Remove(stateTracker);
          if (itemRegistration.StateTrackers.Count > 0)
            return;
        }
        // No more state trackers present, revoke item
        IPluginItemBuilder builder = GetOrCreateBuilder(metadata.BuilderName);
        try
        {
          builder.RevokeItem(itemRegistration.Item, metadata, plugin);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error("Error revoking usage of item '{0}' at location '{1}' (item builder is '{2}')", e,
              itemRegistration.Metadata.Id, itemRegistration.Metadata.RegistrationLocation, metadata.BuilderName);
        }
        itemRegistration.Item = null;
        // If we wanted to automatically unload plugins whose items are not accessed any more, this
        // should be done here
      }
      finally
      {
        UnlockPluginStateDependency(plugin);
      }
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
          new KeyValuePair<string, PluginBuilderRegistration>(builderName,
              new PluginBuilderRegistration(builderName, builderInstance.GetType().FullName, null));
      result.Value.Builder = builderInstance;
      return result;
    }

    internal static IDictionary<string, IPluginItemBuilder> GetDefaultBuilders()
    {
      IDictionary<string, IPluginItemBuilder> result = new Dictionary<string, IPluginItemBuilder>
        {
            {"Instance", new InstanceBuilder()},
            {"Resource", new ResourceBuilder()},
            {"Service", new ServiceBuilder()},
        };
      return result;
    }

    internal IPluginItemBuilder GetOrCreateBuilder(string builderName)
    {
      PluginBuilderRegistration builderRegistration;
      lock (_syncObj)
      {
        if (!_builders.TryGetValue(builderName, out builderRegistration))
          throw new FatalException("Builder '{0}' cannot be found. Something is wrong.", builderName);
        if (builderRegistration.IsInstantiated)
          return builderRegistration.Builder;
      }

      PluginRuntime builderPlugin = builderRegistration.PluginRuntime;
      if (!TryActivate(builderPlugin))
        throw new PluginInvalidStateException("Cannot activate plugin providing builder '{0}', which is necessary to build item");
      LockPluginStateDependency(builderPlugin, PluginState.Active);
      try
      {
        object obj = null;
        try
        {
          obj = builderPlugin.InstantiatePluginObject(builderRegistration.BuilderClassName);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error("Error instanciating plugin item builder '{0}' (id '{1}')", e,
            builderPlugin.Metadata.Name, builderPlugin.Metadata.PluginId);
        }
        if (obj == null)
          throw new PluginInternalException("Builder class '{0}' could not be instantiated",
              builderRegistration.BuilderClassName);
        builderRegistration.Builder = obj as IPluginItemBuilder;
        if (builderRegistration.Builder != null)
          return builderRegistration.Builder;
        builderPlugin.RevokePluginObject(builderRegistration.BuilderClassName);
        throw new PluginInternalException("Builder class '{0}' does not implement the plugin item builder interface '{1}'",
            builderRegistration.BuilderClassName, typeof(IPluginItemBuilder).Name);
      }
      finally
      {
        UnlockPluginStateDependency(builderPlugin);
      }
    }

    #endregion

    #region Private & protected methods

    protected static bool IsEnabledOrActive(PluginRuntime plugin)
    {
      return plugin.State == PluginState.Enabled || plugin.State == PluginState.Active;
    }

    protected static bool IsInStoppingProcess(PluginRuntime plugin)
    {
      return plugin.State == PluginState.EndRequest || plugin.State == PluginState.Stopping;
    }

    private static bool StateTrackerRequestEnd(IPluginItemStateTracker stateTracker, PluginItemRegistration itemRegistration)
    {
      try
      {
        return stateTracker.RequestEnd(itemRegistration);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error calling method 'RequestEnd' at plugin item state tracker '{0}'", e, stateTracker);
        return true;
      }
    }

    private static void StateTrackerContinue(IPluginItemStateTracker stateTracker, PluginItemRegistration itemRegistration)
    {
      try
      {
        stateTracker.Continue(itemRegistration);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error calling method 'Continue' at plugin item state tracker '{0}'", e, stateTracker);
      }
    }

    private static void StateTrackerStop(IPluginItemStateTracker stateTracker, PluginItemRegistration itemRegistration)
    {
      try
      {
        stateTracker.Stop(itemRegistration);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error calling method 'Stop' at plugin item state tracker '{0}'", e, stateTracker);
      }
    }

    private static void ContinueOpenEndRequests(
        IEnumerable<KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>> endRequestsToClose)
    {
      foreach (KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>
          itemStateTrackersToFinish in endRequestsToClose)
        foreach (IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value)
          StateTrackerContinue(stateTracker, itemStateTrackersToFinish.Key);
    }

    private static void StopOpenEndRequests(
        IEnumerable<KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>> endRequestsToClose)
    {
      foreach (KeyValuePair<PluginItemRegistration, ICollection<IPluginItemStateTracker>>
        itemStateTrackersToFinish in endRequestsToClose)
        foreach (IPluginItemStateTracker stateTracker in itemStateTrackersToFinish.Value)
          StateTrackerStop(stateTracker, itemStateTrackersToFinish.Key);
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
          if (StateTrackerRequestEnd(stateTracker, itemRegistration))
            succeededStataTrackers.Add(stateTracker);
          else
            failedStateTrackers.Add(stateTracker);
        }
      }
    }

    /// <summary>
    /// Method to lock a plugin to its current state until <see cref="UnlockPluginStateDependency"/> gets called. This avoids state
    /// changes for a plugin, for example while some progress is assuming the current plugin state.
    /// State changes could happen if a component of the outside tries to trigger a state change while
    /// it is called back from a state changing method.
    /// </summary>
    /// <param name="plugin">The plugin to be locked.</param>
    /// <param name="statesToLock">The plugin must be in one of those states to be locked.</param>
    protected void LockPluginStateDependency(PluginRuntime plugin, params PluginState[] statesToLock)
    {
      lock (_syncObj)
      {
        if (statesToLock.Length > 0)
        {
          ICollection<PluginState> states = new List<PluginState>(statesToLock);
          if (!states.Contains(plugin.State))
            throw new PluginInvalidStateException("Plugin '{0}' (id '{1}') is not in one of the desired states ('{2}')",
                plugin.Metadata.Name, plugin.Metadata.PluginId, StringUtils.Join(", ", statesToLock));
        }
        plugin.LockForStateDependency();
      }
    }

    protected void UnlockPluginStateDependency(PluginRuntime plugin)
    {
      plugin.UnlockStateDependency();
    }

    protected void ChangeReadLockToWriteLock(PluginRuntime plugin)
    {
      plugin.ChangeReadLockToWriteLock();
    }

    /// <summary>
    /// Method to lock a plugin to its current state until <see cref="UnlockPluginStateForWrite"/> gets called. This sets
    /// a writer lock at the plugin's state which avoids other writer or reader locks.
    /// State changes could happen if a component of the outside tries to trigger a state change while
    /// it is called back from a state changing method.
    /// </summary>
    /// <param name="plugin">The plugin to be locked.</param>
    protected void LockPluginStateForWrite(PluginRuntime plugin)
    {
      plugin.LockForStateWrite();
    }

    /// <summary>
    /// Method to unlock a plugin's writer lock, which was set by
    /// <see cref="LockPluginStateForWrite(MediaPortal.Core.PluginManager.PluginRuntime)"/>.
    /// </summary>
    /// <param name="plugin">The plugin for that method <see cref="LockPluginStateForWrite(MediaPortal.Core.PluginManager.PluginRuntime)"/>
    /// was called before.</param>
    protected void UnlockPluginStateForWrite(PluginRuntime plugin)
    {
      plugin.UnlockStateForWrite();
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
      bool lockedForWrite = false;
      LockPluginStateDependency(plugin); // First lock for read
      ICollection<PluginRuntime> lockedPluginStateDependencies = new List<PluginRuntime> {plugin};
      try
      {
        // Plugin is already available - check its current state
        if (IsEnabledOrActive(plugin))
          return true;
        if (IsInStoppingProcess(plugin))
          return false;
        ILogger logger = ServiceScope.Get<ILogger>();
        string pluginName = plugin.Metadata.Name;
        Guid pluginId = plugin.Metadata.PluginId;
        logger.Debug("PluginManager: Trying to enable plugin '{0}' (id '{1}')", pluginName, pluginId);
        if (FindConflicts(plugin.Metadata).Count > 0)
        {
          logger.Info("PluginManager: Plugin '{0}' (id '{1}') cannot be enabled - there are plugin conflicts", pluginName, pluginId);
          return false;
        }

        // Check if builder dependencies are explicitly named
        lock (_syncObj)
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
              logger.Warn("Plugin '{0}' (id '{1}'): Builder '{2}' is not available - plugin won't be enabled",
                pluginName, pluginId, builderName);
              return false;
            }
            if (builderRegistration.PluginRuntime == null)
              // Builder is a default builder
              continue;
            if (!plugin.Metadata.DependsOn.Contains(builderRegistration.PluginRuntime.Metadata.PluginId))
            {
              logger.Error(
                  "Plugin '{0}' (id '{1}'): Builder '{2}' (implemented by plugin '{3}') is used, but this plugin dependency is not explicitly specified - plugin won't be enabled",
                  pluginName, pluginId, builderName, builderRegistration.PluginRuntime.Metadata.Name);
              return false;
            }
          }

        // Handle dependencies
        ICollection<PluginRuntime> pendingChildRegistrations = new List<PluginRuntime>();
        foreach (Guid parentId in plugin.Metadata.DependsOn)
        {
          PluginRuntime parentPlugin;
          lock (_syncObj)
            if (!_availablePlugins.TryGetValue(parentId, out parentPlugin))
            {
              logger.Warn("Plugin '{0}' (id '{1}'): Dependency '{2}' is not available", pluginName, pluginId, parentId);
              return false;
            }
          if (!TryEnable(parentPlugin, doAutoActivate))
          {
            logger.Warn("Plugin '{0}' (id '{1}'): Dependency '{2}' cannot be enabled", pluginName, pluginId, parentId);
            return false;
          }
          LockPluginStateDependency(parentPlugin, PluginState.Enabled, PluginState.Active);
          lockedPluginStateDependencies.Add(parentPlugin);
          pendingChildRegistrations.Add(parentPlugin); // Remember parent -> have to register return value as dependent plugin later
        }

        // All checks passed and preconditions met, enable plugin
        ChangeReadLockToWriteLock(plugin);
        lockedForWrite = true;
        lockedPluginStateDependencies.Remove(plugin);
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
        lock (_syncObj)
        {
          CollectionUtils.AddAll(_builders, CreateBuilderRegistrations(plugin));
          foreach (PluginRuntime parent in pendingChildRegistrations)
            parent.AddDependentPlugin(plugin);
          plugin.State = PluginState.Enabled;
        }
        logger.Info("PluginManager: Plugin '{0}' (id '{1}') enabled.", pluginName, pluginId);
      }
      finally
      {
        if (lockedForWrite)
          UnlockPluginStateForWrite(plugin);
        foreach (PluginRuntime lockedPlugin in lockedPluginStateDependencies)
          UnlockPluginStateDependency(lockedPlugin);
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
      bool lockedForWrite = false;
      LockPluginStateDependency(plugin); // First lock for read
      ICollection<PluginRuntime> lockedPluginStateDependencies = new List<PluginRuntime> {plugin};
      try
      {
        if (plugin.State == PluginState.Active)
          return true;
        string pluginName = plugin.Metadata.Name;
        Guid pluginId = plugin.Metadata.PluginId;
        ILogger logger = ServiceScope.Get<ILogger>();
        logger.Debug("PluginManager: Trying to activate plugin '{0}' (id '{1}')", pluginName, pluginId);
        // Activate parent plugins - Load their assemblies etc.
        IDictionary<Guid, PluginRuntime> availablePlugins;
        lock (_syncObj)
          availablePlugins = new Dictionary<Guid, PluginRuntime>(_availablePlugins);
        foreach (Guid parentId in plugin.Metadata.DependsOn)
        {
          PluginRuntime parentPlugin = availablePlugins[parentId];
          logger.Debug("PluginManager: Checking activation of plugin dependency '{0}' for plugin '{1}'",
              parentId, pluginName);
          if (!TryActivate(parentPlugin))
          {
            logger.Debug("PluginManager: Dependent plugin '{0}' could not be activated. Activation of plugin '{1}' was not successful.",
                parentId, pluginName);
            return false;
          }
          LockPluginStateDependency(parentPlugin, PluginState.Active);
          lockedPluginStateDependencies.Add(parentPlugin);
        }
        // All checks passed and preconditions met, activate plugin
        ChangeReadLockToWriteLock(plugin);
        lockedForWrite = true;
        lockedPluginStateDependencies.Remove(plugin);
        lock (_syncObj)
        {
          plugin.LoadAssemblies();
          plugin.State = PluginState.Active;
        }
        if (plugin.Metadata.StateTrackerClassName != null && !_maintenanceMode)
        {
          try
          {
            object obj = plugin.InstantiatePluginObject(plugin.Metadata.StateTrackerClassName);
            IPluginStateTracker stateTracker = obj as IPluginStateTracker;
            if (obj == null)
              logger.Error("PluginManager: Couldn't instantiate plugin state tracker class '{0}' for plugin '{1}'",
                  plugin.Metadata.StateTrackerClassName, pluginName);
            else if (stateTracker != null)
            {
              plugin.StateTracker = stateTracker;
              try
              {
                stateTracker.Activated(plugin);
              }
              catch (Exception e)
              {
                ServiceScope.Get<ILogger>().Warn("Error activating plugin state tracker '{0}' in plugin '{1}' (id '{2})", e,
                    stateTracker, plugin.Metadata.Name, plugin.Metadata.PluginId);
              }
            }
            else
            {
              logger.Error("PluginManager: Plugin state tracker class '{0}' of plugin '{1}' doesn't implement interface {3}",
                  plugin.Metadata.StateTrackerClassName, pluginName, typeof(IPluginStateTracker).Name);
              plugin.RevokePluginObject(plugin.Metadata.StateTrackerClassName);
            }
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
        if (lockedForWrite)
          UnlockPluginStateForWrite(plugin);
        foreach (PluginRuntime lockedPlugin in lockedPluginStateDependencies)
          UnlockPluginStateDependency(lockedPlugin);
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
      bool lockedForWrite = false;
      LockPluginStateDependency(plugin); // First lock for read
      ICollection<PluginRuntime> lockedPluginStateDependencies = new List<PluginRuntime> {plugin};
      try
      {
        if (plugin.State == PluginState.Disabled)
          return true;
        if (IsInStoppingProcess(plugin))
          throw new PluginInvalidStateException("Plugin is in stopping process");
        ILogger logger = ServiceScope.Get<ILogger>();
        string pluginName = plugin.Metadata.Name;
        Guid pluginId = plugin.Metadata.PluginId;
        logger.Debug("PluginManager: Trying to disable plugin '{0}' (id '{1}')", pluginName, pluginId);
        // Handle dependencies
        ICollection<PluginRuntime> dependencies;
        lock (_syncObj)
          dependencies = plugin.DependentPlugins;
        if (dependencies != null)
          foreach (PluginRuntime childPlugin in dependencies)
          {
            if (!TryDisable(childPlugin))
            {
              logger.Debug("PluginManager: Cannot disable plugin '{0}' because dependent plugin '{1}' cannot be disabled",
                  pluginName, childPlugin.Metadata.PluginId);
              return false;
            }
            LockPluginStateDependency(childPlugin, PluginState.Disabled);
            lockedPluginStateDependencies.Add(childPlugin);
          }
        // All checks passed and preconditions met, change plugin state
        ChangeReadLockToWriteLock(plugin);
        lockedForWrite = true;
        lockedPluginStateDependencies.Remove(plugin);
        if (plugin.State == PluginState.Active)
        {
          plugin.State = PluginState.EndRequest;
          IPluginStateTracker stateTracker = plugin.StateTracker;
          if (stateTracker != null)
            if (!stateTracker.RequestEnd())
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
            if (stateTracker != null)
            {
              try
              {
                stateTracker.Stop();
              }
              catch (Exception e)
              {
                ServiceScope.Get<ILogger>().Warn("Error stopping plugin state tracker '{0}' in plugin '{1}' (id '{2})", e,
                    stateTracker, plugin.Metadata.Name, plugin.Metadata.PluginId);
              }
              plugin.StateTracker = null;
              plugin.RevokePluginObject(stateTracker.GetType().FullName);
            }
            StopOpenEndRequests(endRequestsToClose);

            plugin.UnregisterItems();
            foreach (string builderName in plugin.Metadata.Builders.Keys)
            {
              object builder = _builders[builderName];
              plugin.RevokePluginObject(builder.GetType().FullName);
              lock (_syncObj)
                _builders.Remove(builderName);
            }
          }
          else
          {
            logger.Debug("PluginManager: Cannot disable plugin '{0}', because it is still in use by '{1}'",
                pluginName, failedStateTrackers.Select(failedStateTracker => failedStateTracker.UsageDescription));
            if (stateTracker != null)
              stateTracker.Continue();
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
        if (lockedForWrite)
          UnlockPluginStateForWrite(plugin);
        foreach (PluginRuntime lockedPlugin in lockedPluginStateDependencies)
          UnlockPluginStateDependency(lockedPlugin);
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
      IList<string> result = new List<string> {"=== PlugInManager"};
      foreach (PluginRuntime plugin in _availablePlugins.Values)
        result.Add(string.Format("  Plugin '{0}': {1}", plugin.Metadata.Name, plugin.State));
      return result;
    }

    #endregion
  }
}
