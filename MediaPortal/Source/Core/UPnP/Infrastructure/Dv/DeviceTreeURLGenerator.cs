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
    /// <remarks>
    /// All description-relevant information will be cleared first, if necessary.
    /// </remarks>
    /// <param name="server">UPnP server to generate URLs for.</param>
    /// <param name="config">UPnP endpoint to generate the URLs for.</param>
    public static void GenerateObjectURLs(UPnPServer server, EndpointConfiguration config)
    {
      config.RootDeviceDescriptionPaths.Clear();
      config.RootDeviceDescriptionPathsToRootDevices.Clear();
      config.ServicePaths.Clear();
      config.SCPDPathsToServices.Clear();
      config.ControlPathsToServices.Clear();
      config.EventSubPathsToServices.Clear();

      string descriptionBase = config.DescriptionPathBase;
      foreach (DvDevice rootDevice in server.RootDevices)
      {
        string path = descriptionBase + rootDevice.UDN;
        config.RootDeviceDescriptionPathsToRootDevices.Add(path, rootDevice);
        config.RootDeviceDescriptionPaths.Add(rootDevice, path);
        GeneratePathsRecursive(rootDevice, config);
      }
    }

    protected static void GeneratePathsRecursive(DvDevice device, EndpointConfiguration config)
    {
      string deviceRelUrl = StringUtils.CheckSuffix(device.UDN, "/");
      foreach (DvService service in device.Services)
      {
        ServicePaths servicePaths = new ServicePaths
          {
              SCPDPath =
                  GenerateAndAddUniquePath(config.SCPDPathsToServices,
                      config.DescriptionPathBase + deviceRelUrl + service.ServiceTypeVersion_URN, ".xml", service),
              ControlPath = 
                  GenerateAndAddUniquePath(config.ControlPathsToServices,
                      config.ControlPathBase + deviceRelUrl + service.ServiceTypeVersion_URN, string.Empty, service),
              EventSubPath = 
                  GenerateAndAddUniquePath(config.EventSubPathsToServices,
                      config.EventSubPathBase + deviceRelUrl + service.ServiceTypeVersion_URN, string.Empty, service)
          };
        config.ServicePaths.Add(service, servicePaths);
      }
      foreach (DvDevice embeddedDevice in device.EmbeddedDevices)
        GeneratePathsRecursive(embeddedDevice, config);
    }

    protected static string GenerateAndAddUniquePath<T>(IDictionary<string, T> mapping, string prefix, string suffix, T obj)
    {
      string result = prefix + suffix;
      ICollection<string> availablePaths = mapping.Keys;
      if (!availablePaths.Contains(result))
      {
        mapping.Add(result, obj);
        return result;
      }
      int i = 0;
      while (availablePaths.Contains(result = (prefix + i) + suffix))
        i++;
      mapping.Add(result, obj);
      return result;
    }
  }
}
