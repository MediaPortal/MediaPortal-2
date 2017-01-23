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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.Threading;
using MediaPortal.ServiceMonitor.UPNP.Settings;
using UPnP.Infrastructure.CP;
using ILogger = MediaPortal.Common.Logging.ILogger;
using UPnP.Infrastructure;

namespace MediaPortal.ServiceMonitor.UPNP
{
  public class ServerConnectionManager : IServerConnectionManager
  {
    protected UPnPServerWatcher _serverWatcher = null;
    protected UPnPClientControlPoint _controlPoint = null;
    protected bool _isHomeServerConnected = false;
    protected ICollection<Guid> _currentlyImportingSharesProxy = new List<Guid>();
    protected object _syncObj = new object();

    public ServerConnectionManager()
    {
      IsStarted = false;

      ServerSettings serverSettings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      UPnPConfiguration.USE_IPV4 = serverSettings.UseIPv4;
      UPnPConfiguration.USE_IPV6 = serverSettings.UseIPv6;
      UPnPConfiguration.IP_ADDRESS_BINDINGS = serverSettings.IPAddressBindingsList;

      string homeServerSystemId = HomeServerSystemId;
      if (string.IsNullOrEmpty(homeServerSystemId))
        // Watch for all MP 2 media servers, if we don't have a homeserver yet
        _serverWatcher = BuildServerWatcher();
      else
        // If we have a homeserver set, we'll try to connect to it
        _controlPoint = BuildClientControlPoint(homeServerSystemId);
    }

    static void OnAvailableBackendServersChanged(ICollection<ServerDescriptor> allAvailableServers, bool serversWereAdded)
    {
      ServerConnectionMessaging.SendAvailableServersChangedMessage(allAvailableServers, serversWereAdded);

      if (allAvailableServers.Count < 1) return;
      var scm = ServiceRegistration.Get<IServerConnectionManager>();
      var availableServersUUID = allAvailableServers.Select(sd => sd.MPBackendServerUUID).ToList();
      scm.SetNewHomeServer(availableServersUUID[0]);
    }

    private void OnBackendServerConnected(DeviceConnection connection)
    {
      ServerDescriptor serverDescriptor = ServerDescriptor.GetMPBackendServerDescriptor(connection.RootDescriptor);
      if (serverDescriptor == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("ServerConnectionManager: Could not connect to home server - Unable to verify UPnP root descriptor");
        return;
      }
      SystemName preferredLink = serverDescriptor.GetPreferredLink();
      ServiceRegistration.Get<ILogger>().Info("ServerConnectionManager: Connected to home server '{0}' at host '{1}' (IP address: '{2}')", serverDescriptor.MPBackendServerUUID, preferredLink.HostName, preferredLink.Address);
      lock (_syncObj)
      {
        _isHomeServerConnected = true;
        SaveLastHomeServerData(serverDescriptor);
      }

      ServerConnectionMessaging.SendServerConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerConnected);
      ServiceRegistration.Get<IThreadPool>().Add(CompleteServerConnection);
    }

