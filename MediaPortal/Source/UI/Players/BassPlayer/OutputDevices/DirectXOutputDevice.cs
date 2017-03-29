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
using System.Threading;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.PlayerComponents;
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer.OutputDevices
{
  /// <summary>
  /// Represents the user-selected DirectX outputdevice.
  /// </summary>
  internal class DirectXOutputDevice : AbstractOutputDevice
  {
    #region Fields

    private readonly STREAMPROC _streamWriteProcDelegate;
    private BassStream _outputStream = null;

    #endregion

    public DirectXOutputDevice(Controller controller)
      : base(controller)
    {
      _streamWriteProcDelegate = OutputStreamWriteProc;
      _deviceNo = GetDeviceNo();

      // TODO: move to SetStream?
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
      
      int ms = Convert.ToInt32(Controller.GetSettings().DirectSoundBufferSizeMilliSecs);

      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, ms))
        throw new BassLibraryException("BASS_SetConfig");

      // Enable update thread while the output device is active
      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, ms / 4))
        throw new BassLibraryException("BASS_SetConfig");
    }

    #region IDisposable Members

    public override void Dispose()
    {
      base.Dispose();

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

    public override void SetInputStream(BassStream stream, bool passThrough)
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
        _fader = new BassStreamFader(_inputStream, TimeSpan.FromMilliseconds(Controller.GetSettings().FadeDurationMilliSecs));

      ResetState();
    }

    public override void Start()
    {
      if (_deviceState == DeviceState.Started)
        return;

      Log.Debug("Starting output");
      _deviceState = DeviceState.Started;

      if (!Bass.BASS_ChannelPlay(_outputStream.Handle, false))
        throw new BassLibraryException("BASS_ChannelPlay");
    }

    public override void Stop()
    {
      if (_deviceState == DeviceState.Stopped)
        return;

      Log.Debug("Stopping output");
      _deviceState = DeviceState.Stopped;

      if (!Bass.BASS_ChannelStop(_outputStream.Handle))
        throw new BassLibraryException("BASS_ChannelStop");
    }

    public override void ClearBuffers()
    {
      Bass.BASS_ChannelSetPosition(_outputStream.Handle, 0L);
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

      if (String.IsNullOrEmpty(deviceName) || deviceName == Controller.GetSettings().DirectSoundDevice)
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
    /// Blocks the current thread until our output device has finished and then calls
    /// <see cref="PlaybackProcessor.HandleOutputStreamEnded"/>.
    /// </summary>
    protected internal override void WaitAndHandleOutputEnd_Sync()
    {
      DateTime timeout = DateTime.Now + CurrentDeviceInfo.Latency + TimeSpan.FromSeconds(1);
      BassStream stream = _outputStream;
      if (stream != null)
        while (Bass.BASS_ChannelIsActive(stream.Handle) != BASSActive.BASS_ACTIVE_STOPPED && DateTime.Now < timeout)
          Thread.Sleep(10);
      base.WaitAndHandleOutputEnd_Sync();
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
      return WriteOutputStream(buffer, requestedBytes);
    }

    #endregion
  }
}
