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

using System.Net;
using System.Net.Sockets;

namespace UPnP.Infrastructure.CP
{
  public class EndpointConfiguration
  {
    /// <summary>
    /// Convenience property for the call to <c>EndPointIPAddress.AddressFamily</c>.
    /// </summary>
    public AddressFamily AddressFamily
    {
      get { return EndPointIPAddress.AddressFamily; }
    }

    /// <summary>
    /// The IP address which specifies this endpoint.
    /// </summary>
    public IPAddress EndPointIPAddress;

    /// <summary>
    /// Socket which is bound to all necessary multicast groups for this endpoint and which is used to
    /// receive multicast messages for the SSDP protocol.
    /// </summary>
    public Socket SSDP_UDP_MulticastReceiveSocket;

    /// <summary>
    /// Socket which is bound to all necessary multicast groups for this endpoint and which is used to
    /// receive multicast event messages for the GENA protocol.
    /// </summary>
    public Socket GENA_UDP_MulticastReceiveSocket;

    /// <summary>
    /// Socket which is used to a) send unicast and multicast messages and b) receive unicast messages for the SSDP protocol.
    /// </summary>
    public Socket SSDP_UDP_UnicastSocket;

    /// <summary>
    /// Address which is used to receive and send multicast messages for this endpoint. This address takes into account
    /// that for IPv6 messages, the scope must be set according to the type of endpoint IP address.
    /// </summary>
    public IPAddress SSDPMulticastAddress;
  }
}
