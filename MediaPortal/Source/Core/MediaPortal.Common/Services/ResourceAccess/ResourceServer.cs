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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HttpServer;
using HttpServer.Authentication;
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

    protected readonly IDictionary<IPAddress, HttpServer.HttpServer> _httpServers;

    public ResourceServer()
    {
      _httpServers = new Dictionary<IPAddress, HttpServer.HttpServer>();
      CreateServers();
      ResourceAccessModule module = new ResourceAccessModule();
      AddHttpModule(module);
    }

    private void CreateServers()
    {
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      List<string> filters = settings.IPAddressBindingsList;
      List<IPAddress> validAddresses = new List<IPAddress>();

      if (settings.UseIPv4)
        validAddresses.AddRange(NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetwork, filters));
      if (settings.UseIPv6)
        validAddresses.AddRange(NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetworkV6, filters));

      foreach (IPAddress address in validAddresses)
      {
        HttpServer.HttpServer httpServer = new HttpServer.HttpServer(new HttpLogWriter());
        _httpServers[address] = httpServer;
      }
    }

    public void Dispose()
    {
      StopServers();
      DisposeServers();
    }

    public void StartServers()
    {
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      string addressType = string.Empty;
      foreach (KeyValuePair<IPAddress, HttpServer.HttpServer> addressServerPair in _httpServers)
      {
        try
        {
          var address = addressServerPair.Key;
          var server = addressServerPair.Value;
          addressType = address.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6";
          server.Start(address, settings.HttpServerPort);
          ServiceRegistration.Get<ILogger>().Info("ResourceServer: Started HTTP server ({0}) on address {1} at port {2}", addressType, address, server.Port);
        }
        catch (SocketException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error starting HTTP server ({0})", e, addressType);
        }
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
      _httpServers.Values.ToList().ForEach(StopServer);
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
      _httpServers.Values.ToList().ForEach(DisposeServer);
      _httpServers.Clear();
    }

    #region IResourceServer implementation

    public int GetPortForIP(IPAddress ipAddress)
    {
      HttpServer.HttpServer server;
      if (_httpServers.TryGetValue(ipAddress, out server))
        return server.Port;

      server = _httpServers.Values.FirstOrDefault(s => ipAddress.AddressFamily == AddressFamily.InterNetwork ? s.IsIPv4 : s.IsIPv6);
      return server != null ? server.Port : 0;
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
      _httpServers.Values.ToList().ForEach(x => x.Add(module));
    }

    public void AddAuthenticationModule(AuthenticationModule module)
    {
      _httpServers.Values.ToList().ForEach(x => x.AuthenticationModules.Add(module));
    }

    public void RemoveHttpModule(HttpModule module)
    {
      _httpServers.Values.ToList().ForEach(x => x.Remove(module));
    }

    #endregion
  }
}
