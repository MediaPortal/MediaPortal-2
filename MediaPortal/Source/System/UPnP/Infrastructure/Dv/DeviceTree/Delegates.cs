#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Globalization;
using System.Net;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
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
}
