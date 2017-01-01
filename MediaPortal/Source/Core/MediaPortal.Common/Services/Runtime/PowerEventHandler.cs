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
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Common.Services.Runtime
{
  public class PowerEventHandler : IDisposable
  {
    #region Fields

    private uint _powerEventThreadId;
    private Thread _powerEventThread;
    private IntPtr _dummyWindow;

    #endregion

    #region Constructor

    public PowerEventHandler()
    {
      _powerEventThread = new Thread(PowerEventHandlerThread) { Name = "PwrEvtHnd", IsBackground = true, Priority = ThreadPriority.AboveNormal };
      _powerEventThread.Start();
    }

    #endregion

    #region Public properties and events

    public delegate bool OnQuerySuspendDelegate();
    public delegate void OnPowerEventDelegate(PowerBroadcastStatus status);

    public event OnPowerEventDelegate OnPowerEvent;
    public event OnQuerySuspendDelegate OnQuerySuspend;

    #endregion

    #region Protected members

    protected void PowerEventHandlerThread()
    {
      Thread.BeginThreadAffinity();
      try
      {
        _powerEventThreadId = NativeMethods.GetCurrentThreadId();

        NativeMethods.WindowClass wndclass;
        wndclass.style = 0;
        wndclass.lpfnWndProc = PowerEventThreadWndProc;
        wndclass.cbClsExtra = 0;
        wndclass.cbWndExtra = 0;
        wndclass.hInstance = Process.GetCurrentProcess().Handle;
        wndclass.hIcon = IntPtr.Zero;
        wndclass.hCursor = IntPtr.Zero;
        wndclass.hbrBackground = IntPtr.Zero;
        wndclass.lpszMenuName = null;
        wndclass.lpszClassName = "PowerEventHandlerThreadWndClass";

        NativeMethods.RegisterClass(ref wndclass);

        _dummyWindow = NativeMethods.CreateWindowEx(0x80, wndclass.lpszClassName, "", 0x80000000, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, wndclass.hInstance, IntPtr.Zero);

        if (_dummyWindow.Equals(IntPtr.Zero))
        {
          SafeLogD("PowerEventHandlerThread cannot create window handle, exiting thread");
          return;
        }

        // This thread needs a message loop to handle power messages from Windows.
        SafeLogD("PowerEventHandlerThread message loop is running");
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
            SafeLogE("PowerEventHandlerThread", ex);
          }
        }
      }
      finally
      {
        Thread.EndThreadAffinity();
        SafeLogD("PowerEventHandlerThread finished");
      }
    }

    protected int PowerEventThreadWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
      if (msg == NativeMethods.WM_POWERBROADCAST)
      {
        PowerBroadcastStatus status = (PowerBroadcastStatus) wParam.ToInt32();
        SafeLogD("PowerEventHandler received {0}", status);
        switch (status)
        {
          case PowerBroadcastStatus.QuerySuspend:
            var onQuerySuspend = OnQuerySuspend;
            if (onQuerySuspend != null)
              if (!onQuerySuspend())
                return NativeMethods.BROADCAST_QUERY_DENY;
            break;
          default:
            var onPowerEvent = OnPowerEvent;
            if (onPowerEvent != null)
              onPowerEvent(status);
            break;
        }
      }
      return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Logging wrapper to avoid exceptions when ILogger service is already removed on application end.
    /// </summary>
    protected void SafeLogD(string message, params object[] args)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Debug(message, args);
    }

    /// <summary>
    /// Logging wrapper to avoid exceptions when ILogger service is already removed on application end.
    /// </summary>
    protected void SafeLogE(string message, Exception ex, params object[] args)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Error(message, ex, args);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_powerEventThreadId != 0)
      {
        NativeMethods.PostThreadMessage(_powerEventThreadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _powerEventThread.Join();
        _powerEventThreadId = 0;
        _powerEventThread = null;
      }
      if (_dummyWindow != IntPtr.Zero)
        NativeMethods.DestroyWindow(_dummyWindow);
    }

    #endregion
  }
}
