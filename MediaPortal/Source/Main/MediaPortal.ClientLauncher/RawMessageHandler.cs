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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.SystemAPI;
using SharpLib.Hid;
using SharpLib.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace MediaPortal.Client.Launcher
{
  public class RawMessageHandler : IDisposable
  {
    private readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

    private uint _hidThreadId;
    private Thread _hidEventThread;
    private IntPtr _dummyWindow;
    private SharpLib.Hid.Handler _hidHandler;

    public event EventHandler OnStartRequest;

    public RawMessageHandler()
    {
      _hidEventThread = new Thread(HidEventHandlerThread) { Name = "hidEvtHnd", IsBackground = true, Priority = ThreadPriority.Normal };
      _hidEventThread.Start();
    }

    private void HidEventHandlerThread()
    {
      Thread.BeginThreadAffinity();
      try
      {
        _hidThreadId = NativeMethods.GetCurrentThreadId();

        NativeMethods.WindowClass wndclass;
        wndclass.style = 0;
        wndclass.lpfnWndProc = HidEventThreadWndProc;
        wndclass.cbClsExtra = 0;
        wndclass.cbWndExtra = 0;
        wndclass.hInstance = Process.GetCurrentProcess().Handle;
        wndclass.hIcon = IntPtr.Zero;
        wndclass.hCursor = IntPtr.Zero;
        wndclass.hbrBackground = IntPtr.Zero;
        wndclass.lpszMenuName = null;
        wndclass.lpszClassName = "HidEventHandlerThreadWndClass";

        NativeMethods.RegisterClass(ref wndclass);

        _dummyWindow = NativeMethods.CreateWindowEx(0x80, wndclass.lpszClassName, "", 0x80000000, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wndclass.hInstance, IntPtr.Zero);
        if (_dummyWindow.Equals(IntPtr.Zero))
        {
          ServiceRegistration.Get<ILogger>().Debug("HidEventHandlerThread cannot create window handle, exiting thread");
          return;
        }

        SharpLib.Win32.RawInputDeviceFlags flags = SharpLib.Win32.RawInputDeviceFlags.RIDEV_INPUTSINK;
        IntPtr handle = _dummyWindow;
        List<RAWINPUTDEVICE> devices = new List<RAWINPUTDEVICE>();
        devices.Add(new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)SharpLib.Hid.UsagePage.WindowsMediaCenterRemoteControl,
          usUsage = (ushort)SharpLib.Hid.UsageCollection.WindowsMediaCenter.WindowsMediaCenterRemoteControl,
          dwFlags = flags,
          hwndTarget = handle
        });
        devices.Add(new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
          usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.ConsumerControl,
          dwFlags = flags,
          hwndTarget = handle
        });
        devices.Add(new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
          usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.Keyboard,
          dwFlags = flags,
          hwndTarget = handle
        });

        _hidHandler = new SharpLib.Hid.Handler(devices.ToArray(), true, -1, -1);
        _hidHandler.OnHidEvent += new Handler.HidEventHandler(OnHidEvent);

        // This thread needs a message loop to handle power messages from Windows.
        ServiceRegistration.Get<ILogger>().Debug("HidEventHandlerThread message loop is running");
        while (true)
        {
          try
          {
            NativeMethods.Message msgApi = new NativeMethods.Message();
            if (!NativeMethods.GetMessageA(ref msgApi, IntPtr.Zero, 0, 0)) // returns false on WM_QUIT
              return;

            NativeMethods.TranslateMessage(ref msgApi);
            NativeMethods.DispatchMessageA(ref msgApi);
          }
          catch (Exception ex)
          {
            ServiceRegistration.Get<ILogger>().Error("HidEventHandlerThread", ex);
          }
        }
      }
      finally
      {
        Thread.EndThreadAffinity();
        ServiceRegistration.Get<ILogger>().Debug("HidEventHandlerThread finished");
      }
    }

    private int HidEventThreadWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
      var message = new Message { HWnd = hWnd, LParam = lParam, WParam = wParam, Msg = (int)msg };
      _hidHandler?.ProcessInput(ref message);
      return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void OnHidEvent(object sender, SharpLib.Hid.Event hidEvent)
    {
      try
      {
        if (hidEvent.IsButtonUp)
        {
          if (hidEvent.IsGeneric)
          {
            var id = hidEvent.Usages.FirstOrDefault();
            if (hidEvent.UsagePageEnum == UsagePage.WindowsMediaCenterRemoteControl)
            {
              if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.WindowsMediaCenterRemoteControl), id) && (SharpLib.Hid.Usage.WindowsMediaCenterRemoteControl)id == SharpLib.Hid.Usage.WindowsMediaCenterRemoteControl.GreenStart)
              {
                OnStartRequest?.Invoke(this, EventArgs.Empty);
              }
            }
            else if (hidEvent.UsagePageEnum == UsagePage.Consumer)
            {
              if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.ConsumerControl), id) && ((SharpLib.Hid.Usage.ConsumerControl)id == SharpLib.Hid.Usage.ConsumerControl.AppLaunchEntertainmentContentBrowser ||
                (SharpLib.Hid.Usage.ConsumerControl)id == SharpLib.Hid.Usage.ConsumerControl.AppLaunchMovieBrowser))
              {
                OnStartRequest?.Invoke(this, EventArgs.Empty);
              }
            }
          }
          else if (hidEvent.IsKeyboard)
          {
            if (hidEvent.VirtualKey == Keys.LaunchApplication1)
            {
              OnStartRequest?.Invoke(this, EventArgs.Empty);
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("HID event failed", ex);
      }
    }

    public void Dispose()
    {
      if (_hidThreadId != 0)
      {
        NativeMethods.PostThreadMessage(_hidThreadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _hidEventThread.Join();
        _hidThreadId = 0;
        _hidEventThread = null;
      }
      if (_hidHandler != null)
      {
        //First de-register
        _hidHandler.Dispose();
        _hidHandler = null;
      }
      if (_dummyWindow != IntPtr.Zero)
        NativeMethods.DestroyWindow(_dummyWindow);
    }
  }
}
