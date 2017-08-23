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
using System.Net;
using System.Net.Sockets;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.GENA;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv
{
  /// <summary>
  /// Stores all URLs parameters for a UPnP service.
  /// </summary>
  public class ServicePaths
  {
    /// <summary>
    /// The URL where the service's description can be requested.
    /// </summary>
    public string SCPDPath;

    /// <summary>
    /// The URL to be used for control of the service.
    /// </summary>
    public string ControlPath;

    /// <summary>
    /// The URL to be used for event subscriptions.
    /// </summary>
    public string EventSubPath;
  }

  /// <summary>
  /// Stores the configuration data for one UPnP endpoint. A UPnP endpoint basically represents a local IP address which is
  /// available in the network.
  /// </summary>
  public class EndpointConfiguration
  {
    protected Socket _ssdpUdpUnicastSocket = null;
    protected Socket _ssdpUdpMulticastReceiveSocket = null;
    protected Socket _genaUdpSocket = null;
    protected IPAddress _endpointIPAddress = null;
    protected int _httpServerPort = 0;
    protected bool _ssdpUsesSpecialSearchPort = false;
    protected int _ssdpSearchPort = UPnPConsts.DEFAULT_SSDP_SEARCH_PORT;
    protected IPAddress _ssdpMulticastAddress = null;
    protected IPAddress _genaMulticastAddress = null;
    protected string _descriptionPathBase = null;
    protected string _controlURLBase = null;
    protected string _eventSubURLBase = null;
    protected IDictionary<string, DvDevice> _rootDeviceDescriptionPathsToRootDevices = new Dictionary<string, DvDevice>(StringComparer.InvariantCultureIgnoreCase);
    protected IDictionary<DvDevice, string> _rootDeviceDescriptionPaths = new Dictionary<DvDevice, string>();
    protected IDictionary<string, DvService> _scpdPathsToServices = new Dictionary<string, DvService>(StringComparer.InvariantCultureIgnoreCase);
    protected IDictionary<string, DvService> _controlPathsToServices = new Dictionary<string, DvService>(StringComparer.InvariantCultureIgnoreCase);
    protected IDictionary<string, DvService> _eventSubPathsToServices = new Dictionary<string, DvService>(StringComparer.InvariantCultureIgnoreCase);
    protected IDictionary<DvService, ServicePaths> _servicePaths = new Dictionary<DvService, ServicePaths>();
    
    protected ICollection<EventSubscription> _eventSubscriptions = new List<EventSubscription>();
    protected Int32 _configId = 0;

    /// <summary>
    /// Socket which is used to a) send unicast and multicast messages and b) receive unicast messages for the SSDP protocol.
    /// </summary>
    public Socket SSDP_UDP_UnicastSocket
    {
      get { return _ssdpUdpUnicastSocket; }
      internal set { _ssdpUdpUnicastSocket = value; }
    }

    /// <summary>
    /// UDP socket which is used for SSDP to receive multicast messages over this UPnP endpoint.
    /// </summary>
    public Socket SSDP_UDP_MulticastReceiveSocket
    {
      get { return _ssdpUdpMulticastReceiveSocket; }
      internal set { _ssdpUdpMulticastReceiveSocket = value; }
    }

    /// <summary>
    /// UDP socket which is used for GENA to send messages over this UPnP endpoint.
    /// </summary>
    public Socket GENA_UDP_Socket
    {
      get { return _genaUdpSocket; }
      internal set { _genaUdpSocket = value; }
    }

    /// <summary>
    /// Address family (IPv4/IPv6) of this endpoint.
    /// </summary>
    public AddressFamily AddressFamily
    {
      get { return _endpointIPAddress.AddressFamily; }
    }

    /// <summary>
    /// The local IP address of this endpoint.
    /// </summary>
    public IPAddress EndPointIPAddress
    {
      get { return _endpointIPAddress; }
      internal set { _endpointIPAddress = value; }
    }

    /// <summary>
    /// The port where the HTTP server, which corresponds to this endpoint, listens.
    /// </summary>
    public int HTTPServerPort
    {
      get { return _httpServerPort; }
      internal set { _httpServerPort = value; }
    }

    /// <summary>
    /// Returns the information if the <see cref="SSDPSearchPort"/> is another port than
    /// <see cref="UPnPConsts.DEFAULT_SSDP_SEARCH_PORT"/>.
    /// </summary>
    public bool SSDPUsesSpecialSearchPort
    {
      get { return _ssdpUsesSpecialSearchPort; }
    }

    /// <summary>
    /// Port to be used for SSDP unicast messages.
    /// </summary>
    public int SSDPSearchPort
    {
      get { return _ssdpSearchPort; }
      set
      {
        _ssdpSearchPort = value;
        _ssdpUsesSpecialSearchPort = value != UPnPConsts.DEFAULT_SSDP_SEARCH_PORT;
      }
    }

    /// <summary>
    /// Multicast address to that this endpoint is bound for the SSDP protocol.
    /// </summary>
    public IPAddress SSDPMulticastAddress
    {
      get { return _ssdpMulticastAddress; }
      internal set { _ssdpMulticastAddress = value; }
    }

    /// <summary>
    /// Multicast address this endpoint uses for the GENA protocol.
    /// </summary>
    public IPAddress GENAMulticastAddress
    {
      get { return _genaMulticastAddress; }
      internal set { _genaMulticastAddress = value; }
    }

    /// <summary>
    /// Base path for all description documents (device description and SCPD documents).
    /// Contains no server and port. Contains leading and trailing '/' characters. Depends on the UPnP endpoint.
    /// </summary>
    public string DescriptionPathBase
    {
      get { return _descriptionPathBase; }
      internal set { _descriptionPathBase = StringUtils.CheckPrefix(StringUtils.CheckSuffix(value, "/"), "/"); }
    }

    /// <summary>
    /// Base url for control. Contains leading and trailing '/' characters. Depends on the UPnP endpoint.
    /// </summary>
    public string ControlPathBase
    {
      get { return _controlURLBase; }
      internal set { _controlURLBase = StringUtils.CheckPrefix(StringUtils.CheckSuffix(value, "/"), "/"); }
    }

    /// <summary>
    /// Base url for eventing. Contains leading and trailing '/' characters. Depends on the UPnP endpoint.
    /// </summary>
    public string EventSubPathBase
    {
      get { return _eventSubURLBase; }
      internal set { _eventSubURLBase = StringUtils.CheckPrefix(StringUtils.CheckSuffix(value, "/"), "/"); }
    }

    // URLs which are used for communication over this UPnP endpoint

    /// <summary>
    /// Mapping of root device description URL paths to the associated root device in this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvDevice> RootDeviceDescriptionPathsToRootDevices
    {
      get { return _rootDeviceDescriptionPathsToRootDevices; }
    }

    /// <summary>
    /// Mapping of root devices to their associated description URL paths.
    /// </summary>
    public IDictionary<DvDevice, string> RootDeviceDescriptionPaths
    {
      get { return _rootDeviceDescriptionPaths; }
    }

    /// <summary>
    /// Mapping of service description (SCPD document) URL paths to the associated UPnP service for this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvService> SCPDPathsToServices
    {
      get { return _scpdPathsToServices; }
    }

    /// <summary>
    /// Mapping of service control URL paths to the associated UPnP service for this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvService> ControlPathsToServices
    {
      get { return _controlPathsToServices; }
    }

    /// <summary>
    /// Mapping of service event subscription URL paths to the associated UPnP service for this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvService> EventSubPathsToServices
    {
      get { return _eventSubPathsToServices; }
    }

    /// <summary>
    /// Mapping of services to its associated paths.
    /// </summary>
    public IDictionary<DvService, ServicePaths> ServicePaths
    {
      get { return _servicePaths; }
    }

    /// <summary>
    /// Holds all data for event subscriptions which were made over this connection endpoint.
    /// </summary>
    public ICollection<EventSubscription> EventSubscriptions
    {
      get { return _eventSubscriptions; }
    }

    /// <summary>
    /// UPnP CONFIGID. Contains a value indicating the current UPnP server configuration.
    /// Must be increased when the configuration changes. Must be in the range from 0 to 16777215 (2^24-1).
    /// </summary>
    public Int32 ConfigId
    {
      get { return _configId; }
      internal set { _configId = value; }
    }

    /// <summary>
    /// Returns an URL which points to the root device description of the given <paramref name="rootDevice"/> on this endpoint.
    /// </summary>
    /// <param name="rootDevice">Root device to get the description URL for.</param>
    /// <returns>Absolute URL to the device description.</returns>
    public string GetRootDeviceDescriptionURL(DvDevice rootDevice)
    {
      return GetEndpointHttpPrefixString() + _rootDeviceDescriptionPaths[rootDevice];
    }

    public string GetEndpointHttpPrefixString()
    {
      return "http://" + NetworkHelper.IPEndPointToString(EndPointIPAddress, HTTPServerPort);
    }
  }
}
