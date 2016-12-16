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
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable 618

namespace MediaPortal.Plugins.MceRemoteReceiver.Hardware
{
  public class Remote : Device
  {
    #region Consts

    const int VAL_END_READ = 25;
    const int DEV_BUF_INDEX = 9;

    #endregion

    #region Fields

    private static Remote _deviceSingleton;
    private int _doubleClickTime = -1;
    private int _doubleClickTick = 0;
    private RemoteButton _doubleClickButton;

    #endregion Fields

    #region Events

    public static RemoteEventHandler Click = null;
    public static RemoteEventHandler DoubleClick = null;

    #endregion Events

    #region Constructor

    static Remote()
    {
      _deviceSingleton = new Remote();
      _deviceSingleton.Init();
    }

    #endregion Constructor

    #region Implementation

    private void Init()
    {
      try
      {
        _deviceClass = HidGuid;
        _doubleClickTime = GetDoubleClickTime();

        _deviceBuffer = new byte[256];

        _deviceWatcher = new DeviceWatcher();
        _deviceWatcher.Create();
        _deviceWatcher.Class = _deviceClass;
        _deviceWatcher.DeviceArrival += new DeviceEventHandler(OnDeviceArrival);
        _deviceWatcher.DeviceRemoval += new DeviceEventHandler(OnDeviceRemoval);
        _deviceWatcher.SettingsChanged += new SettingsChanged(OnSettingsChanged);
        _deviceWatcher.RegisterDeviceArrival();

        Open();
      }
      catch (Exception e)
      {
        MceRemoteReceiver.LogInfo("Init: {0}", e.Message);
      }
    }

    protected override void Open()
    {
      string devicePath = FindDevice(_deviceClass);

      if (devicePath == null)
      {
        return;
      }
      if (LogVerbose)
      {
        MceRemoteReceiver.LogInfo("Using {0}", devicePath);
      }

      SafeFileHandle deviceHandle = CreateFile(devicePath, FileAccess.Read, FileShare.ReadWrite, 0, FileMode.Open,
                                               FileFlag.Overlapped, 0);

      if (deviceHandle.IsInvalid)
      {
        throw new Exception(string.Format("Failed to open remote ({0})", GetLastError()));
      }

      _deviceWatcher.RegisterDeviceRemoval(deviceHandle);

      // open a stream from the device and begin an asynchronous read
      _deviceStream = new FileStream(deviceHandle, FileAccess.Read, 128, true);
      _deviceStream.BeginRead(_deviceBuffer, 0, _deviceBuffer.Length, new AsyncCallback(OnReadComplete), null);
    }

    private void OnReadComplete(IAsyncResult asyncResult)
    {
      try
      {

        if (_deviceStream.EndRead(asyncResult) == VAL_END_READ && _deviceBuffer[1] == 1)
        {
          if (_deviceBuffer[DEV_BUF_INDEX] == (int) _doubleClickButton &&
              Environment.TickCount - _doubleClickTick <= _doubleClickTime)
          {
            if (DoubleClick != null)
            {
              DoubleClick(this, new RemoteEventArgs(_doubleClickButton));
            }
          }
          else
          {
            _doubleClickButton = (RemoteButton) _deviceBuffer[DEV_BUF_INDEX];
            _doubleClickTick = Environment.TickCount;

            if (Click != null)
            {
              Click(this, new RemoteEventArgs(_doubleClickButton));
            }
          }
        }
        // begin another asynchronous read from the device
        _deviceStream.BeginRead(_deviceBuffer, 0, _deviceBuffer.Length, new AsyncCallback(OnReadComplete), null);
      }
      catch (Exception)
      {
      }
    }

    private void OnSettingsChanged()
    {
      _doubleClickTime = GetDoubleClickTime();
    }

    #endregion Implementation

    #region Interop

    [DllImport("user32")]
    private static extern int GetDoubleClickTime();

    #endregion Interop
  }
}