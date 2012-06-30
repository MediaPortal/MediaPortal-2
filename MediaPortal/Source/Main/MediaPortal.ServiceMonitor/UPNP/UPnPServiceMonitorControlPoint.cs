#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.ServiceMonitor.UPNP
{
  public delegate void BackendServerConnectedDlgt(DeviceConnection connection);
  public delegate void BackendServerDisconnectedDlgt(DeviceConnection connection);

  public class UPnPServiceMonitorControlPoint
  {
    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;

    protected UPnPServerControllerServiceProxy _serverControllerService = null;

    protected string _homeServerSystemId;
    protected DeviceConnection _connection = null;

    public event BackendServerConnectedDlgt BackendServerConnected;
    public event BackendServerDisconnectedDlgt BackendServerDisconnected;
    

    public UPnPServiceMonitorControlPoint(string homeServerSystemId)
    {
      _homeServerSystemId = homeServerSystemId;
      var cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _controlPoint = new UPnPControlPoint(_networkTracker);
      _networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
    }

    public IResourceInformationService ResourceInformationService
    {
      get { return null; }
    }

    public UPnPServerControllerServiceProxy ServerControllerService
    {
      get { return _serverControllerService; }
    }

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      TryConnect(rootDescriptor);
    }

    protected void TryConnect(RootDescriptor rootDescriptor)
    {
      DeviceConnection connection;
      string deviceUuid;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        if (_connection != null)
          return;
        var deviceElementNav = rootDescriptor.FindFirstDeviceElement(
          UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        if (deviceElementNav == null)
          return;
        deviceUuid = RootDescriptor.GetDeviceUUID(deviceElementNav);
        var nsmgr = new XmlNamespaceManager(deviceElementNav.NameTable);
        nsmgr.AddNamespace("d", UPnP.Infrastructure.UPnPConsts.NS_DEVICE_DESCRIPTION);
        var friendlyName = ParserHelper.SelectText(deviceElementNav, "d:friendlyName/text()", nsmgr);
        var system = new SystemName(new Uri(rootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host);
        if (deviceUuid == _homeServerSystemId)
          ServiceRegistration.Get<ILogger>().Debug(
            "UPnPServiceMonitorControlPoint: Found MP 2 home server '{0}' (system ID '{1}') at host '{2}' (IP address: '{3}')",
            friendlyName, deviceUuid, system.HostName, system.Address);
        else
        {
          ServiceRegistration.Get<ILogger>().Debug(
            "UPnPServiceMonitorControlPoint: Found foreign MP 2 server '{0}' (system ID '{1}') at host '{2}' (IP address: '{3}')",
            friendlyName, deviceUuid, system.HostName, system.Address);
          return;
        }
        try
        {
          connection =
            _connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn(
            "UPnPServiceMonitorControlPoint: Error connecting to UPnP MP 2 backend server '{0}'", e, deviceUuid);
          return;
        }
      }
      connection.DeviceDisconnected += OnUPnPDeviceDisconnected;

      try
      {
        var scsStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.SERVER_CONTROLLER_SERVICE_ID);
        if (scsStub == null)
          throw new InvalidDataException("ServerController service not found in device '{0}' of type '{1}:{2}'",
              deviceUuid, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        
        lock (_networkTracker.SharedControlPointData.SyncObj)
        {
          _serverControllerService = new UPnPServerControllerServiceProxy(scsStub);
        }
        // TODO: other services
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("UPnPClientControlPoint: Error connecting to services of UPnP MP 2 backend server '{0}'", e, deviceUuid);
        connection.DeviceDisconnected -= OnUPnPDeviceDisconnected;
        _controlPoint.Disconnect(deviceUuid);
        return;
      }
      InvokeBackendServerDeviceConnected(connection);
    }

    void OnUPnPDeviceDisconnected(DeviceConnection connection)
    {
      ServiceRegistration.Get<ILogger>().Debug(
        "UPnPServiceMonitorControlPoint: Disconnect from MP 2 home server (IP address: '{0}')", connection.DeviceUUID);
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        _connection = null;
        _serverControllerService = null;
      }
      InvokeBackendServerDeviceDisconnected(connection);
    }

    protected void InvokeBackendServerDeviceConnected(DeviceConnection connection)
    {
      var dlgt = BackendServerConnected;
      if (dlgt != null)
        dlgt(connection);
    }

    protected void InvokeBackendServerDeviceDisconnected(DeviceConnection connection)
    {
      var dlgt = BackendServerDisconnected;
      if (dlgt != null)
        dlgt(connection);
    }


    public void Start()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts. See docs of Start() method.
      _networkTracker.Start();
    }

    public void Stop()
    {
      _serverControllerService = null;
      _networkTracker.Close();
      _controlPoint.Close(); // Close the control point after the network tracker was closed. See docs of Close() method.
    }
  }
}
