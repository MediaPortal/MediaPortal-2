#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
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
        IPEndPoint ep = new IPEndPoint(config.SSDPMulticastAddress, UPnPConsts.SSDP_MULTICAST_PORT);
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
