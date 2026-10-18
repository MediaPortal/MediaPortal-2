#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading;
using UPnP.Infrastructure.Dv;

namespace UPnP.Infrastructure.Http
{
  /// <summary>
  /// Replaces the http.sys feature that lets several independent web hosts listen on the same TCP port, each
  /// one owning a different URL prefix. http.sys only exists on Windows, so without an equivalent the MP2
  /// server cannot start anywhere else: the resource server and every UPnP device and control point instance
  /// open their own host on port 55555, and a second Kestrel listener on a bound port simply fails to bind.
  /// </summary>
  /// <remarks>
  /// <para>
  /// On the platforms without http.sys all endpoints of a port are folded into one Kestrel listener which
  /// dispatches incoming requests to the endpoint owning the longest matching URL prefix - the same rule
  /// http.sys applies. Prefixes come from the very same URLs that <see cref="HttpServer.BuildUrls"/> builds
  /// for http.sys, so the addresses MP2 advertises (SSDP LOCATION headers, resource access URLs) do not
  /// change between platforms.
  /// </para>
  /// <para>
  /// Requests are dispatched without touching <c>HttpRequest.PathBase</c> and <c>HttpRequest.Path</c>: with
  /// http.sys the path of a request to a host registered for <c>http://+:55555/MyPrefix</c> still contains
  /// <c>/MyPrefix</c> and <c>PathBase</c> stays empty (the resource server relies on that - its MVC route
  /// template contains the prefix). Endpoints therefore see exactly the same request here.
  /// </para>
  /// <para>
  /// A prefix that serves an ASP.NET Core pipeline (MVC controllers and middlewares) has to provide its
  /// services while the listener is built, because the DI container is closed afterwards. Such a
  /// registration must be the first one for its port; plain request handlers can be added and removed while
  /// the listener runs. MP2 matches that ordering: the resource server is started with the core services,
  /// the UPnP servers are started later, from plugin startup.
  /// </para>
  /// </remarks>
  public static class SharedHttpPortHost
  {
    /// <summary>
    /// Endpoint registration: the URL prefix it owns and the delegate handling its requests. The handler is
    /// filled in when the listener is built for endpoints that are served by a pipeline.
    /// </summary>
    private class Endpoint
    {
      public string Prefix;
      public RequestDelegate Handler;
    }

    /// <summary>
    /// All endpoints sharing one TCP port, served by a single Kestrel listener.
    /// </summary>
    private class SharedPort
    {
      /// <summary>
      /// Snapshot of <see cref="_endpoints"/> used by the request dispatcher. Rebuilt on every change so
      /// that requests never see a partially updated list and never need a lock.
      /// </summary>
      private volatile Endpoint[] _dispatchTable = new Endpoint[0];

      private readonly List<Endpoint> _endpoints = new List<Endpoint>();
      private readonly HashSet<IPAddress> _addresses = new HashSet<IPAddress>();
      private readonly int _port;
      private Action<IServiceCollection> _services;
      private Action<IApplicationBuilder> _pipeline;
      private WebApplication _app;

      public int Port { get { return _port; } }

      /// <summary>
      /// Whether the listener of this port can be stopped because no endpoint uses it any more.
      /// </summary>
      public bool IsUnused { get { return _endpoints.Count == 0; } }

      public SharedPort(int port)
      {
        _port = port;
      }

      /// <summary>
      /// Adds the endpoints described by <paramref name="entries"/>. Called with the global lock held.
      /// </summary>
      /// <returns>The endpoints that have to be removed again when the registration is disposed.</returns>
      public List<Endpoint> Add(IList<UrlEntry> entries, Action<IServiceCollection> services,
          Action<IApplicationBuilder> pipeline, RequestDelegate handler)
      {
        if (pipeline != null)
        {
          if (_app != null || _pipeline != null || _endpoints.Count > 0)
            throw new InvalidOperationException(string.Format(
                "Port {0} is already served by a shared HTTP listener with {1} endpoint(s). An endpoint that "
                + "needs its own service collection (MVC controllers, middlewares) has to be registered before "
                + "any other endpoint on that port, because the listener's service provider cannot be extended "
                + "after it has been built.",
                _port, _endpoints.Count));
          _services = services;
          _pipeline = pipeline;
        }

        var added = new List<Endpoint>();
        foreach (UrlEntry entry in entries)
        {
          if (_endpoints.Any(endpoint => SamePrefix(endpoint.Prefix, entry.Prefix)))
            throw new InvalidOperationException(string.Format(
                "An endpoint for the URL prefix '{0}' is already registered on port {1}.", entry.Prefix, _port));
          // Endpoints of a pipeline get their handler when the listener builds the pipeline branch.
          var endpoint = new Endpoint { Prefix = entry.Prefix, Handler = pipeline == null ? handler : null };
          _endpoints.Add(endpoint);
          added.Add(endpoint);
          _addresses.Add(entry.Address);
        }
        UpdateDispatchTable();
        return added;
      }

