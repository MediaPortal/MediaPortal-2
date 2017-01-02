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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WakeOnLan.Client.Helpers
{
  public class WakeOnLanHelper
  {
    public const int DEFAULT_PING_TIMEOUT = 5000;

    /// <summary>
    /// Determines whether the specified <paramref name="hwAddress"/> appears to be valid.
    /// </summary>
    /// <param name="hwAddress">Byte array containing the hardware address.</param>
    /// <returns></returns>
    public static bool IsValidHardwareAddress(byte[] hwAddress)
    {
      return hwAddress != null && hwAddress.Length == 6 && hwAddress.Any(b => b != 0);
    }

    /// <summary>
    /// Asynchronously pings the specified computer using the default timeout and returns whether the ping was successful.
    /// </summary>
    /// <param name="hostNameOrAddress">The host name or IP address of the computer to ping.</param>
    /// <returns>True if a response was received from the specified computer.</returns>
    public static async Task<bool> PingAsync(string hostNameOrAddress)
    {
      return await PingAsync(hostNameOrAddress, DEFAULT_PING_TIMEOUT);
    }

    /// <summary>
    /// Asynchronously pings the specified computer and returns whether the ping was successful.
    /// </summary>
    /// <param name="hostNameOrAddress">The host name or IP address of the computer to ping.</param>
    /// <param name="timeout">The maximum number of milliseconds to wait for a response.</param>
    /// <returns>True if a response was received from the specified computer.</returns>
    public static async Task<bool> PingAsync(string hostNameOrAddress, int timeout)
    {
      using (Ping ping = new Ping())
        return (await ping.SendPingAsync(hostNameOrAddress, timeout)).Status == IPStatus.Success;
    }

    /// <summary>
    /// Asynchronously sends a Wake-On-LAN 'magic' packet to the computer with the specified hardware address.
    /// </summary>
    /// <param name="hwAddress">The hardware address of the computer to wake.</param>
    public static async Task SendWOLPacketAsync(byte[] hwAddress)
    {
      if (!IsValidHardwareAddress(hwAddress))
        throw new ArgumentException(
            "Invalid hardware address.",
            "hwAddress",
            null);

      // WOL 'magic' packet is sent over UDP.
      using (UdpClient client = new UdpClient())
      {
        // Send to: 255.255.255.0:40000 over UDP.
        client.Connect(IPAddress.Broadcast, 40000);

        // Two parts to a 'magic' packet:
        //     First is 0xFFFFFFFFFFFF,
        //     Second is 16 * MACAddress.
        byte[] packet = new byte[17 * 6];

        // Set to: 0xFFFFFFFFFFFF.
        for (int i = 0; i < 6; i++)
        {
          packet[i] = 0xFF;
        }

        // Set to: 16 * MACAddress
        for (int i = 1; i <= 16; i++)
        {
          for (int j = 0; j < 6; j++)
          {
            packet[i * 6 + j] = hwAddress[j];
          }
        }

        // Send WOL 'magic' packet.
        await client.SendAsync(packet, packet.Length);
      }
    }
  }
}