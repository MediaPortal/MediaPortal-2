#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *  Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Net;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.SSDP
{
  /// <summary>
  /// Contains data which is necessary to communicate with a UPnP device. Multi-homed devices are accessible via multiple
  /// links; each possible link configuration is stored in its own instance of <see cref="LinkData"/>.
  /// </summary>
  public class LinkData : IComparable<LinkData>
  {
    protected string _descriptionLocation;
    protected string _descriptionServer;
    protected EndpointConfiguration _endpoint;
    protected HTTPVersion _httpVersion;
    protected int _searchPort = UPnPConsts.DEFAULT_SSDP_SEARCH_PORT;

    /// <summary>
    /// Creates a new link configuration for the communication with a UPnP device.
    /// </summary>
    /// <param name="endpoint">UPnP endpoint where the advertisement was received.</param>
    /// <param name="descriptionLocation">Location of the description document for this link.</param>
    /// <param name="httpVersion">HTTP version our partner is using.</param>
    /// <param name="searchPort">Search port used in the new link.</param>
    public LinkData(EndpointConfiguration endpoint, string descriptionLocation, HTTPVersion httpVersion, int searchPort)
    {
      _endpoint = endpoint;
      _descriptionLocation = descriptionLocation;
      _descriptionServer = new Uri(_descriptionLocation).Host;
      _httpVersion = httpVersion;
      _searchPort = searchPort;
    }

    /// <summary>
    /// Gets or sets the URL of the description for this device advertisement.
    /// </summary>
    public string DescriptionLocation
    {
      get { return _descriptionLocation; }
    }

    /// <summary>
    /// Returns the network endpoint where this root device's advertisement is tracked.
    /// </summary>
    public EndpointConfiguration Endpoint
    {
      get { return _endpoint; }
    }

    /// <summary>
    /// Returns the search port the remote SSDP server uses for unicast messaging.
    /// </summary>
    public int SearchPort
    {
      get { return _searchPort; }
    }

    /// <summary>
    /// Gets the distance of the description location. See <see cref="NetworkHelper.ZERO_DISTANCE"/>, <see cref="NetworkHelper.LINK_LOCAL_DISTANCE"/>,
    /// <see cref="NetworkHelper.SITE_LOCAL_DISTANCE"/> and <see cref="NetworkHelper.GLOBAL_DISTANCE"/>.
    /// </summary>
    public int LinkDistance
    {
      get
      {
        IPAddress address;
        return IPAddress.TryParse(_descriptionServer, out address) ?
            NetworkHelper.GetLinkDistance(address) : NetworkHelper.GLOBAL_DISTANCE;
      }
    }

    /// <summary>
    /// Returns the HTTP version the device's server uses for communication.
    /// </summary>
    public HTTPVersion HTTPVersion
    {
      get { return _httpVersion; }
    }

    public bool IsNearer(LinkData other)
    {
      return CompareTo(other) < 0;
    }

    public int CompareTo(LinkData other)
    {
      return LinkDistance - other.LinkDistance;
    }
  }

  /// <summary>
  /// Contains SSDP advertisement data for a collection of device and service advertisements which are located in the same root
  /// device.
  /// </summary>
  /// <remarks>
  /// The entries are lazily initialized, as the SSDP protocol doesn't provide a strict order of advertisement messages of
  /// the single entries.
  /// </remarks>
  public class RootEntry
  {
    protected UPnPVersion _upnpVersion;
    protected string _osVersion;
    protected string _productVersion;
    protected DateTime _expirationTime;
    protected LinkData _preferredLink = null;
    protected IDictionary<string, LinkData> _linkConfigurations = new Dictionary<string, LinkData>();
    protected string _rootDeviceID; // UUID of the root device
    protected IDictionary<string, DeviceEntry> _devices = new Dictionary<string, DeviceEntry>(); // Device UIDs to DeviceEntry structures
    protected uint _bootID = 0;
    protected IDictionary<IPEndPoint, uint> _configIDs = new Dictionary<IPEndPoint, uint>();
    protected IDictionary<string, object> _clientProperties = new Dictionary<string, object>();

    /// <summary>
    /// Creates a new <see cref="RootEntry"/> instance.
    /// </summary>
    /// <param name="deviceUUID">UUID of the root device.</param>
    /// <param name="upnpVersion">UPnP version the remote device is using.</param>
    /// <param name="osVersion">OS and version our partner is using.</param>
    /// <param name="productVersion">Product and version our partner is using.</param>
    /// <param name="expirationTime">Time when the advertisement will expire.</param>
    public RootEntry(string deviceUUID, UPnPVersion upnpVersion, string osVersion, string productVersion, DateTime expirationTime)
    {
      _rootDeviceID = deviceUUID;
      _upnpVersion = upnpVersion;
      _osVersion = osVersion;
      _productVersion = productVersion;
      _expirationTime = expirationTime;
    }

    /// <summary>
    /// Returns the UPnP version the device's server uses for communication.
    /// </summary>
    public UPnPVersion UPnPVersion
    {
      get { return _upnpVersion; }
    }

    /// <summary>
    /// Returns the OS and version of the device's server.
    /// </summary>
    public string OS_Version
    {
      get { return _osVersion; }
    }

    /// <summary>
    /// Returns the product and version of the device.
    /// </summary>
    public string Product_Version
    {
      get { return _productVersion; }
    }

    /// <summary>
    /// Gets or sets the boot ID of the UPnP server.
    /// </summary>
    public uint BootID
    {
      get { return _bootID; }
      internal set { _bootID = value; }
    }

    /// <summary>
    /// Gets or sets the expiration time for the last advertisement of any of this root entrie's devices or services.
    /// </summary>
    public DateTime ExpirationTime
    {
      get { return _expirationTime; }
      internal set { _expirationTime = value; }
    }

    /// <summary>
    /// Gets the best available link to the device of this device root entry.
    /// </summary>
    public LinkData PreferredLink
    {
      get { return _preferredLink; }
    }

    /// <summary>
    /// Gets a mapping of device UUIDs to <see cref="DeviceEntry"/> instances describing the contained devices.
    /// </summary>
    public IDictionary<string, DeviceEntry> Devices
    {
      get { return _devices; }
    }

    /// <summary>
    /// Gets the root device's UUID.
    /// </summary>
    public string RootDeviceID
    {
      get { return _rootDeviceID; }
    }

    /// <summary>
    /// Returns a dictionary where clients can add additional properties to this root entry.
    /// </summary>
    public IDictionary<string, object> ClientProperties
    {
      get { return _clientProperties; }
    }

    /// <summary>
    /// Gets the configuration ID of the UPnP server for the given remote endpoint.
    /// If the configuration ID changes, all description document caches must be flushed and rebuilt, if necessary.
    /// The configuration ID depends on the remote endpoint because description documents are different for different server endpoints
    /// and thus have different config ids.
    /// </summary>
    public uint GetConfigID(IPEndPoint remoteEndPoint)
    {
      uint result;
      if (_configIDs.TryGetValue(remoteEndPoint, out result))
        return result;
      return 0;
    }

    /// <summary>
    /// Sets the configuration ID of the UPnP server for the given remote endpoint.
    /// If the configuration ID changes, all description document caches must be flushed and rebuilt, if necessary.
    /// </summary>
    internal void SetConfigID(IPEndPoint remoteEndPoint, uint value)
    {
      _configIDs[remoteEndPoint] = value;
    }

    internal LinkData AddOrUpdateLink(EndpointConfiguration endpoint, string descriptionLocation,
        HTTPVersion httpVersion, int searchPort)
    {
      LinkData result;
      if (!_linkConfigurations.TryGetValue(descriptionLocation, out result))
        _linkConfigurations.Add(descriptionLocation, result = new LinkData(endpoint, descriptionLocation, httpVersion, searchPort));
      if (_preferredLink == null || result.IsNearer(_preferredLink))
        _preferredLink = result;
      return result;
    }

    /// <summary>
    /// Gets the existing device entry contained in this root entry for the specified <paramref name="uuid"/> or creates it.
    /// </summary>
    /// <param name="uuid">UUID of the device, whose entry should be returned.</param>
    /// <returns><see cref="DeviceEntry"/> instance with the given <see cref="uuid"/>.</returns>
    internal DeviceEntry GetOrCreateDeviceEntry(string uuid)
    {
      DeviceEntry result;
      if (_devices.TryGetValue(uuid, out result))
        return result;
      return _devices[uuid] = new DeviceEntry(uuid);
    }
  }
}
