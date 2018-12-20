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
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MediaPortal.Plugins.InputDeviceManager.RawInput
{
   public class RawInputHandler : NativeWindow
   {
      static RawKeyboard _keyboardDriver;
      readonly IntPtr _devNotifyHandle;
      static readonly Guid DeviceInterfaceHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");
      private PreMessageFilter _filter;

      public event RawKeyboard.DeviceEventHandler KeyPressed
      {
         add { _keyboardDriver.KeyPressed += value; }
         remove { _keyboardDriver.KeyPressed -= value; }
      }

      public int NumberOfKeyboards
      {
         get { return _keyboardDriver.NumberOfKeyboards; }
      }

      public void AddMessageFilter()
      {
         if (null != _filter) return;

         _filter = new PreMessageFilter();
         Application.AddMessageFilter(_filter);
      }

      private void RemoveMessageFilter()
      {
         if (null == _filter) return;

         Application.RemoveMessageFilter(_filter);
      }

      public RawInputHandler(IntPtr parentHandle, bool captureOnlyInForeground)
      {
         AssignHandle(parentHandle);

         _keyboardDriver = new RawKeyboard(parentHandle, captureOnlyInForeground);
         _keyboardDriver.EnumerateDevices();
         _devNotifyHandle = RegisterForDeviceNotifications(parentHandle);
      }

      static IntPtr RegisterForDeviceNotifications(IntPtr parent)
      {
         var usbNotifyHandle = IntPtr.Zero;
         var bdi = new BroadcastDeviceInterface();
         bdi.DbccSize = Marshal.SizeOf(bdi);
         bdi.BroadcastDeviceType = BroadcastDeviceType.DBT_DEVTYP_DEVICEINTERFACE;
         bdi.DbccClassguid = DeviceInterfaceHid;

         var mem = IntPtr.Zero;
         try
         {
            mem = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BroadcastDeviceInterface)));
            Marshal.StructureToPtr(bdi, mem, false);
            usbNotifyHandle = Win32.RegisterDeviceNotification(parent, mem, DeviceNotification.DEVICE_NOTIFY_WINDOW_HANDLE);
         }
         catch (Exception e)
         {
            Debug.WriteLine("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
            Debug.WriteLine(e.StackTrace);
         }
         finally
         {
            Marshal.FreeHGlobal(mem);
         }

         if (usbNotifyHandle == IntPtr.Zero)
         {
            Debug.WriteLine("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
         }

         return usbNotifyHandle;
      }

      protected override void WndProc(ref Message message)
      {
         switch (message.Msg)
         {
            case Win32.WM_INPUT:
               {
                  if (_keyboardDriver.ProcessRawInput(message.LParam))
                  {
                     message.Result = IntPtr.Zero;
                     return;
                  }
               }
               break;

            case Win32.WM_USB_DEVICECHANGE:
               {
                  Debug.WriteLine("USB Device Arrival / Removal");
                  _keyboardDriver.EnumerateDevices();
               }
               break;
         }

         base.WndProc(ref message);
      }

      ~RawInputHandler()
      {
         Win32.UnregisterDeviceNotification(_devNotifyHandle);
         RemoveMessageFilter();
      }
   }
}
