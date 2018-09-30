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

using System.Runtime.InteropServices;

namespace WakeOnLan.Common.NetworkInformation
{
  [StructLayout(LayoutKind.Sequential)]
  public struct MIB_IPNET_ROW2
  {
    [MarshalAs(UnmanagedType.Struct)]
    public SOCKADDR_INET Address;

    [MarshalAs(UnmanagedType.U4)]
    public uint InterfaceIndex;

    [MarshalAs(UnmanagedType.U8)]
    public ulong InterfaceLuid;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] PhysicalAddress;

    [MarshalAs(UnmanagedType.U4)]
    public uint PhysicalAddressLength;

    [MarshalAs(UnmanagedType.U4)]
    public NL_NEIGHBOR_STATE State;

    [MarshalAs(UnmanagedType.U4)]
    public uint Flags;

    [MarshalAs(UnmanagedType.U4)]
    public uint LastReachable;
  }
}
