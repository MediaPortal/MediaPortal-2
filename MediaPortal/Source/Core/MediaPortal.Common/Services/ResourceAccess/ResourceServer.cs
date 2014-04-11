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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HttpServer;
using HttpServer.HttpModules;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceServer : IResourceServer, IDisposable
  {
    internal class HttpLogWriter : ILogWriter
    {
      public void Write(object source, LogPrio priority, string message)
      {
        string msg = source + ": " + message;
        ILogger logger = ServiceRegistration.Get<ILogger>();
        switch (priority)
        {
          case LogPrio.Trace:
            // Don't write trace messages (we don't support a trace level in MP - would have to map it to debug level)
            break;
          case LogPrio.Debug:
            logger.Debug(msg);
            break;
          case LogPrio.Info:
            logger.Info(msg);
            break;
          case LogPrio.Warning:
            logger.Warn(msg);
            break;
          case LogPrio.Error:
            logger.Error(msg);
            break;
          case LogPrio.Fatal:
            logger.Critical(msg);
            break;
        }
      }
    }

    protected readonly List<HttpServer.HttpServer> _httpServers;

    public ResourceServer()
    {
      _httpServers = new List<HttpServer.HttpServer>();
      ResourceAccessModule module = new ResourceAccessModule();
      AddHttpModule(module);
    }

    public void Dispose()
    {
      StopServers();
      DisposeServers();
    }

    public void StartServers()
    {
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      List<string> filters = settings.IPAddressBindingsList;
      if (settings.UseIPv4)
        try
        {
          foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetwork, filters))
          {
            HttpServer.HttpServer httpServerV4 = new HttpServer.HttpServer(new HttpLogWriter());
            httpServerV4.Start(address, settings.HttpServerPort);
            ServiceRegistration.Get<ILogger>().Info("ResourceServer: Started HTTP server (IPv4) on address {0} at port {1}", address, httpServerV4.Port);
            _httpServers.Add(httpServerV4);
          }
        }
        catch (SocketException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error starting HTTP server (IPv4)", e);
        }
      if (settings.UseIPv6)
        try
        {
          foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetworkV6, filters))
          {
            HttpServer.HttpServer httpServerV6 = new HttpServer.HttpServer(new HttpLogWriter());
            httpServerV6.Start(address, settings.HttpServerPort);
            ServiceRegistration.Get<ILogger>().Info("ResourceServer: Started HTTP server (IPv6) on address {0} at port {1}", address, httpServerV6.Port);
            _httpServers.Add(httpServerV6);
          }
        }
        catch (SocketException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error starting HTTP server (IPv6)", e);
        }
    }

    private void StopServer(HttpServer.HttpServer server)
    {
      try
      {
        server.Stop();
      }
      catch (SocketException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error stopping HTTP server", e);
      }
    }

    public void StopServers()
    {
      _httpServers.ForEach(StopServer);
    }

    public void DisposeServer(HttpServer.HttpServer server)
    {
      try
      {
        server.Dispose();
      }
      catch (SocketException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error stopping HTTP server", e);
      }
    }

    public void DisposeServers()
    {
      _httpServers.ForEach(DisposeServer);
    }

    #region IResourceServer implementation

    public int PortIPv4
    {
      get
      {
        var server = _httpServers.FirstOrDefault(s => s.IsIPv4);
        return server != null ? server.Port : 0;
      }
    }

    public int PortIPv6
    {
      get
      {
        var server = _httpServers.FirstOrDefault(s => s.IsIPv6);
        return server != null ? server.Port : 0;
      }
    }

    public void Startup()
    {
      StartServers();
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: Shutting down HTTP servers");
      StopServers();
    }

    public void RestartHttpServers()
    {
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: Restarting HTTP servers");
      StopServers();
      StartServers();
    }

    public void AddHttpModule(HttpModule module)
    {
      _httpServers.ForEach(x => x.Add(module));
    }

    public void RemoveHttpModule(HttpModule module)
    {
      _httpServers.ForEach(x => x.Remove(module));
    }

    #endregion
  }
}