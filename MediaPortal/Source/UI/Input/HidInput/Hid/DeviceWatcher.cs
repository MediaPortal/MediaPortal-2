#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Windows.Forms;

namespace HidInput.Hid
{
  /// <summary>
  /// Handles <see cref="WM_DEVICECHANGE"/> windows messages passed to <see cref="HandleMessage(ref Message)"/>
  /// and triggers the <see cref="DeviceChange"/> event when a USB device is added or removed.
  /// </summary>
  public class DeviceWatcher : IDisposable
  {
    #region Win API

    [DllImport("user32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
    static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterDeviceNotification(IntPtr Handle);

    public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x0000;
    public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
    public static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    public const int WM_DEVICECHANGE = 0x0219;
    public const int DBT_DEVICEARRIVAL = 0x8000;
    public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DEV_BROADCAST_DEVICEINTERFACE
    {
      public uint dbcc_size;
      public uint dbcc_devicetype;
      public uint dbcc_reserved;
      public Guid dbcc_classguid;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
      public string dbcc_name;
    }

    #endregion

    protected IntPtr _deviceNotifyHandle;

    public void Register(IntPtr hWnd)
    {
      if (_deviceNotifyHandle != IntPtr.Zero)
        return;

      DEV_BROADCAST_DEVICEINTERFACE dbdi = new DEV_BROADCAST_DEVICEINTERFACE
      {
        dbcc_size = (uint)Marshal.SizeOf(new DEV_BROADCAST_DEVICEINTERFACE()),
        dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
        dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE
      };
      IntPtr diBuffer = Marshal.AllocHGlobal((int)dbdi.dbcc_size);
      try
      {
        _deviceNotifyHandle = RegisterDeviceNotification(hWnd, diBuffer, DEVICE_NOTIFY_WINDOW_HANDLE);
      }
      finally
      {
        Marshal.FreeHGlobal(diBuffer);
      }
    }

    public void Unregister()
    {
      if (_deviceNotifyHandle != IntPtr.Zero)
      {
        UnregisterDeviceNotification(_deviceNotifyHandle);
        _deviceNotifyHandle = IntPtr.Zero;
      }
    }

    public void HandleMessage(ref Message message)
    {
      if (message.Msg == WM_DEVICECHANGE)
        DeviceChange?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler DeviceChange;

    public void Dispose()
    {
      Unregister();
    }
  }
}
