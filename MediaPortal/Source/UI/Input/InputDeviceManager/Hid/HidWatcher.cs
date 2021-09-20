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
using SharpLib.Hid;
using SharpLib.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MediaPortal.Plugins.InputDeviceManager.Hid
{
  public class HidWatcher : NativeWindow, IDisposable
  {
    protected readonly object _syncObj = new object();
    protected Handler _handler;

    internal void Create()
    {
      lock (_syncObj)
      {
        if (Handle != IntPtr.Zero)
          return;

        CreateParams Params = new CreateParams();
        Params.ExStyle = 0x80;
        Params.Style = unchecked((int)0x80000000);
        CreateHandle(Params);
      }
    }

    public void RegisterInputDevices(IEnumerable<RAWINPUTDEVICE> inputDevices, int repeatDelay = -1, int repeatSpeed = -1)
    {
      lock (_syncObj)
      {
        DisposeHandler();

        _handler = new Handler(inputDevices.ToArray(), true, repeatDelay, repeatSpeed);

        if (_handler.IsRegistered)
          _handler.OnHidEvent += OnHidEvent;
        else
          Logger.Error("HidWatcher: Failed to register raw input devices: " + Marshal.GetLastWin32Error().ToString());
      }
    }

    public void UnregisterInputDevices()
    {
      lock (_syncObj)
        DisposeHandler();
    }

    public event Handler.HidEventHandler HidEvent;

    private void OnHidEvent(object aSender, Event aHidEvent)
    {
      //Logger.Info("HidEvent: {0}", aHidEvent.ToLog());
      HidEvent?.Invoke(this, aHidEvent);
    }

    protected override void WndProc(ref Message m)
    {
      _handler?.ProcessInput(ref m);
      base.WndProc(ref m);
    }

    protected void DisposeHandler()
    {
      if (_handler == null)
        return;
      _handler.OnHidEvent -= OnHidEvent;
      _handler.Dispose();
      _handler = null;
    }

    public void Dispose()
    {
      lock (_syncObj)
      {
        DisposeHandler();
        DestroyHandle();
      }
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
