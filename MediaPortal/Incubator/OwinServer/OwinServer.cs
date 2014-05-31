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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Microsoft.Owin.Hosting;
using Owin;

namespace MediaPortal.Plugins.WebServices.OwinServer
{
  /// <summary>
  /// Implementation of <see cref="IOwinServer"/> using the Katana Project.
  /// </summary>
  /// <remarks>
  /// For details on the Katana Project see http://katanaproject.codeplex.com/
  /// All public methods of this class are thread safe.
  /// It allows to start WebApps that can make use of many web technologies such as WebAPI, SignalR, etc.
  /// Although the underlying HttpListener would allow overlapping base paths, we do not to avoid that 
  /// one WebApp (partly) overrides the base path of another WebApp (e.g. WebApp_1 registers base path '/'
  /// and expects that all requests to a port a forwarded to it. If WebApp_2 was allowed to register base
  /// path '/new/' on the same port, requests to '/new/' (or any sub path) on that port would only be
  /// forwarded to WebApp_2.
  /// </remarks>
  public class OwinServer : IOwinServer
  {

    #region Inner classes

    private class OverlappingStringComparer : IEqualityComparer<String>
    {
      public bool Equals(String x, String y)
      {
        return x.StartsWith(y, StringComparison.OrdinalIgnoreCase) ||
               y.StartsWith(x, StringComparison.OrdinalIgnoreCase);
      }

      // obj must start with "http://*:{0}/" where {0} is the port number
      public int GetHashCode(String obj)
      {
        return obj.Substring(0, obj.IndexOf('/', 7)).GetHashCode();
      }
    }

    #endregion

    #region Variables

    /// <summary>
    /// Holds references to the WebApps, which were successfully started by this service.
    /// </summary>
    /// <remarks>
    /// When a WepApp is started, Katana returns an <see cref="IDisposable"/> that represents
    /// the WebApp. To stop the respective WebApp, we have to call <see cref="IDisposable.Dispose()"/>.
    /// </remarks> 
    private readonly ConcurrentDictionary<String, IDisposable> _webApps = new ConcurrentDictionary<String, IDisposable>(new OverlappingStringComparer());

    /// <summary>
    /// Must be entered before accessing <see cref="_isRunning"/> or <see cref="_startingWebApps"/>
    /// </summary>
    private readonly SemaphoreSlim _startupShutdownCoordinator = new SemaphoreSlim(1);
    
    /// <summary>
    /// Holds a Task for each WebApp that is about to startup. Once startup is completed or
    /// failed, the respective Task completes and is removed from <see cref="_startingWebApps"/>
    /// </summary>
    private readonly Dictionary<String, Task> _startingWebApps = new Dictionary<String, Task>();
    
    /// <summary>
    /// <c>true</c> if new WebApps can be started. <c>false</c> after shutdown
    /// </summary>
    private volatile bool _isRunning = true;

    #endregion

    #region Private methods

    /// <summary>
    /// Validates a given base path
    /// </summary>
    /// <param name="basePath">Base path to validate</param>
    /// <returns><c>true</c> if <param name="basePath"></param> is valid, else <c>false</c></returns>
    /// <remarks>
    /// A base path must
    ///   - start with a '/' and end with a '/'
    ///   - consist of any number of path segments separated by '/'
    ///   - not contain characters other than 'a'-'z', 'A'-'Z', '0'-'9', '/', '-' or '_'
    /// If <param name="basePath"></param> is null or an empty string, it is modified to "/" and considered valid
    /// If <param name="basePath"></param> does not end with '/' but is otherwise valid, an '/' is appended.
    /// </remarks>
    private bool ValidateBasePath(ref String basePath)
    {
      if (String.IsNullOrEmpty(basePath))
      {
        basePath = "/";
        return true;
      }

      if (basePath[0] != '/')
        return false;

      for (var i = 1; i < basePath.Length; i++)
      {
        var c = basePath[i];
        // See http://www.ietf.org/rfc/rfc3986.txt
        var safe = (('a' <= c && c <= 'z')
                    || ('A' <= c && c <= 'Z')
                    || ('0' <= c && c <= '9')
                    || c == '/' || c == '-' || c == '_');
        if (!safe)
          return false;
      }

      if (basePath[basePath.Length - 1] != '/')
        basePath += '/';

      return true;
    }

