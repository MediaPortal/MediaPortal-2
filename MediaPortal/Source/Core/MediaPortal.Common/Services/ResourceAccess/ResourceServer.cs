#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Net;
using System.Net.Sockets;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using Microsoft.Owin.Hosting;
using UPnP.Infrastructure.Dv;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceServer : IResourceServer, IDisposable
  {
    protected readonly List<Type> _middleWares = new List<Type>();
    protected IDisposable _httpServer;
    protected int _serverPort = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
    protected readonly object _syncObj = new object();
    protected string _servicePrefix;

    public ResourceServer()
    {
      AddHttpModule(typeof(ResourceAccessModule));
      CreateAndStartServer();
    }

    private void CreateAndStartServer()
    {
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      List<string> filters = settings.IPAddressBindingsList;

      _servicePrefix = ResourceHttpAccessUrlUtils.RESOURCE_SERVER_BASE_PATH + Guid.NewGuid().GetHashCode().ToString("X");
      var startOptions = UPnPServer.BuildStartOptions(_servicePrefix, filters);

      lock (_syncObj)
      {
        _httpServer = WebApp.Start(startOptions, builder =>
        {
          foreach (Type middleWareType in _middleWares)
          {
            builder.Use(middleWareType);
          }
        });
      }
    }

    public void Dispose()
    {
      StopServer();
    }

    private void StopServer()
    {
      try
      {
        lock (_syncObj)
        {
          _httpServer?.Dispose();
          _httpServer = null;
        }
      }
      catch (SocketException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error stopping HTTP server", e);
      }
    }

    #region IResourceServer implementation

    public string GetServiceUrl(IPAddress ipAddress)
    {
      return ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ?
        string.Format("http://[{0}]:{1}{2}", RemoveScope(ipAddress.ToString()), _serverPort, _servicePrefix) :
        string.Format("http://{0}:{1}{2}", ipAddress, _serverPort, _servicePrefix);
    }

    private string RemoveScope(string ipAddress)
    {
      // %x is appended, but if we like to connect this address, we need to remove this scope identifier
      var SCOPE_DELIMITER = "%";
      if (!ipAddress.Contains(SCOPE_DELIMITER))
        return ipAddress;
      return ipAddress.Substring(0, ipAddress.IndexOf(SCOPE_DELIMITER));
    }

    public int GetPortForIP(IPAddress ipAddress)
    {
      // We use only one server that binds to multiple addresses
      return _serverPort;
    }

    public void Startup()
    {
      CreateAndStartServer();
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: Shutting down HTTP servers");
      StopServer();
    }

    public void RestartHttpServers()
    {
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: Restarting HTTP servers");
      StopServer();
      CreateAndStartServer();
    }

    public void AddHttpModule(Type moduleType)
    {
      _middleWares.Add(moduleType);
      if (_httpServer != null)
      {
        // Note: the Owin pipeline is not designed to allow dynamic changes, so we have to rebuild it completely.
        StopServer();
        CreateAndStartServer();
      }
    }

    public void RemoveHttpModule(Type moduleType)
    {
      _middleWares.Remove(moduleType);
    }

    #endregion
  }
}
