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
    protected int _serverPort = 55555; // TODO
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
      //List<IPAddress> validAddresses = new List<IPAddress>();

      _servicePrefix = ResourceHttpAccessUrlUtils.RESOURCE_SERVER_BASE_PATH + Guid.NewGuid().GetHashCode().ToString("X");
      var startOptions = UPnPServer.BuildStartOptions(_servicePrefix, filters);

      //if (settings.UseIPv4)
      //  validAddresses.AddRange(NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetwork, filters));
      //if (settings.UseIPv6)
      //  validAddresses.AddRange(NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetworkV6, filters));

      lock (_syncObj)
      {
        //_serverPort = NetworkHelper.GetFreePort(0);
        //foreach (IPAddress address in validAddresses)
        //{
        //  var bindableAddress = NetworkHelper.TranslateBindableAddress(address);
        //  startOption.Urls.Add($"http://{bindableAddress}:{_serverPort}/");
        //}
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
      return string.Format("http://{0}:{1}{2}", ipAddress, _serverPort, _servicePrefix);
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

    public void AddHttpModule(Type module)
    {
      _middleWares.Add(module);
      if (_httpServer != null)
      {
        StopServer();
        CreateAndStartServer();
      }
    }

    //public void AddAuthenticationModule(AuthenticationModule module)
    //{
    //  _httpServers.Values.ToList().ForEach(x => x.AuthenticationModules.Add(module));
    //}

    public void RemoveHttpModule(Type module)
    {
      _middleWares.Remove(module);
    }

    #endregion
  }
}
