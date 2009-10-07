#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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
    public static ICollection<IPAddress> GetLocalIPAddresses()
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
    /// Broadcasts the given message <paramref name="data"/> to the given SSDP multicast address using the given
    /// <paramref name="localAddress"/>.
    /// </summary>
    /// <param name="localAddress">IP address to use as local ip adddress.</param>
    /// <param name="multicastAddress">Multicast address to use. The port will be <see cref="Consts.SSDP_MULTICAST_PORT"/>.</param>
    /// <param name="data">Message data to multicast.</param>
    public static void MulticastMessage(IPAddress localAddress, IPAddress multicastAddress, byte[] data)
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.Bind(new IPEndPoint(localAddress, 0));
      AddressFamily family = localAddress.AddressFamily;
      if (family == AddressFamily.InterNetwork)
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, Configuration.DEFAULT_SSDP_UDP_TTL_V4);
      if (localAddress != IPAddress.Loopback && localAddress != IPAddress.IPv6Loopback)
      {
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, 1);
        if (family == AddressFamily.InterNetwork)
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
              new MulticastOption(multicastAddress, localAddress));
        else
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
              new IPv6MulticastOption(multicastAddress));
      }
      // Add membership to multicast group here?
      IPEndPoint multicastEndpoint = new IPEndPoint(multicastAddress, Consts.SSDP_MULTICAST_PORT);
      socket.SendTo(data, multicastEndpoint);
      socket.SendTo(data, multicastEndpoint);
      // Drop member to multicast group, if appropriate
      socket.Close();
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
