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
using System.Security;
using System.Threading;
using MediaPortal.Utilities;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// Contains helper methods for network handling.
  /// </summary>
  /// <remarks>
  /// Some methods are doubled from <see cref="MediaPortal.Utilities.Network.NetworkUtils"/>. This is made by design to
  /// make the UPnP library as independent as possible. Furthermore I don't want to depend on inconsiderate
  /// implementation changes of networking methods in the MediaPortal system.
  /// </remarks>
  public class NetworkHelper
  {
    public static int ZERO_DISTANCE = 0;
    public static int LINK_LOCAL_DISTANCE = 1;
    public static int SITE_LOCAL_DISTANCE = 2;
    public static int GLOBAL_DISTANCE = 3;

    /// <summary>
    /// Returns all local ip addresses which are known via DNS.
    /// </summary>
    /// <param name="filters">Human readable list of IPs (mixed IPv4 or IPv6) used to filter the collection</param>
    /// <returns>Collection of local IP addresses.</returns>
    public static ICollection<IPAddress> GetExternalIPAddresses(List<string> filters)
    {
      try
      {
        string hostName = Dns.GetHostName();
        ICollection<IPAddress> addresses = new HashSet<IPAddress>(Dns.GetHostAddresses(hostName));
        FilterNetworkAddresses(ref addresses, filters, false);
        return addresses;
      }
      catch (SocketException)
      {
        UPnPConfiguration.LOGGER.Error("NetworkHelper: Error retrieving external IP addresses for the UPnP system");
        return new List<IPAddress>();
      }
    }

    /// <summary>
    /// Collects a filtered list IP addresses to bind to, or Any if filtering is not required.
    /// </summary>
    /// <param name="family">IPv4 or IPv6</param>
    /// <param name="filters">Human readable list of IPs (mixed IPv4 or IPv6) used to filter the collection</param>
    /// <returns>Collection of IP addresses.</returns>
    public static ICollection<IPAddress> GetBindableIPAddresses(AddressFamily family, List<string> filters)
    {
      ICollection<IPAddress> result = new HashSet<IPAddress>();
      if (filters == null || filters.Count == 0)
      {
        result.Add((family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any));
      }
      else
      {
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface inf in interfaces)
          CollectionUtils.AddAll(result, inf.GetIPProperties().UnicastAddresses.Select(x => x.Address).Where(x => x.AddressFamily == family));

        FilterNetworkAddresses(ref result, filters, true, family);
        result.Add(family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Loopback : IPAddress.Loopback);
      }
      return result;
    }

    /// <summary>
    /// Helper method to filter list of <paramref name="availableAddresses"/> using the given <paramref name="addressFilters"/>. If <paramref name="filterByFamily"/> is set to <c>true</c>,
    /// the lists will be filtered for given <paramref name="family"/>, otherwise all entries are checked.
    /// If an invalid filter is defined, a warning will be logged.
    /// </summary>
    /// <param name="availableAddresses">List of available IPAddresses</param>
    /// <param name="addressFilters">List of filters to include</param>
    /// <param name="filterByFamily"><c>true</c> to filter by family</param>
    /// <param name="family">Address family</param>
    private static void FilterNetworkAddresses(ref ICollection<IPAddress> availableAddresses, List<string> addressFilters, bool filterByFamily = true, AddressFamily family = AddressFamily.InterNetwork)
    {
      // Nothing to filter
      if (addressFilters == null || addressFilters.Count == 0)
        return;

      // Check for invalid filter addresses
      foreach (string filter in addressFilters)
      {
        IPAddress tmpAddress;
        if (!IPAddress.TryParse(filter, out tmpAddress) || filterByFamily && tmpAddress.AddressFamily != family)
          continue;
        if (!availableAddresses.Contains(tmpAddress) && !tmpAddress.Equals(IPAddress.Loopback) && !tmpAddress.Equals(IPAddress.IPv6Loopback))
          UPnPConfiguration.LOGGER.Warn("FilterNetworkAddresses: The defined IP address filter contains the address '{0}' that is not available. Please check your IP configuration for changed addresses to avoid network accessing issues.", tmpAddress);
      }

      // Ignore case can be required for IPv6 addresses (hexadecimal numbers)
      availableAddresses = new HashSet<IPAddress>(availableAddresses.Where(x => addressFilters.Contains(x.ToString(), StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Returns all local ip addresses.
    /// </summary>
    /// <remarks>
    /// This is an alternative way to find network interfaces to <see cref="GetExternalIPAddresses"/>. Doesn't find all addresses, especially those IPv6 addresses
    /// with scope id 0.
    /// </remarks>
    /// <returns>Collection of local IP addresses.</returns>
    public static ICollection<IPAddress> GetLocalIPAddresses()
    {
      ICollection<IPAddress> result = new List<IPAddress>();
      NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
      foreach (NetworkInterface intf in
          interfaces.Where(intf => !intf.IsReceiveOnly && intf.OperationalStatus == OperationalStatus.Up && intf.SupportsMulticast))
        CollectionUtils.AddAll(result, intf.GetIPProperties().UnicastAddresses.Select(addrInfo => addrInfo.Address).Where(addr => !addr.Equals(IPAddress.IPv6Loopback)));
      return result;
    }

    /// <summary>
    /// Collects all interfaces where the UPnP system should be active.
    /// </summary>
    /// <param name="filters">Restrict IPs to only this list (plus loopback)</param>
    /// <returns>Collection of IP addresses to bind to receive UPnP messages.</returns>
    public static ICollection<IPAddress> GetUPnPEnabledIPAddresses(List<string> filters)
    {
      ICollection<IPAddress> result = new List<IPAddress>(GetExternalIPAddresses(filters));
      if (!result.Contains(IPAddress.Loopback))
        result.Add(IPAddress.Loopback);
      return result;
    }

    /// <summary>
    /// Broadcasts the given message <paramref name="data"/> to the given SSDP multicast address and SSDP multicast port
    /// over the given <paramref name="socket"/>.
    /// </summary>
    /// <param name="socket">Socket to be used to send the data.</param>
    /// <param name="multicastAddress">Multicast address to use. The port will be <see cref="UPnPConsts.SSDP_MULTICAST_PORT"/>.</param>
    /// <param name="data">Message data to multicast.</param>
    public static void MulticastMessage(Socket socket, IPAddress multicastAddress, byte[] data)
    {
      IPEndPoint multicastEndpoint = new IPEndPoint(multicastAddress, UPnPConsts.SSDP_MULTICAST_PORT);
      SendData(socket, multicastEndpoint, data, 2);
    }

    /// <summary>
    /// Sends the given <paramref name="data"/> over the given <paramref name="socket"/> to the given
    /// <paramref name="endpoint"/> <paramref name="count"/> times. Exceptions will be catched and ignored.
    /// </summary>
    /// <param name="socket">Socket to send to.</param>
    /// <param name="endpoint">Endpoint to send data to.</param>
    /// <param name="data">Data to send.</param>
    /// <param name="count">Number of times the data should be send.</param>
    public static void SendData(Socket socket, IPEndPoint endpoint, byte[] data, int count)
    {
      try
      {
        for (int i = 0; i < count; i++)
          socket.SendTo(data, endpoint);
      }
      // Simply ignore if we cannot send a multicast message
      catch (SocketException) { }
      catch (SecurityException) { }
    }

    private static void OnPendingRequestTimeout(object state, bool timedOut)
    {
      if (timedOut)
      {
        HttpWebRequest request = (HttpWebRequest)state;
        if (request != null)
          request.Abort();
      }
    }

    /// <summary>
    /// Aborts the given web <paramref name="request"/> if the given asynch <paramref name="result"/> doesn't return
    /// in <paramref name="timeoutMsecs"/> milli seconds.
    /// </summary>
    /// <param name="request">Request to track. Will be aborted (see <see cref="HttpWebRequest.Abort"/>) if the given
    /// asynchronous <paramref name="result"/> handle doen't return in the given time.</param>
    /// <param name="result">Asynchronous result handle to track. Should have been returned by a BeginXXX method of
    /// the given <paramref name="request"/>.</param>
    /// <param name="timeoutMsecs">Timeout in milliseconds, after that the request will be aborted.</param>
    public static void AddTimeout(HttpWebRequest request, IAsyncResult result, uint timeoutMsecs)
    {
      ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, OnPendingRequestTimeout, request, timeoutMsecs, true);
    }

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

    public static string IPAddrToHostName(IPAddress address)
    {
      return address.AddressFamily == AddressFamily.InterNetworkV6 ? '[' + IPAddrToString(address) + ']' : IPAddrToString(address);
    }

    /// <summary>
    /// Returns a string representation of an ip endpoint consisting of an <see cref="IPAddress"/> and a port in the form
    /// <c>123.123.123.123:1234</c> (for IPv4 addresses) or <c>[ABCD:ABCD::ABCD]:1234</c> (for IPv6 addresses).
    /// This method is different from the <see cref="IPEndPoint.ToString"/> method; it avoids the zone index which is
    /// added to IPv6 addresses by that method.
    /// </summary>
    /// <param name="address">IP address of the ip endpoint to create a textural representation for. May be of address family
    /// <see cref="AddressFamily.InterNetwork"/> or <see cref="AddressFamily.InterNetworkV6"/>.</param>
    /// <param name="port">Port of the ip endpoint to create a textural representation for.</param>
    /// <returns>String representation of the given ip endpoint.</returns>
    public static string IPEndPointToString(IPAddress address, int port)
    {
      return address.AddressFamily == AddressFamily.InterNetworkV6 ?
          '[' + IPAddrToString(address) + "]:" + port :
          IPAddrToString(address) + ":" + port;
    }

    public static string IPEndPointToString(IPEndPoint ep)
    {
      return IPEndPointToString(ep.Address, ep.Port);
    }

    public static bool HostNamesEqual(string host1, string host2)
    {
      if (host1.StartsWith("[") && host1.EndsWith("]") && host2.StartsWith("[") && host2.EndsWith("]"))
      {
        host1 = host1.Substring(1);
        host1 = host1.Substring(0, host1.Length - 1);
        host2 = host2.Substring(1);
        host2 = host2.Substring(0, host2.Length - 1);
      }
      IPAddress addr1;
      IPAddress addr2;
      if (IPAddress.TryParse(host1, out addr1) && IPAddress.TryParse(host2, out addr2) && addr1.Equals(addr2))
        return true;
      return host1 == host2;
    }

    /// <summary>
    /// Returns a distance value for the given IP address which is bigger the bigger the scope of the given address is.
    /// </summary>
    /// <remarks>
    /// The return value can be used to compare two addresses concerning the cost of sending packets via that address.
    /// The order of the distance value is: loopback addresses, link-local addresses, site-local addresses, global addresses.
    /// </remarks>
    /// <param name="address">A remote IP address.</param>
    /// <returns>Distance value of the given address.</returns>
    public static int GetLinkDistance(IPAddress address)
    {
      AddressFamily family = address.AddressFamily;
      int result = int.MaxValue;
      if (family == AddressFamily.InterNetwork)
        result = Equals(address, IPAddress.Loopback) ? ZERO_DISTANCE : GLOBAL_DISTANCE;
      else if (family == AddressFamily.InterNetworkV6)
        if (Equals(address, IPAddress.IPv6Loopback))
          result = ZERO_DISTANCE;
        else if (address.IsIPv6LinkLocal)
          result = LINK_LOCAL_DISTANCE;
        else if (address.IsIPv6SiteLocal)
          result = SITE_LOCAL_DISTANCE;
        else
          result = GLOBAL_DISTANCE;
      return result;
    }

    public static int CompareLinkDistance(IPAddress address1, IPAddress address2)
    {
      return GetLinkDistance(address1) - GetLinkDistance(address2);
    }

    public static IList<IPAddress> OrderAddressesByScope(IEnumerable<IPAddress> addresses)
    {
      List<IPAddress> result = new List<IPAddress>(addresses);
      result.Sort(CompareLinkDistance);
      return result;
    }

    public static IPAddress GetSSDPMulticastAddressForInterface(IPAddress interfaceAddress)
    {
      // Hint: Loopback addresses don't work - we don't receive packets when the multicast socket is bound to a loopback adapter.
      // If we would get it managed to receive packets via a loopback adapter, we shoud add IPv4 and IPv6 loopback
      // adapters to the set of UPnP enabled addresses and calculate the loopback address as return value of this
      // method for the loopback interface address as input value
      AddressFamily family = interfaceAddress.AddressFamily;
      if (family == AddressFamily.InterNetwork)
        return UPnPConsts.SSDP_MULTICAST_ADDRESS_V4;
      if (family == AddressFamily.InterNetworkV6)
        if (interfaceAddress.IsIPv6LinkLocal)
          return UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_LINK_LOCAL;
        else if (interfaceAddress.IsIPv6SiteLocal || UPnPConfiguration.SITE_LOCAL_OPERATION)
          return UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_SITE_LOCAL;
        else
          return UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_GLOBAL;
      return IPAddress.None;
    }

    public static IPAddress GetGENAMulticastAddressForInterface(IPAddress interfaceAddress)
    {
      // Hint: Loopback addresses don't work - we don't receive packets when the multicast socket is bound to a loopback adapter.
      // If we would get it managed to receive packets via a loopback adapter, we shoud add IPv4 and IPv6 loopback
      // adapters to the set of UPnP enabled addresses and calculate the loopback address as return value of this
      // method for the loopback interface address as input value
      AddressFamily family = interfaceAddress.AddressFamily;
      if (family == AddressFamily.InterNetwork)
        return UPnPConsts.SSDP_MULTICAST_ADDRESS_V4;
      if (family == AddressFamily.InterNetworkV6)
        if (interfaceAddress.IsIPv6LinkLocal)
          return UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_LINK_LOCAL;
        else if (interfaceAddress.IsIPv6SiteLocal || UPnPConfiguration.SITE_LOCAL_OPERATION)
          return UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_SITE_LOCAL;
        else
          return UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_GLOBAL;
      return IPAddress.None;
    }

    public static void BindAndConfigureSSDPMulticastSocket(Socket socket, IPAddress address)
    {
      AddressFamily family = address.AddressFamily;
      // Need to bind the multicast socket to the multicast port.
      // Albert: Which meaning does the IP address have?
      socket.Bind(new IPEndPoint(address, UPnPConsts.SSDP_MULTICAST_PORT));
      if (family == AddressFamily.InterNetwork)
        try
        {
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V4, address));
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
        catch (SocketException e)
        {
          UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set IPv4 options", e);
        }
      else if (family == AddressFamily.InterNetworkV6)
        try
        {
          // We only receive in those multicast groups where we joined
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_NODE_LOCAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_LINK_LOCAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_SITE_LOCAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_GLOBAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
        }
        catch (SocketException e)
        {
          UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not add socket to IPv6 multicast groups", e);
        }
    }

    public static void BindAndConfigureGENAMulticastSocket(Socket socket, IPAddress address)
    {
      AddressFamily family = address.AddressFamily;
      // Need to bind the multicast socket to the multicast port.
      // Albert: Which meaning does the IP address have?
      socket.Bind(new IPEndPoint(address, UPnPConsts.GENA_MULTICAST_PORT));
      if (family == AddressFamily.InterNetwork)
        try
        {
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V4, address));
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
        catch (SocketException e)
        {
          UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set IPv4 options", e);
        }
      else if (family == AddressFamily.InterNetworkV6)
        try
        {
          // We only receive in those multicast groups where we joined
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_NODE_LOCAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_LINK_LOCAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_SITE_LOCAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_GLOBAL, address.ScopeId));
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
        }
        catch (SocketException e)
        {
          UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not add socket to IPv6 multicast groups", e);
        }
    }

    public static void DisposeSSDPMulticastSocket(Socket socket)
    {
      try
      {
        if (socket.AddressFamily == AddressFamily.InterNetwork)
          try
          {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V4));
          }
          catch (SocketException e)
          {
            UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set IPv4 options", e);
          }
        else if (socket.AddressFamily == AddressFamily.InterNetworkV6)
          try
          {
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_NODE_LOCAL));
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_LINK_LOCAL));
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_SITE_LOCAL));
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.SSDP_MULTICAST_ADDRESS_V6_GLOBAL));
          }
          catch (SocketException e)
          {
            UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not remove socket from IPv6 multicast groups", e);
          }
      }
      catch (ObjectDisposedException)
      { }
      catch (SocketException)
      { }
      socket.Close();
    }

    public static void DisposeGENAMulticastSocket(Socket socket)
    {
      try
      {
        if (socket.AddressFamily == AddressFamily.InterNetwork)
          try
          {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V4));
          }
          catch (SocketException e)
          {
            UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set IPv4 options", e);
          }
        else if (socket.AddressFamily == AddressFamily.InterNetworkV6)
          try
          {
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_NODE_LOCAL));
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_LINK_LOCAL));
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_SITE_LOCAL));
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, new IPv6MulticastOption(UPnPConsts.GENA_MULTICAST_ADDRESS_V6_GLOBAL));
          }
          catch (SocketException e)
          {
            UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not remove socket from IPv6 multicast groups", e);
          }
      }
      catch (ObjectDisposedException)
      { }
      catch (SocketException)
      { }
      socket.Close();
    }
  }
}
