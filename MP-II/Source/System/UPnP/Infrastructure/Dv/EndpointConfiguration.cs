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
using System.Net;
using System.Net.Sockets;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.GENA;

namespace UPnP.Infrastructure.Dv
{
  /// <summary>
  /// Stores all URLs parameters for a UPnP service.
  /// </summary>
  public class ServiceURLs
  {
    /// <summary>
    /// The URL where the service's description can be requested.
    /// </summary>
    public string SCDPURL;

    /// <summary>
    /// The URL to be used for control of the service.
    /// </summary>
    public string ControlURL;

    /// <summary>
    /// The URL to be used for event subscriptions.
    /// </summary>
    public string EventSubURL;
  }

  /// <summary>
  /// Stores the configuration data for one UPnP endpoint. A UPnP endpoint basically represents a local IP address which is
  /// available in the network.
  /// </summary>
  public class EndpointConfiguration
  {
    protected Socket _ssdpUdpUnicastSocket = null;
    protected Socket _ssdpUdpMulticastReceiveSocket = null;
    protected UdpClient _genaUdpClient = null;
    protected IPAddress _endpointIPAddress = null;
    protected bool _ssdpUsesSpecialSearchPort = false;
    protected int _ssdpSearchPort = UPnPConsts.DEFAULT_SSDP_SEARCH_PORT;
    protected int _endPointGENAPort = 0;
    protected IPAddress _ssdpMulticastAddress = null;
    protected IPAddress _genaMulticastAddress = null;
    protected string _descriptionURLBase = null;
    protected string _controlURLBase = null;
    protected string _eventSubURLBase = null;
    protected IDictionary<string, DvDevice> _rootDeviceDescriptionURLsToRootDevices = new Dictionary<string, DvDevice>();
    protected IDictionary<DvDevice, string> _rootDeviceDescriptionURLs = new Dictionary<DvDevice, string>();
    protected IDictionary<string, DvService> _scpdURLsToServices = new Dictionary<string, DvService>();
    protected IDictionary<string, DvService> _controlURLsToServices = new Dictionary<string, DvService>();
    protected IDictionary<string, DvService> _eventSubURLsToServices = new Dictionary<string, DvService>();
    protected IDictionary<DvService, ServiceURLs> _serviceURLs = new Dictionary<DvService, ServiceURLs>();
    protected ICollection<EventSubscription> _eventSubscriptions = new List<EventSubscription>();

    /// <summary>
    /// UDP socket which is used for SSDP to send unicast messages over this UPnP endpoint.
    /// </summary>
    public Socket SSDP_UDP_UnicastSocket
    {
      get { return _ssdpUdpUnicastSocket; }
      internal set { _ssdpUdpUnicastSocket = value; }
    }

    /// <summary>
    /// UDP socket which is used for SSDP to receive over this UPnP endpoint.
    /// </summary>
    public Socket SSDP_UDP_MulticastReceiveSocket
    {
      get { return _ssdpUdpMulticastReceiveSocket; }
      internal set { _ssdpUdpMulticastReceiveSocket = value; }
    }

    /// <summary>
    /// UDP client which is used for GENA to send and receive over this UPnP endpoint.
    /// </summary>
    public UdpClient GENA_UDPClient
    {
      get { return _genaUdpClient; }
      internal set { _genaUdpClient = value; }
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
    /// Port to be used for GENA messages.
    /// </summary>
    public int EndPointGENAPort
    {
      get { return _endPointGENAPort; }
      internal set { _endPointGENAPort = value; }
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
    /// Base url for all description documents (device description and SCPD documents).
    /// Contains a trailing '/' character. Depends on the UPnP endpoint.
    /// </summary>
    public string DescriptionURLBase
    {
      get { return _descriptionURLBase; }
      internal set { _descriptionURLBase = value; }
    }

    /// <summary>
    /// Base url for control. Contains a trailing '/' character. Depends on the UPnP endpoint.
    /// </summary>
    public string ControlURLBase
    {
      get { return _controlURLBase; }
      internal set { _controlURLBase = value; }
    }

    /// <summary>
    /// Base url for eventing. Contains a trailing '/' character. Depends on the UPnP endpoint.
    /// </summary>
    public string EventSubURLBase
    {
      get { return _eventSubURLBase; }
      internal set { _eventSubURLBase = value; }
    }

    // URLs which are used for communication over this UPnP endpoint

    /// <summary>
    /// Mapping of root device description URLs to the associated root device in this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvDevice> RootDeviceDescriptionURLsToRootDevices
    {
      get { return _rootDeviceDescriptionURLsToRootDevices; }
    }

    /// <summary>
    /// Mapping of root devices to their associated description URLs.
    /// </summary>
    public IDictionary<DvDevice, string> RootDeviceDescriptionURLs
    {
      get { return _rootDeviceDescriptionURLs; }
    }

    /// <summary>
    /// Mapping of service description (SCPD document) URLs to the associated UPnP service for this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvService> SCPDURLsToServices
    {
      get { return _scpdURLsToServices; }
    }

    /// <summary>
    /// Mapping of service control URLs to the associated UPnP service for this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvService> ControlURLsToServices
    {
      get { return _controlURLsToServices; }
    }

    /// <summary>
    /// Mapping of service event subscription URLs to the associated UPnP service for this UPnP endpoint.
    /// </summary>
    public IDictionary<string, DvService> EventSubURLsToServices
    {
      get { return _eventSubURLsToServices; }
    }

    /// <summary>
    /// Mapping of services to its associated URLs.
    /// </summary>
    public IDictionary<DvService, ServiceURLs> ServiceURLs
    {
      get { return _serviceURLs; }
    }

    /// <summary>
    /// Holds all data for event subscriptions which were made over this connection endpoint.
    /// </summary>
    public ICollection<EventSubscription> EventSubscriptions
    {
      get { return _eventSubscriptions; }
    }
  }
}
