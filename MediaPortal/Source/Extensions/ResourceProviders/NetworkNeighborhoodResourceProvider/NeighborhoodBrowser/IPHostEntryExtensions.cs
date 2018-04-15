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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  /// <summary>
  /// Extension methods to the <see cref="IPHostEntry"/> class
  /// </summary>
  public static class IpHostEntryExtensions
  {

    private const string UNC_PREFIX = @"\\";
    private const string IPV6_SUFFIX = @".ipv6-literal.net";
    
    /// <summary>
    /// Provides a UNC path string to the host represented by the respective <see cref="IPHostEntry"/>
    /// </summary>
    /// <param name="host"></param>
    /// <returns>UNC path string</returns>
    /// <remarks>
    /// If there is a HostName, the returned string will be @"\\[HostName]"
    /// If there is no HostName (null or empty), but there is at least one IPv4 address, it takes the first IPv4 address
    ///   and returns @"\\[w].[x].[y].[z]"
    /// If there is no HostName and no IPv4 address, but at least one IPv6 address, it takes the first IPv6 address and
    ///   returns @"\\[IPv6Literal]" where [Ipv6Literal] is the IPv6 literal for UNC paths as described here:
    ///   http://en.wikipedia.org/wiki/IPv6_address#Literal_IPv6_addresses_in_UNC_path_names
    /// If there is no HostName, not IPv4 address and no IPv6 address, it returns <c>null</c>
    /// </remarks>
    public static string GetUncString(this IPHostEntry host)
    {
      if (!String.IsNullOrEmpty(host.HostName))
        return StringUtils.CheckPrefix(host.HostName, UNC_PREFIX);

      if (host.AddressList != null && host.AddressList.Any())
      {
        var ipv4 = host.AddressList.FirstOrDefault(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork);
        if (ipv4 != null)
          return StringUtils.CheckPrefix(ipv4.ToString(), UNC_PREFIX);

        var ipv6 = host.AddressList.FirstOrDefault(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetworkV6);
        if (ipv6 != null)
          return StringUtils.CheckPrefix(ipv6.ToString().Replace(':', '-').Replace('%', 's') + IPV6_SUFFIX, UNC_PREFIX);
      }

      return null;
    }
  }
}
