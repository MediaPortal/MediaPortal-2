#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Threading;
using MediaPortal.ServerCommunication;
using MediaPortal.Services.ServerConnection.Settings;
using MediaPortal.Shares;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Services.ServerConnection
{
  public class ServerConnectionManager : IServerConnectionManager
  {
    protected UPnPServerWatcher _serverWatcher = null;
    protected UPnPClientControlPoint _controlPoint = null;
    protected bool _isHomeServerConnected = false;
    protected object _syncObj = new object();

    public ServerConnectionManager()
    {
      string homeServerUUID = HomeServerUUID;
      if (string.IsNullOrEmpty(homeServerUUID))
        // Watch for all MP-II media servers, if we don't have a homeserver yet
        _serverWatcher = BuildServerWatcher();
      else
        // If we have a homeserver set, we'll try to connect to it
        _controlPoint = BuildClientControlPoint(homeServerUUID);
    }

    static void OnAvailableMediaServersChanged(ICollection<ServerDescriptor> allAvailableServers, bool serversWereAdded)
    {
      ServerConnectionMessaging.SendAvailableServersChangedMessage(allAvailableServers, serversWereAdded);
    }

    void OnMediaServerConnected(DeviceConnection connection)
    {
      ServerDescriptor serverDescriptor = UPnPServerWatcher.GetMPMediaServerDescriptor(connection.RootDescriptor);
      lock (_syncObj)
      {
        _isHomeServerConnected = true;
        if (serverDescriptor != null)
          SaveLastHomeServerData(serverDescriptor);
      }

      ServerConnectionMessaging.SendConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerConnected);
      ServiceScope.Get<IThreadPool>().Add(SynchronizeSharesWithServer);
      // TODO: Call ClientAvailable method at MP server's client manager service
    }

    void OnMediaServerDisconnected(DeviceConnection connection)
    {
      lock (_syncObj)
        _isHomeServerConnected = false;
      ServerConnectionMessaging.SendConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerDisconnected);
    }

    /// <summary>
    /// When a home server is connected, we store the connection data of the server to be able to
    /// provide the home server's data also when the connection is down. We'll refresh the data each time
    /// the server is connected to track changes in the server's location, name, ...
    /// <summary>
    protected static void SaveLastHomeServerData(ServerDescriptor serverDescriptor)
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
      settings.LastHomeServerName = serverDescriptor.ServerName;
      settings.LastHomeServerSystem = serverDescriptor.System;
      settingsManager.Save(settings);
    }

    protected UPnPServerWatcher BuildServerWatcher()
    {
      UPnPServerWatcher result = new UPnPServerWatcher();
      result.AvailableMediaServersChanged += OnAvailableMediaServersChanged;
      return result;
    }

    protected UPnPClientControlPoint BuildClientControlPoint(string homeServerUUID)
    {
      UPnPClientControlPoint result = new UPnPClientControlPoint(homeServerUUID);
      result.MediaServerConnected += OnMediaServerConnected;
      result.MediaServerDisconnected += OnMediaServerDisconnected;
      return result;
    }

    /// <summary>
    /// Synchronously synchronizes all local shares with the MediaPortal server.
    /// </summary>
    protected void SynchronizeSharesWithServer()
    {
      try
      {
        UPnPContentDirectoryService cds = ContentDirectoryService;
        if (cds == null)
          return;
        ServiceScope.Get<ILogger>().Info("ServerConnectionManager: Synchronizing shares with home server");
        IDictionary<Guid, Share> serverShares = new Dictionary<Guid, Share>();
        foreach (Share share in cds.GetShares(SystemName.GetLocalSystemName(), SharesFilter.All))
          serverShares.Add(share.ShareId, share);
        IDictionary<Guid, Share> localShares = ServiceScope.Get<ILocalSharesManagement>().Shares;
        foreach (Share localShare in localShares.Values)
          if (!serverShares.ContainsKey(localShare.ShareId))
            cds.RegisterShare(localShare);
        foreach (Guid serverShareId in serverShares.Keys)
          if (!localShares.ContainsKey(serverShareId))
            cds.RemoveShare(serverShareId);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("ServerConnectionManager: Could not synchronize local shares with server", e);
      }
    }

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

    public string HomeServerUUID
    {
      get
      {
        ServerConnectionSettings settings = ServiceScope.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.HomeServerUUID;
      }
    }

    public string LastHomeServerName
    {
      get
      {
        ServerConnectionSettings settings = ServiceScope.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.LastHomeServerName;
      }
    }

    public SystemName LastHomeServerSystem
    {
      get
      {
        ServerConnectionSettings settings = ServiceScope.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.LastHomeServerSystem;
      }
    }

    public UPnPContentDirectoryService ContentDirectoryService
    {
      get
      {
        lock (_syncObj)
          return _controlPoint.ContentDirectoryService;
      }
    }

    public void Startup()
    {
      lock (_syncObj)
      {
        if (_serverWatcher != null)
          _serverWatcher.Start();
        if (_controlPoint != null)
          _controlPoint.Start();
      }
    }

    public void Shutdown()
    {
      lock (_syncObj)
      {
        if (_serverWatcher != null)
          _serverWatcher.Stop();
        if (_controlPoint != null)
          _controlPoint.Stop();
      }
    }

    public void DetachFromHomeServer()
    {
      ServiceScope.Get<ILogger>().Info("ServerConnectionManager: Detaching from home server");
      UPnPClientControlPoint cp;
      lock (_syncObj)
        cp = _controlPoint;
      if (cp != null)
        cp.Stop(); // Must be outside the lock - sends messages
      lock (_syncObj)
      {
        ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
        ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
        settings.HomeServerUUID = null;
        settings.LastHomeServerName = null;
        settings.LastHomeServerSystem = null;
        settingsManager.Save(settings);
        _controlPoint = null;
      }
      ServerConnectionMessaging.SendConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerDetached);
      if (_serverWatcher == null)
      {
        lock (_syncObj)
          _serverWatcher = BuildServerWatcher();
        _serverWatcher.Start(); // Outside the lock
      }
    }

    public void SetNewHomeServer(string mpMediaServerUUID)
    {
      lock (_syncObj)
        if (_serverWatcher != null)
        {
          _serverWatcher.Stop();
          _serverWatcher = null;
        }
      UPnPClientControlPoint cp;
      lock (_syncObj)
        cp = _controlPoint;
      if (cp != null)
        cp.Stop(); // Must be outside the lock - sends messages
      lock (_syncObj)
      {
        ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
        ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
        // Here, we only set the UUID of the new home server. The server's UUID will remain in the settings
        // until method SetNewHomeServer is called again.
        settings.HomeServerUUID = mpMediaServerUUID;
        settingsManager.Save(settings);
        _controlPoint = BuildClientControlPoint(mpMediaServerUUID);
      }
      _controlPoint.Start(); // Outside the lock
      ServerConnectionMessaging.SendConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerAttached);
    }
  }
}
