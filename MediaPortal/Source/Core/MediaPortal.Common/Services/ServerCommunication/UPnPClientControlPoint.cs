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
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.Description;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.Services.ServerCommunication
{
  public delegate void BackendServerConnectedDlgt(DeviceConnection connection);
  public delegate void BackendServerDisconnectedDlgt(DeviceConnection connection);
  public delegate UPnPServiceProxyBase AdditionalServiceRegisterDlgt(DeviceConnection connection);

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
    protected UPnPResourceInformationServiceProxy _resourceInformationService = null;
    protected UPnPUserProfileDataManagementServiceProxy _userProfileDataManagementService = null;
    protected ICollection<AdditionalServiceRegisterDlgt> _additionalServiceRegistrations = new List<AdditionalServiceRegisterDlgt>();
    protected ICollection<UPnPServiceProxyBase> _additionalServices = new List<UPnPServiceProxyBase>();

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
    /// Gets or sets the system ID of the MediaPortal 2 server, which is this UPnP client's homeserver.
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

    public UPnPResourceInformationServiceProxy ResourceInformationService
    {
      get { return _resourceInformationService; }
    }

    public UPnPServerControllerServiceProxy ServerControllerService
    {
      get { return _serverControllerService; }
    }

    public UPnPUserProfileDataManagementServiceProxy UserProfileDataManagementService
    {
      get { return _userProfileDataManagementService; }
    }

    public DeviceConnection Connection
    {
      get { return _connection; }
    }

    /// <summary>
    /// Returns all additional registered services which have been created by registered service creator delegates.
    /// </summary>
    /// <remarks>
    /// <see cref="RegisterAdditionalService"/>
    /// </remarks>
    public ICollection<UPnPServiceProxyBase> AdditionalServices
    {
      get
      {
        lock (_networkTracker.SharedControlPointData.SyncObj)
          return new List<UPnPServiceProxyBase>(_additionalServices);
      }
    }

    /// <summary>
    /// Registers a delegate for creating an additional UPnP service proxy object upon device connection. This control point keeps track of the
    /// connection state, i.e. calls the given <paramref name="additionalServiceCreatorDlgt"/> delegate when the device is connected and disposes the
    /// service proxy object when the device is disconnected.
    /// </summary>
    /// <remarks>
    /// This method will typically be used by plugins which want to register custom UPnP service proxy objects. The service proxy object, which is created
    /// by the caller of this method, will typically be made available for usage in the given <paramref name="additionalServiceCreatorDlgt"/> method and
    /// revoked by the <see cref="IDisposable.Dispose"/> method of the custom service proxy object.
    /// </remarks>
    /// <param name="additionalServiceCreatorDlgt">Delegate which is able to create a service proxy object.</param>
    public void RegisterAdditionalService(AdditionalServiceRegisterDlgt additionalServiceCreatorDlgt)
    {
      lock (_networkTracker.SharedControlPointData.SyncObj)
        _additionalServiceRegistrations.Add(additionalServiceCreatorDlgt);
    }

    public void Start()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts. See docs of Start() method.
      _networkTracker.Start();
    }

    public void Stop()
    {
      _contentDirectoryService = null;
      _resourceInformationService = null;
      _serverControllerService = null;
      _networkTracker.Close();
      _controlPoint.Close(); // Close the control point after the network tracker was closed. See docs of Close() method.
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
        DeviceDescriptor rootDeviceDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(rootDescriptor);
        DeviceDescriptor backendServerDescriptor = rootDeviceDescriptor.FindFirstDevice(
            UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        if (backendServerDescriptor == null)
          return;
        deviceUuid = backendServerDescriptor.DeviceUUID;
        string friendlyName = backendServerDescriptor.FriendlyName;
        SystemName system = new SystemName(new Uri(rootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host);
        if (deviceUuid == _homeServerSystemId)
          ServiceRegistration.Get<ILogger>().Debug("UPnPClientControlPoint: Found MP2 home server '{0}' (system ID '{1}') at host '{2}' (IP address: '{3}')",
              friendlyName, deviceUuid, system.HostName, system.Address);
        else
        {
          ServiceRegistration.Get<ILogger>().Debug("UPnPClientControlPoint: Found foreign MP2 server '{0}' (system ID '{1}') at host '{2}' (IP address: '{3}')",
              friendlyName, deviceUuid, system.HostName, system.Address);
          return;
        }
        try
        {
          connection = _connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("UPnPClientControlPoint: Error connecting to UPnP MP2 backend server '{0}'", e, deviceUuid);
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
        CpService risStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.RESOURCE_INFORMATION_SERVICE_ID);
        if (risStub == null)
          throw new InvalidDataException("ResourceAccess service not found in device '{0}' of type '{1}:{2}'",
              deviceUuid, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        CpService scsStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.SERVER_CONTROLLER_SERVICE_ID);
        if (scsStub == null)
          throw new InvalidDataException("ServerController service not found in device '{0}' of type '{1}:{2}'",
              deviceUuid, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        CpService updmStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.USER_PROFILE_DATA_MANAGEMENT_SERVICE_ID);
        if (updmStub == null)
          throw new InvalidDataException("UserProfileDataManagement service not found in device '{0}' of type '{1}:{2}'",
              deviceUuid, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
        lock (_networkTracker.SharedControlPointData.SyncObj)
        {
          _contentDirectoryService = new UPnPContentDirectoryServiceProxy(cdsStub);
          _resourceInformationService = new UPnPResourceInformationServiceProxy(risStub);
          _serverControllerService = new UPnPServerControllerServiceProxy(scsStub);
          _userProfileDataManagementService = new UPnPUserProfileDataManagementServiceProxy(updmStub);
        }

        ICollection<UPnPServiceProxyBase> additionalServices = new List<UPnPServiceProxyBase>();
        foreach (AdditionalServiceRegisterDlgt additionalServiceRegistration in _additionalServiceRegistrations)
        {
          try
          {
            additionalServices.Add(additionalServiceRegistration(connection));
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("UPnPClientControlPoint: Error registering user service for UPnP MP2 backend server '{0}'", e, deviceUuid);
          }
        }
        lock (_networkTracker.SharedControlPointData.SyncObj)
          _additionalServices = additionalServices;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("UPnPClientControlPoint: Error connecting to services of UPnP MP2 backend server '{0}'", e, deviceUuid);
        connection.DeviceDisconnected -= OnUPnPDeviceDisconnected;
        _controlPoint.Disconnect(deviceUuid);
        return;
      }
      InvokeBackendServerDeviceConnected(connection);
    }

    void OnUPnPDeviceDisconnected(DeviceConnection connection)
    {
      IEnumerable<UPnPServiceProxyBase> servicesToDispose;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        _connection = null;
        _contentDirectoryService = null;
        _resourceInformationService = null;
        _serverControllerService = null;
        servicesToDispose = new List<UPnPServiceProxyBase>(_additionalServices);
        _additionalServices.Clear();
      }
      // Dispose all additional services if possible, to allow proper shutdown and cleanup
      foreach (UPnPServiceProxyBase service in servicesToDispose)
      {
        IDisposable disposable = service as IDisposable;
        if (disposable == null)
          continue;
        try
        {
          disposable.Dispose();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("UPnPClientControlPoint: Error disposing additional service '{0}'", service, e);
        }
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
