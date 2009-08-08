using System.Net;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.HTTP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// SSDP message producer class for the "ssdp:alive" message.
  /// </summary>
  internal class AliveMessageSender : ISSDPDiscoveryMessageSender
  {
    protected ServerData _serverData;

    public AliveMessageSender(ServerData serverData)
    {
      _serverData = serverData;
    }

    /// <summary>
    /// Sends a NOTIFY packet "ssdp:alive" to all UPnP endpoints.
    /// </summary>
    /// <param name="NT">Notification type.</param>
    /// <param name="USN">Unique Service Name.</param>
    /// <param name="rootDevice">Root device for that the message should be send.</param>
    public void SendMessage(string NT, string USN, DvDevice rootDevice)
    {
      SimpleHTTPRequest response = new SimpleHTTPRequest("NOTIFY", "*");
      response.SetHeader("CACHE-CONTROL", "max-age = " + _serverData.AdvertisementExpirationTime);
      response.SetHeader("NT", NT);
      response.SetHeader("NTS", "ssdp:alive");
      response.SetHeader("SERVER", Configuration.UPnPMachineInfoHeader);
      response.SetHeader("USN", USN);
      response.SetHeader("BOOTID.UPNP.ORG", _serverData.BootId.ToString());
      response.SetHeader("CONFIGID.UPNP.ORG", _serverData.ConfigId.ToString());
      // Currently, we don't support SEARCHPORT.UPNP.ORG function and header

      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
      {
        IPEndPoint ep = new IPEndPoint(config.SSDPMulticastAddress, Consts.SSDP_MULTICAST_PORT);
        response.SetHeader("HOST", ep.ToString());
        if (config.SSDPUsesSpecialSearchPort)
          response.SetHeader("SEARCHPORT.UPNP.ORG", config.SSDPSearchPort.ToString());
        response.SetHeader("LOCATION", config.RootDeviceDescriptionURLs[rootDevice]);
        byte[] bytes = response.Encode();
        NetworkHelper.MulticastMessage(config.EndPointIPAddress, config.SSDPMulticastAddress, bytes);
      }
    }
  }
}