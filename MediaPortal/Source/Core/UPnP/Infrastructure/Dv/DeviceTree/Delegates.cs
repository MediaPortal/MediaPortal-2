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

using HttpServer;
using System.Globalization;
using System.Net;
using System.Xml;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Contains all possible hook positions during the device generation, where the device
  /// generation hook will be called.
  /// </summary>
  /// <remarks>
  /// This enum is not complete by design to avoid superfluous work. If the device
  /// generation hook should be called for more positions in the future,
  /// this enum and the appropriate calls in the device generation methods can be extended.
  /// </remarks>
  public enum GenerationPosition
  {
    /// <summary>
    /// Denotes the position just after the start tag of the root device element.
    /// </summary>
    RootDeviceStart,

    /// <summary>
    /// Denotes the position just after the start tag of the device element.
    /// </summary>
    DeviceStart,

    //[...]
    //ServiceStart,
    //[...]
    //ServiceEnd,
    //[...]

    /// <summary>
    /// Denotes the position after the deviceList element.
    /// </summary>
    AfterDeviceList,

    /// <summary>
    /// Denotes the position just before the closing of the device element.
    /// </summary>
    DeviceEnd,

    /// <summary>
    /// Denotes the position just before the closing of the root device element.
    /// </summary>
    RootDeviceEnd,
  }

  /// <summary>
  /// Delegate which is called during the device description generation. Implementors can add additional XML elements.
  /// </summary>
  /// <param name="request">The request for the device description.</param>
  /// <param name="writer">The writer used to create the XML document.</param>
  /// <param name="device">The device which is being written.</param>
  /// <param name="pos">The current description XML position.</param>
  /// <param name="config">The endpoint configuration which requested the description.</param>
  /// <param name="culture">The culture of the client which requested the description.</param>
  public delegate void GenerateDescriptionDlgt(IHttpRequest request, XmlWriter writer, DvDevice device, GenerationPosition pos, EndpointConfiguration config, CultureInfo culture);

  /// <summary>
  /// Delegate which can return a URL which is available over the specified <paramref name="endPointIPAddress"/>.
  /// The URL may be localized for the given <paramref name="culture"/>.
  /// </summary>
  /// <remarks>
  /// Be careful with the <see cref="IPAddress.ToString"/> method. This method adds the zone identifier at the end of
  /// the string for IPv6 addresses, like this: <c>ABCD:ABCD::ABCD%12</c>. When creating an URL for the given IP address,
  /// the zone identifier must be removed from the IP address. Use method <see cref="NetworkHelper.IPAddrToString"/>
  /// (or <see cref="NetworkHelper.IPEndPointToString(System.Net.IPEndPoint)"/>).
  /// </remarks>
  /// <param name="endPointIPAddress">IP address where the returned url must be reachable.</param>
  /// <param name="culture">The culture to localize the returned URL.</param>
  /// <returns>URL to an external resource. The URL/resource is not managed by the UPnP subsystem.</returns>
  public delegate string GetURLForEndpointDlgt(IPAddress endPointIPAddress, CultureInfo culture);

  /// <summary>
  /// Delegate which is called during the device information generation. Implementors can override device information properties.
  /// </summary>
  /// <param name="request">The request for the device information.</param>
  /// <param name="deviceInfo">The device information that will be sent if not overridden.</param>
  /// <param name="overrideDeviceInfo">The overriden device information to send.</param>
  public delegate void GetDeviceInfoForEndpointDlgt(IHttpRequest request, ILocalizedDeviceInformation deviceInfo, ref ILocalizedDeviceInformation overrideDeviceInfo);
}
