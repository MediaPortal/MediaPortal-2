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
  public enum DeviceChangeType
  {
    Arrived,
    Removed
  }

  public class DeviceChangeEventArgs : EventArgs
  {
    public DeviceChangeEventArgs(DeviceChangeType changeType, string deviceName)
    {
      ChangeType = changeType;
      DeviceName = deviceName;
    }

    public DeviceChangeType ChangeType { get; }
    public string DeviceName { get; }
  }

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
    public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x0004;
    public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
    public static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    public const int WM_DEVICECHANGE = 0x0219;
    public static readonly IntPtr DBT_DEVICEARRIVAL = (IntPtr)0x8000;
    public static readonly IntPtr DBT_DEVICEREMOVECOMPLETE = (IntPtr)0x8004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DEV_BROADCAST_DEVICEINTERFACE
    {
      public int dbcc_size;
      public int dbcc_devicetype;
      public int dbcc_reserved;
      public Guid dbcc_classguid;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
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
        dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
      };
      dbdi.dbcc_size = Marshal.SizeOf(dbdi);
      IntPtr diBuffer = Marshal.AllocHGlobal(dbdi.dbcc_size);
      try
      {
        Marshal.StructureToPtr(dbdi, diBuffer, true);
        _deviceNotifyHandle = RegisterDeviceNotification(hWnd, diBuffer, DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
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
      {
        if (message.WParam == DBT_DEVICEARRIVAL || message.WParam == DBT_DEVICEREMOVECOMPLETE)
        {
          DEV_BROADCAST_DEVICEINTERFACE dbdi = Marshal.PtrToStructure<DEV_BROADCAST_DEVICEINTERFACE>(message.LParam);
          DeviceChange?.Invoke(this, new DeviceChangeEventArgs(message.WParam == DBT_DEVICEARRIVAL ? DeviceChangeType.Arrived : DeviceChangeType.Removed, dbdi.dbcc_name));
        }
      }
    }

    public event EventHandler<DeviceChangeEventArgs> DeviceChange;

    public void Dispose()
    {
      Unregister();
    }
  }
}
