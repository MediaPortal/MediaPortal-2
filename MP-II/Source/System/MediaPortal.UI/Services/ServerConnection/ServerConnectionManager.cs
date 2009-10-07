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
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Threading;
using MediaPortal.ServerConnection;
using MediaPortal.Services.ServerConnection.Settings;
using MediaPortal.Services.UPnP;
using MediaPortal.Shares;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Services.ServerConnection
{
  public class ServerConnectionManager : IServerConnectionManager
  {
    protected UPnPServerWatcher _serverWatcher = null;
    protected UPnPClientControlPoint _controlPoint = null;
    protected object _syncObj = new object();

    public ServerConnectionManager()
    {
      ServerConnectionSettings serverConnectionSettings = ServiceScope.Get<ISettingsManager>().Load<ServerConnectionSettings>();
      string homeServerUUID = serverConnectionSettings.HomeServerUUID;
      if (string.IsNullOrEmpty(homeServerUUID))
      { // Watch for all MP-II media servers, if we don't have a homeserver yet
        lock (_syncObj)
          _serverWatcher = new UPnPServerWatcher();
        _serverWatcher.AvailableMediaServersChanged += OnAvailableMediaServersChanged;
      }
      else
      { // If we have a homeserver set, we'll try to connect to it
        lock (_syncObj)
          _controlPoint = new UPnPClientControlPoint(homeServerUUID);
        _controlPoint.MediaServerConnected += OnMediaServerConnected;
        _controlPoint.MediaServerDisconnected += OnMediaServerDisconnected;
      }
    }

    static void OnAvailableMediaServersChanged(IDictionary<SystemName, RootDescriptor> allAvailableServers)
    {
      ICollection<AvailableServer> availableServers = new List<AvailableServer>();
      foreach (RootDescriptor serverRootDescriptor in allAvailableServers.Values)
        availableServers.Add(new AvailableServer(serverRootDescriptor));
      ServerConnectionMessaging.SendAvailableServersChangeMessage(availableServers);
    }

    void OnMediaServerConnected(DeviceConnection connection)
    {
      ServerConnectionMessaging.SendConnectionStateChangeMessage(ServerConnectionMessaging.MessageType.HomeServerConnected);
      ServiceScope.Get<IThreadPool>().Add(SynchronizeSharesWithServer);
      // TODO: Call ClientAvailable method at MP server's client manager service
    }

    static void OnMediaServerDisconnected(DeviceConnection connection)
    {
      ServerConnectionMessaging.SendConnectionStateChangeMessage(ServerConnectionMessaging.MessageType.HomeServerDisconnected);
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
        IDictionary<Guid, Share> serverShares = new Dictionary<Guid, Share>();
        foreach (Share share in cds.GetShares(SystemName.GetLocalSystemName(), SharesFilter.All))
          serverShares.Add(share.ShareId, share);
        IDictionary<Guid, Share> localShares = ServiceScope.Get<ILocalSharesManagement>().Shares;
        foreach (Share localShare in localShares.Values)
          if (!serverShares.ContainsKey(localShare.ShareId))
            cds.RegisterShare(localShare);
        foreach (Guid serverShareId in serverShares.Keys)
        {
          if (!localShares.ContainsKey(serverShareId))
            cds.RemoveShare(serverShareId);
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Info("Could not synchronize local shares with server", e);
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
  }
}