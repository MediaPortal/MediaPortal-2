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

// Type: OnlineVideos.MPUrlSourceFilter.IFilterStateEx
// Assembly: OnlineVideos, Version=1.10.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 8F27759C-CFBE-47CB-A39F-F16055EE5D07
// Assembly location: M:\Programmieren\C#\MediaPortal 2\MediaPortal\Incubator\UPnPRenderer\references\OnlineVideos.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter
{
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [Guid("505C28D8-01F4-41C7-BD51-013FA6DBBD39")]
  [ComImport]
  public interface IFilterStateEx : IFilterState
  {
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsFilterReadyToConnectPins([MarshalAs(UnmanagedType.Bool)] out bool ready);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetCacheFileName([MarshalAs(UnmanagedType.LPWStr)] out string cacheFileName);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetVersion([MarshalAs(UnmanagedType.U4)] out uint version);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsFilterError([MarshalAs(UnmanagedType.Bool)] out bool isFilterError, [MarshalAs(UnmanagedType.I4), In] int error);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetErrorDescription([MarshalAs(UnmanagedType.I4), In] int error, [MarshalAs(UnmanagedType.LPWStr)] out string description);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int LoadAsync([MarshalAs(UnmanagedType.LPWStr), In] string url);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsStreamOpened([MarshalAs(UnmanagedType.Bool)] out bool opened);
  }
}
