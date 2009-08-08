using System.Collections.Generic;

namespace UPnP.Infrastructure.CP.SSDP
{
  /// <summary>
  /// Contains SSDP advertisement data for a the advertisements of a special device.
  /// The entries are lazily initialized, as the SSDP protocol doesn't provide a strict order of advertisement messages of
  /// the single entries.
  /// </summary>
  public class DeviceEntry
  {
    protected string _deviceType;
    protected int _deviceTypeVersion;
    protected string _uuid; // UUID of the device
    protected ICollection<string> _services = new HashSet<string>(); // Types and versions of the services

    /// <summary>
    /// Creates a new <see cref="DeviceEntry"/> instance.
    /// </summary>
    /// <param name="uuid">UUID of the device which gets represented by the new instance.</param>
    public DeviceEntry(string uuid)
    {
      _uuid = uuid;
    }

    /// <summary>
    /// UUID of the device which gets represented by this instance.
    /// </summary>
    public string UUID
    {
      get { return _uuid; }
    }

    /// <summary>
    /// Gets or sets the type of the represented device.
    /// </summary>
    public string DeviceType
    {
      get { return _deviceType; }
      internal set { _deviceType = value; }
    }

    /// <summary>
    /// Gets or sets the type version of the represented device.
    /// </summary>
    public int DeviceTypeVersion
    {
      get { return _deviceTypeVersion; }
      internal set { _deviceTypeVersion = value; }
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
    /// Returns a collection of service type URNs with version, in the format
    /// "urn:schemas-upnp-org:service:[service-type]:[version]" or "urn:domain-name:service:[service-type]:[version]".
    /// </summary>
    public ICollection<string> Services
    {
      get { return _services; }
    }
  }
}