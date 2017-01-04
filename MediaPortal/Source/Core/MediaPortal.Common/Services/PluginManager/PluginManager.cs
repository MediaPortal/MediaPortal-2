#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.IO;
using MediaPortal.Attributes;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Services.PluginManager.Builders;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities;
using System.Collections;
using System.Linq;

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

    protected object _syncObj = new object();

    protected IDictionary<string, CoreAPIAttribute> _coreComponents = new Dictionary<string, CoreAPIAttribute>();
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

    public IDictionary<string, CoreAPIAttribute> CoreComponents
    {
      get { return _coreComponents; }
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
        ServiceRegistration.Get<ILogger>().Info("PluginManager: Initialize");
        _state = PluginManagerState.Initializing;
        ServiceRegistration.Get<ILogger>().Info("PluginManager: Reflecting Core API");
        ReflectCoreAPI();
        ServiceRegistration.Get<ILogger>().Debug("PluginManager: Loading plugins");
        IDictionary<Guid, IPluginMetadata> loadedPlugins = LoadPluginsData();
        foreach (IPluginMetadata pm in loadedPlugins.Values)
          AddPlugin(pm);
        ServiceRegistration.Get<ILogger>().Debug("PluginManager: Initialized");
      }
    }

    public void Startup(bool maintenanceMode)
    {
      lock (_syncObj)
      {
        ServiceRegistration.Get<ILogger>().Info(
            maintenanceMode ? "PluginManager: Startup in maintenance mode" : "PluginManager: Startup");
        _maintenanceMode = maintenanceMode;
        _state = PluginManagerState.Starting;
      }
      PluginManagerMessaging.SendPluginManagerMessage(PluginManagerMessaging.MessageType.Startup);
      ServiceRegistration.Get<ILogger>().Debug("PluginManager: Checking dependencies");
      ICollection<Guid> disabledPlugins;
      ICollection<PluginRuntime> availablePlugins;
      lock (_syncObj)
      {
        availablePlugins = new List<PluginRuntime>(_availablePlugins.Values);
        PluginManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<PluginManagerSettings>();
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
      ServiceRegistration.Get<ILogger>().Debug(
          maintenanceMode ? "PluginManager: Running in maintenance mode" : "PluginManager: Ready");
      ServiceRegistration.LoadServicesFromPlugins();
    }

    public void Shutdown()
    {
      ServiceRegistration.RemoveAndDisposePluginServices();
      ServiceRegistration.Get<ILogger>().Info("PluginManager: Shutdown");
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
            ServiceRegistration.Get<ILogger>().Warn("Error shutting plugin state tracker '{0}' down in plugin '{1}' (id '{2})", e,
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
          PluginManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<PluginManagerSettings>();
          settings.RemoveUserDisabledPlugin(pluginId);
          ServiceRegistration.Get<ISettingsManager>().Save(settings);
        }
      return result;
    }

    public bool TryStopPlugin(Guid pluginId)
    {
      lock (_syncObj)
      {
        PluginManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<PluginManagerSettings>();
        settings.AddUserDisabledPlugin(pluginId);
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
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
      return FindConflicts(plugin, new HashSet<Guid>());
    }

    /// <summary>
    /// Conflicts are searched recursive, but plugins might be referenced multiple times in the hierarchy.
    /// So in order to speed up this process and prevent a StackOverflowException we pass a list of already checked plugin Ids.
    /// </summary>
    /// <param name="plugin"></param>
    /// <param name="alreadyCheckedPlugins"></param>
    /// <returns></returns>
    protected ICollection<Guid> FindConflicts(IPluginMetadata plugin, HashSet<Guid> alreadyCheckedPlugins)
    {
      ICollection<Guid> result = new HashSet<Guid>();
      lock (_syncObj)
      {
        if (alreadyCheckedPlugins.Contains(plugin.PluginId))
          return result;
        else
          alreadyCheckedPlugins.Add(plugin.PluginId);

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
        foreach (PluginDependencyInfo dependencyInfo in plugin.DependsOn)
        {
          if (dependencyInfo.IsCoreDependency)
          {
            CoreAPIAttribute api;
            if (!_coreComponents.TryGetValue(dependencyInfo.CoreDependencyName, out api))
              throw new PluginMissingDependencyException("Plugin dependency '{0}' is not available", dependencyInfo.CoreDependencyName);
            if (api.MinCompatibleAPI > dependencyInfo.CompatibleAPI || api.CurrentAPI < dependencyInfo.CompatibleAPI)
              throw new PluginIncompatibleException("Dependency '{0}' requires API level ({1}) and available is [min compatible ({2}) -> ({3}) current]", dependencyInfo.CoreDependencyName, dependencyInfo.CompatibleAPI, api.MinCompatibleAPI, api.CurrentAPI);
          }
          else
          {
            PluginRuntime pr;
            if (!_availablePlugins.TryGetValue(dependencyInfo.PluginId, out pr))
              throw new PluginMissingDependencyException("Plugin dependency '{0}' is not available", dependencyInfo.PluginId);
            if (pr.Metadata.MinCompatibleAPI > dependencyInfo.CompatibleAPI || pr.Metadata.CurrentAPI < dependencyInfo.CompatibleAPI)
              throw new PluginIncompatibleException("Dependency '{0}' requires API level ({1}) and available is [min compatible ({2}) -> ({3}) current]", pr.Metadata.Name, dependencyInfo.CompatibleAPI, pr.Metadata.MinCompatibleAPI, pr.Metadata.CurrentAPI);
            CollectionUtils.AddAll(result, FindConflicts(pr.Metadata, alreadyCheckedPlugins));
          }
        }
      }
      return result;
    }

    public ICollection<Guid> FindMissingDependencies(IPluginMetadata plugin)
    {
      ICollection<Guid> result = new HashSet<Guid>();
      lock (_syncObj)
        foreach (PluginDependencyInfo dependencyInfo in plugin.DependsOn)
        {
          if (!dependencyInfo.IsCoreDependency)
          {
            PluginRuntime pr;
            if (!_availablePlugins.TryGetValue(dependencyInfo.PluginId, out pr))
              result.Add(dependencyInfo.PluginId);
            CollectionUtils.AddAll(result, FindMissingDependencies(pr.Metadata));
          }
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
      return PluginRuntime.GetItemRegistrations(location).Select(registration => registration.Metadata).ToList();
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
      return PluginRuntime.GetItemRegistrations(location).Select(
          itemRegistration =>
            {
              try
              {
                return (T) RequestItem(itemRegistration, typeof(T), stateTracker);
              }
              catch (PluginInvalidStateException e)
              {
                ServiceRegistration.Get<ILogger>().Warn("Cannot request {0}", e, itemRegistration.Metadata);
              }
              return null;
            }).Where(item => item != null).ToList();
    }

    public ICollection RequestAllPluginItems(string location, Type type, IPluginItemStateTracker stateTracker)
    {
      IList result = new ArrayList();
      foreach (PluginItemRegistration itemRegistration in PluginRuntime.GetItemRegistrations(location))
      {
        try
        {
          object item = RequestItem(itemRegistration, type, stateTracker);
          if (item != null)
            result.Add(item);
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot request {0}", e, itemRegistration.Metadata);
        }
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
          // Hint: As of the specification (see PluginState.EndRequest), we would have to make the current
          // thread sleep until a possible ongoing stopping procedure is finished or cancelled. This means we have to
          // implement a notification mechanism in the stop request methods to re-awake this thread.
          // By now, we simply let the item request fail (which is not exactly the specified behavior!)
          throw new PluginInvalidStateException("Plugin '{0}' (id '{1}') neither is enabled nor active; cannot request items in this state",
              plugin.Metadata.Name, plugin.Metadata.PluginId);
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
          LockPluginStateDependency(plugin, false, PluginState.Active);
        else
          LockPluginStateDependency(plugin, false, PluginState.Active, PluginState.Enabled);
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
              if (type.IsAssignableFrom(result.GetType()))
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
          UnlockPluginState(plugin);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("PluginManager: Error building plugin item '{0}' at location '{1}'",
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
      LockPluginStateDependency(plugin, false, PluginState.Active, PluginState.Enabled);
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
          ServiceRegistration.Get<ILogger>().Error("Error revoking usage of item '{0}' at location '{1}' (item builder is '{2}')", e,
              itemRegistration.Metadata.Id, itemRegistration.Metadata.RegistrationLocation, metadata.BuilderName);
        }
        itemRegistration.Item = null;
        // If we wanted to automatically unload plugins whose items are not accessed any more, this
        // should be done here
      }
      finally
      {
        UnlockPluginState(plugin);
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
      LockPluginStateDependency(builderPlugin, false, PluginState.Active);
      try
      {
        object obj = null;
        try
        {
          obj = builderPlugin.InstantiatePluginObject(builderRegistration.BuilderClassName);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Error instanciating plugin item builder '{0}' (id '{1}')", e,
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
        UnlockPluginState(builderPlugin);
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
        ServiceRegistration.Get<ILogger>().Error("Error calling method 'RequestEnd' at plugin item state tracker '{0}'", e, stateTracker);
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
        ServiceRegistration.Get<ILogger>().Error("Error calling method 'Continue' at plugin item state tracker '{0}'", e, stateTracker);
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
        ServiceRegistration.Get<ILogger>().Error("Error calling method 'Stop' at plugin item state tracker '{0}'", e, stateTracker);
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
    /// Method to lock a plugin to its current state until <see cref="UnlockPluginState"/> gets called. This avoids state
    /// changes for a plugin, for example while some progress is assuming the current plugin state.
    /// State changes could happen if a component of the outside tries to trigger a state change while
    /// it is called back from a state changing method.
    /// </summary>
    /// <param name="plugin">The plugin whose state should be locked.</param>
    /// <param name="upgradableToWriteLock">Set to <c>true</c>, if the acquired lock should be upgradable to a writer lock.
    /// This enables method <see cref="UpgradeReadLockToWriteLock"/> to be called.</param>
    /// <param name="statesToLock">The plugin must be in one of those states to be locked.</param>
    protected void LockPluginStateDependency(PluginRuntime plugin, bool upgradableToWriteLock, params PluginState[] statesToLock)
    {
      lock (_syncObj)
      {
        if (statesToLock.Length > 0)
        {
          ICollection<PluginState> states = new List<PluginState>(statesToLock);
          if (!states.Contains(plugin.State))
            throw new PluginInvalidStateException("Plugin '{0}' (id '{1}') is in state '{2}' but is supposed to be in one of the states ('{3}')",
                plugin.Metadata.Name, plugin.Metadata.PluginId, plugin.State, StringUtils.Join(", ", statesToLock));
        }
        plugin.LockForStateDependency(upgradableToWriteLock);
      }
    }

    /// <summary>
    /// Upgrades a read lock to a write lock. May only be called if parameter <c>upgradableToWriteLock</c> of method
    /// <see cref="LockPluginStateDependency"/> was set to <c>true</c>.
    /// </summary>
    /// <param name="plugin">The plugin whose state should be locked.</param>
    protected void UpgradeReadLockToWriteLock(PluginRuntime plugin)
    {
      plugin.UpgradeReadLockToWriteLock();
    }

    /// <summary>
    /// Method to lock a plugin to its current state until <see cref="UnlockPluginState"/> gets called. This sets
    /// a writer lock at the plugin's state which avoids other writer or reader locks.
    /// State changes could happen if a component of the outside tries to trigger a state change while
    /// it is called back from a state changing method.
    /// </summary>
    /// <param name="plugin">The plugin whose state should be locked.</param>
    protected void LockPluginStateForWrite(PluginRuntime plugin)
    {
      plugin.LockForStateWrite();
    }

    /// <summary>
    /// Method to unlock a plugin's reader or writer lock, which was acquired by <see cref="LockPluginStateDependency"/> or
    /// <see cref="LockPluginStateForWrite"/>.
    /// </summary>
    /// <param name="plugin">The plugin for that method <see cref="LockPluginStateForWrite"/>
    /// was called before.</param>
    protected void UnlockPluginState(PluginRuntime plugin)
    {
      plugin.UnlockState();
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
      ICollection<PluginRuntime> autoActivatePlugins = new List<PluginRuntime>();
      bool result = TryEnable(plugin, autoActivatePlugins, new HashSet<Guid>());
      if (doAutoActivate)
        foreach (PluginRuntime autoActivatePlugin in autoActivatePlugins)
          TryActivate(autoActivatePlugin);
      return result;
    }

    /// <summary>
    /// Tries to enable the given <paramref name="plugin"/>. The caller has to activate the plugins which are given in
    /// <paramref name="autoActivatePlugins"/>, if auto activation is desired.
    /// </summary>
    /// <param name="plugin">Plugin to enable.</param>
    /// <param name="autoActivatePlugins">This collection will be filled with plugins which have their auto-activate flag set.</param>
    /// <returns><c>true</c>, if the specified <paramref name="plugin"/> and all its dependencies could
    /// be enabled, else <c>false</c>.</returns>
    protected bool TryEnable(PluginRuntime plugin, ICollection<PluginRuntime> autoActivatePlugins, HashSet<Guid> pluginsPendingActivation)
    {
      if (pluginsPendingActivation.Contains(plugin.Metadata.PluginId))
        return true;

      LockPluginStateDependency(plugin, false); // First lock for read
      ICollection<PluginRuntime> lockedPluginStates = new List<PluginRuntime> {plugin};
      try
      {
        // Check current plugin state - if it's already enabled or stopping, we can break the method early
        if (IsEnabledOrActive(plugin))
          return true;
        if (IsInStoppingProcess(plugin))
          return false;
        // Exchange pure read lock by upgradable read lock
        UnlockPluginState(plugin);
        LockPluginStateDependency(plugin, true, PluginState.Available, PluginState.Disabled);
        ILogger logger = ServiceRegistration.Get<ILogger>();
        string pluginDisplayName = string.Format("'{0}' [Version: {1}; Authors: {2}; ID: '{3}']", plugin.Metadata.Name, plugin.Metadata.PluginVersion, plugin.Metadata.Author, plugin.Metadata.PluginId);
        logger.Debug("PluginManager: Trying to enable plugin {0}", pluginDisplayName);
        try
        {
          if (FindConflicts(plugin.Metadata).Count > 0)
          {
            logger.Warn("PluginManager: Plugin '{0}' cannot be enabled - there are plugin conflicts", plugin.Metadata.Name);
            return false;
          }
        }
        catch (PluginMissingDependencyException missingEx)
        {
          logger.Warn("PluginManager: Plugin '{0}' cannot be enabled: {1}", plugin.Metadata.Name, missingEx.Message);
          return false;
        }
        catch (PluginIncompatibleException incompEx)
        {
          logger.Warn("PluginManager: Plugin '{0}' cannot be enabled: {1}", plugin.Metadata.Name, incompEx.Message);
          return false;
        }

        pluginsPendingActivation.Add(plugin.Metadata.PluginId);

        // Handle dependencies
        ICollection<PluginRuntime> pendingChildRegistrations = new List<PluginRuntime>();
        foreach (PluginDependencyInfo parentDependency in plugin.Metadata.DependsOn)
        {
          if (!parentDependency.IsCoreDependency)
          {
            PluginRuntime parentPlugin;
            lock (_syncObj)
              if (!_availablePlugins.TryGetValue(parentDependency.PluginId, out parentPlugin))
              {
                logger.Warn("Plugin {0}: Dependency '{1}' is not available", pluginDisplayName, parentDependency.PluginId);
                return false;
              }
            if (!pluginsPendingActivation.Contains(parentPlugin.Metadata.PluginId))
            {
              if (!TryEnable(parentPlugin, autoActivatePlugins, pluginsPendingActivation))
              {
                logger.Warn("Plugin {0}: Dependency '{1}' cannot be enabled", pluginDisplayName, parentDependency.PluginId);
                return false;
              }
              LockPluginStateDependency(parentPlugin, false, PluginState.Enabled, PluginState.Active);
              lockedPluginStates.Add(parentPlugin);
              pendingChildRegistrations.Add(parentPlugin); // Remember parent -> have to register return value as dependent plugin later
            }
          }
        }

        pluginsPendingActivation.Remove(plugin.Metadata.PluginId);

        // Check if builder dependencies are explicitly named (has to be done after dependencies are loaded - builders could be added by dependent plugins)
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
              logger.Warn("Plugin {0}: Builder '{1}' is not available - plugin won't be enabled",
                pluginDisplayName, builderName);
              return false;
            }
            if (builderRegistration.PluginRuntime == null)
              // Builder is a default builder
              continue;
            if (!plugin.Metadata.DependsOn.Any(d => !d.IsCoreDependency && d.PluginId == builderRegistration.PluginRuntime.Metadata.PluginId))
            {
              logger.Error(
                  "Plugin {0}: Builder '{1}' (implemented by plugin '{2}') is used, but this plugin dependency is not explicitly specified - plugin won't be enabled",
                  pluginDisplayName, builderName, builderRegistration.PluginRuntime.Metadata.Name);
              return false;
            }
          }

        // All checks passed and preconditions met, enable plugin
        UpgradeReadLockToWriteLock(plugin);
        try
        {
          plugin.RegisterItems();
        }
        catch (Exception e)
        {
          logger.Error("Error registering plugin items for plugin {0}", e, pluginDisplayName);
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
        logger.Info("PluginManager: Plugin {0} enabled.", pluginDisplayName);
      }
      finally
      {
        foreach (PluginRuntime lockedPlugin in lockedPluginStates)
          UnlockPluginState(lockedPlugin);
      }
      if (plugin.Metadata.AutoActivate)
        autoActivatePlugins.Add(plugin);
      return true;
    }

    /// <summary>
    /// Tries to activate the specified <paramref name="plugin"/>. This method first tries to enable the plugin.
    /// </summary>
    /// <param name="plugin">Plugin to activate.</param>
    /// <returns><c>true</c>, if the plugin could be activated or was already active, else <c>false</c>.</returns>
    public bool TryActivate(PluginRuntime plugin)
    {
      return TryActivate(plugin, new HashSet<Guid>());
    }

    public bool TryActivate(PluginRuntime plugin, HashSet<Guid> pluginsPendingActivation)
    {
      if (pluginsPendingActivation.Contains(plugin.Metadata.PluginId))
        return true;

      if (!TryEnable(plugin, false))
        return false;
      if (IsInStoppingProcess(plugin))
        return false;
      LockPluginStateDependency(plugin, false); // First lock for read
      ICollection<PluginRuntime> lockedPluginStates = new List<PluginRuntime> {plugin};
      try
      {
        // Check current plugin state - if it's already active, we can break the method early
        if (plugin.State == PluginState.Active)
          return true;
        // Exchange pure read lock by upgradable read lock
        UnlockPluginState(plugin);
        LockPluginStateDependency(plugin, true, PluginState.Disabled, PluginState.Available, PluginState.Enabled);
        string pluginName = plugin.Metadata.Name;
        Guid pluginId = plugin.Metadata.PluginId;
        ILogger logger = ServiceRegistration.Get<ILogger>();
        logger.Debug("PluginManager: Trying to activate plugin '{0}' (id '{1}')", pluginName, pluginId);

        pluginsPendingActivation.Add(plugin.Metadata.PluginId);
        
        // Activate parent plugins - Load their assemblies etc.
        IDictionary<Guid, PluginRuntime> availablePlugins;
        lock (_syncObj)
          availablePlugins = new Dictionary<Guid, PluginRuntime>(_availablePlugins);
        foreach (PluginDependencyInfo parentDependency in plugin.Metadata.DependsOn)
        {
          if (!parentDependency.IsCoreDependency)
          {
            PluginRuntime parentPlugin = availablePlugins[parentDependency.PluginId];
            logger.Debug("PluginManager: Checking activation of plugin dependency '{0}' for plugin '{1}'",
                parentDependency.PluginId, pluginName);
            if (!pluginsPendingActivation.Contains(parentPlugin.Metadata.PluginId))
            {
              if (!TryActivate(parentPlugin, pluginsPendingActivation))
              {
                logger.Debug("PluginManager: Dependent plugin '{0}' could not be activated. Activation of plugin '{1}' was not successful.",
                    parentDependency.PluginId, pluginName);
                return false;
              }
              LockPluginStateDependency(parentPlugin, false, PluginState.Active);
              lockedPluginStates.Add(parentPlugin);
            }
          }
        }

        pluginsPendingActivation.Remove(plugin.Metadata.PluginId);

        // All checks passed and preconditions met, activate plugin
        UpgradeReadLockToWriteLock(plugin);
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
                ServiceRegistration.Get<ILogger>().Warn("Error activating plugin state tracker '{0}' in plugin '{1}' (id '{2})", e,
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
        foreach (PluginRuntime lockedPlugin in lockedPluginStates)
          UnlockPluginState(lockedPlugin);
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
      LockPluginStateDependency(plugin, false); // First lock for read
      ICollection<PluginRuntime> lockedPluginStates = new List<PluginRuntime> {plugin};
      try
      {
        // Check current plugin state - if it's already disabled or stopping, we can break the method early
        if (plugin.State == PluginState.Disabled)
          return true;
        if (IsInStoppingProcess(plugin))
          throw new PluginInvalidStateException("Plugin is in stopping process");
        // Exchange pure read lock by upgradable read lock
        UnlockPluginState(plugin);
        LockPluginStateDependency(plugin, true, PluginState.Enabled, PluginState.Active);
        ILogger logger = ServiceRegistration.Get<ILogger>();
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
            LockPluginStateDependency(childPlugin, false, PluginState.Disabled);
            lockedPluginStates.Add(childPlugin);
          }
        // All checks passed and preconditions met, change plugin state
        UpgradeReadLockToWriteLock(plugin);
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
                ServiceRegistration.Get<ILogger>().Warn("Error stopping plugin state tracker '{0}' in plugin '{1}' (id '{2})", e,
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
        foreach (PluginRuntime lockedPlugin in lockedPluginStates)
          UnlockPluginState(lockedPlugin);
      }
    }

    /// <summary>
    /// Loads all available plugin descriptors from all known plugin directories.
    /// </summary>
    /// <returns>Mapping of plugin ids to metadata descriptors.</returns>
    public IDictionary<Guid, IPluginMetadata> LoadPluginsData()
    {
      IDictionary<Guid, IPluginMetadata> result = new Dictionary<Guid, IPluginMetadata>();
      String pluginsDirectoryPath = ServiceRegistration.Get<IPathManager>().GetPath("<PLUGINS>");
      foreach (string pluginDirectoryPath in Directory.GetDirectories(pluginsDirectoryPath))
      {
        if ((Path.GetFileName(pluginDirectoryPath) ?? string.Empty).StartsWith("."))
          continue;
        try
        {
          IPluginMetadata pm = new PluginDirectoryDescriptor(pluginDirectoryPath);
          if (result.ContainsKey(pm.PluginId))
            throw new ArgumentException(string.Format(
                "Duplicate: plugin '{0}' has the same plugin id as {1}", pm.Name, result[pm.PluginId]));
          result.Add(pm.PluginId, pm);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Error loading plugin in directory '{0}'", e, pluginDirectoryPath);
        }
      }
      return result;
    }

    internal void ReflectCoreAPI()
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        var attributes = assembly.GetCustomAttributes(typeof(CoreAPIAttribute), false);
        if (attributes != null && attributes.Length > 0)
        {
          var coreAPI = attributes[0] as CoreAPIAttribute;
          var componentName = assembly.GetName().Name;
          _coreComponents.Add(componentName, coreAPI);
        }
      }
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
