#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using WakeOnLan.Common.NetworkInformation;

namespace WakeOnLan.Common
{
  /// <summary>
  /// Helper class to resolve the hardware address of a remote IPv4 or IPv6 address.
  /// </summary>
  public static class NetworkInformationHelper
  {
    #region Constants

    const ushort AF_UNSPEC = 0;
    const ushort AF_INET = 2;
    const ushort AF_INET6 = 23;

    const int PHYSICAL_ADDRESS_LENGTH = 6;

    #endregion

    #region Public Methods

    /// <summary>
    /// Tries to find the hardware address of <paramref name="remoteAddress"/>.
    /// </summary>
    /// <param name="localAddress">The IP address of the local endpoint to use to resolve the <paramref name="remoteAddress"/>.</param>
    /// <param name="remoteAddress">The romote IP address.</param>
    /// <param name="hwAddress">If successful, the hardware address of the <paramref name="remoteAddress"/>.</param>
    /// <returns><c>true</c>  if the hardware address was found.</returns>
    public static bool TryGetRemoteHardwareAddress(IPAddress localAddress, IPAddress remoteAddress, out byte[] hwAddress)
    {
      if (!IsAddressValid(localAddress))
        throw new ArgumentException(string.Format("{0} is not a valid IPv4 or IPv6 address", localAddress), "localAddress");
      if (!IsAddressValid(remoteAddress))
        throw new ArgumentException(string.Format("{0} is not a valid IPv4 or IPv6 address", remoteAddress), "remoteAddress");

      hwAddress = null;
      MIB_IPNET_ROW2 entry;
      //Try and find an entry in the IP neighbor table, if none found try and resolve the entry directly
      if (!TryGetEntryFromNetTable(remoteAddress, out entry) && !TryResolveIPNetEntry(localAddress, remoteAddress, out entry))
        return false;

      hwAddress = new byte[entry.PhysicalAddressLength];
      Buffer.BlockCopy(entry.PhysicalAddress, 0, hwAddress, 0, hwAddress.Length);
      //MediaPortal.Common.ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("WakeOnLan: Got physical address {0} for {1}", BytesToHex(hwAddress), remoteAddress);
      return true;
    }

