#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.UPnP;
using UPnP.Infrastructure.CP.SSDP;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  public delegate void ClientStateChangedDlgt(ClientDescriptor client);

  /// <summary>
  /// Tracks all available MP clients and the connection state of all attached clients.
  /// </summary>
  public class UPnPServerControlPoint
  {
    protected ICollection<ClientDescriptor> _availableClients = new List<ClientDescriptor>();
    protected ICollection<string> _attachedClientSystemIds = new List<string>();
    protected IDictionary<string, ClientConnection> _clientConnections = new Dictionary<string, ClientConnection>();
    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;

    public UPnPServerControlPoint()
    {
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _controlPoint = new UPnPControlPoint(_networkTracker);
      _networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
      _networkTracker.RootDeviceRemoved += OnUPnPRootDeviceRemoved;
    }

    /// <summary>
    /// Gets all MP clients which are available in the network.
    /// </summary>
    public ICollection<ClientDescriptor> AvailableClients
    {
      get
      {
        lock (_networkTracker.SharedControlPointData.SyncObj)
          return new List<ClientDescriptor>(_availableClients);
      }
    }

    /// <summary>
    /// Gets the system IDs of all clients which are attached to this server.
    /// </summary>
    public ICollection<string> AttachedClientSystemIds
    {
      get
      {
        lock (_networkTracker.SharedControlPointData.SyncObj)
          return new List<string>(_attachedClientSystemIds);
      }
      set
      {
        lock (_networkTracker.SharedControlPointData.SyncObj)
          _attachedClientSystemIds = new List<string>(value);
      }
    }

    /// <summary>
    /// Gets a mapping of client system IDs to client connection objects for all clients which are currently connected.
    /// If only one client connection should be retrieved by system ID, it is better to call method
    /// <see cref="GetClientConnection"/>.
    /// </summary>
    public IDictionary<string, ClientConnection> ClientConnections
    {
      get
      {
        lock (_networkTracker.SharedControlPointData.SyncObj)
          return new Dictionary<string, ClientConnection>(_clientConnections);
      }
    }

    public event ClientStateChangedDlgt ClientAvailable;
    public event ClientStateChangedDlgt ClientUnavailable;
    public event ClientStateChangedDlgt ClientConnected;
    public event ClientStateChangedDlgt ClientDisconnected;

    public void Start()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts, else the network tracker could fire events before the control point is ready
      _networkTracker.Start();
    }

    public void Stop()
    {
      _clientConnections.Clear();
      _availableClients.Clear();
      _networkTracker.Close();
      _controlPoint.Close();
    }

    public ClientConnection GetClientConnection(string systemId)
    {
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        ClientConnection result;
        if (_clientConnections.TryGetValue(systemId, out result))
          return result;
        return null;
      }
    }

    public void AddAttachedClient(string systemId)
    {
      ClientDescriptor availableClientDescriptor = null;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        _attachedClientSystemIds.Add(systemId);
        // Check if the attached client is available in the network...
        foreach (ClientDescriptor client in _availableClients)
          if (client.MPFrontendServerUUID == systemId)
          {
            availableClientDescriptor = client;
            break;
          }
      }
      if (availableClientDescriptor != null)
        // ... and connect to it
        CheckConnect(availableClientDescriptor);
    }

    public void RemoveAttachedClient(string systemId)
    {
      ClientConnection connectionToDisconnect;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        _attachedClientSystemIds.Remove(systemId);
        if (!_clientConnections.TryGetValue(systemId, out connectionToDisconnect))
          return;
        _clientConnections.Remove(systemId);
      }
      if (connectionToDisconnect != null)
        try
        {
          connectionToDisconnect.Disconnect();
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Info("UPnPServerControlPoint: Error while disconnecting from client '{0}'", e, systemId);
        }
    }

    public static ClientDescriptor GetMPFrontendServerDescriptor(RootDescriptor rootDescriptor)
    {
      try
      {
        XPathNavigator deviceElementNav = rootDescriptor.FindFirstDeviceElement(
            UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE_VERSION);
        if (deviceElementNav == null)
          return null;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(deviceElementNav.NameTable);
        nsmgr.AddNamespace("d", UPnP.Infrastructure.UPnPConsts.NS_DEVICE_DESCRIPTION);
        string udn = RootDescriptor.GetDeviceUDN(deviceElementNav, nsmgr);
        string friendlyName = ParserHelper.SelectText(deviceElementNav, "d:friendlyName/text()", nsmgr);
        SystemName system = new SystemName(new Uri(rootDescriptor.SSDPRootEntry.DescriptionLocation).Host);
        return new ClientDescriptor(rootDescriptor, ParserHelper.ExtractUUIDFromUDN(udn), friendlyName, system);
      }
      catch (Exception e)
      {
        RootEntry rootEntry = rootDescriptor.SSDPRootEntry;
        ServiceScope.Get<ILogger>().Warn("UPnPServerControlPoint: Error parsing UPnP device description for root device '{0}' at location '{1}'", e,
            rootEntry.RootDeviceID, rootEntry.DescriptionLocation);
        return null;
      }
    }

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      ClientDescriptor clientDescriptor;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        clientDescriptor = GetMPFrontendServerDescriptor(rootDescriptor);
        if (clientDescriptor == null || _availableClients.Contains(clientDescriptor))
          return;
        ServiceScope.Get<ILogger>().Debug("UPnPServerControlPoint: Found MP-II client '{0}' (system ID '{1}') at host '{2}'",
            clientDescriptor.ClientName, clientDescriptor.MPFrontendServerUUID, clientDescriptor.System.HostName);
        _availableClients.Add(clientDescriptor);
      }
      InvokeClientAvailable(clientDescriptor);
      CheckConnect(clientDescriptor);
    }

    void OnUPnPRootDeviceRemoved(RootDescriptor rootDescriptor)
    {
      ClientDescriptor clientDescriptor;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        clientDescriptor = GetMPFrontendServerDescriptor(rootDescriptor);
        if (clientDescriptor == null || !_availableClients.Contains(clientDescriptor))
          return;
        ServiceScope.Get<ILogger>().Debug("UPnPServerControlPoint: MP-II client '{0}' (system ID '{1}') at host '{2}' was removed from the network",
            clientDescriptor.ClientName, clientDescriptor.MPFrontendServerUUID, clientDescriptor.System.HostName);
        _availableClients.Remove(clientDescriptor);
      }
      InvokeClientUnavailable(clientDescriptor);
      // The client connection has its own event handler for disconnects - it will trigger method OnClientDisconnected
      // as result of disconnection, this will remove the client connection from the _clientConnections collection
    }

    void OnClientDisconnected(ClientConnection clientConnection)
    {
      ClientDescriptor descriptor = clientConnection.Descriptor;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        string deviceUuid = descriptor.MPFrontendServerUUID;
        if (!_clientConnections.ContainsKey(deviceUuid))
          return;
        _clientConnections.Remove(deviceUuid);
      }
      InvokeClientDisconnected(descriptor);
    }

    protected void CheckConnect(ClientDescriptor clientDescriptor)
    {
      // Check if client is attached and connect if it is an attached client
      string clientSystemId = clientDescriptor.MPFrontendServerUUID;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        if (!_attachedClientSystemIds.Contains(clientSystemId))
          return;
        if (_clientConnections.ContainsKey(clientSystemId))
          return;
      }
      DeviceConnection connection;
      try
      {
        connection = _controlPoint.Connect(clientDescriptor.UPnPRootDescriptor, clientSystemId,
            UPnPExtendedDataTypes.ResolveDataType);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn(
            "UPnPServerControlPoint: Error connecting to UPnP MP-II frontend server '{0}'", e, clientSystemId);
        return;
      }
      try
      {
        ClientConnection clientConnection = new ClientConnection(_controlPoint, connection, clientDescriptor);
        lock (_networkTracker.SharedControlPointData.SyncObj)
          _clientConnections.Add(clientDescriptor.MPFrontendServerUUID, clientConnection);
        clientConnection.ClientDeviceDisconnected += OnClientDisconnected;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn(
            "UPnPServerControlPoint: Error connecting to services of UPnP MP-II frontend server '{0}'", e, clientSystemId);
        _controlPoint.Disconnect(connection);
        return;
      }
      InvokeClientConnected(clientDescriptor);
    }

    protected void InvokeClientAvailable(ClientDescriptor clientDescriptor)
    {
      ClientStateChangedDlgt dlgt = ClientAvailable;
      if (dlgt != null)
        dlgt(clientDescriptor);
    }

    protected void InvokeClientUnavailable(ClientDescriptor clientDescriptor)
    {
      ClientStateChangedDlgt dlgt = ClientUnavailable;
      if (dlgt != null)
        dlgt(clientDescriptor);
    }

    protected void InvokeClientConnected(ClientDescriptor clientDescriptor)
    {
      ClientStateChangedDlgt dlgt = ClientConnected;
      if (dlgt != null)
        dlgt(clientDescriptor);
    }

    protected void InvokeClientDisconnected(ClientDescriptor clientDescriptor)
    {
      ClientStateChangedDlgt dlgt = ClientDisconnected;
      if (dlgt != null)
        dlgt(clientDescriptor);
    }

  }
}
