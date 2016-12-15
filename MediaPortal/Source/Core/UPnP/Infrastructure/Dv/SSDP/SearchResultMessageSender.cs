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
using System.Net;
using System.Net.Sockets;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils;
using UPnP.Infrastructure.Utils.HTTP;

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
      response.SetHeader("DATE", DateTime.Now.ToUniversalTime().ToString("R"));
      response.SetHeader("EXT", string.Empty);
      response.SetHeader("SERVER", UPnPConfiguration.UPnPMachineInfoHeader);
      response.SetHeader("ST", NT);
      response.SetHeader("USN", USN);
      response.SetHeader("BOOTID.UPNP.ORG", _serverData.BootId.ToString());
      response.SetHeader("CONFIGID.UPNP.ORG", _localEndpointConfiguration.ConfigId.ToString());
      if (_localEndpointConfiguration.AddressFamily == AddressFamily.InterNetworkV6)
      {
        response.SetHeader("OPT", "\"http://schemas.upnp.org/upnp/1/0/\"; ns=01");
        response.SetHeader("01-NLS", _serverData.BootId.ToString());
      }
      if (_localEndpointConfiguration.SSDPUsesSpecialSearchPort)
        response.SetHeader("SEARCHPORT.UPNP.ORG", _localEndpointConfiguration.SSDPSearchPort.ToString());

      response.SetHeader("LOCATION", _localEndpointConfiguration.GetRootDeviceDescriptionURL(rootDevice));
      byte[] bytes = response.Encode();
      Socket socket = _localEndpointConfiguration.SSDP_UDP_UnicastSocket;
      if (socket != null)
        NetworkHelper.SendData(socket, _receiverEndPoint, bytes, 1);
    }
  }
}
