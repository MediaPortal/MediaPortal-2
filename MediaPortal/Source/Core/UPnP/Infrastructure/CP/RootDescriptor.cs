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

using System.Collections.Generic;
using System.Xml.XPath;
using UPnP.Infrastructure.CP.Description;
using UPnP.Infrastructure.CP.SSDP;

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
    protected readonly RootEntry _rootEntry;
    protected XPathDocument _deviceDescription = null;
    protected readonly IDictionary<string, IDictionary<string, ServiceDescriptor>> _serviceDescriptors =
        new Dictionary<string, IDictionary<string, ServiceDescriptor>>();
    protected RootDescriptorState _state = RootDescriptorState.Initializing;

    internal RootDescriptor(RootEntry rootEntry)
    {
      _rootEntry = rootEntry;
    }

    /// <summary>
    /// Gets the ynchronization object to use inside <c>lock()</c> statements when accessing data in this instance.
    /// </summary>
    public object SyncObj
    {
      get { return _rootEntry.SyncObj; }
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
    public XPathDocument DeviceDescription
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
    }

    /// <summary>
    /// Returns the root entry provided by the SSDP discovery protocol for this root descriptor.
    /// </summary>
    public RootEntry SSDPRootEntry
    {
      get { return _rootEntry; }
    }
  }
}
