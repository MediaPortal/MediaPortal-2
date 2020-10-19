#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using CecSharp;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.CECRemote.Settings;
using System.Threading;

namespace MediaPortal.UiComponents.CECRemote
{
  public class CecRemoteEventArgs : EventArgs
  {
    private readonly CecKeypress _key;
    private readonly CecLogMessage _message;
    private readonly CecCommand _command;

    public CecRemoteEventArgs(CecKeypress key)
    {
      _key = key;
    }

    public CecRemoteEventArgs(CecLogMessage message)
    {
      _message = message;
    }

    public CecRemoteEventArgs(CecCommand command)
    {
      _command = command;
    }

    public CecKeypress Key
    {
      get { return _key; }
    }

    public CecLogMessage Message
    {
      get { return _message; }
    }

    public CecCommand Command
    {
      get { return _command; }
    }
  }

  public delegate void CecRemoteKeyEventHandler(object sender, CecRemoteEventArgs e);
  public delegate void CecRemoteLogEventHandler(object sender, CecRemoteEventArgs e);
  public delegate void CecRemoteCommandEventHandler(object sender, CecRemoteEventArgs e);



  class CecClient : CecCallbackMethods
  {
    private int _logLevel;
    private LibCecSharp _lib;
    private LibCECConfiguration _config;
    private Object _connectLock = new Object();
    private bool _connected;
    private byte hdmiPort;
    private string deviceName;
    private CecDeviceType deviceType;
    private CecLogLevel level;


    public event CecRemoteKeyEventHandler CecRemoteKeyEvent;
    public event CecRemoteLogEventHandler CecRemoteLogEvent;
    public event CecRemoteCommandEventHandler CecRemoteCommandEvent;

    public CecClient(CECRemoteSettings settings, byte hdmiPortIn, string deviceNameIn, CecDeviceType deviceTypeIn, CecLogLevel levelIn)
    {

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      CECRemoteSettings _cecConfig = settingsManager.Load<CECRemoteSettings>();


      hdmiPort = (byte)_cecConfig.HDMIPort;
      deviceName = _cecConfig.DeviceName;
      deviceType = deviceTypeIn;
      level = levelIn;

      init();

    }

    public void init()
    {
      _connected = false;

      if (_config == null)
      {
        _config = new LibCECConfiguration();
        _config.SetCallbacks(this);
      }

      _config.DeviceTypes.Types[0] = deviceType;
      _config.DeviceName = deviceName;
      _config.ClientVersion = LibCECConfiguration.CurrentVersion;
      _config.AutodetectAddress = true;
      _config.WakeDevices.Clear();
      _config.PowerOffDevices.Clear();
      _config.ActivateSource = false;  // Control this manually based on settings.
      _config.PhysicalAddress = 0;
      _config.HDMIPort = hdmiPort;
      _logLevel = (int)level;

      if (_lib == null)
      {
        _lib = new LibCecSharp(_config);

      }
      else
      {
        _lib.SetConfiguration(_config);
        _lib.EnableCallbacks(this);

      }
    }



    protected virtual void OnCecRemoteKeyEvent(CecRemoteEventArgs e)
    {
      if (CecRemoteKeyEvent != null)
      {
        CecRemoteKeyEvent(this, e);
      }
    }

    protected virtual void OnCecRemoteLogEvent(CecRemoteEventArgs e)
    {
      if (CecRemoteLogEvent != null)
      {
        CecRemoteLogEvent(this, e);
      }
    }

    protected virtual void OnCecRemoteCommandEvent(CecRemoteEventArgs e)
    {
      if (CecRemoteCommandEvent != null)
      {
        CecRemoteCommandEvent(this, e);
      }
    }

    public override int ReceiveCommand(CecCommand command)
    {
      //test fix for samsung play/stop keys
      if (command.Opcode == CecOpcode.Play || command.Opcode == CecOpcode.DeckControl)
      {
        CecKeypress key = new CecKeypress();
        key.Duration = 0;
        if (command.Opcode == CecOpcode.Play)
        {
          key.Keycode = CecUserControlCode.Play;
        }
        else
        {
          key.Keycode = CecUserControlCode.Stop;
        }

        CecRemoteEventArgs e = new CecRemoteEventArgs(key);
        OnCecRemoteKeyEvent(e);
      }
      else
      {
        CecRemoteEventArgs e = new CecRemoteEventArgs(command);
        OnCecRemoteCommandEvent(e);
      }
      return 1;
    }

    public override int ReceiveKeypress(CecKeypress key)
    {
      CecRemoteEventArgs e = new CecRemoteEventArgs(key);
      OnCecRemoteKeyEvent(e);

      return 1;
    }

    public override int ReceiveLogMessage(CecLogMessage message)
    {
      if ((int)message.Level <= _logLevel)
      {
        CecRemoteEventArgs e = new CecRemoteEventArgs(message);
        OnCecRemoteLogEvent(e);
      }
      return 1;
    }