    /// <summary>
    /// Validates a given port number
    /// </summary>
    /// <param name="port">Port number to validate</param>
    /// <returns><c>true</c> if <param name="port"></param> is valid, else <c>false</c></returns>
    private bool ValidatePort(int port)
    {
      return (port > 0 && port < 65536);
    }

    /// <summary>
    /// Returns a properly formatted Url based on the given <param name="port"></param> and <param name="basePath"></param>
    /// </summary>
    /// <param name="port"></param>
    /// <param name="basePath"></param>
    /// <returns></returns>
    private String GetUrl(int port, String basePath)
    {
      return String.Format("http://*:{0}{1}", port, basePath);
    }

    /// <summary>
    /// Starts a WebApp asynchronously and takes care of exception handling
    /// </summary>
    /// <param name="startup">Delegate used to configure the WebApp</param>
    /// <param name="url">Url used to start the WebApp</param>
    /// <returns>An IDisposable representing the WebApp or null, if startup wasn't successful</returns>
    private Task<IDisposable> DoStartWebAppAsync(Action<IAppBuilder> startup, String url)
    {
      return Task.Run(() =>
      {
        var options = new StartOptions(url);
        IDisposable webApp;
        try
        {
          webApp = WebApp.Start(options, startup);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("OwinServer: Error while starting WebApp at '{0}'.", e, url);
          webApp = null;
        }        
        return webApp;
      });
    }

    private Task<bool> DoStopWebAppAsync(IDisposable webApp, String url)
    {
      return Task.Run(() =>
      {
        try
        {
          webApp.Dispose();
          return true;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("OwinServer: Error while stopping WebApp at '{0}'.", e, url);
          return false;
        }        
      });
    }

    #endregion

    #region IOwinServer implementation

    /// <summary>
    /// Starts an Owin WebApp on the given TCP port and the given base path.
    /// </summary>
    /// <remarks>
    /// Most of the examples for Katana use the generic overoad of WebApp.Start, which
    /// requires a type of a Startup class as argument. This would not allow OwinServer
    /// to modify the WebApp configuration because the Startup class is instantiated only
    /// within Katana.
    /// We use a <paramref name="startup"/> delegate instead. This enables OwinServer to
    /// modify the delegate by making it a multicast delegate and adding additional
    /// startup code to be passed to WebApp.Start. That way we can e.g. add a TraceWriter
    /// to every WebApp that is started using OwinServer when we are in Debug mode.
    /// Maybe we should also handle authentication that way using the MP2 user infrastructure
    /// once it is fully implemented so that authentication happens transparently for all
    /// plugins using the OwinServer.
    /// </remarks>
    /// <param name="startup">Delegate that uses the <see cref="IAppBuilder"/> parameter to configure the WebApp.</param>
    /// <param name="port">TCP port on which the WebApp is supposed to listen.</param>
    /// <param name="basePath">Base path at which the WebApp is supposed to listen.</param>
    /// <returns>
    /// A Task that completes when the WebApp has started or failed to start.
    /// The Task's result is <c>true</c> if the WebApp started successfully, else <c>false</c>.
    /// </returns>
    public async Task<bool> TryStartWebAppAsync(Action<IAppBuilder> startup, int port, String basePath)
    {
      ServiceRegistration.Get<ILogger>().Info("OwinServer: Starting WebApp at port {0}, base path '{1}'.", port, basePath);

      // Validate the parameters
      if (startup == null)
      {
        ServiceRegistration.Get<ILogger>().Error("OwinServer: Starting WebApp at port {0}, base path '{1}' failed. Startup delegate must not be null.", port, basePath);
        return false;
      }

      if (!ValidatePort(port))
      {
        ServiceRegistration.Get<ILogger>().Error("OwinServer: Starting WebApp at port {0}, base path '{1}' failed. Port must be between 1 and 65535.", port, basePath);
        return false;
      }

      if (!ValidateBasePath(ref basePath))
      {
        ServiceRegistration.Get<ILogger>().Error("OwinServer: Starting WebApp at port {0}, base path '{1}' failed. Base path invalid.", port, basePath);
        return false;
      }

      // Check whether OwinServer is still running and if so,
      // register that there is one more WepApp about to start.
      var url = GetUrl(port, basePath);
      TaskCompletionSource<object> tcs;
      try
      {
        await _startupShutdownCoordinator.WaitAsync();
        if (!_isRunning)
        {
          ServiceRegistration.Get<ILogger>().Error("OwinServer: Starting WebApp at port {0}, base path '{1}' failed. OwinServer is shut down.", port, basePath);
          return false;
        }
        tcs = new TaskCompletionSource<object>();
        _startingWebApps.Add(url, tcs.Task);
      }
      finally
      {
        _startupShutdownCoordinator.Release();
      }

      // Check for conflicts with other WebApps
      var result = false;
      if (_webApps.TryAdd(url, default(IDisposable)))
      {
        // No conflict - start the WebApp and replace the dummy value in _webApps with the real one,
        // if successful, otherwise remove the dummy
        var webApp = await DoStartWebAppAsync(startup, url);
        if (webApp != null)
        {
          _webApps[url] = webApp;
          ServiceRegistration.Get<ILogger>().Info("OwinServer: WebApp is listening at '{0}'.", url);
          result = true;
        }
        else
          _webApps.TryRemove(url, out webApp);
      }
      else
        ServiceRegistration.Get<ILogger>().Error("OwinServer: Starting WebApp at '{0}' failed. There is already a WebApp running with an overlapping url.", url);

      // Cleanup
      try
      {
        tcs.TrySetResult(null);
        await _startupShutdownCoordinator.WaitAsync();
        _startingWebApps.Remove(url);
      }
      finally
      {
        _startupShutdownCoordinator.Release();
      }

      return result;
    }

