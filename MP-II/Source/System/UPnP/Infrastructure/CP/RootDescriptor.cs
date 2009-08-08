using System.Collections.Generic;
using System.Xml;
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
    /// Returns the XML device description document of this root descriptor or <c>null</c>, if the description wasn't fetched
    /// yet.
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
  }
}
