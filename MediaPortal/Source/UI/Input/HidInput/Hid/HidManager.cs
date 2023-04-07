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

using HidInput.Messaging;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpLib.Hid;
using SharpLib.Hid.UsageCollection;
using SharpLib.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HidInput.Hid
{
  public class HidManager : IDisposable
  {
    private const bool CAPTURE_ONLY_IN_FOREGROUND = true;

    protected HidWatcher _watcher;
    protected List<Device> _connectedDevices;

    protected bool _isAttachedToActivated = false;
    protected bool _isApplicationActivated = false;
    private RawInputMouseButtonFlags? _lastMouseFlags = null;

    public HidManager()
    {
      Init();
    }

    public List<Device> ConnectedDevices
    {
      get 
      {
        if (_connectedDevices == null)
          _connectedDevices = GetConnectedDevices();
        return _connectedDevices;
      }
    }

    protected void Init()
    {
      if (_watcher != null)
        return;

      _watcher = new HidWatcher();
      _watcher.DeviceChange += WatcherDeviceChange;
      _watcher.HidEvent += WatcherHidEvent;
      _watcher.Create();

      RegisterHidDevices();
    }

    protected void RegisterHidDevices()
    {
      //Handle background events only if foreground app doesn't process them
      RawInputDeviceFlags flags = RawInputDeviceFlags.RIDEV_EXINPUTSINK;
      IList<RAWINPUTDEVICE> inputDevices = GetRawInputDevices(_watcher.Handle, flags);
      _watcher.RegisterInputDevices(inputDevices);
    }

    private void WatcherDeviceChange(object sender, DeviceChangeEventArgs e)
    {
      // Force recreation of device list the next time it is requested
      _connectedDevices = null;

      if (e.ChangeType == DeviceChangeType.Arrived)
        HidMessaging.BroadcastDeviceArrivedMessage(e.DeviceName);
      else if (e.ChangeType == DeviceChangeType.Removed)
        HidMessaging.BroadcastDeviceRemovedMessage(e.DeviceName);
    }

    private void WatcherHidEvent(object sender, Event hidEvent)
    {
      try
      {
        CheckAttachedToActivatedState();

        if (hidEvent == null)
          return;

        if (!hidEvent.IsValid)
        {
          Logger.Debug("HidManager: HID event invalid");
          return;
        }

        if (CAPTURE_ONLY_IN_FOREGROUND && !_isApplicationActivated)
          return;
        
        if (!IsHidEventNeeded(hidEvent))
          return;

        HidMessaging.BroadcastHidEventMessage(hidEvent);
      }
      catch (Exception ex)
      {
        Logger.Error("HidManager: HID event failed", ex);
      }
    }

    protected void CheckAttachedToActivatedState()
    {
      if (_isAttachedToActivated)
        return;
      Form mainForm = SkinContext.Form;
      // Form might not be created yet
      if (mainForm == null)
        return;
      _isAttachedToActivated = true;
      mainForm.Activated += Activated;
      mainForm.Deactivate += Deactivate;
      _isApplicationActivated = Form.ActiveForm == mainForm;
    }

    private bool IsHidEventNeeded(Event hidEvent)
    {
      if (hidEvent.IsMouse)
      {
        if (_lastMouseFlags == hidEvent.RawInput.data.mouse.mouseData.buttonsStr.usButtonFlags)
          return false; //Only send button event changes for mouse to avoid overloading the message queue with mouse move etc.

        _lastMouseFlags = hidEvent.RawInput.data.mouse.mouseData.buttonsStr.usButtonFlags;
      }
      return true;
    }

    private void Activated(object sender, EventArgs e)
    {
      _isApplicationActivated = true;
      HidMessaging.BroadcastActivatedMessage();
    }

    private void Deactivate(object sender, EventArgs e)
    {
      _isApplicationActivated = false;
      HidMessaging.BroadcastDeactivatedMessage();
    }

    public void Dispose()
    {
      _watcher.HidEvent -= WatcherHidEvent;
      _watcher.Dispose();
      _watcher = null;
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    #region Input Devices

    protected List<Device> GetConnectedDevices()
    {
      RAWINPUTDEVICELIST[] ridList = null;
      uint deviceCount = 0;
      int res = Function.GetRawInputDeviceList(ridList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
      if (res == -1)
      {
        // Failure case
        return new List<Device>(0);
      }

      ridList = new RAWINPUTDEVICELIST[deviceCount];
      res = Function.GetRawInputDeviceList(ridList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
      if (res != deviceCount)
      {
        // Failure case
        return new List<Device>(0);
      }

      List<Device> devices = new List<Device>();
      foreach (RAWINPUTDEVICELIST rid in ridList)
      {
        try
        {
          devices.Add(new Device(rid.hDevice));
        }
        catch
        {
          continue;
        }
      }
      return devices;
    }

    protected static IList<RAWINPUTDEVICE> GetRawInputDevices(IntPtr handle, RawInputDeviceFlags flags)
    {
      IList<RAWINPUTDEVICE> devices = new List<RAWINPUTDEVICE>
      {
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.WindowsMediaCenterRemoteControl,
          usUsage = (ushort)WindowsMediaCenter.WindowsMediaCenterRemoteControl,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.ConsumerControl,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.ApplicationLaunchButtons,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.FunctionButtons,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.GenericGuiApplicationControls,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.MediaSelection,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.NumericKeyPad,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.PlaybackSpeed,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.ProgrammableButtons,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.SelectDisc,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.Consumer,
          usUsage = (ushort)Consumer.Selection,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.GenericDesktopControls,
          usUsage = (ushort)GenericDesktop.SystemControl,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.GenericDesktopControls,
          usUsage = (ushort)GenericDesktop.GamePad,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.GenericDesktopControls,
          usUsage = (ushort)GenericDesktop.Joystick,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.GenericDesktopControls,
          usUsage = (ushort)GenericDesktop.Keyboard,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.GenericDesktopControls,
          usUsage = (ushort)GenericDesktop.KeyPad,
          dwFlags = flags,
          hwndTarget = handle
        },
        new RAWINPUTDEVICE
        {
          usUsagePage = (ushort)UsagePage.GenericDesktopControls,
          usUsage = (ushort)GenericDesktop.Mouse,
          dwFlags = flags,
          hwndTarget = handle
        },
      };

      return devices;
    }

    #endregion
  }
}
