using System;
using System.Net;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.HTTP;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// SSDP message producer class for a search result message.
  /// </summary>
  internal class SearchResultMessageSender : ISSDPDiscoveryMessageSender
  {
    protected ServerData _serverData;
    protected EndpointConfiguration _localEndpointConfiguration;
    protected IPEndPoint _receiverEndPoint;

    public SearchResultMessageSender(ServerData serverData, EndpointConfiguration localEndpointConfiguration, IPEndPoint receiverEndPoint)
    {
      _serverData = serverData;
      _localEndpointConfiguration = localEndpointConfiguration;
      _receiverEndPoint = receiverEndPoint;
    }

    public void SendMessage(string NT, string USN, DvDevice rootDevice)
    {
      SimpleHTTPResponse response = new SimpleHTTPResponse(HTTPResponseCode.Ok);
      response.SetHeader("CACHE-CONTROL", "max-age = " + _serverData.AdvertisementExpirationTime);
      response.SetHeader("DATE", DateTime.Now.ToString("R"));
      response.SetHeader("EXT", string.Empty);
      response.SetHeader("SERVER", Configuration.UPnPMachineInfoHeader);
      response.SetHeader("ST", NT);
      response.SetHeader("USN", USN);
      response.SetHeader("BOOTID.UPNP.ORG", _serverData.BootId.ToString());
      response.SetHeader("CONFIGID.UPNP.ORG", _serverData.ConfigId.ToString());
      if (_localEndpointConfiguration.SSDPUsesSpecialSearchPort)
        response.SetHeader("SEARCHPORT.UPNP.ORG", _localEndpointConfiguration.SSDPSearchPort.ToString());

      response.SetHeader("LOCATION", _localEndpointConfiguration.RootDeviceDescriptionURLs[rootDevice]);
      byte[] bytes = response.Encode();
      _localEndpointConfiguration.SSDP_UDP_UnicastSocket.SendTo(bytes, _receiverEndPoint);
    }
  }
}
