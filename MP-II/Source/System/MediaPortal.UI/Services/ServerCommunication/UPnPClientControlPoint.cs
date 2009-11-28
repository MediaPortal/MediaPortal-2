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
using System.Xml.XPath;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.UPnP;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  public delegate void BackendServerConnectedDlgt(DeviceConnection connection);
  public delegate void BackendServerDisconnectedDlgt(DeviceConnection connection);

  /// <summary>
  /// Tracks the connection state of a defined home server.
  /// </summary>
  public class UPnPClientControlPoint
  {
    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;

    protected string _homeServerSystemId;
    protected DeviceConnection _connection = null;
    protected UPnPContentDirectoryServiceProxy _contentDirectoryService = null;
    protected UPnPServerControllerServiceProxy _serverControllerService = null;

    public UPnPClientControlPoint(string homeServerSystemId)
    {
      _homeServerSystemId = homeServerSystemId;
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _controlPoint = new UPnPControlPoint(_networkTracker);
      _networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
    }

    public event BackendServerConnectedDlgt BackendServerConnected;
    public event BackendServerDisconnectedDlgt BackendServerDisconnected;

    /// <summary>
    /// Gets or sets the system ID of the MediaPortal-II server, which is this UPnP client's homeserver.
    /// The control point automatically connects to the homeserver.
    /// </summary>
    public string HomeServerSystemId
    {
      get { return _homeServerSystemId; }
      set { _homeServerSystemId = value; }
    }

    public UPnPContentDirectoryServiceProxy ContentDirectoryService
    {
      get { return _contentDirectoryService; }
    }

    public UPnPServerControllerServiceProxy ServerControllerService
    {
      get { return _serverControllerService; }
    }

    public void Start()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts, else the network tracker could fire events before the control point is ready
      _networkTracker.Start();
    }

    public void Stop()
    {
      _contentDirectoryService = null;
      _serverControllerService = null;
      _networkTracker.Close();
      _controlPoint.Close();
    }

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      DeviceConnection connection;
      string deviceUuid;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        if (_connection != null)
          return;
        XPathNavigator deviceElementNav = rootDescriptor.FindFirstDeviceElement(
            UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        if (deviceElementNav == null)
          return;
        deviceUuid = RootDescriptor.GetDeviceUUID(deviceElementNav);
        if (deviceUuid != _homeServerSystemId)
          return;
        try
        {
          connection = _connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("Error connecting to UPnP MP-II backend server '{0}'", e, deviceUuid);
          return;
        }
      }
      connection.DeviceDisconnected += OnUPnPDeviceDisconnected;
      try
      {
        CpService cdsStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_ID);
        if (cdsStub == null)
          throw new InvalidDataException("ContentDirectory service not found in device '{0}' of type '{1}:{2}'",
              deviceUuid, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        CpService scsStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.SERVER_CONTROLLER_SERVICE_ID);
        if (scsStub == null)
          throw new InvalidDataException("ServerController service not found in device '{0}' of type '{1}:{2}'",
              deviceUuid, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        lock (_networkTracker.SharedControlPointData.SyncObj)
        {
          _contentDirectoryService = new UPnPContentDirectoryServiceProxy(cdsStub);
          _serverControllerService = new UPnPServerControllerServiceProxy(scsStub);
        }
        // TODO: other services
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("Error connecting to services of UPnP MP-II backend server '{0}'", e, deviceUuid);
        connection.DeviceDisconnected -= OnUPnPDeviceDisconnected;
        _controlPoint.Disconnect(deviceUuid);
        return;
      }
      InvokeBackendServerDeviceConnected(connection);
    }

    void OnUPnPDeviceDisconnected(DeviceConnection connection)
    {
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        _connection = null;
        _contentDirectoryService = null;
        _serverControllerService = null;
      }
      InvokeBackendServerDeviceDisconnected(connection);
    }

    protected void InvokeBackendServerDeviceConnected(DeviceConnection connection)
    {
      BackendServerConnectedDlgt dlgt = BackendServerConnected;
      if (dlgt != null)
        dlgt(connection);
    }

    protected void InvokeBackendServerDeviceDisconnected(DeviceConnection connection)
    {
      BackendServerDisconnectedDlgt dlgt = BackendServerDisconnected;
      if (dlgt != null)
        dlgt(connection);
    }
  }
}
