#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Core.Threading;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.ServerCommunication.Settings;
using MediaPortal.UI.Services.Shares;
using MediaPortal.UI.Shares;
using UPnP.Infrastructure.CP;
using RelocationMode=MediaPortal.UI.Shares.RelocationMode;

namespace MediaPortal.UI.Services.ServerCommunication
{
  public class ServerConnectionManager : IServerConnectionManager
  {
    protected AsynchronousMessageQueue _messageQueue;
    protected UPnPServerWatcher _serverWatcher = null;
    protected UPnPClientControlPoint _controlPoint = null;
    protected bool _isHomeServerConnected = false;
    protected object _syncObj = new object();

    public ServerConnectionManager()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            SharesMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
      string homeServerSystemId = HomeServerSystemId;
      if (string.IsNullOrEmpty(homeServerSystemId))
        // Watch for all MP-II media servers, if we don't have a homeserver yet
        _serverWatcher = BuildServerWatcher();
      else
        // If we have a homeserver set, we'll try to connect to it
        _controlPoint = BuildClientControlPoint(homeServerSystemId);
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == SharesMessaging.CHANNEL)
      {
        IContentDirectory cd = ContentDirectory;
        if (cd == null)
          return;
        ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
        SharesMessaging.MessageType messageType =
            (SharesMessaging.MessageType) message.MessageType;
        Guid shareId;
        Share share;
        switch (messageType)
        {
          case SharesMessaging.MessageType.ShareAdded:
            shareId = (Guid) message.MessageData[SharesMessaging.SHARE_ID];
            share = sharesManagement.GetShare(shareId);
            if (share != null)
              cd.RegisterShare(share);
            break;
          case SharesMessaging.MessageType.ShareRemoved:
            shareId = (Guid) message.MessageData[SharesMessaging.SHARE_ID];
            cd.RemoveShare(shareId);
            break;
          case SharesMessaging.MessageType.ShareChanged:
            shareId = (Guid) message.MessageData[SharesMessaging.SHARE_ID];
            RelocationMode relocationMode = (RelocationMode) message.MessageData[SharesMessaging.RELOCATION_MODE];
            share = sharesManagement.GetShare(shareId);
            if (share != null)
              cd.UpdateShare(shareId, share.BaseResourcePath, share.Name, share.MediaCategories,
                  relocationMode == RelocationMode.Relocate ? UI.ServerCommunication.RelocationMode.Relocate :
                  UI.ServerCommunication.RelocationMode.ClearAndReImport);
            break;
        }
      }
    }

    static void OnAvailableBackendServersChanged(ICollection<ServerDescriptor> allAvailableServers, bool serversWereAdded)
    {
      ServerConnectionMessaging.SendAvailableServersChangedMessage(allAvailableServers, serversWereAdded);
    }

    void OnBackendServerConnected(DeviceConnection connection)
    {
      ServerDescriptor serverDescriptor = UPnPServerWatcher.GetMPBackendServerDescriptor(connection.RootDescriptor);
      if (serverDescriptor == null)
      {
        ServiceScope.Get<ILogger>().Warn("ServerConnectionManager: Could not connect to home server - Unable to verify UPnP root descriptor");
        return;
      }
      ServiceScope.Get<ILogger>().Info("ServerConnectionManager: Connected to home server '{0}'", serverDescriptor.MPBackendServerUUID);
      lock (_syncObj)
      {
        _isHomeServerConnected = true;
        SaveLastHomeServerData(serverDescriptor);
      }

      ServerConnectionMessaging.SendConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerConnected);
      ServiceScope.Get<IThreadPool>().Add(SynchronizeDataWithServer);
    }

    void OnBackendServerDisconnected(DeviceConnection connection)
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
    /// Synchronously synchronizes all local shares and media item aspect types with the MediaPortal server.
    /// </summary>
    protected void SynchronizeDataWithServer()
    {
      IServerController sc = ServerController;
      ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
      if (sc != null)
        try
        {
          if (!sc.IsClientAttached(systemResolver.LocalSystemId))
            sc.AttachClient(systemResolver.LocalSystemId);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("ServerConnectionManager: Error attaching to home server '{0}'", e, HomeServerSystemId);
          return; // As this is a real error case, we don't need to try any other service calls
        }
      IContentDirectory cd = ContentDirectory;
      if (cd != null)
      {
        try
        {
          ServiceScope.Get<ILogger>().Info("ServerConnectionManager: Synchronizing shares with home server");
          IDictionary<Guid, Share> serverShares = new Dictionary<Guid, Share>();
          foreach (Share share in cd.GetShares(systemResolver.LocalSystemId, SharesFilter.All))
            serverShares.Add(share.ShareId, share);
          IDictionary<Guid, Share> localShares = ServiceScope.Get<ILocalSharesManagement>().Shares;
          foreach (Share localShare in localShares.Values)
            if (!serverShares.ContainsKey(localShare.ShareId))
              cd.RegisterShare(localShare);
          foreach (Guid serverShareId in serverShares.Keys)
            if (!localShares.ContainsKey(serverShareId))
              cd.RemoveShare(serverShareId);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("ServerConnectionManager: Could not synchronize local shares with server", e);
        }
        try
        {
          IMediaItemAspectTypeRegistration miatr = ServiceScope.Get<IMediaItemAspectTypeRegistration>();
          ServiceScope.Get<ILogger>().Info("ServerConnectionManager: Adding unregistered media item aspect types at home server");
          ICollection<Guid> serverMIATypes = cd.GetAllManagedMediaItemAspectMetadataIds();
          foreach (KeyValuePair<Guid, MediaItemAspectMetadata> localMiaType in miatr.LocallyKnownMediaItemAspectTypes)
            if (!serverMIATypes.Contains(localMiaType.Key))
            {
              ServiceScope.Get<ILogger>().Info("ServerConnectionManager: Adding unregistered media item aspect type '{0}' (ID '{1}') at home server",
                  localMiaType.Value.Name, localMiaType.Key);
              cd.AddMediaItemAspectStorage(localMiaType.Value);
            }
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("ServerConnectionManager: Could not synchronize local media item aspect types with server", e);
        }
      }
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
        ServerConnectionSettings settings = ServiceScope.Get<ISettingsManager>().Load<ServerConnectionSettings>();
        return settings.HomeServerSystemId;
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

    public IContentDirectory ContentDirectory
    {
      get
      {
        UPnPClientControlPoint cp;
        lock (_syncObj)
          cp = _controlPoint;
        return cp == null ? null : cp.ContentDirectoryService;
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
      IServerController sc = ServerController;
      ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
      if (sc != null)
        try
        {
          sc.DetachClient(systemResolver.LocalSystemId);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("ServerConnectionManager: Error detaching from home server '{0}'", e, HomeServerSystemId);
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
        settings.HomeServerSystemId = null;
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

    public void SetNewHomeServer(string backendServerSystemId)
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
        // Here, we only set the system ID of the new home server. The server's system ID will remain in the settings
        // until method SetNewHomeServer is called again.
        settings.HomeServerSystemId = backendServerSystemId;
        settingsManager.Save(settings);
        _controlPoint = BuildClientControlPoint(backendServerSystemId);
      }
      _controlPoint.Start(); // Outside the lock
      ServerConnectionMessaging.SendConnectionStateChangedMessage(ServerConnectionMessaging.MessageType.HomeServerAttached);
    }

    #endregion
  }
}
