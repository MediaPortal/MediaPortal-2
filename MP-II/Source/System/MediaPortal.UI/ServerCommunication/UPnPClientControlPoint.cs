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
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.ServerCommunication
{
  public delegate void MediaServerConnectedDlgt(DeviceConnection connection);
  public delegate void MediaServerDisconnectedDlgt(DeviceConnection connection);

  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP client control point.
  /// </summary>
  public class UPnPClientControlPoint
  {
    protected string _homeServerUUID;
    protected DeviceConnection _connection = null;
    protected UPnPContentDirectoryService _contentDirectoryService = null;
    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;

    public UPnPClientControlPoint(string homeServerUUID)
    {
      _homeServerUUID = homeServerUUID;
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _controlPoint = new UPnPControlPoint(_networkTracker);
      _networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
    }

    public event MediaServerConnectedDlgt MediaServerConnected;
    public event MediaServerDisconnectedDlgt MediaServerDisconnected;

    /// <summary>
    /// Gets or sets the UUID of the MediaPortal-II server, which is this UPnP client's homeserver.
    /// The control point automatically connects to the homeserver.
    /// </summary>
    public string HomeServerUUID
    {
      get { return _homeServerUUID; }
      set { _homeServerUUID = value; }
    }

    public UPnPContentDirectoryService ContentDirectoryService
    {
      get { return _contentDirectoryService; }
    }

    public void Start()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts, to catch all device appearances
      _networkTracker.Start();
    }

    public void Stop()
    {
      _contentDirectoryService = null;
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
        XPathNavigator mediaServerDeviceElementNav = rootDescriptor.FindFirstDeviceElement(UPnPTypesAndIds.MEDIA_SERVER_DEVICE_TYPE, UPnPTypesAndIds.MEDIA_SERVER_DEVICE_TYPE_VERSION);
        if (mediaServerDeviceElementNav == null)
          return;
        deviceUuid = RootDescriptor.GetDeviceUUID(mediaServerDeviceElementNav);
        if (deviceUuid != _homeServerUUID)
          return;
        try
        {
          connection = _connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("Error attaching to UPnP MP-II content directory service", e);
          return;
        }
      }
      connection.DeviceDisconnected += OnUPnPDeviceDisconnected;
      try
      {
        CpService cdsStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_ID);
        if (cdsStub == null)
          throw new InvalidDataException("ContentDirectory service not found in device '{0}' of type '{1}:{2}'", deviceUuid, UPnPTypesAndIds.MEDIA_SERVER_DEVICE_TYPE, UPnPTypesAndIds.MEDIA_SERVER_DEVICE_TYPE_VERSION);
        lock (_networkTracker.SharedControlPointData.SyncObj)
          _contentDirectoryService = new UPnPContentDirectoryService(cdsStub);
        // TODO: other services
        InvokeMediaServerDeviceConnected(connection);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("Error attaching to UPnP MP-II content directory service", e);
        _controlPoint.Disconnect(deviceUuid);
      }
    }

    void OnUPnPDeviceDisconnected(DeviceConnection connection)
    {
      _contentDirectoryService = null;
      InvokeMediaServerDeviceDisconnected(connection);
    }

    protected void InvokeMediaServerDeviceConnected(DeviceConnection connection)
    {
      MediaServerConnectedDlgt dlgt = MediaServerConnected;
      if (dlgt != null)
        dlgt(connection);
    }

    protected void InvokeMediaServerDeviceDisconnected(DeviceConnection connection)
    {
      MediaServerDisconnectedDlgt dlgt = MediaServerDisconnected;
      if (dlgt != null)
        dlgt(connection);
    }
  }
}
