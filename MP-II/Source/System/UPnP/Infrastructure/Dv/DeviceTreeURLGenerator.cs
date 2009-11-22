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

using System.Collections.Generic;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv
{
  /// <summary>
  /// Generates URLs to be used for device description and presentation and service description, control and eventing.
  /// </summary>
  /// <remarks>
  /// The generated URLs need to be adapted to the UPnP endpoint over that the returned description document is requested.
  /// For example, if the description document is requested over IPv4, the description will be generated with embedded
  /// IPv4 URLs, else, if it is requested over IPv6, it will contain IPv6 URLs.
  /// Furthermore, the IP address of the generated URLs will be adapted. For example, if the description is requested over
  /// the loopback adapter, the URLs will look like this: "http://127.0.0.1:.../..." (or "http://[::1]:.../..." for IPv6,
  /// respectively). If the description is requested over an ethernet adapter, an external address of that adapter will be
  /// used for the URLs.
  /// </remarks>
  internal class DeviceTreeURLGenerator
  {
    /// <summary>
    /// Generates all URLs that are needed for the device tree(s) starting at all root devices of the specified
    /// <paramref name="server"/>.
    /// </summary>
    /// <param name="server">UPnP server to generate URLs for.</param>
    /// <param name="config">UPnP endpoint to generate the URLs for.</param>
    public static void GenerateObjectURLs(UPnPServer server, EndpointConfiguration config)
    {
      config.SCPDURLsToServices.Clear();
      config.ControlURLsToServices.Clear();
      config.EventSubURLsToServices.Clear();

      string descriptionBase = StringUtils.CheckSuffix(config.DescriptionURLBase, "/");
      foreach (DvDevice rootDevice in server.RootDevices)
      {
        string url = descriptionBase + rootDevice.UDN;
        config.RootDeviceDescriptionURLsToRootDevices.Add(url, rootDevice);
        config.RootDeviceDescriptionURLs.Add(rootDevice, url);
        GenerateURLsRecursive(rootDevice, config);
      }
    }

    protected static void GenerateURLsRecursive(DvDevice device, EndpointConfiguration config)
    {
      string descriptionBase = StringUtils.CheckSuffix(config.DescriptionURLBase, "/");
      string controlBase = StringUtils.CheckSuffix(config.ControlURLBase, "/");
      string eventBase = StringUtils.CheckSuffix(config.EventSubURLBase, "/");

      string deviceRelUrl = StringUtils.CheckSuffix(device.UDN, "/");
      foreach (DvService service in device.Services)
      {
        ServiceURLs serviceURLs = new ServiceURLs
          {
              SCDPURL = GenerateAndAddUniqueURL(
                  config.SCPDURLsToServices, descriptionBase + deviceRelUrl + service.ServiceTypeVersion_URN, ".xml", service),
              ControlURL = GenerateAndAddUniqueURL(
                  config.ControlURLsToServices, controlBase + deviceRelUrl + service.ServiceTypeVersion_URN, string.Empty, service),
              EventSubURL = GenerateAndAddUniqueURL(
                  config.EventSubURLsToServices, eventBase + deviceRelUrl + service.ServiceTypeVersion_URN, string.Empty, service)
          };
        config.ServiceURLs.Add(service, serviceURLs);
      }
      foreach (DvDevice embeddedDevice in device.EmbeddedDevices)
        GenerateURLsRecursive(embeddedDevice, config);
    }

    protected static string GenerateAndAddUniqueURL<T>(IDictionary<string, T> mapping, string prefix, string suffix, T obj)
    {
      string result = prefix + suffix;
      ICollection<string> availableURLs = mapping.Keys;
      if (!availableURLs.Contains(result))
      {
        mapping.Add(result, obj);
        return result;
      }
      int i = 0;
      while (availableURLs.Contains(result = (prefix + i) + suffix))
        i++;
      mapping.Add(result, obj);
      return result;
    }
  }
}
