using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
  }
}
