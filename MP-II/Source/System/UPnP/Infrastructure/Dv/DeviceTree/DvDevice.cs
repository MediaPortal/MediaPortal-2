#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Delegate for the change event telling subscribers that a device's configuration changed.
  /// </summary>
  /// <param name="device">The device which changed its configuration.</param>
  public delegate void DeviceConfigurationChangedDlgt(DvDevice device);

  /// <summary>
  /// Delegate for the change event telling subscribers that a service's configuration changed.
  /// </summary>
  /// <param name="service">The service which changed its configuration.</param>
  public delegate void ServiceConfigurationChangedDlgt(DvService service);

  /// <summary>
  /// Base UPnP device class with all functionality to host a UPnP device.
  /// To build special device configurations, either subclasses can be implemented doing the device initialization or
  /// an instance of this class can be created and configured from outside.
  /// </summary>
  public class DvDevice
  {
    protected DvDevice _parentDevice = null;
    protected IList<DvDevice> _embeddedDevices = new List<DvDevice>();
    protected IList<DvService> _services = new List<DvService>();
    protected string _deviceType;
    protected int _deviceTypeVersion;
    protected string _uuid;
    protected ILocalizedDeviceInformation _deviceInformation;
    
    /// <summary>
    /// Creates a new UPnP device instance at the server (device) side.
    /// </summary>
    /// <param name="deviceType">Type of the new device instance, in the format "schemas-upnp-org:device:[device-type]" or
    /// "vendor-domain:device:[device-type]". Note that in vendor-defined types, all dots in the vendors domain must
    /// be replaced by hyphens.</param>
    /// <param name="deviceTypeVersion">Version of the implemented device type.</param>
    /// <param name="uuid">Globally unique id for the new device. Vendor-defined. Should be a GUID string. That ID should
    /// be the same over time for a specific device instance, i.e. must survive reboots. No "uuid:" prefix.</param>
    /// <param name="descriptor">Data structure containing informational data about the new UPnP device.</param>
    public DvDevice(string deviceType, int deviceTypeVersion, string uuid, ILocalizedDeviceInformation descriptor)
    {
      _deviceType = deviceType;
      _deviceTypeVersion = deviceTypeVersion;
      _uuid = uuid;
      _deviceInformation = descriptor;
    }

    /// <summary>
    /// Gets or sets a delegate function which can return a presentation URL for this device, depending on the
    /// network interface for a given UPnP endpoint.
    /// </summary>
    public GetURLForEndpointDlgt GetPresentationURLDelegate { get; set; }

    /// <summary>
    /// Gets or sets the device which contains this device. If this is the root device, the parent device is <c>null</c>.
    /// </summary>
    public DvDevice ParentDevice
    {
      get { return _parentDevice; }
      internal set { _parentDevice = value; }
    }

    /// <summary>
    /// Gets the root device of the device tree where this device is part of.
    /// </summary>
    public DvDevice RootDevice
    {
      get
      {
        DvDevice current = this;
        while (current._parentDevice != null)
          current = current._parentDevice;
        return current;
      }
    }

      /// <summary>
    /// Returns a read-only collection of embedded devices of this device.
    /// </summary>
    public ICollection<DvDevice> EmbeddedDevices
    {
      get { return _embeddedDevices; }
    }

    /// <summary>
    /// Returns a read-only collection of services of this device.
    /// </summary>
    public ICollection<DvService> Services
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
    /// Returns the device type URN with version, in the format "urn:schemas-upnp-org:device:[device-type]:[version]" or
    /// "urn:domain-name:device:[device-type]:[version]".
    /// </summary>
    public string DeviceTypeVersion_URN
    {
      get { return "urn:" + _deviceType + ":" + _deviceTypeVersion; }
    }

    /// <summary>
    /// Returns the globally unique device id of this device.
    /// </summary>
    public string UUID
    {
      get { return _uuid; }
    }

    /// <summary>
    /// Returns the Unique device name of this device. Is the string "uuid:" + <see cref="UUID"/>.
    /// </summary>
    public string UDN
    {
      get { return "uuid:" + _uuid; }
    }

    /// <summary>
    /// Returns a query interface providing localized informational (vendor-defined) data about this device.
    /// </summary>
    public ILocalizedDeviceInformation DeviceInformation
    {
      get { return _deviceInformation; }
    }

    /// <summary>
    /// Adds the specified embedded <paramref name="device"/>.
    /// </summary>
    /// <remarks>
    /// Should be done at the time when the UPnP system is not bound to the network yet, but if the binding is already
    /// established, the system will notify a configuration update.
    /// Note that default devices (defined by a UPnP Forum working committee) must be added first.
    /// </remarks>
    /// <param name="device">Device to add to the embedded devices.</param>
    public void AddEmbeddedDevice(DvDevice device)
    {
      device.ParentDevice = this;
      _embeddedDevices.Add(device);
    }

    /// <summary>
    /// Adds the specified <paramref name="service"/>.
    /// </summary>
    /// <remarks>
    /// Should be done at the time when the UPnP system is not bound to the network yet, but if the binding is already
    /// established, the system will notify a configuration update.
    /// Note that default services (defined by a UPnP Forum working committee) must be added first.
    /// </remarks>
    /// <param name="service">Service to add to this device.</param>
    public void AddService(DvService service)
    {
      service.ParentDevice = this;
      _services.Add(service);
    }

    /// <summary>
    /// Gets all service type URNs with version number (ServiceTypeVersion_URN) for all contained services.
    /// duplicate entries will be eliminated.
    /// </summary>
    public ICollection<string> GetServiceTypeVersion_URNs()
    {
      ICollection<string> result = new List<string>();
      foreach (DvService service in _services)
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
    public DvDevice FindDeviceByUDN(string deviceUDN)
    {
      if (UDN == deviceUDN)
        return this;
      foreach (DvDevice embeddedDevice in _embeddedDevices)
      {
        DvDevice result = embeddedDevice.FindDeviceByUDN(deviceUDN);
        if (result != null)
          return result;
      }
      return null;
    }

    /// <summary>
    /// Finds all devices in the device tree starting with this device with the specified device
    /// <paramref name="type"/> and <paramref name="version"/>.
    /// </summary>
    /// <param name="type">Device type to search.</param>
    /// <param name="version">Version number of the device type to search.</param>
    /// <param name="searchCompatible">If set to <c>true</c>, this method also searches compatible devices,
    /// i.e. devices with a higher version number than requested.</param>
    public IEnumerable<DvDevice> FindDevicesByDeviceTypeAndVersion(string type, int version, bool searchCompatible)
    {
      if (_deviceType == type && (_deviceTypeVersion == version || (searchCompatible && _deviceTypeVersion > version)))
        yield return this;
      foreach (DvDevice embeddedDevice in _embeddedDevices)
        foreach (DvDevice matchingDevice in embeddedDevice.FindDevicesByDeviceTypeAndVersion(type, version, searchCompatible))
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
    public IEnumerable<DvService> FindServicesByServiceTypeAndVersion(string type, int version, bool searchCompatible)
    {
      foreach (DvService service in _services)
        if (service.ServiceType == type && service.ServiceTypeVersion == version ||
            searchCompatible && service.IsCompatible(type, version))
          yield return service;
      foreach (DvDevice embeddedDevice in _embeddedDevices)
        foreach (DvService matchingService in embeddedDevice.FindServicesByServiceTypeAndVersion(type, version, searchCompatible))
          yield return matchingService;
    }

    #region Description generation

    /// <summary>
    /// Creates the UPnP description document for this root device and all embedded devices.
    /// </summary>
    /// <param name="serverData">Current server data structure.</param>
    /// <param name="config">UPnP endpoint which will be used to create the endpoint specific information. The comment of
    /// <see cref="DeviceTreeURLGenerator"/> describes why URLs must be adapted for each UPnP endpoint.</param>
    /// <param name="culture">The culture to localize strings and URLs of the returned description document.</param>
    /// <returns>UPnP device description document for this root device and all embedded devices.</returns>
    public string BuildRootDeviceDescription(ServerData serverData, EndpointConfiguration config, CultureInfo culture)
    {
      StringBuilder result = new StringBuilder(10000);
      XmlWriter writer = XmlWriter.Create(new StringWriterWithEncoding(result, Encoding.UTF8));
      writer.WriteStartDocument();
      writer.WriteStartElement(string.Empty, "root", UPnPConsts.NS_DEVICE_DESCRIPTION);

      writer.WriteAttributeString("configId", serverData.ConfigId.ToString());
      writer.WriteStartElement("specVersion");
      writer.WriteElementString("major", UPnPConsts.UPNP_VERSION_MAJOR.ToString());
      writer.WriteElementString("minor", UPnPConsts.UPNP_VERSION_MINOR.ToString());
      writer.WriteEndElement(); // specVersion
      
      AddDeviceDescriptionsRecursive(writer, config, culture);
      writer.WriteEndElement(); // root
      writer.Close();
      return result.ToString();
    }

    /// <summary>
    /// Creates the UPnP device description XML fragment for this device, all embedded devices and all services.
    /// </summary>
    /// <param name="writer">Result XML writer to add the device descriptions fragment to.</param>
    /// <param name="config">UPnP endpoint which will be used to create the endpoint specific information.</param>
    /// <param name="culture">Culture to create the culture specific information.</param>
    internal void AddDeviceDescriptionsRecursive(XmlWriter writer, EndpointConfiguration config, CultureInfo culture)
    {
      ILocalizedDeviceInformation deviceInformation = _deviceInformation;
      writer.WriteStartElement("device");
      writer.WriteElementString("deviceType", DeviceTypeVersion_URN);
      writer.WriteElementString("friendlyName", deviceInformation.GetFriendlyName(culture));
      writer.WriteElementString("manufacturer", deviceInformation.GetManufacturer(culture));
      string manufacturerURL = deviceInformation.GetManufacturerURL(culture);
      if (!string.IsNullOrEmpty(manufacturerURL))
        writer.WriteElementString("manufacturerURL", manufacturerURL);
      string modelDescription = deviceInformation.GetModelDescription(culture);
      if (!string.IsNullOrEmpty(modelDescription))
        writer.WriteElementString("modelDescription", modelDescription);
      writer.WriteElementString("modelName", deviceInformation.GetModelName(culture));
      string modelNumber = deviceInformation.GetModelNumber(culture);
      if (!string.IsNullOrEmpty(modelNumber))
        writer.WriteElementString("modelNumber", modelNumber);
      string modelURL = deviceInformation.GetModelURL(culture);
      if (!string.IsNullOrEmpty(modelURL))
        writer.WriteElementString("modelURL", modelURL);
      string serialNumber = deviceInformation.GetSerialNumber(culture);
      if (!string.IsNullOrEmpty(serialNumber))
        writer.WriteElementString("serialNumber", serialNumber);
      writer.WriteElementString("UDN", UDN);
      string upc = deviceInformation.GetUPC();
      if (!string.IsNullOrEmpty(upc))
        writer.WriteElementString("UPC", upc);
      ICollection<IconDescriptor> icons = deviceInformation.GetIcons(culture);
      if (icons.Count > 0)
      {
        writer.WriteStartElement("iconList");
        foreach (IconDescriptor icon in icons)
        {
          writer.WriteStartElement("icon");
          writer.WriteElementString("mimetype", icon.MimeType);
          writer.WriteElementString("width", icon.Width.ToString());
          writer.WriteElementString("height", icon.Height.ToString());
          writer.WriteElementString("depth", icon.ColorDepth.ToString());
          writer.WriteElementString("url", icon.GetIconURLDelegate(config.EndPointIPAddress, culture));
          writer.WriteEndElement(); // icon
        }
        writer.WriteEndElement(); // iconList
      }
      ICollection<DvService> services = _services;
      if (services.Count > 0)
      {
        writer.WriteStartElement("serviceList");
        foreach (DvService service in services)
          service.AddDeviceDescriptionForService(writer, config);
        writer.WriteEndElement(); // serviceList
      }
      ICollection<DvDevice> embeddedDevices = _embeddedDevices;
      if (embeddedDevices.Count > 0)
      {
        writer.WriteStartElement("deviceList");
        foreach (DvDevice embeddedDevice in embeddedDevices)
          embeddedDevice.AddDeviceDescriptionsRecursive(writer, config, culture);
        writer.WriteEndElement(); // deviceList
      }
      GetURLForEndpointDlgt presentationURLGetter = GetPresentationURLDelegate;
      string presentationURL = null;
      if (presentationURLGetter != null)
        presentationURL = presentationURLGetter(config.EndPointIPAddress, culture);
      if (!string.IsNullOrEmpty(presentationURL))
        writer.WriteElementString("presentationURL", presentationURL);
      writer.WriteEndElement(); // device
    }

    #endregion
  }
}