      /// <summary>
      /// Removes the given endpoints and stops the listener if the port became unused. Called with the global
      /// lock held.
      /// </summary>
      public void Remove(List<Endpoint> endpoints)
      {
        foreach (Endpoint endpoint in endpoints)
          _endpoints.Remove(endpoint);
        UpdateDispatchTable();
        if (_endpoints.Count == 0)
          Stop();
      }

      public void StartIfNeeded()
      {
        if (_app != null)
          return;
        if (_endpoints.Count == 0)
          throw new InvalidOperationException(string.Format("No endpoint is registered for port {0}.", _port));

        // The address sets a listener for this port is started with, in order. Endpoints that were
        // registered by URL bind exactly the address of that URL. A wildcard URL has to make MP2 reachable
        // through every address of the machine, and one dual stack IPv6 socket already accepts the IPv4
        // connections - binding 0.0.0.0 next to it fails with "SocketException (98): Address already in
        // use", measured on Linux. So a wildcard is served by a single socket, preferring the dual stack
        // one and falling back to plain IPv4 where IPv6 cannot be bound, which happens on machines with
        // IPv6 disabled.
        var attempts = new List<IList<IPAddress>>();
        List<IPAddress> explicitAddresses = _addresses.Where(address => !Equals(address, WildcardAddress)).ToList();
        if (_addresses.Contains(WildcardAddress))
        {
          attempts.Add(new List<IPAddress>(explicitAddresses) { IPAddress.IPv6Any });
          attempts.Add(new List<IPAddress>(explicitAddresses) { IPAddress.Any });
        }
        else
          attempts.Add(explicitAddresses);

        Exception failure = null;
        foreach (IList<IPAddress> addresses in attempts)
        {
          try
          {
            Start(addresses);
            return;
          }
          catch (Exception e)
          {
            failure = new IOException(string.Format(
                "The shared HTTP listener of port {0} could not be bound to {1}.", _port,
                string.Join(", ", addresses.Select(address => address + ":" + _port))), e);
          }
        }
        throw failure;
      }

      private void Start(IList<IPAddress> addresses)
      {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
          Args = new string[0],
          ContentRootPath = AppContext.BaseDirectory,
          EnvironmentName = Environments.Production,
        });
        // MP2 logs through its own facilities. Without clearing the providers ASP.NET Core would write its
        // request logging to the console in a format the MP2 log files are not prepared for.
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options =>
        {
          // http.sys is configured with AllowSynchronousIO for the same reason: the XmlSerializer used by the
          // GENA event notification handling reads the request stream synchronously. On Kestrel this is a
          // server option, too.
          options.AllowSynchronousIO = true;
          foreach (IPAddress address in addresses)
            options.Listen(address, _port);
        });

        _services?.Invoke(builder.Services);
        WebApplication app = builder.Build();

        if (_pipeline != null)
        {
          // Build the pipeline of the port owner as a branch. The dispatch table refers to it like to any
          // other endpoint, so it only ever sees requests below its own prefix, exactly like under http.sys.
          IApplicationBuilder branch = ((IApplicationBuilder) app).New();
          _pipeline(branch);
          RequestDelegate pipelineHandler = branch.Build();
          foreach (Endpoint endpoint in _endpoints.Where(endpoint => endpoint.Handler == null))
            endpoint.Handler = pipelineHandler;
          UpdateDispatchTable();
        }

