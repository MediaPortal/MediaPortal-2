using System.Net;
using System.Net.Sockets;

namespace UPnP.Infrastructure.CP
{
  public class EndpointConfiguration
  {
    public IPAddress EndPointIPAddress;

    public Socket MulticastReceiveSocket;

    public Socket UnicastSocket;

    public IPAddress SSDPMulticastAddress;
  }
}
