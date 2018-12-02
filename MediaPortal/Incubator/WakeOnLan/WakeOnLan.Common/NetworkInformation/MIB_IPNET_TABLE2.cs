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
  [StructLayout(LayoutKind.Sequential)]
  public struct MIB_IPNET_TABLE2
  {
    [MarshalAs(UnmanagedType.U4)]
    public uint NumEntries;

    public static MIB_IPNET_ROW2[] GetTable(IntPtr pMIB_IPNET_TABLE2)
    {
      MIB_IPNET_ROW2[] table = null;
      try
      {        
        MIB_IPNET_TABLE2 mib_ipnet_table2 = (MIB_IPNET_TABLE2)Marshal.PtrToStructure(
            pMIB_IPNET_TABLE2,
            typeof(MIB_IPNET_TABLE2));
        
        table = new MIB_IPNET_ROW2[mib_ipnet_table2.NumEntries];
        
        IntPtr currentPointer = pMIB_IPNET_TABLE2 + 8;

        for (int i = 0; i < mib_ipnet_table2.NumEntries; i++)
        {
          table[i] = (MIB_IPNET_ROW2)Marshal.PtrToStructure(
              currentPointer,
              typeof(MIB_IPNET_ROW2));

          currentPointer += Marshal.SizeOf(table[i]);
        }

        return table;
      }
      catch
      {
        return null;
      }
    }
  }
}
