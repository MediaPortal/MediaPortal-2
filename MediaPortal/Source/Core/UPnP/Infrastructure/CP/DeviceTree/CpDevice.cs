#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using UPnP.Infrastructure.CP.Description;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// UPnP device template which gets instantiated at the client (control point) side for each device (root and embedded)
  /// the control point wants to connect to.
  /// </summary>
  /// <remarks>
  /// Parts of this class are intentionally parallel to the implementation in <see cref="UPnP.Infrastructure.Dv.DeviceTree.DvDevice"/>.
  /// </remarks>
  public class CpDevice
  {
    protected CpDevice _parentDevice;
    protected IDictionary<string, CpDevice> _embeddedDevices = new Dictionary<string, CpDevice>();
    protected IDictionary<string, CpService> _services = new Dictionary<string, CpService>();
    protected string _deviceType;
    protected int _deviceTypeVersion;
    protected bool _isOptional = true;
    protected DeviceConnection _connection = null;
    protected string _uuid;

    /// <summary>
    /// Creates a new UPnP device instance at the control point (client) side.
    /// </summary>
    /// <param name="connection">Device connection instance which attends the connection with the server side.</param>
    /// <param name="deviceType">Type of the device instance to match, in the format "schemas-upnp-org:device:[device-type]" or
    /// "vendor-domain:device:[device-type]". Note that in vendor-defined types, all dots in the vendors domain are
    /// replaced by hyphens.</param>
    /// <param name="deviceTypeVersion">Version of the device type to match.</param>
    /// <param name="uuid">UUID of the device we are connected to.</param>
    public CpDevice(DeviceConnection connection, string deviceType, int deviceTypeVersion, string uuid)
    {
      _connection = connection;
      _deviceType = deviceType;
      _deviceTypeVersion = deviceTypeVersion;
      _uuid = uuid;
    }

    /// <summary>
    /// Gets or sets a flag which controls the control point's matching behaviour. Can only be set in embedded device-templates.
    /// If <see cref="IsOptional"/> is set to <c>true</c>, the control point will also return root devices from the network
    /// which don't contain this embedded device. If this flag is set to <c>false</c>, root devices without an embedded device
    /// matching this embedded device template won't match the root device and thus won't be notified as matching devices.
    /// </summary>
    public bool IsOptional
    {
      get { return _isOptional; }
      set { _isOptional = value; }
    }

    /// <summary>
    /// Returns the information if this device template is connected to a matching UPnP device.
    /// </summary>
    public bool IsConnected
    {
      get { return _connection != null; }
    }

    /// <summary>
    /// Returns the device's connection instance, if <see cref="IsConnected"/>.
    /// </summary>
    public DeviceConnection Connection
    {
      get { return _connection; }
    }

    /// <summary>
    /// Returns the device which contains this device. If this is the root device, returns <c>null</c>.
    /// </summary>
    public CpDevice ParentDevice
    {
      get { return _parentDevice; }
    }

    /// <summary>
    /// Returns the full qualified name of this device in the form "[RootDeviceName].[DeviceName]".
    /// </summary>
    public string FullQualifiedName
    {
      get
      {
        string root = _parentDevice == null ? string.Empty : (_parentDevice.FullQualifiedName + ".");
        return root + _deviceType + ":" + _deviceTypeVersion;
      }
    }

    /// <summary>
    /// Gets the root device of the device tree where this device is part of.
    /// </summary>
    public CpDevice RootDevice
    {
      get
      {
        CpDevice current = this;
        while (current._parentDevice != null)
          current = current._parentDevice;
        return current;
      }
    }

    /// <summary>
    /// Returns a mapping of device UUIDs to embedded devices of this device.
    /// </summary>
    public IDictionary<string, CpDevice> EmbeddedDevices
    {
      get { return _embeddedDevices; }
    }

    /// <summary>
    /// Returns a mapping of service ids to services of this device.
    /// </summary>
    public IDictionary<string, CpService> Services
    {
      get { return _services; }
    }

    /// <summary>
    /// Returns the device type, in the format "schemas-upnp-org:device:[device-type]" or
    /// "vendor-domain:device:[device-type]".
    /// </summary>
    public string DeviceType
    {
      get { return _deviceType; }
    }

    /// <summary>
    /// Returns the version of the type of this device.
    /// </summary>
    public int DeviceTypeVersion
    {
      get { return _deviceTypeVersion; }
    }

    /// <summary>
    /// Returns the device type URN with version, in the format "urn:schemas-upnp-org:device:[device-type]:[version]".
    /// </summary>
    public string DeviceTypeVersion_URN
    {
      get { return "urn:" + _deviceType + ":" + _deviceTypeVersion; }
    }

    /// <summary>
    /// Returns the globally unique device id this device instance is connected with.
    /// </summary>
    public string UUID
    {
      get { return _uuid; }
    }

    /// <summary>
    /// Returns the Unique device name of the device this device instance is connected with.
    /// The UDN has the form "uuid:" + <see cref="UUID"/>.
    /// </summary>
    public string UDN
    {
      get { return "uuid:" + _uuid; }
    }

    /// <summary>
    /// Gets all service type URNs with version number (ServiceTypeVersion_URN) for all contained services.
    /// duplicate entries will be eliminated.
    /// </summary>
    public ICollection<string> GetServiceTypeVURNs()
    {
      ICollection<string> result = new List<string>();
      foreach (CpService service in _services.Values)
      {
        string stuv = service.ServiceTypeVersion_URN;
        if (!result.Contains(stuv))
          result.Add(stuv);
      }
      return result;
    }

    /// <summary>
    /// Finds the device with the specified <paramref name="deviceUDN"/> in the device tree starting with this device instance.
    /// </summary>
    /// <param name="deviceUDN">Device UDN to search. The device UDN needs to be in the format "uuid:[device-UUID]".</param>
    /// <returns>UPnP device instance with the given <paramref name="deviceUDN"/> or <c>null</c>, if no device in the device
    /// tree starting with this device contains a device with the given name.</returns>
    public CpDevice FindDeviceByUDN(string deviceUDN)
    {
      if (UDN == deviceUDN)
        return this;
      return _embeddedDevices.Values.Select(embeddedDevice => embeddedDevice.FindDeviceByUDN(deviceUDN)).FirstOrDefault(result => result != null);
    }

    /// <summary>
    /// Finds all devices in the device tree starting with this device with the specified device
    /// <paramref name="type"/> and <paramref name="version"/>.
    /// </summary>
    /// <param name="type">Device type to search.</param>
    /// <param name="version">Version number of the device type to search.</param>
    /// <param name="searchCompatible">If set to <c>true</c>, this method also searches compatible devices,
    /// i.e. devices with a higher version number than requested.</param>
    public IEnumerable<CpDevice> FindDevicesByDeviceTypeAndVersion(string type, int version, bool searchCompatible)
    {
      if (_deviceType == type && (_deviceTypeVersion == version || (searchCompatible && _deviceTypeVersion > version)))
        yield return this;
      foreach (CpDevice embeddedDevice in _embeddedDevices.Values)
        foreach (CpDevice matchingDevice in embeddedDevice.FindDevicesByDeviceTypeAndVersion(type, version, searchCompatible))
          yield return matchingDevice;
    }

    /// <summary>
    /// Finds all services in the device tree starting with this device with the specified service <paramref name="type"/>
    /// and <paramref name="version"/>.
    /// </summary>
    /// <param name="type">Service type to search.</param>
    /// <param name="version">Version number of the service type to search.</param>
    /// <param name="searchCompatible">If set to <c>true</c>, this method also searches compatible services,
    /// i.e. services with a higher version number than requested.</param>
    /// <returns>Enumeration of services with the given type and version (and compatible services, if
    /// <paramref name="searchCompatible"/> is <c>true</c>.</returns>
    public IEnumerable<CpService> FindServicesByServiceTypeAndVersion(string type, int version, bool searchCompatible)
    {
      foreach (CpService service in _services.Values)
        if (service.ServiceType == type && service.ServiceTypeVersion == version ||
            searchCompatible && service.IsCompatible(type, version))
          yield return service;
      foreach (CpDevice embeddedDevice in _embeddedDevices.Values)
        foreach (CpService matchingService in embeddedDevice.FindServicesByServiceTypeAndVersion(type, version, searchCompatible))
          yield return matchingService;
    }

    /// <summary>
    /// Finds the service with the given <paramref name="serviceId"/> in this device.
    /// </summary>
    /// <param name="serviceId">Id of the service to search.</param>
    /// <returns>Service with the given <paramref name="serviceId"/> or <c>null</c>, if there is no such service.</returns>
    public CpService FindServiceByServiceId(string serviceId)
    {
      CpService result;
      if (_services.TryGetValue(serviceId, out result))
        return result;
      return null;
    }

    #region Connection

    /// <summary>
    /// Adds the specified embedded template <paramref name="device"/>.
    /// </summary>
    /// <param name="device">Device to add to the embedded devices.</param>
    internal void AddEmbeddedDevice(CpDevice device)
    {
      _embeddedDevices.Add(device.UUID, device);
    }

    /// <summary>
    /// Adds the specified <paramref name="service"/>.
    /// </summary>
    /// <param name="service">Service to add to this device.</param>
    internal void AddService(CpService service)
    {
      _services.Add(service.ServiceId, service);
    }

    internal static CpDevice ConnectDevice(DeviceConnection connection, DeviceDescriptor deviceDescriptor, DataTypeResolverDlgt dataTypeResolver)
    {
      lock (connection.CPData.SyncObj)
      {
        string type;
        int version;
        if (!deviceDescriptor.GetTypeAndVersion(out type, out version))
          throw new ArgumentException(string.Format("Invalid device type/version URN '{0}'", deviceDescriptor.TypeVersion_URN));
        CpDevice result = new CpDevice(connection, type, version, deviceDescriptor.DeviceUUID);

        foreach (DeviceDescriptor childDevice in deviceDescriptor.ChildDevices)
          result.AddEmbeddedDevice(ConnectDevice(connection, childDevice, dataTypeResolver));
        IDictionary<string, ServiceDescriptor> serviceDescriptors;
        if (deviceDescriptor.RootDescriptor.ServiceDescriptors.TryGetValue(deviceDescriptor.DeviceUUID, out serviceDescriptors))
          foreach (ServiceDescriptor serviceDescriptor in serviceDescriptors.Values)
            if (serviceDescriptor.State == ServiceDescriptorState.Ready)
              result.AddService(CpService.ConnectService(connection, result, serviceDescriptor, dataTypeResolver));
            else
              UPnPConfiguration.LOGGER.Warn("CpDevice.ConnectDevice: Unable to connect to service '{0}' (type '{1}', version '{2}') - the service descriptor was not initialized properly",
                  serviceDescriptor.ServiceId, serviceDescriptor.ServiceType, serviceDescriptor.ServiceTypeVersion);
        return result;
      }
    }

    internal void Disconnect()
    {
      DeviceConnection connection = _connection;
      if (connection == null)
        return;
      ICollection<CpDevice> embeddedDevices;
      ICollection<CpService> services;
      lock (connection.CPData.SyncObj)
      {
        _connection = null;
        embeddedDevices = new List<CpDevice>(_embeddedDevices.Values);
        services = new List<CpService>(_services.Values);
      }
      foreach (CpDevice embeddedDevice in embeddedDevices)
        embeddedDevice.Disconnect();
      foreach (CpService service in services)
        service.Disconnect();
    }

    #endregion
  }
}