    static bool IsAddressValid(IPAddress address)
    {
      return address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    #endregion

    #region IPNetTable

    /// <summary>
    /// Tries to find a matching row in the IP neighbor table for the <paramref name="remoteAddress"/>. 
    /// </summary>
    /// <param name="remoteAddress">The romote IP address.</param>
    /// <param name="entry">If successful, the matching row in the neighbor table for the <paramref name="remoteAddress"/>.</param>
    /// <returns><c>true</c> if a matching row was found.</returns>
    static bool TryGetEntryFromNetTable(IPAddress remoteAddress, out MIB_IPNET_ROW2 entry)
    {
      //LogRows();
      ushort family = remoteAddress.AddressFamily == AddressFamily.InterNetworkV6 ? AF_INET6 : AF_INET;
      var rows = GetIPNetTableRows(family);
      if (rows != null)
      {
        foreach (var row in rows)
          if (HasPhysicalAddress(row) && IsMatchingRow(remoteAddress, row))
          {
            entry = row;
            return true;
          }
      }
      entry = default(MIB_IPNET_ROW2);
      return false;
    }

    static void LogRows()
    {
      MIB_IPNET_ROW2[] rows = GetIPNetTableRows(AF_INET6);
      if (rows == null)
      {
        MediaPortal.Common.ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("WakeOnLan: Found 0 IPNET rows");
        return;
      }
      MediaPortal.Common.ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("WakeOnLan: Found {0} IPNET rows", rows.Length);
      foreach (var row in rows)
      {
        byte[] addressBytes;
        switch (row.Address.si_family)
        {
          case AF_INET:
            addressBytes = row.Address.Ipv4.Address;
            break;
          case AF_INET6:
            addressBytes = row.Address.Ipv6.Address;
            break;
          default:
            MediaPortal.Common.ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("WakeOnLan: Invalid address family {0}", row.Address.si_family);
            continue;
        }
        var address = new IPAddress(addressBytes);
        var hwAddress = BitConverter.ToString(row.PhysicalAddress);
        MediaPortal.Common.ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("    {0} ({1}) : {2} - {3}", address, address.ScopeId, row.PhysicalAddressLength, hwAddress);
      }
    }

    static MIB_IPNET_ROW2[] GetIPNetTableRows(ushort family)
    {
      IntPtr ipNetTable = IntPtr.Zero;
      try
      {
        int hr = NativeMethods.GetIpNetTable2(family, out ipNetTable);
        Marshal.ThrowExceptionForHR(hr);
        return MIB_IPNET_TABLE2.GetTable(ipNetTable);
      }
      finally
      {
        if (ipNetTable != IntPtr.Zero)
          Marshal.FreeCoTaskMem(ipNetTable);
      }
    }

    static bool IsMatchingRow(IPAddress remoteAddress, MIB_IPNET_ROW2 row)
    {
      byte[] addressBytes;
      switch (row.Address.si_family)
      {
        case AF_INET:
          addressBytes = row.Address.Ipv4.Address;
          break;
        case AF_INET6:
          addressBytes = row.Address.Ipv6.Address;
          break;
        default:
          return false;
      }
      return remoteAddress.Equals(new IPAddress(addressBytes));
    }

    static bool HasPhysicalAddress(MIB_IPNET_ROW2 row)
    {
      return row.PhysicalAddressLength == PHYSICAL_ADDRESS_LENGTH && row.PhysicalAddress.Any(b => b != 0);
    }

    #endregion

    #region IPNetEntry Resolution

    /// <summary>
    /// Tries to resolve the hardware address for the <paramref name="remoteAddress"/>.
    /// </summary>
    /// <param name="localAddress">The address of the local endpoint to use to send the resolution request.</param>
    /// <param name="remoteAddress">The romote IP address to resolve.</param>
    /// <param name="entry">If successful, an <see cref="MIB_IPNET_ROW2"/> for the <paramref name="remoteAddress"/>.</param>
    /// <returns></returns>
    static bool TryResolveIPNetEntry(IPAddress localAddress, IPAddress remoteAddress, out MIB_IPNET_ROW2 entry)
    {
      //Set up target address
      entry = new MIB_IPNET_ROW2();
      entry.PhysicalAddress = new byte[32];
      entry.State = NL_NEIGHBOR_STATE.NlnsReachable;

      //Either InterfaceLuid or InterfaceIndex must be filled
      entry.InterfaceIndex = (uint)GetAdapterIndex(localAddress);

      //Populate the IP address depending on whether the address is IPv4 or IPv6
      if (remoteAddress.AddressFamily == AddressFamily.InterNetwork)
      {
        entry.Address.Ipv4.sin_family = AF_INET;
        entry.Address.Ipv4.Address = remoteAddress.GetAddressBytes();
      }
      else
      {
        entry.Address.Ipv6.sin6_family = AF_INET6;
        entry.Address.Ipv6.Address = remoteAddress.GetAddressBytes();
      }

      //Set up the local address to use to resolve the remote address
      byte[] localAddressBytes = localAddress.GetAddressBytes();
      SOCKADDR_INET sourceAddress = new SOCKADDR_INET();
      if (localAddress.AddressFamily == AddressFamily.InterNetworkV6)
        sourceAddress.Ipv6.Address = localAddressBytes;
      else
        sourceAddress.Ipv4.Address = localAddressBytes;

      //Try and resolve the address
      int hr = NativeMethods.ResolveIpNetEntry2(ref entry, ref sourceAddress);
      Marshal.ThrowExceptionForHR(hr);
      return hr == 0 && HasPhysicalAddress(entry);
    }

    static int GetAdapterIndex(IPAddress localAddress)
    {
      var interfaces = NetworkInterface.GetAllNetworkInterfaces();
      foreach (var ni in interfaces)
      {
        var properties = ni.GetIPProperties();
        if (properties.UnicastAddresses.Any(ua => ua.Address.Equals(localAddress)))
        {
          if (localAddress.AddressFamily == AddressFamily.InterNetworkV6)
            return properties.GetIPv6Properties().Index;
          else
            return properties.GetIPv4Properties().Index;
        }
      }
      return 0;
    }

    #endregion
  }
}
