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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MediaPortal.Utilities.Network
{
  [StructLayout(LayoutKind.Sequential)]
  public class NetResource
  {
    public ResourceScope dwScope = 0;
    public ResourceType dwType = 0;
    public ResourceDisplayType dwDisplayType = 0;
    public ResourceUsage dwUsage = 0;
    public string lpLocalName = null;
    public string lpRemoteName = null;
    public string lpComment = null;
    public string lpProvider = null;
  };

  public enum ResourceScope
  {
    Connected = 1,
    GlobalNet,
    Remembered,
    Recent,
    Context
  };

  public enum ResourceType
  {
    Any,
    Disk,
    Print,
    Reserved
  };

  [Flags]
  public enum ResourceUsage
  {
    Connectable = 0x00000001,
    Container = 0x00000002,
    NoLocalDevice = 0x00000004,
    Sibling = 0x00000008,
    Attached = 0x00000010,
    All = (Connectable | Container | Attached),
  };

  public enum ResourceDisplayType
  {
    Generic,
    Domain,
    Server,
    Share,
    File,
    Group,
    Network,
    Root,
    ShareAdmin,
    Directory,
    Tree,
    Ndscontainer
  };

  /// <summary>
  /// Enumerator for several types of network resources, see parameters of <see cref="EnumerateResources"/>.
  /// </summary>
  public static class NetworkResourcesEnumerator
  {
    private enum ErrorCodes
    {
      NoError = 0,
      ErrorNoMoreItems = 259
    };

    #region Windows API functions

    [DllImport("Mpr.dll", EntryPoint = "WNetOpenEnumA", CallingConvention = CallingConvention.Winapi)]
    private static extern ErrorCodes WNetOpenEnum(ResourceScope dwScope, ResourceType dwType, ResourceUsage dwUsage, NetResource p, out IntPtr lphEnum);

    [DllImport("Mpr.dll", EntryPoint = "WNetCloseEnum", CallingConvention = CallingConvention.Winapi)]
    private static extern ErrorCodes WNetCloseEnum(IntPtr hEnum);

    [DllImport("Mpr.dll", EntryPoint = "WNetEnumResourceA", CallingConvention = CallingConvention.Winapi)]
    private static extern ErrorCodes WNetEnumResource(IntPtr hEnum, ref uint lpcCount, IntPtr buffer, ref uint lpBufferSize);

    #endregion

    public static ICollection<string> EnumerateResources(ResourceScope scope, ResourceType type, ResourceUsage usage, ResourceDisplayType displayType)
    {
      NetResource pRsrc = new NetResource();
      return EnumerateResources(pRsrc, scope, type, usage, displayType);
    }

    private static ICollection<string> EnumerateResources(NetResource pRsrc, ResourceScope scope, ResourceType type,
        ResourceUsage usage, ResourceDisplayType displayType)
    {
      List<string> result = new List<string>();
      uint bufferSize = 16384;
      IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
      try
      {
        IntPtr handle;
        uint cEntries = 1;

        ErrorCodes res = WNetOpenEnum(scope, type, usage, pRsrc, out handle);

        if (res == ErrorCodes.NoError)
          try
          {
            do
            {
              res = WNetEnumResource(handle, ref cEntries, buffer, ref bufferSize);

              if (res == ErrorCodes.NoError)
              {
                Marshal.PtrToStructure(buffer, pRsrc);

                if (pRsrc.dwDisplayType == displayType)
                  result.Add(pRsrc.lpRemoteName);

                // If the current NetworkResource is a container, we call EnumerateResources recursively.
                // In some situations, the lpRemoteName in the NetworkResource is null or empty. In this case
                // we do not call EnumerateResources recursively as this leads to an infinite loop of
                // recursive calls. For details see Jira MP2-356
                if ((pRsrc.dwUsage & ResourceUsage.Container) == ResourceUsage.Container && !String.IsNullOrEmpty(pRsrc.lpRemoteName))
                  result.AddRange(EnumerateResources(pRsrc, scope, type, usage, displayType));
              }
              else if (res != ErrorCodes.ErrorNoMoreItems)
                break;
            } while (res != ErrorCodes.ErrorNoMoreItems);
          }
          finally
          {
            WNetCloseEnum(handle);
          }
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
      return result;
    }
  }
}