    private void OnBackendServerDisconnected(DeviceConnection connection)
    {
      lock (_syncObj)
        _isHomeServerConnected = false;
      ServerConnectionMessaging.SendServerConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerDisconnected);
    }

    protected internal UPnPContentDirectoryServiceProxy ContentDirectoryServiceProxy
    {
      get
      {
        UPnPClientControlPoint cp;
        lock (_syncObj)
          cp = _controlPoint;
        return cp == null ? null : cp.ContentDirectoryService;
      }
    }

    protected internal UPnPServerControllerServiceProxy ServerControllerServiceProxy
    {
      get
      {
        UPnPClientControlPoint cp;
        lock (_syncObj)
          cp = _controlPoint;
        return cp == null ? null : cp.ServerControllerService;
      }
    }

    /// <summary>
    /// When a home server is connected, we store the connection data of the server to be able to
    /// provide the home server's data also when the connection is down. We'll refresh the data each time
    /// the server is connected to track changes in the server's location, name, ...
    /// </summary>
    protected static void SaveLastHomeServerData(ServerDescriptor serverDescriptor)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
      settings.LastHomeServerName = serverDescriptor.ServerName;
      settings.LastHomeServerSystem = serverDescriptor.GetPreferredLink();
      settingsManager.Save(settings);
    }

    protected UPnPServerWatcher BuildServerWatcher()
    {
      UPnPServerWatcher result = new UPnPServerWatcher();
      result.AvailableBackendServersChanged += OnAvailableBackendServersChanged;
      return result;
    }

    protected UPnPClientControlPoint BuildClientControlPoint(string homeServerSystemId)
    {
      UPnPClientControlPoint result = new UPnPClientControlPoint(homeServerSystemId);
      result.BackendServerConnected += OnBackendServerConnected;
      result.BackendServerDisconnected += OnBackendServerDisconnected;
      return result;
    }

    /// <summary>
    /// Registers event handlers to get notifications about client connections.
    /// </summary>
    protected void CompleteServerConnection()
    {
      UPnPServerControllerServiceProxy sc = ServerControllerServiceProxy;
      if (sc != null)
      {
        // Register state variables change events
        sc.AttachedClientsChanged += OnAttachedClientsChanged;
        sc.ConnectedClientsChanged += OnConnectedClientsChanged;
      }
    }

    private static void OnAttachedClientsChanged()
    {
      // Not implemented because this event isn't really interesting for the MP2 client yet
    }

    private static void OnConnectedClientsChanged()
    {
      ServerConnectionMessaging.SendClientConnectionStateChangedMessage();
    }

    #region IServerCommunicationManager implementation

    public ICollection<ServerDescriptor> AvailableServers
    {
      get
      {
        lock (_syncObj)
          if (_serverWatcher != null)
            return new List<ServerDescriptor>(_serverWatcher.AvailableServers);
        return null;
      }
    }

    public bool IsHomeServerConnected
    {
      get
      {
        lock (_syncObj)
          return _isHomeServerConnected;
      }
    }

    public string HomeServerSystemId
    {
      get
      {
        ServerConnectionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.HomeServerSystemId;
      }
    }

    public string LastHomeServerName
    {
      get
      {
        ServerConnectionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.LastHomeServerName;
      }
    }

    public SystemName LastHomeServerSystem
    {
      get
      {
        ServerConnectionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.LastHomeServerSystem;
      }
    }

    public IContentDirectory ContentDirectory
    {
      get { return ContentDirectoryServiceProxy; }
    }

    public IResourceInformationService ResourceInformationService
    {
      get
      {
        UPnPClientControlPoint cp;
        lock (_syncObj)
          cp = _controlPoint;
        return cp == null ? null : cp.ResourceInformationService;
      }
    }

    public IServerController ServerController
    {
      get
      {
        UPnPClientControlPoint cp;
        lock (_syncObj)
          cp = _controlPoint;
        return cp == null ? null : cp.ServerControllerService;
      }
    }

    public UPnPClientControlPoint ControlPoint
    {
      get { return _controlPoint; }
    }

    public bool IsStarted { get; set; }

    public void Startup()
    {
      UPnPServerWatcher watcher;
      UPnPClientControlPoint cp;
      lock (_syncObj)
      {
        IsStarted = true;
        watcher = _serverWatcher;
        cp = _controlPoint;
      }
      if (watcher != null)
        watcher.Start();
      if (cp != null)
        cp.Start();
    }

    public void Shutdown()
    {
      UPnPServerWatcher watcher;
      UPnPClientControlPoint cp;
      lock (_syncObj)
      {
        IsStarted = false;
        watcher = _serverWatcher;
        cp = _controlPoint;
      }
      if (watcher != null)
        watcher.Stop();
      if (cp != null)
        cp.Stop();
    }

    public void DetachFromHomeServer()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
      ServiceRegistration.Get<ILogger>().Info("ServerConnectionManager: Detaching from home server '{0}'", settings.HomeServerSystemId);

      UPnPServerControllerServiceProxy sc = ServerControllerServiceProxy;
      if (sc != null)
        try
        {
          sc.AttachedClientsChanged -= OnAttachedClientsChanged;
          sc.ConnectedClientsChanged -= OnConnectedClientsChanged;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ServerConnectionManager: Error detaching from home server '{0}'", e, HomeServerSystemId);
        }

      ServiceRegistration.Get<ILogger>().Debug("ServerConnectionManager: Closing server connection");
      UPnPClientControlPoint cp;
      lock (_syncObj)
        cp = _controlPoint;
      if (cp != null)
        cp.Stop(); // Must be outside the lock - sends messages
      lock (_syncObj)
      {
        settings.HomeServerSystemId = null;
        settings.LastHomeServerName = null;
        settings.LastHomeServerSystem = null;
        settingsManager.Save(settings);
        _controlPoint = null;
      }
      ServerConnectionMessaging.SendServerConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerDetached);

      ServiceRegistration.Get<ILogger>().Debug("ServerConnectionManager: Starting to watch for MediaPortal servers");
      if (_serverWatcher == null)
      {
        lock (_syncObj)
          _serverWatcher = BuildServerWatcher();
        _serverWatcher.Start(); // Outside the lock
      }
    }

    public void SetNewHomeServer(string backendServerSystemId)
    {
      ServiceRegistration.Get<ILogger>().Info("ServerConnectionManager: Attaching to MediaPortal backend server '{0}'", backendServerSystemId);

      ServiceRegistration.Get<ILogger>().Debug("ServerConnectionManager: Stopping to watch for MediaPortal servers");
      lock (_syncObj)
        if (_serverWatcher != null)
        {
          _serverWatcher.Stop();
          _serverWatcher = null;
        }

      ServiceRegistration.Get<ILogger>().Debug("ServerConnectionManager: Building UPnP control point for communication with the new home server");
      UPnPClientControlPoint cp;
      lock (_syncObj)
        cp = _controlPoint;
      if (cp != null)
        cp.Stop(); // Must be outside the lock - sends messages
      lock (_syncObj)
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
        // Here, we only set the system ID of the new home server. The server's system ID will remain in the settings
        // until method SetNewHomeServer is called again.
        settings.HomeServerSystemId = backendServerSystemId;
        settingsManager.Save(settings);
        _controlPoint = BuildClientControlPoint(backendServerSystemId);
      }
      _controlPoint.Start(); // Outside the lock
      ServerConnectionMessaging.SendServerConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerAttached);
    }

    #endregion
  }
}