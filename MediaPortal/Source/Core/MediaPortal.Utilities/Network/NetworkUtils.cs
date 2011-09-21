#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Reflection;

namespace MediaPortal.Utilities.Network
{
  public static class NetworkUtils
  {
    /// <summary>
    /// Returns a string representation of an <see cref="IPAddress"/> in the form <c>123.123.123.123</c> (for IPv4 addresses)
    /// or <c>ABCD:ABCD::ABCD</c> (for IPv6 addresses). This method is different from the <see cref="IPAddress.ToString"/>
    /// method; it avoids the zone index which is added to IPv6 addresses by that method.
    /// </summary>
    /// <param name="address">IP address to create a textural representation for. May be of address family
    /// <see cref="AddressFamily.InterNetwork"/> or <see cref="AddressFamily.InterNetworkV6"/>.</param>
    /// <returns>String representation of the given ip address.</returns>
    public static string IPAddrToString(IPAddress address)
    {
      string result = address.ToString();
      int i = result.IndexOf('%');
      if (i >= 0) result = result.Substring(0, i);
      return result;
    }

    // FIXME: This is only needed as long as we don't use .net 4.0, since that function will be added in .net 4.0
    public static void AddRange(this HttpWebRequest request, long start, long end)
    {
      MethodInfo method = typeof(WebHeaderCollection).GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
      const string key = "Range";
      string val = string.Format("bytes={0}-{1}", start, end);
      method.Invoke(request.Headers, new object[] { key, val });
    }
  }
}