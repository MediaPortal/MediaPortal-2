#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Services.PluginManager.Builders;

namespace MediaPortal.Core
{
  /// <summary>
  /// The Service Scope class.  It is used to keep track of the scope of services.
  /// The moment you create a new ServiceScope instance, any service instances you
  /// add to it will be automtically used by code that is called while the the 
  /// ServiceScope instance remains in scope (i.e. is not Disposed)
  /// </summary>
  /// <remarks>
  /// <para>A ServiceScope is some kind of repository that holds a reference to 
  /// services that other components could need.</para><para>Instead of making
  /// this class a static with static properties for all types of services we 
  /// choose to create a mechanism that is more flexible.  The biggest advantage of
  /// this implemtentation is that you can create different ServiceScope instances
  /// that will be "stacked" upon one another.</para><para>This way you can (temporarily) 
  /// override a certain service by adding another implementation of the service
  /// interface, which fits you better.  While your new ServiceScope instance 
  /// remains in scope, all code that is executed will automatically (if it is
  /// written correctly of course) use this new service implementation.</para>
  /// <para>
  /// <b>A service scope is only valid in the same thread it was created.</b> 
  /// </para>
  /// The recommended way of passing the current <see cref="ServiceScope"/> to 
  /// another thread is by passing <see cref="ServiceScope.Current"/> with the 
  /// delegate used to start the thread and then use 
  /// <code>ServiceScope.Current = passedContect;</code> to restore it in the 
  /// thread.</para><para>If you do not pass the current ServiceScope to the 
  /// background thread, it will automatically fallback to the <b>global</b> 
  /// ServiceScope.  This is the current ServiceScope of the application thread.
  /// This ServiceScope can be another instance than the one you expect... </para>
  /// </remarks>
  /// <example> This example creates a new ServiceScope and adds its own implementation
  /// of a certain service to it.
  /// <code>
  /// //SomeMethod will log to the old logger here.
  /// SomeMethod();
  /// using(new ServiceScope())
  /// {
  ///   ServiceScope.Add&lt;ILogger&gt;(new FileLogger("blabla.txt"))
  ///   {
  ///     //SomeMethod will now log to our new logger (which will log to blabla.txt)
  ///     SomeMethod();
  ///   }
  /// }
  /// .
  /// .
  /// .
  /// private void SomeMethod()
  /// {
  ///    ILogger logger = ServiceScope.Get&lt;ILogger&gt;();
  ///    logger.Debug("Logging to whatever file our calling method decides");
  /// }
  /// </code></example>
  /// <example>This is an example of how to pass the current ServiceScope to a
  /// timer thread.
  /// <code>
  /// using(Timer timer = new Timer(TimerTick, ServiceScope.Current, 0000, 3000))
  /// {
  ///   //do something useful here while the timer is busy
  /// }
  /// .
  /// .
  /// .
  /// private void TimerTick(object passedScope)
  /// {
  ///   ServiceScope.Current = passedScope as ServiceScope;
  ///   ServiceScope.Get&lt;ILogger&gt;().Info("Timer tick");
  /// }
  /// </code>
  /// </example>
  /// TODO: Remove the stacking functionality; rename ServiceScope to ServiceRegistration
  public sealed class ServiceScope : IDisposable, IStatus
  {
    public const string PLUGIN_TREE_SERVICES_LOCATION = "/Services";

    private static readonly object _syncObj = new object();

    /// <summary>
    /// Pointer to the current <see cref="ServiceScope"/>.
    /// </summary>
    /// <remarks>
    /// This pointer is only static for the current thread.
    /// </remarks>
    [ThreadStatic]
    private static ServiceScope _current;

    /// <summary>
    /// Pointer to the global <see cref="ServiceScope"/>.  This is the 
    /// </summary>
    private static ServiceScope _global;

    private static bool _isRunning = false;
    private static bool _isShuttingDown = false;

    /// <summary>
    /// Pointer to the previous <see cref="ServiceScope"/>.  We need this pointer 
    /// to be able to restore the previous ServiceScope when the <see cref="Dispose()"/>
    /// method is called, and to ask it for services that we do not contain ourselves.
    /// </summary>
    private readonly ServiceScope _oldInstance;

    /// <summary>
    /// Holds the dictionary of services.
    /// </summary>
    private readonly IDictionary<Type, object> _services = new Dictionary<Type, object>();

    /// <summary>
    /// Holds the collection of services which were loaded from the plugin tree.
    /// </summary>
    private readonly ICollection<Type> _pluginServices = new List<Type>();

    /// <summary>
    /// Keeps track whether the instance is already disposed
    /// </summary>
    private bool _isDisposed = false;


    public ServiceScope(bool isFirst)
    {
      lock (_syncObj)
      {
        bool updateGlobal = _global == _current;
        _oldInstance = _current;
        _current = this;
        if (updateGlobal)
        {
          _global = this;
        }
        if (isFirst)
        {
          _isRunning = true;
        }
      }
    }

    /// <summary>
    /// Creates a new <see cref="ServiceScope"/> instance and initialize it.
    /// </summary>
    public ServiceScope() : this(false) { }

    public static void RemoveAndDisposePluginServices()
    {
      Current.DoRemoveAndDisposePluginServices();
    }

