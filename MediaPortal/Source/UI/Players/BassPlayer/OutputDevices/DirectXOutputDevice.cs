#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.Generic;
using System.Threading;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.PlayerComponents;
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer.OutputDevices
{
  /// <summary>
  /// Represents the user-selected DirectX outputdevice.
  /// </summary>
  internal class DirectXOutputDevice : IOutputDevice
  {
    #region Static members

    protected static readonly Dictionary<int, DeviceInfo> _deviceInfos = new Dictionary<int, DeviceInfo>();

    #endregion

    #region Fields

    private readonly Controller _controller;
    private readonly STREAMPROC _streamWriteProcDelegate;
    private readonly int _deviceNo;
    private readonly Silence _silence;
    private volatile DeviceState _deviceState;

    private BassStream _inputStream = null;
    private BassStream _outputStream = null;
    private BassStreamFader _fader = null;
    private bool _outputStreamEnded = false;

    #endregion

    public DirectXOutputDevice(Controller controller)
    {
      _controller = controller;
      _deviceState = DeviceState.Stopped;
      _streamWriteProcDelegate = OutputStreamWriteProc;
      _silence = new Silence();

      _deviceNo = GetDeviceNo();

      BASSInit flags = BASSInit.BASS_DEVICE_DEFAULT;

      // Because all deviceinfo is saved in a static dictionary,
      // we need to determine the latency only once.
      if (!_deviceInfos.ContainsKey(_deviceNo))
        flags |= BASSInit.BASS_DEVICE_LATENCY;

      bool result = Bass.BASS_Init(
          _deviceNo,
          44100, //Only relevant for -> pre-XP (VxD drivers)
          flags,
          IntPtr.Zero);

      BASSError? bassInitErrorCode = result ? null : new BASSError?(Bass.BASS_ErrorGetCode());

      // If the GetDeviceNo() method returned BassConstants.BassDefaultDevice, we must request the actual device number
      // of the choosen default device
      _deviceNo = Bass.BASS_GetDevice();

      if (bassInitErrorCode.HasValue)
      {
        if (bassInitErrorCode.Value == BASSError.BASS_ERROR_ALREADY)
        {
          if (!Bass.BASS_SetDevice(_deviceNo))
            throw new BassLibraryException("BASS_SetDevice");
          bassInitErrorCode = null;
        }
      }

      if (bassInitErrorCode.HasValue)
        throw new BassLibraryException("BASS_Init", bassInitErrorCode.Value);

      CollectDeviceInfo(_deviceNo);

      int ms = Convert.ToInt32(Controller.GetSettings().DirectSoundBufferSize.TotalMilliseconds);

      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, ms))
        throw new BassLibraryException("BASS_SetConfig");

      // Enable update thread while the output device is active
      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, ms / 4))
        throw new BassLibraryException("BASS_SetConfig");
    }

    #region IDisposable Members

    public void Dispose()
    {
      Stop();

      Log.Debug("Disposing output stream");

      _outputStream.Dispose();
      _outputStream = null;

      Log.Debug("Resetting global Bass environment");

      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0))
        throw new BassLibraryException("BASS_SetConfig");

      if (!Bass.BASS_SetDevice(_deviceNo))
        throw new BassLibraryException("BASS_SetDevice");

      if (!Bass.BASS_Free())
        throw new BassLibraryException("BASS_Free");

      if (!Bass.BASS_SetDevice(BassConstants.BassNoSoundDevice))
        throw new BassLibraryException("BASS_SetDevice");
    }

    #endregion

    #region IOutputDevice Members

    public BassStream InputStream
    {
      get { return _inputStream; }
    }

    public DeviceState DeviceState
    {
      get { return _deviceState; }
    }

    public string Name
    {
      get { return _deviceInfos[_deviceNo].Name; }
    }

    public string Driver
    {
      get { return _deviceInfos[_deviceNo].Driver; }
    }

    public int Channels
    {
      get { return _deviceInfos[_deviceNo].Channels; }
    }

    public int MinRate
    {
      get { return _deviceInfos[_deviceNo].MinRate; }
    }

    public int MaxRate
    {
      get { return _deviceInfos[_deviceNo].MaxRate; }
    }

    public TimeSpan Latency
    {
      get { return _deviceInfos[_deviceNo].Latency + Controller.GetSettings().DirectSoundBufferSize; }
    }

    public void SetInputStream(BassStream stream, bool passThrough)
    {
      if (_deviceState != DeviceState.Stopped)
        throw new BassPlayerException("Device state is not 'DeviceState.Stopped'");

      _inputStream = stream;

      Log.Debug("Creating output stream");

      const BASSFlag flags = BASSFlag.BASS_SAMPLE_FLOAT;
      int handle = Bass.BASS_StreamCreate(
          _inputStream.SampleRate,
          _inputStream.Channels,
          flags,
          _streamWriteProcDelegate,
          IntPtr.Zero);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_StreamCreate");

      _outputStream = BassStream.Create(handle);

      if (passThrough)
        _fader = new BassStreamFader(_inputStream, Controller.GetSettings().FadeDuration);

      ResetState();
    }

    public void PrepareFadeIn()
    {
      if (_fader != null)
        _fader.PrepareFadeIn();
    }

    public void FadeIn(bool async)
    {
      if (_fader != null)
        _fader.FadeIn(async);
    }

    public void FadeOut(bool async)
    {
      if (_fader != null && !_outputStreamEnded)
      {
        Log.Debug("Fading out");
        _fader.FadeOut(async);
      }
    }

    public void Start()
    {
      if (_deviceState == DeviceState.Started)
        return;
      
      Log.Debug("Starting output");
      _deviceState = DeviceState.Started;

      if (!Bass.BASS_ChannelPlay(_outputStream.Handle, false))
        throw new BassLibraryException("BASS_ChannelPlay");
    }

    public void Stop()
    {
      if (_deviceState == DeviceState.Stopped)
        return;

      Log.Debug("Stopping output");
      _deviceState = DeviceState.Stopped;

      if (!Bass.BASS_ChannelStop(_outputStream.Handle))
        throw new BassLibraryException("BASS_ChannelStop");
    }

    public void ClearBuffers()
    {
      Bass.BASS_ChannelSetPosition(_outputStream.Handle, 0L);
    }

    #endregion

    #region Public members

    public DeviceInfo CurrentDeviceInfo
    {
      get
      {
        lock (_deviceInfos)
          return _deviceInfos[_deviceNo];
      }
    }

    #endregion

    #region Private members

    /// <summary>
    /// Retrieves information on a device and adds it to the static deviceinfo dictionary do it can be reused later. 
    /// </summary>
    /// <param name="deviceNo">Device number to retrieve information on.</param>
    private void CollectDeviceInfo(int deviceNo)
    {
      // Device info is saved in a dictionary so it can be reused lateron.
      if (!_deviceInfos.ContainsKey(deviceNo))
      {
        Log.Debug("Collecting device info");

        BASS_DEVICEINFO bassDeviceInfo = Bass.BASS_GetDeviceInfo(deviceNo);
        if (bassDeviceInfo == null)
          throw new BassLibraryException("BASS_GetDeviceInfo");

        BASS_INFO bassInfo = Bass.BASS_GetInfo();
        if (bassInfo == null)
          throw new BassLibraryException("BASS_GetInfo");

        DeviceInfo deviceInfo = new DeviceInfo
          {
              Name = bassDeviceInfo.name,
              Driver = bassDeviceInfo.driver,
              Channels = bassInfo.speakers,
              MinRate = bassInfo.minrate,
              MaxRate = bassInfo.maxrate,
              Latency = TimeSpan.FromMilliseconds(bassInfo.latency)
          };

        lock (_deviceInfos)
          _deviceInfos.Add(deviceNo, deviceInfo);
      }
      Log.Debug("DirectSound device info: {0}", _deviceInfos[_deviceNo].ToString());
    }

    /// <summary>
    /// Gets the device number for the selected DirectSound device.
    /// </summary>
    /// <returns>Number of the device to be used for the BASS player.</returns>
    private static int GetDeviceNo()
    {
      string deviceName = Controller.GetSettings().DirectSoundDevice;
      int deviceNo = BassConstants.BassDefaultDevice;

      if (String.IsNullOrEmpty(deviceName) || deviceName == BassPlayerSettings.Defaults.DirectSoundDevice)
        Log.Info("Initializing default DirectSound device");
      else
      {
        bool found = false;
        BASS_DEVICEINFO[] deviceDescriptions = Bass.BASS_GetDeviceInfos();
        for (int i = 0; i < deviceDescriptions.Length; i++)
        {
          if (deviceDescriptions[i].name == deviceName)
          {
            deviceNo = i;
            found = true;
            break;
          }
        }
        if (found)
          Log.Info("Initializing DirectSound device '{0}' (device no {1})", deviceName, deviceNo);
        else
          Log.Warn("Specified DirectSound device '{0}' does not exist. Initializing default DirectSound device", deviceName);
      }
      return deviceNo;
    }

    /// <summary>
    /// Schedules a call to <see cref="PlaybackProcessor.HandleOutputStreamEnded"/> when our output device has finished
    /// playing.
    /// </summary>
    internal void HandleOutputStreamAboutToEnd()
    {
      _controller.EnqueueWorkItem(new WorkItem(new Controller.WorkItemDelegate(WaitAndHandleOutputEnd_Sync)));
    }

    /// <summary>
    /// Blocks the current thread until our output device has finished and then calls
    /// <see cref="PlaybackProcessor.HandleOutputStreamEnded"/>.
    /// </summary>
    internal void WaitAndHandleOutputEnd_Sync()
    {
      DateTime timeout = DateTime.Now + CurrentDeviceInfo.Latency + TimeSpan.FromSeconds(1);
      BassStream stream = _outputStream;
      if (stream != null)
        while (Bass.BASS_ChannelIsActive(stream.Handle) != BASSActive.BASS_ACTIVE_STOPPED && DateTime.Now < timeout)
          Thread.Sleep(10);
      if (_deviceState == DeviceState.Started)
        _controller.PlaybackProcessor.HandleOutputStreamEnded();
    }

    /// <summary>
    /// Callback function for the outputstream.
    /// </summary>
    /// <param name="streamHandle">Bass stream handle that requests sample data.</param>
    /// <param name="buffer">Buffer to write the sampledata in.</param>
    /// <param name="requestedBytes">Requested number of bytes.</param>
    /// <param name="userData"></param>
    /// <returns>Number of bytes read.</returns>
    private int OutputStreamWriteProc(int streamHandle, IntPtr buffer, int requestedBytes, IntPtr userData)
    {
      if (_deviceState == DeviceState.Stopped)
        return (int) BASSStreamProc.BASS_STREAMPROC_END;
      if (_outputStreamEnded)
        return (int) BASSStreamProc.BASS_STREAMPROC_END;
      int read = _inputStream.Read(buffer, requestedBytes);
      if (read == -1)
      {
        // We're done!

        // Play silence until playback has stopped to avoid any buffer underruns.
        read = _silence.Write(buffer, requestedBytes);

        // Set a flag so we call HandleOutputStreamEnded() only once.
        _outputStreamEnded = true;

        // Our input stream is finished, wait for device to end playback
        HandleOutputStreamAboutToEnd();
      }
      return read;
    }

    /// <summary>
    /// Resets all stored state.
    /// </summary>
    private void ResetState()
    {
      _outputStreamEnded = false;
    }

    #endregion
  }
}
