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
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils.HTTP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// SSDP message producer class for the "ssdp:update" message.
  /// </summary>
  internal class UpdateMessageSender : ISSDPDiscoveryMessageSender
  {
    protected ServerData _serverData;
    protected int _lastBootId;
    protected int _nextBootId;

    public UpdateMessageSender(ServerData serverData, int lastBootId, int nextBootId)
    {
      _serverData = serverData;
      _lastBootId = lastBootId;
      _nextBootId = nextBootId;
    }

    /// <summary>
    /// Sends a NOTIFY packet "ssdp:update" to all UPnP endpoints.
    /// </summary>
    /// <param name="NT">Notification type.</param>
    /// <param name="USN">Unique Service Name.</param>
    /// <param name="rootDevice">Root device for that the message should be send.</param>
    public void SendMessage(string NT, string USN, DvDevice rootDevice)
    {
      SimpleHTTPRequest response = new SimpleHTTPRequest("NOTIFY", "*");
      response.SetHeader("NT", NT);
      response.SetHeader("NTS", "ssdp:update");
      response.SetHeader("USN", USN);
      response.SetHeader("BOOTID.UPNP.ORG", _lastBootId.ToString());
      response.SetHeader("NEXTBOOTID.UPNP.ORG", _nextBootId.ToString());
      // Currently, we don't support SEARCHPORT.UPNP.ORG function and header

      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
      {
        if (config.AddressFamily == AddressFamily.InterNetworkV6)
        {
          response.SetHeader("OPT", "\"http://schemas.upnp.org/upnp/1/0/\"; ns=01");
          response.SetHeader("01-NLS", _serverData.BootId.ToString());
        }
        response.SetHeader("CONFIGID.UPNP.ORG", config.ConfigId.ToString());
        IPEndPoint ep = new IPEndPoint(config.SSDPMulticastAddress, UPnPConsts.SSDP_MULTICAST_PORT);
        response.SetHeader("HOST", NetworkHelper.IPEndPointToString(ep));
        if (config.SSDPUsesSpecialSearchPort)
          response.SetHeader("SEARCHPORT.UPNP.ORG", config.SSDPSearchPort.ToString());
        response.SetHeader("LOCATION", config.GetRootDeviceDescriptionURL(rootDevice));
        byte[] bytes = response.Encode();
        NetworkHelper.MulticastMessage(config.SSDP_UDP_UnicastSocket, config.SSDPMulticastAddress, bytes);
      }
    }
  }
}