    public async Task<bool> TryStopWebAppAsync(int port, String basePath)
    {
      ServiceRegistration.Get<ILogger>().Info("OwinServer: Stopping WebApp at port {0}, base path '{1}'.", port, basePath);

      // Validate the parameters
      if (!ValidatePort(port))
      {
        ServiceRegistration.Get<ILogger>().Error("OwinServer: Stopping WebApp at port {0}, base path '{1}' failed. Port must be between 1 and 65535.", port, basePath);
        return false;
      }

      if (!ValidateBasePath(ref basePath))
      {
        ServiceRegistration.Get<ILogger>().Error("OwinServer: Stopping WebApp at port {0}, base path '{1}' failed. Base path invalid.", port, basePath);
        return false;
      }

      // If the WebApp to stop if currently starting up, wait until it is started.
      String url = GetUrl(port, basePath);
      Task waitForStartup;
      try
      {
        await _startupShutdownCoordinator.WaitAsync();
        _startingWebApps.TryGetValue(url, out waitForStartup);
      }
      finally
      {
        _startupShutdownCoordinator.Release();
      }
      if (waitForStartup != null)
        await waitForStartup;
      
      // Coordinate with a potential parallel shutdown request
      IDisposable webApp;
      try
      {
        await _startupShutdownCoordinator.WaitAsync();
        if (!_isRunning)
        {
          ServiceRegistration.Get<ILogger>().Warn("OwinServer: Stopping WebApp at '{0}' not possible. OwinServer is shut(ting) down.", url);
          return false;
        }
        _webApps.TryRemove(url, out webApp);
      }
      finally
      {
        _startupShutdownCoordinator.Release();
      }
      if (webApp == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("OwinServer: Stopping WebApp failed. There is no WebApp running at '{0}'.", url);
        return false;
      }

      // Stop the WebApp
      if (!await DoStopWebAppAsync(webApp, url))
        return false;
      ServiceRegistration.Get<ILogger>().Info("OwinServer: WebApp stopped at '{0}'.", url);
      return true;
    }

    public async Task ShutdownAsync()
    {
      ServiceRegistration.Get<ILogger>().Info("OwinServer: Shutting down.");

      // Make sure that no WebApps can be started anymore
      try
      {
        await _startupShutdownCoordinator.WaitAsync();
        if (!_isRunning)
        {
          ServiceRegistration.Get<ILogger>().Warn("OwinServer: Shutdown requested althought OwinServer is already shutdown.");
          return;
        }
        _isRunning = false;
      }
      finally
      {
        _startupShutdownCoordinator.Release();
      }

      // Wait until all WebApps currently starting up have started
      await Task.WhenAll(_startingWebApps.Values);

      foreach (var kvp in _webApps)
      {
        if (await DoStopWebAppAsync(kvp.Value, kvp.Key))
          ServiceRegistration.Get<ILogger>().Info("OwinServer: WebApp stopped at '{0}'.", kvp.Key);
      }
      _webApps.Clear();
      ServiceRegistration.Get<ILogger>().Info("OwinServer: Shutdown.");      
    }

    #endregion

  }
}
