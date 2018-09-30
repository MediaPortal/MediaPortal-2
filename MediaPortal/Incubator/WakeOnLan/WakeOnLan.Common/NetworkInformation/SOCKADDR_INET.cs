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
  [StructLayout(LayoutKind.Explicit)]
  public struct SOCKADDR_INET
  {
    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.Struct)]
    public SOCKADDR_IN Ipv4;

    [FieldOffset(0)]
    [MarshalAs(UnmanagedType.Struct)]
    public SOCKADDR_IN6 Ipv6;

    [FieldOffset(0)]
    public ushort si_family;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
  public struct SOCKADDR_IN
  {
    public ushort sin_family;
    public ushort sin_port;
    public byte sin_addr0;
    public byte sin_addr1;
    public byte sin_addr2;
    public byte sin_addr3;

    public byte[] Address
    {
      get
      {
        return new byte[] { sin_addr0, sin_addr1, sin_addr2, sin_addr3 };
      }
      set
      {
        sin_addr0 = value[0];
        sin_addr1 = value[1];
        sin_addr2 = value[2];
        sin_addr3 = value[3];
      }
    }
  }

  [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
  public struct SOCKADDR_IN6
  {
    public ushort sin6_family;
    public ushort sin6_port;
    public uint sin6_flowinfo;
    public byte sin6_addr0;
    public byte sin6_addr1;
    public byte sin6_addr2;
    public byte sin6_addr3;
    public byte sin6_addr4;
    public byte sin6_addr5;
    public byte sin6_addr6;
    public byte sin6_addr7;
    public byte sin6_addr8;
    public byte sin6_addr9;
    public byte sin6_addr10;
    public byte sin6_addr11;
    public byte sin6_addr12;
    public byte sin6_addr13;
    public byte sin6_addr14;
    public byte sin6_addr15;
    public uint sin6_scope_id;

    public byte[] Address
    {
      get
      {
        return new byte[] {
                    sin6_addr0, sin6_addr1, sin6_addr2, sin6_addr3 ,
                    sin6_addr4, sin6_addr5, sin6_addr6, sin6_addr7 ,
                    sin6_addr8, sin6_addr9, sin6_addr10, sin6_addr11 ,
                    sin6_addr12, sin6_addr13, sin6_addr14, sin6_addr15 };
      }
      set
      {
        sin6_addr0 = value[0];
        sin6_addr1 = value[1];
        sin6_addr2 = value[2];
        sin6_addr3 = value[3];
        sin6_addr4 = value[4];
        sin6_addr5 = value[5];
        sin6_addr6 = value[6];
        sin6_addr7 = value[7];
        sin6_addr8 = value[8];
        sin6_addr9 = value[9];
        sin6_addr10 = value[10];
        sin6_addr11 = value[11];
        sin6_addr12 = value[12];
        sin6_addr13 = value[13];
        sin6_addr14 = value[14];
        sin6_addr15 = value[15];
      }
    }
  }
}