    /// <summary>
    /// Gets or sets the current <see cref="ServiceScope"/>
    /// </summary>
    public static ServiceScope Current
    {
      get
      {
        if (_current == null)
        {
          if (_global == null)
          {
            new ServiceScope();
          }
          _current = _global;
        }
        return _current;
      }
      set { _current = value; }
    }

    internal static bool IsRunning
    {
      get { return _isRunning; }
    }

    public static bool IsShuttingDown
    {
      get { return _isShuttingDown; }
      set { _isShuttingDown = value; }
    }

    ~ServiceScope()
    {
      Dispose(false);
    }

    #region IDisposable implementation

    /// <summary>
    /// Restores the previous service context.
    /// </summary>
    /// <remarks>
    /// Use the using keyword to automatically call this method when the 
    /// service context goes out of scope.
    /// </remarks>
    public void Dispose()
    {
      Dispose(true);
    }

    #endregion

    private void Dispose(bool alsoManaged)
    {
      if (_isDisposed) // already disposed?
        return;
      if (alsoManaged)
      {
        bool updateGlobal = _current == _global;
        _current = _oldInstance; //set current scope to previous one
        if (updateGlobal)
        {
          _global = _current;
        }
      }
      _isDisposed = true;
    }

    /// <summary>
    /// Adds a new Service to the <see cref="ServiceScope"/>
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of service to add.</typeparam>
    /// <param name="service">The service implementation to add.</param>
    public static void Add<T>(T service) where T : class
    {
      Current.AddService(typeof(T), service);
    }

    public static void Replace<T>(T service) where T : class
    {
      Current.ReplaceService(typeof(T), service);
    }

    public static void Remove<T>() where T : class
    {
      Current.RemoveService(typeof(T));
    }

    public static void RemoveAndDispose<T>() where T : class
    {
      Current.RemoveAndDispose(typeof(T));
    }

    public static bool IsRegistered<T>() where T : class
    {
      return Current.IsServiceRegistered(typeof(T));
    }

    public static bool IsPluginService<T>() where T : class
    {
      return Current.IsPluginService(typeof(T));
    }

    /// <summary>
    /// Gets a service from the current <see cref="ServiceScope"/>
    /// </summary>
    /// <typeparam name="T">the type of the service to get.  This is typically
    /// (but not necessarily) an interface</typeparam>
    /// <returns>the service implementation.</returns>
    /// <exception cref="ServiceNotFoundException">when the requested service type is not found.</exception>
    public static T Get<T>() where T : class
    {
      return (T) Current.GetService(typeof(T), true);
    }

    /// <summary>
    /// Gets a service from the current <see cref="ServiceScope"/>
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
      return (T) Current.GetService(typeof(T), throwIfNotFound);
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
        Current.RemoveService(type);
        IDisposable disposableService = service as IDisposable;
        if (disposableService != null)
          disposableService.Dispose();
      }
    }

    private void DoRemoveAndDisposePluginServices()
    {
      foreach (Type serviceType in _pluginServices)
        RemoveAndDispose(serviceType);
      if (_oldInstance != null)
        _oldInstance.DoRemoveAndDisposePluginServices();
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
      ILogger logger = Get<ILogger>();
      logger.Info("ServiceScope: Loading services from plugin manager at location '{0}'", PLUGIN_TREE_SERVICES_LOCATION);
      ICollection<PluginItemMetadata> metadata = pluginManager.GetAllPluginItemMetadata(PLUGIN_TREE_SERVICES_LOCATION);
      foreach (PluginItemMetadata itemMetadata in metadata)
      {
        ServiceBuilder.ServiceItem item = pluginManager.RequestPluginItem<ServiceBuilder.ServiceItem>(
            PLUGIN_TREE_SERVICES_LOCATION, itemMetadata.Id, new FixedItemStateTracker(string.Format("System services")));
        if (item == null)
        {
          Get<ILogger>().Warn("ServiceScope: Could not register dynamic service with id '{0}'", itemMetadata.Id);
          continue;
        }
        Current._services.Add(item.RegistrationType, item.ServiceInstance);
        Current._pluginServices.Add(item.RegistrationType);
      }
    }

    private object GetService(Type type, bool throwIfNotFound)
    {
      if (_services.ContainsKey(type))
        return _services[type];
      if (_oldInstance == null)
        if (throwIfNotFound)
          throw new ServiceNotFoundException(type);
        else
          return null;
      return _oldInstance.GetService(type, throwIfNotFound);
    }

    private void ReplaceService(Type type, object service)
    {
      RemoveService(type);
      AddService(type, service);
    }

    internal static void Reset()
    {
      _current = null;
      _global = null;
      _isRunning = false;
    }

    #region IStatus implementation

    public IList<string> GetStatus()
    {
      List<string> status = new List<string> { "== ServiceScope List Start" };
      foreach (KeyValuePair<Type, object> service in _services)
      {
        status.Add(String.Format("=== Service = {0}, {1}", service.Key.Name, service.Value));
        IStatus info = service.Value as IStatus;
        if (info != null)
        {
          status.AddRange(info.GetStatus());
        }
      }
      status.Add("== ServiceScope List End");
      return status;
    }

    #endregion
  }
}
