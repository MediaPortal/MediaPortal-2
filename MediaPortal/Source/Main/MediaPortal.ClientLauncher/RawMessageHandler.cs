#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.PathManager;
using MediaPortal.Utilities.SystemAPI;
using SharpLib.Hid;
using SharpLib.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace MediaPortal.Client.Launcher
{
  public class RawMessageHandler : IDisposable
  {
    private readonly IntPtr HWND_MESSAGE = new IntPtr(-3);
    private const string REMOTE_INPUT_TYPE = "Remote";
    private const string CONSUMER_INPUT_TYPE = "Consumer";
    private const string KEYBOARD_INPUT_TYPE = "Keyboard";

    private uint _hidThreadId;
    private Thread _hidEventThread;
    private IntPtr _dummyWindow;
    private SharpLib.Hid.Handler _hidHandler;
    private List<StartCode> _startCodes;
    private Dictionary<string, List<string>> _currentCodes;
    private System.Timers.Timer _resetTimer = new System.Timers.Timer(3000);

    public event EventHandler OnStartRequest;

    public RawMessageHandler()
    {
      _resetTimer.AutoReset = false;
      _resetTimer.Elapsed += (s, e) =>
      {
        ClearInput();
      };

      _currentCodes = new Dictionary<string, List<string>>();
      _currentCodes[REMOTE_INPUT_TYPE] = new List<string>();
      _currentCodes[CONSUMER_INPUT_TYPE] = new List<string>();
      _currentCodes[KEYBOARD_INPUT_TYPE] = new List<string>();

      _startCodes = new List<StartCode>();
      LoadStartCodes();

      _hidEventThread = new Thread(HidEventHandlerThread) { Name = "hidEvtHnd", IsBackground = true, Priority = ThreadPriority.Normal };
      _hidEventThread.Start();
    }

    private void LoadStartCodes()
    {
      _startCodes.Clear();
      string startCodeFile = ServiceRegistration.Get<IPathManager>().GetPath(@"<CONFIG>\StartCodes.xml");
      if (!File.Exists(startCodeFile))
        startCodeFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Defaults\StartCodes.xml");
      if (!File.Exists(startCodeFile))
      {
        ServiceRegistration.Get<ILogger>().Info("Unable to load start codes. Using defaults");
        _startCodes.Add(new StartCode(REMOTE_INPUT_TYPE, "13")); //GreenStart
        _startCodes.Add(new StartCode(CONSUMER_INPUT_TYPE, "440")); //AppLaunchMovieBrowser
        _startCodes.Add(new StartCode(CONSUMER_INPUT_TYPE, "448")); //AppLaunchEntertainmentContentBrowser
        _startCodes.Add(new StartCode(KEYBOARD_INPUT_TYPE, "91,164,13")); //WMC launch
        return;
      }

      XmlSerializer reader = new XmlSerializer(typeof(List<StartCode>));
      using (StreamReader file = new StreamReader(startCodeFile))
      {
        var list = (List<StartCode>)reader.Deserialize(file);
        foreach (var item in list)
          _startCodes.Add(new StartCode(item.Type.Trim(), item.Codes.Replace(" ", "").Trim()));
      }
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
        if (hidEvent.IsButtonDown)
        {
          if (hidEvent.IsGeneric)
          {
            var id = hidEvent.Usages.FirstOrDefault();
            if (hidEvent.UsagePageEnum == UsagePage.WindowsMediaCenterRemoteControl && !_currentCodes[REMOTE_INPUT_TYPE].Contains(id.ToString()))
              _currentCodes[REMOTE_INPUT_TYPE].Add(id.ToString());
            if (hidEvent.UsagePageEnum == UsagePage.Consumer && !_currentCodes[CONSUMER_INPUT_TYPE].Contains(id.ToString()))
              _currentCodes[CONSUMER_INPUT_TYPE].Add(id.ToString());
          }
          else if (hidEvent.IsKeyboard)
          {
            int key = (int)hidEvent.VirtualKey;
            if (!_currentCodes[KEYBOARD_INPUT_TYPE].Contains(key.ToString()))
              _currentCodes[KEYBOARD_INPUT_TYPE].Add(key.ToString());
          }
          _resetTimer.Enabled = true;
        }
        if (hidEvent.IsButtonUp)
        {
          if (hidEvent.IsGeneric)
          {
            var id = hidEvent.Usages.FirstOrDefault();
            if (hidEvent.UsagePageEnum == UsagePage.WindowsMediaCenterRemoteControl)
            {
              if (IsCombinationMatch(REMOTE_INPUT_TYPE))
                OnStartRequest?.Invoke(this, EventArgs.Empty);

              if (_currentCodes[REMOTE_INPUT_TYPE].Contains(id.ToString()))
                _currentCodes[REMOTE_INPUT_TYPE].Remove(id.ToString());
            }
            else if (hidEvent.UsagePageEnum == UsagePage.Consumer)
            {
              if (IsCombinationMatch(CONSUMER_INPUT_TYPE))
                OnStartRequest?.Invoke(this, EventArgs.Empty);

              if (_currentCodes[CONSUMER_INPUT_TYPE].Contains(id.ToString()))
                _currentCodes[CONSUMER_INPUT_TYPE].Remove(id.ToString());
            }
          }
          else if (hidEvent.IsKeyboard)
          {
            if (IsCombinationMatch(KEYBOARD_INPUT_TYPE))
              OnStartRequest?.Invoke(this, EventArgs.Empty);

            int key = (int)hidEvent.VirtualKey;
            if (_currentCodes[KEYBOARD_INPUT_TYPE].Contains(key.ToString()))
              _currentCodes[KEYBOARD_INPUT_TYPE].Remove(key.ToString());
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("HID event failed", ex);
      }
    }

    private void ClearInput()
    {
      _resetTimer.Enabled = false;
      _currentCodes[REMOTE_INPUT_TYPE].Clear();
      _currentCodes[CONSUMER_INPUT_TYPE].Clear();
      _currentCodes[KEYBOARD_INPUT_TYPE].Clear();
    }

    private bool IsCombinationMatch(string type)
    {
      foreach (var startCode in _startCodes.Where(s => s.Type == type))
      {
        string[] codes = startCode.Codes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (_currentCodes[type].All(c => codes.Contains(c)) && codes.All(c => _currentCodes[type].Contains(c)))
        {
          ClearInput();
          return true;
        }
      }
      return false;
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
