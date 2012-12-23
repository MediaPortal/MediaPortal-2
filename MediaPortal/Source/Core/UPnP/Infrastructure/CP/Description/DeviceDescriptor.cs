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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.Description
{
  /// <summary>
  /// Facade which provides access to device properties which are held in a device description XML document.
  /// </summary>
  /// <remarks>
  /// This class doesn't provide access to the <see cref="ServiceDescriptor"/> instances for its services by design.
  /// The service descriptors have another lifetime; they are initialized independently from device descriptors because
  /// they are based on own XML documents. That's why it is necessary to request the <see cref="ServiceDescriptor"/>
  /// instances from the <see cref="RootDescriptor"/>.
  /// </remarks>
  public class DeviceDescriptor
  {
    protected const string DEVICE_DESCRIPTION_NAMESPACE_PREFIX = "d";

    protected readonly RootDescriptor _rootDescriptor;
    protected readonly XPathNavigator _deviceNavigator;
    protected readonly XmlNamespaceManager _nsmgr;

    internal DeviceDescriptor(RootDescriptor rootDescriptor, XPathNavigator deviceNavigator)
    {
      _rootDescriptor = rootDescriptor;
      _deviceNavigator = deviceNavigator;
      _nsmgr = new XmlNamespaceManager(_deviceNavigator.NameTable);
      _nsmgr.AddNamespace(DEVICE_DESCRIPTION_NAMESPACE_PREFIX, UPnPConsts.NS_DEVICE_DESCRIPTION);
    }

    /// <summary>
    /// Creates a <see cref="DeviceDescriptor"/> for the root device from the device description of the given
    /// <paramref name="rootDescriptor"/>.
    /// </summary>
    /// <param name="rootDescriptor">Descriptor for whose root device the <see cref="DeviceDescriptor"/> should be built.</param>
    /// <returns>Device descriptor or <c>null</c>, if the given <paramref name="rootDescriptor"/> doesn't contain a device description
    /// (e.g. if it is erroneous).</returns>
    public static DeviceDescriptor CreateRootDeviceDescriptor(RootDescriptor rootDescriptor)
    {
      XPathDocument xmlDeviceDescription = rootDescriptor.DeviceDescription;
      if (xmlDeviceDescription == null)
        return null;
      XPathNavigator nav = xmlDeviceDescription.CreateNavigator();
      nav.MoveToChild(XPathNodeType.Element);
      XPathNodeIterator rootDeviceIt = nav.SelectChildren("device", "urn:schemas-upnp-org:device-1-0");
      return rootDeviceIt.MoveNext() ? new DeviceDescriptor(rootDescriptor, rootDeviceIt.Current) : null;
    }

    /// <summary>
    /// Returns the UPnP root descriptor.
    /// </summary>
    public RootDescriptor RootDescriptor
    {
      get { return _rootDescriptor; }
    }

    /// <summary>
    /// Returns a copy of the underlaying device XML navigator.
    /// </summary>
    public XPathNavigator DeviceNavigator
    {
      get { return _deviceNavigator.Clone(); }
    }

    /// <summary>
    /// Returns the UDN of the device or <c>null</c> if the device description doesn't contain a UDN entry.
    /// The UDN is specifically of the form "uuid:[Device-ID]".
    /// </summary>
    public string DeviceUDN
    {
      get { return ParserHelper.SelectText(_deviceNavigator, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":UDN/text()", _nsmgr); }
    }

    /// <summary>
    /// Returns the UUID of the device or <c>null</c> if the device description doesn't contain a UDN entry.
    /// The returned UUID is the part of the device's UDN after the "uuid:" prefix.
    /// </summary>
    public string DeviceUUID
    {
      get { return ParserHelper.ExtractUUIDFromUDN(DeviceUDN); }
    }

    public string TypeVersion_URN
    {
      get { return ParserHelper.SelectText(_deviceNavigator, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":deviceType/text()", _nsmgr); }
    }

    /// <summary>
    /// String containing the friendly name of the device or <c>null</c> if the device description doesn't contain a friendly name entry.
    /// </summary>
    public string FriendlyName
    {
      get { return ParserHelper.SelectText(_deviceNavigator, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":friendlyName/text()", _nsmgr); }
    }

    /// <summary>
    /// Returns a collection of descriptors for the child devices of this device.
    /// </summary>
    public ICollection<DeviceDescriptor> ChildDevices
    {
      get
      {
        ICollection<DeviceDescriptor> result = new List<DeviceDescriptor>();
        XPathNodeIterator it = _deviceNavigator.Select(DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":deviceList/" + DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":device", _nsmgr);
        while (it.MoveNext())
          result.Add(new DeviceDescriptor(_rootDescriptor, it.Current));
        return result;
      }
    }

    /// <summary>
    /// Extracts the type and version attributes from the device's <see cref="TypeVersion_URN"/> property.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public bool GetTypeAndVersion(out string type, out int version)
    {
      return ParserHelper.TryParseTypeVersion_URN(TypeVersion_URN, out type, out version);
    }

    internal ICollection<ServiceDescriptor> CreateServiceDescriptors()
    {
      ICollection<ServiceDescriptor> result = new List<ServiceDescriptor>();
      XPathNodeIterator it = _deviceNavigator.Select(DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":serviceList/" + DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":service", _nsmgr);
      while (it.MoveNext())
        result.Add(ExtractServiceDescriptor(_rootDescriptor, it.Current));
      return result;
    }

    /// <summary>
    /// Given an XML &lt;service&gt; element containing a service description, this method extracts the returned
    /// <see cref="ServiceDescriptor"/>.
    /// </summary>
    /// <param name="rd">Root descriptor of the service descriptor to be built.</param>
    /// <param name="serviceNav">XPath navigator pointing to an XML &lt;service&gt; element containing the service
    /// description.</param>
    /// <returns>Extracted service descriptor.</returns>
    protected ServiceDescriptor ExtractServiceDescriptor(RootDescriptor rd, XPathNavigator serviceNav)
    {
      string descriptionURL = ParserHelper.SelectText(serviceNav, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":SCPDURL/text()", _nsmgr);
      string serviceType;
      int serviceTypeVersion;
      if (!ParserHelper.TryParseTypeVersion_URN(ParserHelper.SelectText(serviceNav, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":serviceType/text()", _nsmgr),
          out serviceType, out serviceTypeVersion))
        throw new ArgumentException("'serviceType' content has the wrong format");
      string controlURL = ParserHelper.SelectText(serviceNav, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":controlURL", _nsmgr);
      string eventSubURL = ParserHelper.SelectText(serviceNav, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":eventSubURL", _nsmgr);
      return new ServiceDescriptor(rd, serviceType, serviceTypeVersion,
          ParserHelper.SelectText(serviceNav, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":serviceId/text()", _nsmgr), descriptionURL, controlURL, eventSubURL);
    }

    /// <summary>
    /// Searches through the underlaying device description document and finds all device entries with the given
    /// <paramref name="deviceType"/> and at least the given <paramref name="minDeviceVersion"/>.
    /// </summary>
    /// <param name="deviceType">Device type to search. Only devices will be returned with exactly the specified type.
    /// If this parameter is <c>null</c> or <see cref="string.Empty"/>, the first device will be returned.</param>
    /// <param name="minDeviceVersion">Minimum device version to search. All device elements will be returned with the
    /// specified version or with a higher version.
    /// If this parameter is <c>0</c>, the first device will be returned.</param>
    /// <returns>Enumeration of <see cref="DeviceDescriptor"/>s for devices of the specified device type and version.</returns>
    public IEnumerable<DeviceDescriptor> FindDevices(string deviceType, int minDeviceVersion)
    {
      XPathNavigator nav = _deviceNavigator.Clone();
      XPathNodeIterator it = nav.Select("descendant-or-self::" + DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":device", _nsmgr);
      while (it.MoveNext())
      {
        string type;
        int version;
        if (ParserHelper.TryParseTypeVersion_URN(ParserHelper.SelectText(it.Current, DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":deviceType/text()", _nsmgr),
            out type, out version) &&
            (string.IsNullOrEmpty(deviceType) || minDeviceVersion <= 0 || (type == deviceType && version >= minDeviceVersion)))
          yield return new DeviceDescriptor(_rootDescriptor, it.Current.Clone());
      }
    }

    /// <summary>
    /// Searches through the underlaying device description document and finds the first device entry with the given
    /// <paramref name="deviceType"/> and at least the given <paramref name="minDeviceVersion"/>.
    /// </summary>
    /// <param name="deviceType">Device type to search. Only devices will be returned with exactly the specified type.
    /// If this parameter is <c>null</c> or <see cref="string.Empty"/>, the first device will be returned.</param>
    /// <param name="minDeviceVersion">Minimum device version to search. All device elements will be returned with the
    /// specified version or with a higher version.
    /// If this parameter is <c>0</c>, the first device will be returned.</param>
    /// <returns>Descriptor for the first device which was found with the specified type and version. The search order depends on the
    /// XPath processor which is used by the framework. If no device element with the given criteria was found,
    /// <c>null</c> is returned.</returns>
    public DeviceDescriptor FindFirstDevice(string deviceType, int minDeviceVersion)
    {
      return FindDevices(deviceType, minDeviceVersion).FirstOrDefault();
    }

    /// <summary>
    /// Searches the device with the given <paramref name="deviceUUID"/> in the underlaying device description document.
    /// </summary>
    /// <param name="deviceUUID">UUID of the device to search.</param>
    /// <returns>Descriptor for the device with the given <paramref name="deviceUUID"/>, if present. Else, <c>null</c> is returned.</returns>
    public DeviceDescriptor FindDevice(string deviceUUID)
    {
      XPathNavigator nav = _deviceNavigator.Clone();
      XPathNodeIterator deviceIt = nav.Select("descendant-or-self::" + DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":device[" +
          DEVICE_DESCRIPTION_NAMESPACE_PREFIX + ":UDN/text()=concat(\"uuid:\",\"" + deviceUUID + "\")]", _nsmgr);
      if (!deviceIt.MoveNext())
        throw new ArgumentException(string.Format("Device with the specified id '{0}' isn't present in the given root descriptor", deviceUUID));
      return new DeviceDescriptor(_rootDescriptor, deviceIt.Current);
    }
  }
}