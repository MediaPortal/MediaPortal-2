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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;
using MediaPortal.Utilities;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// Contains helper methods for network handling.
  /// </summary>
  public class NetworkHelper
  {
    /// <summary>
    /// Returns all local ip addresses which are known via DNS.
    /// </summary>
    /// <returns>Collection of local IP addresses.</returns>
    public static ICollection<IPAddress> GetExternalIPAddresses()
    {
      // Collect all interfaces where the UPnP system should be active
      ICollection<IPAddress> result = new List<IPAddress>();
      try
      {
        string hostName = Dns.GetHostName();
        CollectionUtils.AddAll(result, Dns.GetHostAddresses(hostName));
      }
      catch (SocketException) { }
      return result;
    }

    /// <summary>
    /// Returns the first address of the given <paramref name="addresses"/> collection which has the given address
    /// <paramref name="family"/>.
    /// </summary>
    /// <param name="addresses">Collection of addresses to search.</param>
    /// <param name="family">Family of the address to return</param>
    /// <returns>First address of the given address <paramref name="family"/> or <c>null</c>, if no such address is
    /// in the given addresses collection.</returns>
    public static IPAddress FindAddressOfFamily(ICollection<IPAddress> addresses, AddressFamily family)
    {
      foreach (IPAddress address in addresses)
        if (address.AddressFamily == family)
          return address;
      return null;
    }

    /// <summary>
    /// Broadcasts the given message <paramref name="data"/> to the given SSDP multicast address.
    /// </summary>
    /// <param name="family">Address family specifying the protocol type to use (IPv4/IPv6).</param>
    /// <param name="multicastAddress">Multicast address to use. The port will be <see cref="UPnPConsts.SSDP_MULTICAST_PORT"/>.</param>
    /// <param name="data">Message data to multicast.</param>
    public static void MulticastMessage(AddressFamily family, IPAddress multicastAddress, byte[] data)
    {
      Socket socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      try
      {
        if (family == AddressFamily.InterNetwork)
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, Configuration.DEFAULT_SSDP_UDP_TTL_V4);
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, 0);
        IPEndPoint multicastEndpoint = new IPEndPoint(multicastAddress, UPnPConsts.SSDP_MULTICAST_PORT);
        socket.SendTo(data, multicastEndpoint);
        socket.SendTo(data, multicastEndpoint);
        socket.Close();
      }
      // Simply ignore if we cannot send a multicast message
      catch (SocketException) { }
      catch (SecurityException) { }
    }

    /// <summary>
    /// Reads until the end of the given stream, discarding the data.
    /// </summary>
    /// <param name="stream">Stream to read data from.</param>
    public static void DiscardInput(Stream stream)
    {
      const int BUFF_SIZE = 1024;
      byte[] data = new byte[BUFF_SIZE];
      while (stream.Read(data, 0, BUFF_SIZE) == BUFF_SIZE) { }
    }

    private static void OnPendingRequestTimeout(object state, bool timedOut) {
      if (timedOut) {
        HttpWebRequest request = (HttpWebRequest) state;
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
      ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, OnPendingRequestTimeout,
          request, timeoutMsecs, true);
    }
  }
}
