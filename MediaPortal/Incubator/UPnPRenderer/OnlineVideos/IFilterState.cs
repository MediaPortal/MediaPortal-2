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

// Type: OnlineVideos.MPUrlSourceFilter.IFilterState
// Assembly: OnlineVideos, Version=1.10.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 8F27759C-CFBE-47CB-A39F-F16055EE5D07
// Assembly location: M:\Programmieren\C#\MediaPortal 2\MediaPortal\Incubator\UPnPRenderer\references\OnlineVideos.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter
{
  [Guid("420E98EF-0338-472F-B77B-C5BA8997ED10")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface IFilterState
  {
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsFilterReadyToConnectPins([MarshalAs(UnmanagedType.Bool)] out bool ready);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetCacheFileName([MarshalAs(UnmanagedType.LPWStr)] out string cacheFileName);
  }
}
