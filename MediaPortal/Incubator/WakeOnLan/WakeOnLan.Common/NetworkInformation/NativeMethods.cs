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
using System.Runtime.InteropServices;

namespace WakeOnLan.Common.NetworkInformation
{
  public static class NativeMethods
  {
    [DllImport("iphlpapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetIpNetTable2(
            ushort Family,
            [Out] out IntPtr Table);

    [DllImport("Iphlpapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int ResolveIpNetEntry2(
            [In, Out] ref MIB_IPNET_ROW2 Row,
            [In, Out] ref SOCKADDR_INET SourceAddress);
  }
}
