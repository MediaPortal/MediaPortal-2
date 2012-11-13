#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Net.NetworkInformation;
using System.Net.Sockets;

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

    /// <summary>
    /// Used internally as callback delegate for the <see cref="ServicePoint.BindIPEndPointDelegate"/> property
    /// in <see cref="HttpWebRequest.ServicePoint"/>.
    /// </summary>
    /// <param name="servicePoint">The service point which is currently being bound.</param>
    /// <param name="remoteEndPoint">The desired remote endoint to reach.</param>
    /// <param name="preferredLocalIpAddress">Address which specifies the preferred local endpoint. This address is not part of the delegate
    /// signature and must be provided from outside.</param>
    /// <param name="retryCount">The number of times this delegate was called for a specified connection.</param>
    /// <returns>The local IPEndPoint to which the ServicePoint should bind.</returns>
    public static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint,
        IPAddress preferredLocalIpAddress, int retryCount)
    {
      if (retryCount > 0)
        return null;
      IPEndPoint ipe = new IPEndPoint(preferredLocalIpAddress, 0);
      return ipe;
    }

    /// <summary>
    /// Sets the local endpoint/network interface which is used to send the given HTTP <paramref name="request"/>.
    /// </summary>
    /// <remarks>
    /// Due to problems which can avoid HTTP requests being delivered correctly, it is sometimes necessary to give a HTTP request object a
    /// "hint" which local endpoint should be used. If HTTP requests time out (which is often the case when virtual VMWare network interfaces are
    /// installed), it might help to call this method.
    /// </remarks>
    /// <param name="request">HTTP web request to patch.</param>
    /// <param name="preferredLocalIpAddress">Local IP address which is bound to the network interface which should be used to bind the
    /// outgoing HTTP request to.</param>
    public static void SetLocalEndpoint(HttpWebRequest request, IPAddress preferredLocalIpAddress)
    {
      request.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) => BindIPEndPointCallback(servicePoint, remoteEndPoint, preferredLocalIpAddress, retryCount);
    }

    /// <summary>
    /// Indicates whether any network connection is available
    /// Filter connections with virtual network cards.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if a network connection is available; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNetworkConnected { get; private set; }

    /// <summary>
    /// Indicates whether any network connection is available.
    /// Filter connections below a specified speed, as well as virtual network cards.
    /// </summary>
    /// <param name="minimumSpeed">The minimum speed required. Passing 0 will not filter connection using speed.</param>
    /// <returns><c>true</c> if a network connection is available; otherwise, <c>false</c>.</returns>
    public static bool IsNetworkAvailable(long minimumSpeed)
    {
      if (!NetworkInterface.GetIsNetworkAvailable())
        return false;

      foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
      {
        // Discard because of standard reasons
        if ((ni.OperationalStatus != OperationalStatus.Up) ||
            (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
            (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
          continue;

        // This allows to filter modems, serial, etc.
        // I use 10000000 as a minimum speed for most cases
        if (ni.Speed < minimumSpeed)
          continue;

        // Discard virtual cards (virtual box, virtual pc, etc.)
        if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
            (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
          continue;

        // Discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
        if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
          continue;

        return true;
      }
      return false;
    }

    private static void DoNetworkAddressChanged(object sender, EventArgs e)
    {
      IsNetworkConnected = IsNetworkAvailable(0);
    }

    private static void DoNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
    {
      IsNetworkConnected = IsNetworkAvailable(0);
    }

    static NetworkUtils()
    {
      IsNetworkConnected = IsNetworkAvailable(0);
      NetworkChange.NetworkAvailabilityChanged += DoNetworkAvailabilityChanged;
      NetworkChange.NetworkAddressChanged += DoNetworkAddressChanged;
    }
  }
}