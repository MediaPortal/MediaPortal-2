#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AspNetServer.Logger;
using MediaPortal.Plugins.AspNetServer.PlatformServices;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace MediaPortal.Plugins.AspNetServer
{
  /// <summary>
  /// Provides methods to start and stop ASP.NET 5 WebApplications
  /// </summary>
  /// <remarks>
  /// For details on ASP.NET see https://github.com/aspnet
  /// </remarks>
  public class AspNetServerService : IAspNetServerService, IDisposable
  {
    #region Private fields

    /// <summary>
    /// Used to serialize starting and stopping WebApplications
    /// </summary>
    private readonly ActionBlock<AspNetServerAction> _workerBlock;

    /// <summary>
    /// Maps a <see cref="WebApplicationParameter"/> to the respective WebApplication (the disposal of which stops the WebApplication)
    /// </summary>
    /// <remarks>May only be accessed from within the <see cref="Process"/> method to ensure that there is no multithreaded access.</remarks>
    private readonly Dictionary<WebApplicationParameter, IDisposable> _webApplications;

    /// <summary>
    /// Workaround: ToDo: Remove this once Microsoft has removed the dependency to ILibraryManager and ILibraryExporter
    /// </summary>
    private readonly Task _AssemblyLoader;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of this class
    /// </summary>
    public AspNetServerService()
    {
      // Necessary so that the Asp.Net dlls can be resolved even if the WebApplication is contained in another plugin
      AppDomain.CurrentDomain.AssemblyResolve += LoadAssemblyFromPluginFolder;

      // MaxDegreeOfParallelism = 1 ensures that there one action is performed after the other
      _workerBlock = new ActionBlock<AspNetServerAction>(new Action<AspNetServerAction>(Process), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
      _webApplications = new Dictionary<WebApplicationParameter, IDisposable>();

      // Workaround: ToDo: Remove this once Microsoft has removed the dependency to ILibraryManager and ILibraryExporter
      _AssemblyLoader = LoadAllReferences();

      ServiceRegistration.Get<ILogger>().Info("AspNetServerService: Started.");
    }

    #endregion

    #region Static methods

    /// <summary>
    /// Assembly resolver method that loads assemblies from the plugin directory
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="args">Resolve event arguments</param>
    /// <returns>If successfull, the loaded assembly; else null</returns>
    private static Assembly LoadAssemblyFromPluginFolder(object sender, ResolveEventArgs args)
    {
      string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (folderPath == null)
        return null;
      string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
      if (!File.Exists(assemblyPath))
        return null;
      Assembly assembly = Assembly.LoadFrom(assemblyPath);
      return assembly;
    }

    /// <summary>
    /// Loads all assemblies directly and indirectly referenced by this plugin or any dependent plugin of this plugin
    /// </summary>
    /// <remarks>
    /// ToDo: Remove this once Microsoft has removed the dependency to ILibraryManager and ILibraryExporter
    /// </remarks>
    private static Task LoadAllReferences()
    {
      return Task.Run(() =>
      {
        ServiceRegistration.Get<ILogger>().Debug("AspNetServerService: Start loading directly and indirectly referenced assemblies of AspNetServerService and dependent plugins");

        var alreadyProcessed = new HashSet<Assembly>();
        var queue = new Queue<Assembly>();

        queue.Enqueue(Assembly.GetExecutingAssembly());
        var thisPlugin = ServiceRegistration.Get<IPluginManager>().AvailablePlugins.First(kvp => kvp.Value.Metadata.PluginId == Guid.Parse("F2F6988F-C436-4D74-9819-3947E0DD6974")).Value;
        var dependentPluginAssemblies = thisPlugin.DependentPlugins.SelectMany(plugin => plugin.LoadedAssemblies);
        foreach (var assembly in dependentPluginAssemblies)
          queue.Enqueue(assembly);

        while (queue.Count > 0)
        {
          var assembly = queue.Dequeue();

          // Do nothing if this assembly was already processed.
          if (!alreadyProcessed.Add(assembly))
            continue;

          // Find referenced assemblies
          foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
          {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == referencedAssemblyName.FullName);
            if (loadedAssembly == null)
            {
              try
              {
                loadedAssembly = Assembly.Load(referencedAssemblyName);
              }
              catch (Exception e)
              {
                ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot load Assembly {0}", referencedAssemblyName.FullName, e);
              }
            }
            if (loadedAssembly != null)
              queue.Enqueue(loadedAssembly);
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("AspNetServerService: Finished loading directly and indirectly referenced assemblies of AspNetServerService and dependent plugins");
      });
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main method called for every <see cref="AspNetServerAction"/> posted to <see cref="_workerBlock"/>
    /// </summary>
    /// <param name="action">Action to be performed</param>
    private void Process(AspNetServerAction action)
    {
      if (action == null)
      {
        ServiceRegistration.Get<ILogger>().Error("AspNetServerService: Process called with null action.");
        return;
      }
      try
      {
        // Workaround: ToDo: Remove this once Microsoft has removed the dependency to ILibraryManager and ILibraryExporter
        _AssemblyLoader.Wait();

        if (action.Action == AspNetServerAction.ActionType.Start)
          StartWebApplication(action);
        else
          StopWebApplication(action);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("AspNetServerService: Error while starting {0}.", e, action.WebApplicationParameter);
      }
    }

    /// <summary>
    /// Checks for duplicate WebApplicationNames and overlapping BasePaths and, if checks are passed, starts and registers the WebApplication
    /// </summary>
    /// <param name="action">Action to be performed</param>
    private void StartWebApplication(AspNetServerAction action)
    {
      // Make sure that no other WebApplication has the same WebApplicationName
      if (_webApplications.ContainsKey(action.WebApplicationParameter))
      {
        var alreadyRunningWebApplication = _webApplications.Single(kvp => kvp.Key.Equals(action.WebApplicationParameter)).Key;
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. {1} is already running and has the same name.", action.WebApplicationParameter, alreadyRunningWebApplication);
        action.Tcs.TrySetResult(false);
        return;
      }

      // Make sure that no WebApplications have overlapping BasePaths
      var webApplicationsWithOverlappingBasePaths = _webApplications.Where(kvp => kvp.Key.BasePathOverlapsWith(action.WebApplicationParameter)).ToList();
      if (webApplicationsWithOverlappingBasePaths.Any())
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. BasePath overlaps with {1}.", action.WebApplicationParameter, string.Join(", ", webApplicationsWithOverlappingBasePaths));
        action.Tcs.TrySetResult(false);
        return;
      }

      // Kestrel currently doesn't support multiple WebApplications on the same port even if the BasePaths do not overlap.
      // We log a meaningful warning and try to start the WebApplication anyway so that we can see in the logs if Microsoft
      // later implements support for this scenario in Kestrel.
      if (ServiceRegistration.Get<ISettingsManager>().Load<AspNetServerSettings>().CheckAndGetServer() == AspNetServerSettings.KESTREL)
      {
        var webApplicationOnTheSamePort = _webApplications.Where(kvp => kvp.Key.Port == action.WebApplicationParameter.Port).ToList();
        if (webApplicationOnTheSamePort.Any())
        {
          ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Start of {0} is likely to fail due to {1} listening on the same port. Kestrel currently doesn't support multiple WebApplications on the same port. Use WebListener instead.", action.WebApplicationParameter, webApplicationOnTheSamePort[0].Key);
        }
      }

      // Try to start the WebApplication and if successful, register it
      var webApplication = StartWebApplication(action.WebApplicationParameter);
      if (webApplication == null)
      {
        action.Tcs.TrySetResult(false);
        return;
      }
      _webApplications[action.WebApplicationParameter] = webApplication;
      ServiceRegistration.Get<ILogger>().Info("AspNetServerService: {0} started.", action.WebApplicationParameter);
      action.Tcs.TrySetResult(true);
    }

    /// <summary>
    /// Starts a WebApplication based on the given <paramref name="webApplicationParameter"/>
    /// </summary>
    /// <param name="webApplicationParameter">Necessary parameters to start the WebApplication</param>
    /// <returns>If successful, an object, the disposal of which stops the WebApplication; else null</returns>
    private IDisposable StartWebApplication(WebApplicationParameter webApplicationParameter)
    {
      try
      {
        var app = new WebApplicationBuilder()
          .ConfigureServices(services =>
          {
            // Register dependencies necessary before the registration of dependencies provided by the calling plugin
            services.AddSingleton<IApplicationEnvironment>(new MP2ApplicationEnvironment());
            services.AddTransient(typeof(ILibraryManager), typeof(MP2LibraryManager));

            // Register temporary ILibraryExporter
            // ToDo: Remove this if no longer needed
            services.AddSingleton<ILibraryExporter, MP2LibraryExporter>();

            // Register dependencies provided by the calling plugin
            webApplicationParameter.ConfigureServices(services);

            // Dependencies to be registered after the registration of dependencies provided by the calling plugin go here
          })
          .Configure(applicationBuilder =>
          {
            // Configurations to be performed before the calling plugin's configuration is performed go here

            // Configuration provided by the calling plugin
            webApplicationParameter.ConfigureApp(applicationBuilder);

            // Configurations to be performed after the calling plugin's configuration is performed go here
          })
          
          // Use the server (WebListener or Kestrel) as defined in the settings
          .UseServerFactory(ServiceRegistration.Get<ISettingsManager>().Load<AspNetServerSettings>().CheckAndGetServer())
          
          // If enabled in the settings add a DebuLogger
          .ConfigureLogging(loggerFactory => loggerFactory.AddProvider(new MP2LoggerProvider(webApplicationParameter.WebApplicationName)))
          .Build();

        // Set the Url this WebApplication is supposed to listen on
        app.GetAddresses().Clear();
        app.GetAddresses().Add(webApplicationParameter.Url);

        // Start the WebApplication
        return app.Start();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}.", e, webApplicationParameter);
        return null;
      }
    }

    /// <summary>
    /// Checks if a WebApplication with the given WebApplicationName has been started before and, if so, stops and deregisters it
    /// </summary>
    /// <param name="action">Action to be performed</param>
    private void StopWebApplication(AspNetServerAction action)
    {
      IDisposable webApplication;
      if (!_webApplications.TryGetValue(action.WebApplicationParameter, out webApplication))
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot stop {0}. No WebApplicaton with this name has been started.", action.WebApplicationParameter);
        action.Tcs.TrySetResult(false);
        return;
      }
      try
      {
        _webApplications.Remove(action.WebApplicationParameter);
        webApplication.Dispose();
        ServiceRegistration.Get<ILogger>().Info("AspNetServerService: {0} stopped.", action.WebApplicationParameter);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot stop {0}.", e, action.WebApplicationParameter);
        action.Tcs.TrySetResult(false);
        return;
      }
      action.Tcs.TrySetResult(true);
    }

    /// <summary>
    /// Stops all currently running WebApplications
    /// </summary>
    private void Shutdown()
    {
      foreach (var kvp in _webApplications)
      {
        try
        {
          kvp.Value.Dispose();
          ServiceRegistration.Get<ILogger>().Info("AspNetServerService: {0} stopped.", kvp.Key);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot stop {0}.", e, kvp.Key);
        }
      }
    }

    #endregion

    #region IAspNetServerService implementation

    /// <summary>
    /// Starts a WebApplication with the name <param name="webApplicationName"></param> on the given TCP <param name="port"></param> and <param name="basePath"></param>.
    /// </summary>
    /// <param name="webApplicationName">Unique name to identify the WebApplication</param>
    /// <param name="configureServices">Action that uses the <see cref="IServiceCollection"/> parameter to configure the dependencies</param>
    /// <param name="configureApp">Action that uses the <see cref="IApplicationBuilder"/> parameter to configure the WebApplication</param>
    /// <param name="port">TCP port on which the WebApplication is supposed to listen</param>
    /// <param name="basePath">Base path on which the WebApplication is supposed to listen</param>
    /// <returns>
    /// A Task that completes when the WebApplication has started or failed to start.
    /// The Task's result is <c>true</c> if the WebApplication started successfully, else <c>false</c>.
    /// </returns>
    public Task<bool> TryStartWebApplicationAsync(string webApplicationName, Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configureApp, int port, string basePath)
    {
      // Validate parameters
      if (string.IsNullOrWhiteSpace(webApplicationName))
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start WebApplication. WebApplicationName was null, empty or consisted only of whitespace characters.");
        return Task.FromResult(false);
      }
      var webApplicationParameter = new WebApplicationParameter(webApplicationName);
      if (!webApplicationParameter.TryInitialize(configureServices, configureApp, port, basePath))
        return Task.FromResult(false);

      // Enqueue action to start WebApplication
      var action = new AspNetServerAction(AspNetServerAction.ActionType.Start, webApplicationParameter);
      if (!_workerBlock.Post(action))
      {
        // Can only happen if Dispose has completed the _workerBlock
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. AspNetServerSerive has already been disposed.", webApplicationParameter);
        return Task.FromResult(false);
      }
      return action.Tcs.Task;
    }

    /// <summary>
    /// Stops a WebApplication with the given <param name="webApplicationName"></param>.
    /// </summary>
    /// <param name="webApplicationName">Name that was used when starting the WebApplication</param>
    /// <returns>
    /// A Task that completes when the WebApplication has stopped or failed to stop.
    /// The Task's result is <c>true</c> if the WebApplication was stopped successfully, else <c>false</c>.
    /// <c>false</c> can also mean that there was no WebApplication started with the given <param name="webApplicationName"></param>.
    /// </returns>
    public Task<bool> TryStopWebApplicationAsync(string webApplicationName)
    {
      // Validate parameters
      if (string.IsNullOrWhiteSpace(webApplicationName))
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot stop WebApplication. WebApplicationName was null, empty or consisted only of whitespace characters.");
        return Task.FromResult(false);
      }

      // Enqueue action to stop WebApplication
      var action = new AspNetServerAction(AspNetServerAction.ActionType.Stop, new WebApplicationParameter(webApplicationName));
      if (!_workerBlock.Post(action))
      {
        // Can only happen if Dispose has completed the _workerBlock
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot stop {0}. AspNetServerService has already been disposed.", action.WebApplicationParameter);
        return Task.FromResult(false);
      }
      return action.Tcs.Task;
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Makes sure that no more <see cref="AspNetServerAction"/>s can be enqueued, waits for all outstanding <see cref="AspNetServerAction"/>s to finish and stops all running WebApplications
    /// </summary>
    public void Dispose()
    {
      _workerBlock.Complete();
      _workerBlock.Completion.Wait();
      Shutdown();
      AppDomain.CurrentDomain.AssemblyResolve -= LoadAssemblyFromPluginFolder;
      ServiceRegistration.Get<ILogger>().Info("AspNetServerService: Shut down.");
    }

    #endregion
  }
}
