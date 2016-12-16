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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

namespace MediaPortal.Plugins.MceRemoteReceiver.Hardware
{
  internal class DeviceWatcher : NativeWindow
  {
    #region Fields

    private SafeHandle _handleDeviceArrival;
    private SafeHandle _handleDeviceRemoval;
    private SafeHandle _deviceHandle;
    private Guid _deviceClass;

    #endregion Fields

    #region Delegates

    internal DeviceEventHandler DeviceArrival;
    internal DeviceEventHandler DeviceRemoval;
    internal SettingsChanged SettingsChanged;

    #endregion Delegates

    #region Interop

    private const int WM_DEVICECHANGE = 0x0219;
    private const int WM_SETTINGSCHANGE = 0x001A;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceBroadcastHeader
    {
      public int Size;
      public int DeviceType;
      public int Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceBroadcastInterface
    {
      public int Size;
      public int DeviceType;
      public int Reserved;
      public Guid ClassGuid;

      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceBroadcastHandle
    {
      public int Size;
      public int DeviceType;
      public int Reserved;
      public IntPtr Handle;
      public IntPtr HandleNotify;
      public Guid EventGuid;
      public int NameOffset;
      public byte Data;
    }

    [DllImport("user32", SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr handle, ref DeviceBroadcastHandle filter, int flags);

    [DllImport("user32", SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr handle, ref DeviceBroadcastInterface filter,
                                                            int flags);

    [DllImport("user32")]
    private static extern IntPtr UnregisterDeviceNotification(SafeHandle handle);

    [DllImport("kernel32")]
    private static extern int GetLastError();

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool CancelIo(SafeHandle handle);

    #endregion Interop

    #region Methods

    internal void Create()
    {
      if (Handle != IntPtr.Zero)
      {
        return;
      }

      CreateParams Params = new CreateParams();
      Params.ExStyle = 0x80;
      Params.Style = unchecked((int)0x80000000);
      CreateHandle(Params);
    }

    #endregion Methods

    #region Properties

    internal Guid Class
    {
      get { return _deviceClass; }
      set { _deviceClass = value; }
    }

    #endregion Properties

    #region Overrides

    protected override void WndProc(ref Message m)
    {
      if (m.Msg == WM_DEVICECHANGE)
      {
        switch (m.WParam.ToInt32())
        {
          case DBT_DEVICEARRIVAL:
            OnDeviceArrival((DeviceBroadcastHeader)Marshal.PtrToStructure(m.LParam, typeof (DeviceBroadcastHeader)),
                            m.LParam);
            break;
          case DBT_DEVICEREMOVECOMPLETE:
            OnDeviceRemoval((DeviceBroadcastHeader)Marshal.PtrToStructure(m.LParam, typeof (DeviceBroadcastHeader)),
                            m.LParam);
            break;
        }
      }
      else if (m.Msg == WM_SETTINGSCHANGE)
      {
        if (SettingsChanged != null)
        {
          SettingsChanged();
        }
      }

      base.WndProc(ref m);
    }

    #endregion Overrides

    #region Implementation

    internal void RegisterDeviceArrival()
    {
      DeviceBroadcastInterface dbi = new DeviceBroadcastInterface();

      dbi.Size = Marshal.SizeOf(dbi);
      dbi.DeviceType = 0x5;
      dbi.ClassGuid = _deviceClass;

      try
      {
        _handleDeviceArrival = new SafeFileHandle(RegisterDeviceNotification(Handle, ref dbi, 0), true);
      }
      catch
      {
        MceRemoteReceiver.LogInfo("DeviceWatcher.RegisterDeviceArrival: Error={0}.", Marshal.GetLastWin32Error());
      }

      if (_handleDeviceArrival.IsInvalid)
      {
        throw new Exception(string.Format("Failed in call to RegisterDeviceNotification ({0})", GetLastError()));
      }
    }

    internal void RegisterDeviceRemoval(SafeHandle deviceHandle)
    {
      DeviceBroadcastHandle dbh = new DeviceBroadcastHandle();

      dbh.Size = Marshal.SizeOf(dbh);
      dbh.DeviceType = 0x6;
      dbh.Handle = deviceHandle.DangerousGetHandle();

      _deviceHandle = deviceHandle;
      try
      {
        _handleDeviceRemoval = new SafeFileHandle(RegisterDeviceNotification(Handle, ref dbh, 0), true);
      }
      catch
      {
        MceRemoteReceiver.LogInfo("DeviceWatcher.RegisterDeviceRemoval: Error={0}.", Marshal.GetLastWin32Error());
      }

      if (_handleDeviceRemoval.IsInvalid)
      {
        throw new Exception(string.Format("Failed in call to RegisterDeviceNotification ({0})", GetLastError()));
      }
    }

    internal void UnregisterDeviceArrival()
    {
      if (_handleDeviceArrival.IsInvalid)
      {
        return;
      }

      UnregisterDeviceNotification(_handleDeviceArrival);
      _handleDeviceArrival.Close();
    }

    internal void UnregisterDeviceRemoval()
    {
      if (_handleDeviceRemoval.IsInvalid)
      {
        return;
      }

      UnregisterDeviceNotification(_handleDeviceRemoval);
      _handleDeviceRemoval.Close();
      _deviceHandle.Close();
    }

    private void OnDeviceArrival(DeviceBroadcastHeader dbh, IntPtr ptr)
    {
      if (dbh.DeviceType == 0x05)
      {
        DeviceBroadcastInterface dbi =
          (DeviceBroadcastInterface)Marshal.PtrToStructure(ptr, typeof (DeviceBroadcastInterface));

        if (dbi.ClassGuid == _deviceClass && DeviceArrival != null)
        {
          DeviceArrival(this, EventArgs.Empty);
        }
      }
    }

    private void OnDeviceRemoval(DeviceBroadcastHeader header, IntPtr ptr)
    {
      if (header.DeviceType == 0x06)
      {
        DeviceBroadcastHandle dbh = (DeviceBroadcastHandle)Marshal.PtrToStructure(ptr, typeof (DeviceBroadcastHandle));

        if (dbh.Handle != _deviceHandle.DangerousGetHandle())
        {
          return;
        }

        CancelIo(_deviceHandle);
        UnregisterDeviceRemoval();

        if (DeviceRemoval != null)
        {
          DeviceRemoval(this, EventArgs.Empty);
        }
      }
    }

    #endregion Implementation
  }

  public delegate void SettingsChanged();
}