    public override int ConfigurationChanged(LibCECConfiguration _config)
    {
      return 1;
    }

    public bool Connect(int timeout)
    {
      CecAdapter[] adapters = _lib.FindAdapters(string.Empty);
      if (adapters.Length > 0)
      {
        return Connect(adapters[0].ComPort, timeout);
      }
      else
        return false;
    }

    public bool Connect(string port, int timeout)
    {
      return _lib.Open(port, timeout);
    }

    public void Close()
    {
      if (_lib != null)
      {
        _lib.DisableCallbacks();
        _lib.Close();
        _lib.Dispose();
      }

      _lib = null;
      _config = null;

      _connected = false;
    }


    /// <summary>
    /// Send "Power ON" -signal to multiple devices.
    /// </summary>
    public bool WakeDevice(CecLogicalAddresses device)
    {
      bool ret = true;

      foreach (CecLogicalAddress a in device.Addresses)
      {
        if (a != CecLogicalAddress.Unknown)
        {
          if (!WakeDevice(a))
          {
            ret = false;
          }
        }
      }

      return ret;
    }

    /// <summary>
    /// Send "Power ON" -signal to device.
    /// </summary>
    public bool WakeDevice(CecLogicalAddress device)
    {
      bool res = _lib.PowerOnDevices(device);

      return res;
    }

    /// <summary>
    /// Send "Stand by" -signal to multiple devices.
    /// </summary>
    public bool StandByDevice(CecLogicalAddresses device)
    {
      bool ret = true;

      foreach (CecLogicalAddress a in device.Addresses)
      {
        if (a != CecLogicalAddress.Unknown)
        {
          if (!StandByDevice(a))
          {
            ret = false;
          }
        }
      }

      return ret;
    }

    /// <summary>
    /// Send "Stand by" -signal to device.
    /// </summary>
    public bool StandByDevice(CecLogicalAddress device)
    {
      bool res = _lib.StandbyDevices(device);

      return res;
    }

    public void OnStop()
    {

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      CECRemoteSettings _cecConfig = settingsManager.Load<CECRemoteSettings>();

      lock (_connectLock)
      {
        if (_lib == null)
        {
          Close();
          return;
        }

        _lib.DisableCallbacks();

        if (_cecConfig.InactivateSourceOnSleep)
        {
          SetSource(false);
        }

        if (_cecConfig.StandbyDevicesOnSleep)
        {
          // StandByDevice(0);
        }

        Close();
      }
    }

    public void OnSleep()
    {

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      CECRemoteSettings _cecConfig = settingsManager.Load<CECRemoteSettings>();

      lock (_connectLock)
      {

        if (_lib == null)
        {
          Close();
          return;
        }

        _lib.DisableCallbacks();

        //    if (_cecConfig.RequireActiveSourceWhenSleep)
        //    {
        //        if (!_lib.IsLibCECActiveSource())
        //        {
        //            DeInit();
        //            return;
        //        }
        //    }


        if (_cecConfig.InactivateSourceOnSleep)
        {
          SetSource(false);
        }

        if (_cecConfig.StandbyDevicesOnSleep)
        {
          StandByDevice(0);  //TV only for now
        }

        Close();

      }
    }

    public void OnResumeByUser()
    {
      lock (_connectLock)
      {

        if (_lib == null || _connected == false)
        {
          OnResumeByAutomatic();
        }

        //if (_cecConfig.RequireUserInputOnResume)
        //{
        //if (_cecConfig.WakeDevicesOnResume)
        //    {
        //        WakeDevice(0);  // TV only for now
        //    }
        //// Set this client as a active source(Changes TV input, etc.), if set in config.

        //    if (_cecConfig.ActivateSourceOnResume)
        //    {
        //        SetSource(true);
        //    }
        //}
        //_wakeUpByAutoEvent = false;
      }
    }

    public void OnResumeByAutomatic()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      CECRemoteSettings _cecConfig = settingsManager.Load<CECRemoteSettings>();

      lock (_connectLock)
      {

        // If lib is not created, it needs to be initialized first.
        if (_lib == null)
        {
          init();
        }

        if (!_connected)
        {
          // 10000 is default timeout to wait connection to open.
          if (Connect(10000))
          {
            _connected = true;
          }
          else
          {
            return;
          }
        }

        //if (!_config.RequireUserInputOnResume)
        //{
        if (_cecConfig.WakeDevicesOnResume)
        {
          WakeDevice(0); //TV only for now
        }
        // Set this client as a active source(Changes TV input, etc.), if set in config.

        if (_cecConfig.ActivateSourceOnResume)
        {
          SetSource(true);
        }
        //}

      }

    }

    public bool SetSource(bool active)
    {
      if (active)
      {
        return _lib.SetActiveSource(CecDeviceType.Reserved);
      }
      return _lib.SetInactiveView();
    }

  }
}