        app.Run(DispatchAsync);
        // The http.sys hosts were started synchronously; the callers rely on the port being bound when
        // Start() returns, because they advertise the URLs immediately afterwards (SSDP alive messages).
        _app = app;
        try
        {
          app.StartAsync().GetAwaiter().GetResult();
        }
        catch (Exception)
        {
          // Release the sockets of a partially started listener, otherwise the next attempt would find the
          // port taken by the listener that just failed.
          Stop();
          throw;
        }
      }

      private void Stop()
      {
        WebApplication app = _app;
        _app = null;
        if (app == null)
          return;
        try
        {
          app.StopAsync().GetAwaiter().GetResult();
        }
        finally
        {
          app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
      }

      private void UpdateDispatchTable()
      {
        _dispatchTable = _endpoints.Where(endpoint => endpoint.Handler != null).ToArray();
      }

      private async Task DispatchAsync(HttpContext context)
      {
        Endpoint match = null;
        foreach (Endpoint endpoint in _dispatchTable)
        {
          if (context.Request.Path.StartsWithSegments(endpoint.Prefix, StringComparison.OrdinalIgnoreCase) &&
              (match == null || endpoint.Prefix.Length > match.Prefix.Length))
            match = endpoint;
        }

        if (match == null)
        {
          // http.sys answers with 404 for a port it listens on but has no URL prefix for.
          context.Response.StatusCode = StatusCodes.Status404NotFound;
          await context.Response.WriteAsync(string.Format(
              "No MP2 HTTP endpoint is registered for the path '{0}'.", context.Request.Path));
          return;
        }
        await match.Handler(context);
      }
    }

    /// <summary>
    /// The address part of a URL that stands for "all addresses of this machine", the way http.sys spells it
    /// in <c>http://+:55555/...</c>.
    /// </summary>
    private static readonly IPAddress WildcardAddress = IPAddress.Any;

    private static readonly Dictionary<int, SharedPort> _ports = new Dictionary<int, SharedPort>();
    private static readonly object _sync = new object();

    private static bool SamePrefix(string first, string second)
    {
      return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers <paramref name="handler"/> for all URL prefixes of <paramref name="urls"/>. If a shared
    /// listener for one of the ports is already running, the endpoint joins it; otherwise the listener is
    /// created and started before the method returns.
    /// </summary>
    /// <param name="urls">URLs as built by <see cref="HttpServer.BuildUrls"/>, including the base path.</param>
    /// <param name="handler">Handles all requests below the registered prefixes.</param>
    /// <returns>Handle that unregisters the endpoints again.</returns>
    public static IDisposable Start(string[] urls, RequestDelegate handler)
    {
      if (handler == null)
        throw new ArgumentNullException(nameof(handler));
      return Register(urls, null, null, handler);
    }

    /// <summary>
    /// Registers an ASP.NET Core pipeline (MVC controllers, middlewares) for all URL prefixes of
    /// <paramref name="urls"/>. Must be the first registration for the port, see the remarks on the class.
    /// </summary>
    public static IDisposable Start(string[] urls, Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configurePipeline)
    {
      if (configurePipeline == null)
        throw new ArgumentNullException(nameof(configurePipeline));
      return Register(urls, configureServices, configurePipeline, null);
    }

    private static IDisposable Register(string[] urls, Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configurePipeline, RequestDelegate handler)
    {
      IList<UrlEntry> entries = ParseUrls(urls);
      int port = entries[0].Port;
      if (entries.Any(entry => entry.Port != port))
        throw new ArgumentException(string.Format(
            "All URLs of one endpoint have to use the same port, got URLs for ports {0}.",
            string.Join(", ", entries.Select(entry => entry.Port.ToString()))), nameof(urls));

      List<Endpoint> added = null;
      SharedPort portEndpoint;
      // One global lock for all ports: registrations are rare (once per MP2 component) and the dispatcher
      // does not take the lock at all.
      lock (_sync)
      {
        if (!_ports.TryGetValue(port, out portEndpoint))
        {
          portEndpoint = new SharedPort(port);
          _ports[port] = portEndpoint;
        }
        try
        {
          added = portEndpoint.Add(entries, configureServices, configurePipeline, handler);
          portEndpoint.StartIfNeeded();
        }
        catch (Exception)
        {
          portEndpoint.Remove(added ?? new List<Endpoint>());
          if (portEndpoint.IsUnused)
            _ports.Remove(port);
          throw;
        }
      }
      return new Registration(portEndpoint, added);
    }

    /// <summary>
    /// Splits the URLs of one endpoint into the address to bind, the port and the URL prefix the endpoint
    /// owns. Written by hand because the wildcard host <c>+</c> is not a host name <see cref="Uri"/> would
    /// accept for all uses.
    /// </summary>
    internal static IList<UrlEntry> ParseUrls(string[] urls)
    {
      if (urls == null || urls.Length == 0)
        throw new ArgumentException("At least one URL is needed to start an HTTP endpoint.", nameof(urls));

      var result = new List<UrlEntry>(urls.Length);
      foreach (string url in urls)
      {
        if (string.IsNullOrEmpty(url))
          throw new ArgumentException("One of the given URLs is empty.", nameof(urls));
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
          throw new NotSupportedException(string.Format(
              "The shared HTTP listener only supports 'http://' URLs so far, got '{0}'. Secured URLs need a "
              + "certificate which has to be configured for the listener of the port.", url));

        int addressStart = "http://".Length;
        int authorityEnd;
        if (url[addressStart] == '[')
        {
          int closingBracket = url.IndexOf(']', addressStart);
          if (closingBracket < 0)
            throw new ArgumentException(string.Format("The URL '{0}' has an unmatched '['.", url), nameof(urls));
          authorityEnd = closingBracket + 1;
        }
        else
        {
          int slash = url.IndexOf('/', addressStart);
          int colon = url.IndexOf(':', addressStart);
          authorityEnd = colon >= 0 && (slash < 0 || colon < slash) ? colon : (slash < 0 ? url.Length : slash);
        }

        string host = url.Substring(addressStart, authorityEnd - addressStart).Trim('[', ']');
        int port = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
        if (authorityEnd < url.Length && url[authorityEnd] == ':')
        {
          int pathStart = url.IndexOf('/', authorityEnd);
          string portText = pathStart < 0 ? url.Substring(authorityEnd + 1) : url.Substring(authorityEnd + 1, pathStart - authorityEnd - 1);
          if (!int.TryParse(portText, out port) || port < 0 || port > 0xFFFF)
            throw new ArgumentException(string.Format("The URL '{0}' does not contain a valid port.", url), nameof(urls));
        }

        int prefixStart = url.IndexOf('/', authorityEnd);
        string prefix = prefixStart < 0 ? string.Empty : url.Substring(prefixStart).TrimEnd('/');

        IPAddress address = host.Length == 0 || host == "+" || host == "*" || host == "localhost"
            ? WildcardAddress
            : ParseAddress(host);
        result.Add(new UrlEntry(address, port, prefix));
      }
      return result;
    }

    private static IPAddress ParseAddress(string host)
    {
      IPAddress address;
      if (!IPAddress.TryParse(host, out address))
        throw new NotSupportedException(string.Format(
            "The shared HTTP listener needs an IP address to bind, but the URL contains the host name '{0}'.", host));
      return address;
    }

    /// <summary>
    /// One parsed URL: the address to bind, the port and the URL prefix owned by the endpoint.
    /// </summary>
    internal struct UrlEntry
    {
      public UrlEntry(IPAddress address, int port, string prefix)
      {
        Address = address;
        Port = port;
        Prefix = prefix;
      }

      public IPAddress Address;
      public int Port;
      public string Prefix;
    }

    /// <summary>
    /// Handle returned to the registrant. Unregisters its endpoints; the shared listener of the port is
    /// stopped when the last endpoint of that port is gone.
    /// </summary>
    private class Registration : IDisposable
    {
      private readonly SharedPort _port;
      private List<Endpoint> _endpoints;

      public Registration(SharedPort port, List<Endpoint> endpoints)
      {
        _port = port;
        _endpoints = endpoints;
      }

      public void Dispose()
      {
        List<Endpoint> endpoints = System.Threading.Interlocked.Exchange(ref _endpoints, null);
        if (endpoints == null)
          return;
        lock (_sync)
        {
          _port.Remove(endpoints);
          if (_port.IsUnused)
            _ports.Remove(_port.Port);
        }
      }
    }
  }
}
