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
using System.Collections.Generic;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Services.PluginManager.Builders;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core
{
  /// <summary>
  /// The global service provider class. It is used to provide the references to all
  /// globally available services.
  /// </summary>
  /// <remarks>
  /// <para>This class is some kind of repository that holds a reference to
  /// services that other components could need.</para>
  /// </remarks>
  public sealed class ServiceRegistration : IDisposable, IStatus
  {
    public const string PLUGIN_TREE_SERVICES_LOCATION = "/Services";

    #region Classes

    private class ServiceRegistrationChangeListener : IItemRegistrationChangeListener
    {
      private readonly ServiceRegistration _parent;

      public ServiceRegistrationChangeListener(ServiceRegistration parent)
      {
        _parent = parent;
      }

      #region IItemRegistrationChangeListener implementation

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        _parent.AddServiceItems(items);
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // Item removals are not supported
      }

      #endregion
    }

    #endregion

    private static readonly object _syncObj = new object();

    /// <summary>
    /// Singleton instance of the <see cref="ServiceRegistration"/>.
    /// </summary>
    private static ServiceRegistration _instance;

    private static bool _isShuttingDown = false;

    /// <summary>
    /// Holds the dictionary of services.
    /// </summary>
    private readonly IDictionary<Type, object> _services = new Dictionary<Type, object>();
    private static IItemRegistrationChangeListener _servicesRegistrationChangeListener;

    /// <summary>
    /// Holds the collection of services which were loaded from the plugin tree.
    /// </summary>
    private readonly ICollection<Type> _pluginServices = new List<Type>();

    private ServiceRegistration()
    {
      _servicesRegistrationChangeListener = new ServiceRegistrationChangeListener(this);
    }

    public static void RemoveAndDisposePluginServices()
    {
      Instance.DoRemoveAndDisposePluginServices();
    }

    /// <summary>
    /// Gets or sets the current <see cref="ServiceRegistration"/>
    /// </summary>
    public static ServiceRegistration Instance
    {
      get
      {
        lock (_syncObj)
          if (_instance == null)
            _instance = new ServiceRegistration();
        return _instance;
      }
    }

    public static bool IsShuttingDown
    {
      get { return _isShuttingDown; }
      set { _isShuttingDown = value; }
    }

    ~ServiceRegistration()
    {
      Dispose();
    }

    #region IDisposable implementation

    public void Dispose()
    {
      _instance = null;
    }

    #endregion

    /// <summary>
    /// Adds a new Service to the <see cref="ServiceRegistration"/>
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of service to add. This is typically (but not necessarily) an interface
    /// and works as "handle" for the service, i.e. the service is retrieved from the <see cref="ServiceRegistration"/> by
    /// calling <see cref="Get{T}()"/> method with that interface as type parameter.</typeparam>
    /// <param name="service">The service implementation to add.</param>
    public static void Add<T>(T service) where T : class
    {
      Instance.AddService(typeof(T), service);
    }

    public static void Replace<T>(T service) where T : class
    {
      Instance.ReplaceService(typeof(T), service);
    }

    public static void Remove<T>() where T : class
    {
      Instance.RemoveService(typeof(T));
    }

    public static void RemoveAndDispose<T>() where T : class
    {
      Instance.RemoveAndDispose(typeof(T));
    }

    public static bool IsRegistered<T>() where T : class
    {
      return Instance.IsServiceRegistered(typeof(T));
    }

    public static bool IsPluginService<T>() where T : class
    {
      return Instance.IsPluginService(typeof(T));
    }

    /// <summary>
    /// Gets a service from the <see cref="ServiceRegistration"/>
    /// </summary>
    /// <typeparam name="T">the type of the service to get.</typeparam>
    /// <returns>the service implementation.</returns>
    /// <exception cref="ServiceNotFoundException">when the requested service type is not found.</exception>
    public static T Get<T>() where T : class
    {
      return (T) Instance.GetService(typeof(T), true);
    }

    /// <summary>
    /// Gets a service from the current <see cref="ServiceRegistration"/>
    /// </summary>
    /// <typeparam name="T">The type of the service to get. This is typically
    /// (but not necessarily) an interface</typeparam>
    /// <param name="throwIfNotFound">a <b>bool</b> indicating whether to throw a
    /// <see cref="ServiceNotFoundException"/> when the requested service is not found</param>
    /// <returns>the service implementation or <b>null</b> if the service is not available
    /// and <paramref name="throwIfNotFound"/> is false.</returns>
    /// <exception cref="ServiceNotFoundException">when <paramref="throwIfNotFound"/>
    /// is <b>true</b> andthe requested service type is not found.</exception>
    public static T Get<T>(bool throwIfNotFound) where T : class
    {
      return (T) Instance.GetService(typeof(T), throwIfNotFound);
    }

    private void AddService(Type type, object service)
    {
      if (service == null)
        throw new ArgumentException("Service argument must not be null", "service");
      if (!type.IsAssignableFrom(service.GetType()))
        throw new ArgumentException("Given service registration type must be assignable from the type of the given service");
      _services[type] = service;
    }

    private void RemoveService(Type type)
    {
      _services.Remove(type);
    }

    private void RemoveAndDispose(Type type)
    {
      object service = GetService(type, false);
      if (service != null)
      {
        Instance.RemoveService(type);
        IDisposable disposableService = service as IDisposable;
        if (disposableService != null)
          disposableService.Dispose();
      }
    }

    private void DoRemoveAndDisposePluginServices()
    {
      foreach (Type serviceType in _pluginServices)
        RemoveAndDispose(serviceType);
    }

    private bool IsServiceRegistered(Type type)
    {
      return _services.ContainsKey(type);
    }

    private bool IsPluginService(Type type)
    {
      return _pluginServices.Contains(type);
    }

    public static void LoadServicesFromPlugins()
    {
      IPluginManager pluginManager = Get<IPluginManager>();
      pluginManager.AddItemRegistrationChangeListener(PLUGIN_TREE_SERVICES_LOCATION, _servicesRegistrationChangeListener);
      ILogger logger = Get<ILogger>();
      logger.Info("ServiceRegistration: Loading services from plugin manager at location '{0}'", PLUGIN_TREE_SERVICES_LOCATION);
      ICollection<PluginItemMetadata> items = pluginManager.GetAllPluginItemMetadata(PLUGIN_TREE_SERVICES_LOCATION);
      Instance.AddServiceItems(items);
    }

    private void AddServiceItems(IEnumerable<PluginItemMetadata> items)
    {
      IPluginManager pluginManager = Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in items)
      {
        // We cannot use an item state tracker which is able to revoke the service because we cannot
        // know which methods are using the service, so the only safe way is to use a fixed item state tracker
        ServiceBuilder.ServiceItem item = pluginManager.RequestPluginItem<ServiceBuilder.ServiceItem>(
            PLUGIN_TREE_SERVICES_LOCATION, itemMetadata.Id, new FixedItemStateTracker(string.Format("System services")));
        if (item == null)
        {
          Get<ILogger>().Warn("ServiceRegistration: Could not register dynamic service with id '{0}'", itemMetadata.Id);
          continue;
        }
        if (_services.ContainsKey(item.RegistrationType))
          throw new EnvironmentException("ServiceRegistration: A Service with registration type '{0}' is already registered", item.RegistrationType);
        _services.Add(item.RegistrationType, item.ServiceInstance);
        _pluginServices.Add(item.RegistrationType);
      }
    }

    private object GetService(Type type, bool throwIfNotFound)
    {
      if (_services.ContainsKey(type))
        return _services[type];
      if (throwIfNotFound)
        throw new ServiceNotFoundException(type);
      return null;
    }

    private void ReplaceService(Type type, object service)
    {
      RemoveService(type);
      AddService(type, service);
    }

    #region IStatus implementation

    public IList<string> GetStatus()
    {
      List<string> status = new List<string> { "== ServiceRegistration List Start" };
      foreach (KeyValuePair<Type, object> service in _services)
      {
        status.Add(String.Format("=== Service = {0}, {1}", service.Key.Name, service.Value));
        IStatus info = service.Value as IStatus;
        if (info != null)
        {
          status.AddRange(info.GetStatus());
        }
      }
      status.Add("== ServiceRegistration List End");
      return status;
    }

    #endregion
  }
}
