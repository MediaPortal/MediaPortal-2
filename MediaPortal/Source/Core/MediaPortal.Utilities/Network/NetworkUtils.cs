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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MediaPortal.Utilities.Network
{
  /// <summary>
  /// Provides utility methods concerning networking stuff.
  /// </summary>
  public static class NetworkUtils
  {
    /// <summary>
    /// If set to <c>true</c>, ServicePoint.BindIPEndPointDelegate will be set to bind only to specific IP address.
    /// Enabling this option might help on systems with multiple networks attached (i.e. virtual nets), but cause
    /// connection issues in other cases. By default this option should remain <c>false</c>.
    /// </summary>
    public static bool LimitIPEndpoints { get; set; }

    /// <summary>
    /// Returns a string representation of an <see cref="IPAddress"/> in the form <c>123.123.123.123</c> (for IPv4 addresses)
    /// or <c>ABCD:ABCD::ABCD</c> (for IPv6 addresses). This method is different from the <see cref="IPAddress.ToString"/>
    /// method; it avoids the zone index which is added to IPv6 addresses by that method.
    /// </summary>
    /// <param name="address">IP address to create a textural representation for. May be of address family
    /// <see cref="AddressFamily.InterNetwork"/> or <see cref="AddressFamily.InterNetworkV6"/>.</param>
    /// <returns>String representation of the given ip address.</returns>
    public static string IPAddrToString(IPAddress address)
    {
      string result = address.ToString();
      int i = result.IndexOf('%');
      if (i >= 0) result = result.Substring(0, i);
      return result;
    }

    /// <summary>
    /// Used internally as callback delegate for the <see cref="ServicePoint.BindIPEndPointDelegate"/> property
    /// in <see cref="HttpWebRequest.ServicePoint"/>.
    /// </summary>
    /// <param name="servicePoint">The service point which is currently being bound.</param>
    /// <param name="remoteEndPoint">The desired remote endoint to reach.</param>
    /// <param name="preferredLocalIpAddress">Address which specifies the preferred local endpoint. This address is not part of the delegate
    /// signature and must be provided from outside.</param>
    /// <param name="retryCount">The number of times this delegate was called for a specified connection.</param>
    /// <returns>The local IPEndPoint to which the ServicePoint should bind.</returns>
    public static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint,
        IPAddress preferredLocalIpAddress, int retryCount)
    {
      if (retryCount > 0)
        return null;
      IPEndPoint ipe = new IPEndPoint(preferredLocalIpAddress, 0);
      return ipe;
    }

    /// <summary>
    /// Sets the local endpoint/network interface which is used to send the given HTTP <paramref name="request"/>.
    /// </summary>
    /// <remarks>
    /// Due to problems which can avoid HTTP requests being delivered correctly, it is sometimes necessary to give a HTTP request object a
    /// "hint" which local endpoint should be used. If HTTP requests time out (which is often the case when virtual VMWare network interfaces are
    /// installed), it might help to call this method.
    /// Callers should initially set the value <see cref="LimitIPEndpoints"/> to enable this behavior (default: disabled).
    /// </remarks>
    /// <param name="request">HTTP web request to patch.</param>
    /// <param name="preferredLocalIpAddress">Local IP address which is bound to the network interface which should be used to bind the
    /// outgoing HTTP request to.</param>
    public static void SetLocalEndpoint(HttpWebRequest request, IPAddress preferredLocalIpAddress)
    {
      if (LimitIPEndpoints)
        request.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) => BindIPEndPointCallback(servicePoint, remoteEndPoint, preferredLocalIpAddress, retryCount);
    }

    /// <summary>
    /// Determines whether any network connection is available.
    /// Can filter connections below a specified speed, virtual network cards and the <c>"Microsoft Loopback Adapter"</c>.
    /// </summary>
    /// <param name="minimumSpeed">The minimum speed required. Passing <c>null</c> will not filter a minimum speed.</param>
    /// <param name="filterVirtualCards">Controls if virtual network cards are filtered.</param>
    /// <returns><c>true</c> if a network connection is available according to the filter criteria; otherwise, <c>false</c>.</returns>
    public static bool IsNetworkAvailable(long? minimumSpeed, bool filterVirtualCards)
    {
      if (!NetworkInterface.GetIsNetworkAvailable())
        return false;

      foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
      {
        // Discard because of standard reasons
        if ((ni.OperationalStatus != OperationalStatus.Up) ||
            (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
            (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
          continue;

        // This allows to filter modems, serial, etc.
        // I use 10000000 as a minimum speed for most cases
        if (minimumSpeed.HasValue && ni.Speed < minimumSpeed)
          continue;

        // Discard virtual cards (virtual box, virtual pc, etc.)
        if (filterVirtualCards &&
            ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
             (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0)))
          continue;

        // Discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
        if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
          continue;

        return true;
      }
      return false;
    }

    /// <summary>
    /// Returns for each local network interface that is activated, not a loopback or tunnel interface and
    /// not the MS Loopback Adapter all IPv4 <see cref="UnicastIPAddressInformation"/>s 
    /// </summary>
    /// <returns></returns>
    public static ICollection<UnicastIPAddressInformation> GetAllLocalIPv4Networks()
    {
      var result = new HashSet<UnicastIPAddressInformation>();

      if (!NetworkInterface.GetIsNetworkAvailable())
        return result;

      foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
      {
        // Discard because NetworkInterface is down, a loopback adapter or a tunnel adapter
        if ((ni.OperationalStatus != OperationalStatus.Up) ||
            (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
            (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
          continue;

        // Discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
        if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
          continue;

        // Take all IPv4 addresses
        foreach (var unicastIpAddressInformation in ni.GetIPProperties().UnicastAddresses)
          if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
            result.Add(unicastIpAddressInformation);
      }
      return result;
    }

    /// <summary>
    /// Returns a collection of all IP addresses in a given subnet except the network address and the broadcast address
    /// </summary>
    /// <param name="network">Subnet for which the IP addresses should be returned</param>
    /// <returns>Collection of IP addresses in the given <see cref="network"/></returns>
    public static ICollection<IPAddress> GetAllAddressesInSubnet(UnicastIPAddressInformation network)
    {
      var address = ToUInt32(network.Address);
      var netMask = ToUInt32(network.IPv4Mask);
      var networkAddress = address & netMask;
      var broadcastAddress = networkAddress ^ ~netMask;

      // we leave out the networkAdress and the broadcastAddress
      var result = new List<IPAddress>();
      for (var i = networkAddress + 1; i < broadcastAddress; i++)
        result.Add(ToIpAddress(i));

      return result;
    }

    /// <summary>
    /// Converts a <see cref="IPAddress"/> object into a <see cref="UInt32"/>
    /// </summary>
    /// <remarks>
    /// Takes care of the host architecture (BigEndian vs. LittleEndian) and makes sure that
    /// "lower" <see cref="IPAddress"/>es are represented by lower <see cref="UInt32"/> numbers.
    /// I.e.: ToUint32(IPAddress.Parse("192.168.0.1")) is lower than ToUint32(IPAddress.Parse("192.168.0.2"))
    /// </remarks>
    /// <param name="address"><see cref="IPAddress"/> to convert</param>
    /// <returns><see cref="UInt32"/> representing the <see cref="IPAddress"/></returns>
    public static UInt32 ToUInt32(IPAddress address)
    {
      return BitConverter.ToUInt32(BitConverter.IsLittleEndian ? address.GetAddressBytes().Reverse().ToArray() : address.GetAddressBytes(), 0);
    }

    /// <summary>
    /// Converts a <see cref="UInt32"/> into a <see cref="IPAddress"/> object
    /// </summary>
    /// <param name="address"><see cref="UInt32"/> that was obtained from a call to <see cref="ToUInt32"/></param>
    /// <returns><see cref="IPAddress"/> calculated from <param name="address"></param></returns>
    public static IPAddress ToIpAddress(UInt32 address)
    {
      return new IPAddress(BitConverter.IsLittleEndian ? BitConverter.GetBytes(address).Reverse().ToArray() : BitConverter.GetBytes(address));
    }
  }
}
