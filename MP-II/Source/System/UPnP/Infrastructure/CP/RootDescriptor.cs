#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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
using System.Xml;
using UPnP.Infrastructure.CP.SSDP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// State enumeration for the <see cref="RootDescriptor"/>.
  /// </summary>
  public enum RootDescriptorState
  {
    /// <summary>
    /// The UPnP system is initializing the root descriptor.
    /// </summary>
    Initializing,

    /// <summary>
    /// The corresponding UPnP network device is present in the network and the UPnP system is just requesting
    /// the device description document.
    /// </summary>
    AwaitingDeviceDescription,

    /// <summary>
    /// The corresponding UPnP network device is present in the network and the UPnP system is just requesting
    /// the service description documents.
    /// </summary>
    AwaitingServiceDescriptions,

    /// <summary>
    /// The corresponding UPnP network device is present in the network and ready to be used. Some contained devices
    /// and services might already be connected to device and service templates.
    /// </summary>
    Ready,

    /// <summary>
    /// The corresponding UPnP network device is not present in the network any more.
    /// </summary>
    Invalid,

    /// <summary>
    /// There was an unrecoverable error when communicating with the UPnP network device, in any of the communication
    /// protocol layers (for example a description document is erroneous, etc.).
    /// </summary>
    Erroneous,
  }

  /// <summary>
  /// Descriptor which aggregates available information about all UPnP devices and services embedded in a single
  /// UPnP root device.
  /// </summary>
  public class RootDescriptor
  {
    protected RootEntry _rootEntry;
    protected XmlDocument _deviceDescription = null;
    protected IDictionary<string, IDictionary<string, ServiceDescriptor>> _serviceDescriptors =
        new Dictionary<string, IDictionary<string, ServiceDescriptor>>();
    protected RootDescriptorState _state = RootDescriptorState.Initializing;

    internal RootDescriptor(RootEntry rootEntry)
    {
      _rootEntry = rootEntry;
    }

    /// <summary>
    /// Gets or sets the state of this root descriptor. The state provides the information whether the setup of this
    /// descriptor is already finished, whether there were errors while communicating with the UPnP server and some
    /// others. See <see cref="RootDescriptorState"/> for a complete list of states.
    /// </summary>
    public RootDescriptorState State
    {
      get { return _state; }
      internal set { _state = value; }
    }

    /// <summary>
    /// Returns the XML device description document of this root descriptor or <c>null</c>, if the description wasn't
    /// fetched yet.
    /// </summary>
    /// <remarks>
    /// The description is present when the root descriptor is in the <see cref="State"/>s
    /// <see cref="RootDescriptorState.AwaitingServiceDescriptions"/> and <see cref="RootDescriptorState.Ready"/>.
    /// </remarks>
    public XmlDocument DeviceDescription
    {
      get { return _deviceDescription; }
      internal set { _deviceDescription = value; }
    }

    /// <summary>
    /// Returns a mapping of device UUIDs to descriptors of their contained services.
    /// </summary>
    /// <remarks>
    /// The service descriptions are ready to be evaluated when the root descriptor is in <see cref="State"/>
    /// <see cref="RootDescriptorState.Ready"/>. In state <see cref="RootDescriptorState.AwaitingServiceDescriptions"/>,
    /// the <see cref="ServiceDescriptor"/> instances are all present in this dictionary, but their description documents
    /// are not present yet (i.e. their state might not be <see cref="ServiceDescriptorState.Ready"/> yet).
    /// </remarks>
    public IDictionary<string, IDictionary<string, ServiceDescriptor>> ServiceDescriptors
    {
      get { return _serviceDescriptors; }
      internal set { _serviceDescriptors = value; }
    }

    /// <summary>
    /// Returns the root entry provided by the SSDP discovery protocol for this root descriptor.
    /// </summary>
    public RootEntry SSDPRootEntry
    {
      get { return _rootEntry; }
      internal set { _rootEntry = value; }
    }

    /// <summary>
    /// Searches through the <see cref="DeviceDescription"/> XML document and finds all device entries with the given
    /// <paramref name="deviceType"/> and at least the given <paramref name="minDeviceVersion"/>.
    /// </summary>
    /// <param name="deviceType">Device type to search. Only devices will be returned with exactly the specified type.</param>
    /// <param name="minDeviceVersion">Minimum device version to search. All device elements will be returned with the
    /// specified version or with a higher version.</param>
    /// <returns>Enumeration of XML &lt;device&gt; elements of the specified device type and version.</returns>
    public IEnumerable<XmlElement> FindDeviceElements(string deviceType, int minDeviceVersion)
    {
      foreach (XmlElement deviceElement in _deviceDescription.SelectNodes("descendant::device"))
      {
        string type;
        int version;
        if (ParserHelper.TryParseTypeVersion_URN(((XmlText) deviceElement.SelectSingleNode("deviceType/text()")).Data,
            out type, out version) && type == deviceType && version >= minDeviceVersion)
          yield return deviceElement;
      }
      yield break;
    }

    /// <summary>
    /// Searches through the <see cref="DeviceDescription"/> XML document and finds the first device entry with the given
    /// <paramref name="deviceType"/> and at least the given <paramref name="minDeviceVersion"/>.
    /// </summary>
    /// <param name="deviceType">Device type to search. A device will only be returned if its type equals the
    /// specified type.</param>
    /// <param name="minDeviceVersion">Minimum device version to search. A device element will be returned if its version
    /// is exactly the specified version or higher.</param>
    /// <returns>First device which was found with the specified type and version. The search order depends on the
    /// XPath processor which is used by the framework.</returns>
    public XmlElement FindFirstDeviceElement(string deviceType, int minDeviceVersion)
    {
      foreach (XmlElement deviceElement in FindDeviceElements(deviceType, minDeviceVersion))
        return deviceElement;
      return null;
    }

    /// <summary>
    /// Reads the UDN of the device with the given device's description XML element.
    /// </summary>
    /// <param name="deviceElement">&lt;device&gt; element of a device's description</param>
    /// <returns>UDN of the device or <c>null</c> if the given <paramref name="deviceElement"/> doesn't contain
    /// an UDN entry. The UDN is specifically of the form "uuid:[Device-ID]".</returns>
    public static string GetDeviceUDN(XmlElement deviceElement)
    {
      return ((XmlText) deviceElement.SelectSingleNode("UDN/text()")).Data;
    }

    /// <summary>
    /// Reads the UUID of the device with the given device's description XML element.
    /// </summary>
    /// <param name="deviceElement">&lt;device&gt; element of a device's description</param>
    /// <returns>UUID of the device or <c>null</c> if the given <paramref name="deviceElement"/> doesn't contain
    /// an UDN entry. The returned UUID is the part of the device's UDN after the "uuid:" prefix.</returns>
    public static string GetDeviceUUID(XmlElement deviceElement)
    {
      string udn = GetDeviceUDN(deviceElement);
      if (udn == null || !udn.StartsWith("uuid:"))
        return null;
      return udn.Substring("uuid:".Length);
    }
  }
}
