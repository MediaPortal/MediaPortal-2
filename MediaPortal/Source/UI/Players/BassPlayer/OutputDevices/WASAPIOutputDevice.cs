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
using System.Linq;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Players.BassPlayer.Utils;
using MediaPortal.UI.Presentation.Players;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.BassWasapi;

namespace MediaPortal.UI.Players.BassPlayer.OutputDevices
{
  /// <summary>
  /// Represents the user-selected WASAPI outputdevice.
  /// </summary>
  internal class WASAPIOutputDevice : AbstractOutputDevice, IAudioPlayerAnalyze
  {
    #region Fields

    protected const BASSFlag MIXER_FLAGS = BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_STREAM_DECODE;

    protected readonly WASAPIPROC _streamWriteProcDelegate;
    protected readonly SYNCPROC _onPlaybackEndDelegate;
    protected BASSWASAPIInit _flags;
    protected BassStream _mixer;
    protected int _mixerHandle;
    protected readonly int _maxFFT = (int)(BASSData.BASS_DATA_AVAILABLE | BASSData.BASS_DATA_FFT4096);

    #endregion

    public WASAPIOutputDevice(Controller controller)
      : base(controller)
    {
      _streamWriteProcDelegate = OutputStreamWriteProc;
      _onPlaybackEndDelegate = OnPlaybackEnd;
      _deviceNo = GetDeviceNo();
    }

    #region IDisposable Members

    public override void Dispose()
    {
      base.Dispose();

      Log.Debug("Disposing mixer stream");

      if (_mixer != null)
      {
        _mixer.Dispose();
        _mixer = null;
      }

      Log.Debug("Resetting global Bass environment");

      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0))
        throw new BassLibraryException("BASS_SetConfig");

      if (!BassWasapi.BASS_WASAPI_Free())
        throw new BassLibraryException("BASS_WASAPI_Free");
    }

    #endregion

    #region IOutputDevice Members

    /// <summary>
    /// If the device is used in shared mode, we use a mixer. In this case it will be the stream to read from.
    /// </summary>
    protected override BassStream ReadStream
    {
      get { return _mixer ?? _inputStream; }
    }

    public override void SetInputStream(BassStream stream, bool passThrough)
    {
      if (_deviceState != DeviceState.Stopped)
        throw new BassPlayerException("Device state is not 'DeviceState.Stopped'");

      _inputStream = stream;
      _flags = BASSWASAPIInit.BASS_WASAPI_AUTOFORMAT | BASSWASAPIInit.BASS_WASAPI_BUFFER;

      // If Exclusive mode is used, check, if that would be supported, otherwise init in shared mode
      bool isExclusive = Controller.GetSettings().WASAPIExclusiveMode;

      if (isExclusive)
      {
        _flags |= BASSWASAPIInit.BASS_WASAPI_EXCLUSIVE;

        BASSWASAPIFormat wasapiFormat = BassWasapi.BASS_WASAPI_CheckFormat(_deviceNo,
                                                                           _inputStream.SampleRate,
                                                                           _inputStream.Channels,
                                                                           BASSWASAPIInit.BASS_WASAPI_EXCLUSIVE);
        if (wasapiFormat == BASSWASAPIFormat.BASS_WASAPI_FORMAT_UNKNOWN)
        {
          Log.Info("BASS: WASAPI exclusive mode not directly supported for samplerate of {0} and {1} channels", _inputStream.SampleRate, _inputStream.Channels);
          isExclusive = false;
        }
      }

    retry:
      if (!isExclusive)
      {
        Log.Debug("BASS: Init WASAPI shared mode with Event driven system enabled.");
        _flags &= ~BASSWASAPIInit.BASS_WASAPI_EXCLUSIVE;
        _flags |= BASSWASAPIInit.BASS_WASAPI_SHARED | BASSWASAPIInit.BASS_WASAPI_EVENT;
      }

      Log.Debug("BASS: Try to init WASAPI with a samplerate of {0} and {1} channels", _inputStream.SampleRate, _inputStream.Channels);

      bool result = BassWasapi.BASS_WASAPI_Init(_deviceNo, _inputStream.SampleRate, _inputStream.Channels, _flags, 0.5f, 0f, _streamWriteProcDelegate, IntPtr.Zero);

      BASSError? bassInitErrorCode = result ? null : new BASSError?(Bass.BASS_ErrorGetCode());

      if (bassInitErrorCode.HasValue)
      {
        if (bassInitErrorCode.Value == BASSError.BASS_ERROR_ALREADY)
        {
          if (!BassWasapi.BASS_WASAPI_SetDevice(_deviceNo))
            throw new BassLibraryException("BASS_WASAPI_SetDevice");
        }
        else if (isExclusive)
        {
          // Allow one retry in shared mode
          Log.Warn("BASS: Failed to initialize WASAPI exclusive mode for samplerate of {0} and {1} channels. Trying fallback to shared mode.", _inputStream.SampleRate, _inputStream.Channels);
          isExclusive = false;
          goto retry;
        }
        else
          throw new BassLibraryException("BASS_WASAPI_Init");
      }

      // If the GetDeviceNo() method returned BassConstants.BassDefaultDevice, we must request the actual device number
      // of the choosen default device
      _deviceNo = BassWasapi.BASS_WASAPI_GetDevice();

      CollectDeviceInfo(_deviceNo);
      BASS_WASAPI_INFO wasapiInfo = BassWasapi.BASS_WASAPI_GetInfo();

      Log.Debug("BASS: ---------------------------------------------");
      Log.Debug("BASS: Buffer Length: {0}", wasapiInfo.buflen);
      Log.Debug("BASS: Channels: {0}", wasapiInfo.chans);
      Log.Debug("BASS: Frequency: {0}", wasapiInfo.freq);
      Log.Debug("BASS: Format: {0}", wasapiInfo.format.ToString());
      Log.Debug("BASS: InitFlags: {0}", wasapiInfo.initflags.ToString());
      Log.Debug("BASS: Exclusive: {0}", wasapiInfo.IsExclusive.ToString());
      Log.Debug("BASS: ---------------------------------------------");
      Log.Info("BASS: WASAPI Device successfully initialised");

      // For shared mode we require a mixer to change the sampling rates of input stream to device output stream.
      if (!wasapiInfo.IsExclusive)
      {
        // Recreate Mixer with new value
        Log.Debug("BASS: Creating new {0} channel mixer for frequency {1}", wasapiInfo.chans, wasapiInfo.freq);
        _mixerHandle = BassMix.BASS_Mixer_StreamCreate(wasapiInfo.freq, wasapiInfo.chans, MIXER_FLAGS);
        if (_mixerHandle == BassConstants.BassInvalidHandle)
          throw new BassLibraryException("BASS_Mixer_StreamCreate");
        _mixer = BassStream.Create(_mixerHandle);
        AttachStream();
      }

      int ms = Convert.ToInt32(Controller.GetSettings().DirectSoundBufferSize.TotalMilliseconds);

      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, ms))
        throw new BassLibraryException("BASS_SetConfig");

      // Enable update thread while the output device is active
      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, ms / 4))
        throw new BassLibraryException("BASS_SetConfig");

      if (passThrough)
        _fader = new BassStreamFader(_inputStream, Controller.GetSettings().FadeDuration);

      ResetState();
    }

    /// <summary>
    /// Attach a stream to the Mixer
    /// </summary>
    /// <returns></returns>
    public bool AttachStream()
    {
      try
      {
        Bass.BASS_ChannelLock(_mixerHandle, true);

        RegisterStreamFreedEvent(_inputStream);

        bool result = BassMix.BASS_Mixer_StreamAddChannel(_mixerHandle, _inputStream.Handle,
                                          BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_BUFFER |
                                          BASSFlag.BASS_MIXER_MATRIX | BASSFlag.BASS_MIXER_DOWNMIX |
                                          BASSFlag.BASS_STREAM_AUTOFREE
                                          );
        if (!result)
          throw new BassLibraryException("BASS_Mixer_StreamAddChannel");

        return true;
      }
      finally
      {
        Bass.BASS_ChannelLock(_mixerHandle, false);
      }
    }

    /// <summary>
    /// Register the Stream Freed Event
    /// </summary>
    /// <returns></returns>
    private void RegisterStreamFreedEvent(BassStream stream)
    {
      int syncHandle = Bass.BASS_ChannelSetSync(stream.Handle, BASSSync.BASS_SYNC_FREE | BASSSync.BASS_SYNC_MIXTIME, 0, _onPlaybackEndDelegate, IntPtr.Zero);

      if (syncHandle == 0)
        Log.Debug("BASS: RegisterStreamFreedEvent of stream {0} failed with error {1}", stream.Handle, Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()));
      else
        stream.SyncProcHandles.Add(syncHandle);
    }

    private void OnPlaybackEnd(int handle, int channel, int data, IntPtr user)
    {
      // Our input stream is finished, wait for device to end playback
      HandleOutputStreamAboutToEnd();
    }

    /// <summary>
    /// Detect the supported output formats for WASAPI
    /// </summary>
    private void GetDeviceFormats(int deviceNo, DeviceInfo deviceInfo)
    {
      int[] channels = { 1, 2, 3, 4, 5, 6, 7, 8 };
      int[] sampleRates = { 22050, 32000, 44100, 48000, 88200, 96000, 192000 };

      deviceInfo.MaxRate = sampleRates.First();
      deviceInfo.MinRate = sampleRates.Last();
      foreach (BASSWASAPIInit init in new[] { BASSWASAPIInit.BASS_WASAPI_EXCLUSIVE, BASSWASAPIInit.BASS_WASAPI_SHARED })
      {
        Log.Debug("Check {0} mode", init);
        foreach (var sampleRate in sampleRates)
        {
          foreach (var channel in channels)
          {
            BASSWASAPIFormat format = BassWasapi.BASS_WASAPI_CheckFormat(deviceNo, sampleRate, channel, init);
            if (format != BASSWASAPIFormat.BASS_WASAPI_FORMAT_UNKNOWN)
            {
              Log.Debug("- {0,-6} Hz / {1} ch: {2}", sampleRate, channel, format);
              if (channel > deviceInfo.Channels)
                deviceInfo.Channels = channel;
              if (sampleRate > deviceInfo.MaxRate)
                deviceInfo.MaxRate = sampleRate;
              if (sampleRate < deviceInfo.MinRate)
                deviceInfo.MinRate = sampleRate;
            }
          }
        }
      }
    }

    public override void Start()
    {
      if (_deviceState == DeviceState.Started)
        return;

      Log.Debug("Starting output");
      _deviceState = DeviceState.Started;

      if (!BassWasapi.BASS_WASAPI_Start())
        throw new BassLibraryException("BASS_WASAPI_Start");
    }

    public override void Stop()
    {
      if (_deviceState == DeviceState.Stopped)
        return;

      Log.Debug("Stopping output");
      _deviceState = DeviceState.Stopped;

      if (!BassWasapi.BASS_WASAPI_Stop(true))
        throw new BassLibraryException("BASS_WASAPI_Stop");
    }

    #endregion

    #region Private members

    /// <summary>
    /// Retrieves information on a device and adds it to the static deviceinfo dictionary do it can be reused later. 
    /// </summary>
    /// <param name="deviceNo">Device number to retrieve information on.</param>
    protected void CollectDeviceInfo(int deviceNo)
    {
      // Device info is saved in a dictionary so it can be reused lateron.
      if (!_deviceInfos.ContainsKey(deviceNo))
      {
        Log.Debug("Collecting device info");

        BASS_WASAPI_DEVICEINFO bassDeviceInfo = BassWasapi.BASS_WASAPI_GetDeviceInfo(deviceNo);
        if (bassDeviceInfo == null)
          throw new BassLibraryException("BASS_WASAPI_GetDeviceInfo");

        BASS_WASAPI_INFO bassInfo = BassWasapi.BASS_WASAPI_GetInfo();
        if (bassInfo == null)
          throw new BassLibraryException("BASS_WASAPI_GetInfo");

        DeviceInfo deviceInfo = new DeviceInfo
          {
            Name = bassDeviceInfo.name,
            Driver = "WASAPI",
            Channels = bassInfo.chans,
            MinRate = bassInfo.freq,
            MaxRate = bassInfo.freq,
          };

        GetDeviceFormats(deviceNo, deviceInfo);
        lock (_deviceInfos)
          _deviceInfos.Add(deviceNo, deviceInfo);
      }
      Log.Debug("WASAPI device info: {0}", _deviceInfos[_deviceNo].ToString());
    }

    /// <summary>
    /// Checks if the given <paramref name="deviceInfo"/> represents a valid, currently available output device.
    /// </summary>
    /// <param name="deviceInfo">Device info</param>
    /// <returns><c>true</c> if valid.</returns>
    public static bool IsValidDevice(BASS_WASAPI_DEVICEINFO deviceInfo)
    {
      return !(deviceInfo.IsDisabled || deviceInfo.IsUnplugged || deviceInfo.IsInput || deviceInfo.IsLoopback || deviceInfo.flags == BASSWASAPIDeviceInfo.BASS_DEVICE_UNKNOWN);
    }

    /// <summary>
    /// Gets the device number for the selected DirectSound device.
    /// </summary>
    /// <returns>Number of the device to be used for the BASS player.</returns>
    protected static int GetDeviceNo()
    {
      string deviceName = Controller.GetSettings().WASAPIDevice;
      int deviceNo = BassConstants.BassDefaultDevice;

      if (String.IsNullOrEmpty(deviceName) || deviceName == BassPlayerSettings.Defaults.WASAPIDevice)
        Log.Info("Initializing default WASAPI device");
      else
      {
        bool found = false;
        BASS_WASAPI_DEVICEINFO[] deviceDescriptions = BassWasapi.BASS_WASAPI_GetDeviceInfos();
        for (int i = 0; i < deviceDescriptions.Length; i++)
        {
          var deviceInfo = deviceDescriptions[i];

          // Skip input devices, they have same name as output devices.
          if (!IsValidDevice(deviceInfo))
            continue;

          if (deviceInfo.name == deviceName)
          {
            deviceNo = i;
            found = true;
            break;
          }
        }
        if (found)
          Log.Info("Initializing WASAPI device '{0}' (device no {1})", deviceName, deviceNo);
        else
          Log.Warn("Specified WASAPI device '{0}' does not exist. Initializing default WASAPI device", deviceName);
      }
      return deviceNo;
    }

    /// <summary>
    /// Callback function for the outputstream.
    /// </summary>
    /// <param name="buffer">Bass stream handle that requests sample data.</param>
    /// <param name="requestedBytes">Requested number of bytes.</param>
    /// <param name="userData"></param>
    /// <returns>Number of bytes read.</returns>
    protected int OutputStreamWriteProc(IntPtr buffer, int requestedBytes, IntPtr userData)
    {
      return WriteOutputStream(buffer, requestedBytes);
    }

    #endregion

    #region IAudioPlayerAnalyze Member

    public bool GetWaveData32(int length, out float[] waveData32)
    {
      waveData32 = null;
      if (!BassWasapi.BASS_WASAPI_IsStarted())
        return false;
      waveData32 = new float[length];
      return BassWasapi.BASS_WASAPI_GetData(waveData32, length) == (int)BASSError.BASS_OK;
    }

    public bool GetFFTData(float[] fftDataBuffer)
    {
      if (!BassWasapi.BASS_WASAPI_IsStarted())
        return false;
      return BassWasapi.BASS_WASAPI_GetData(fftDataBuffer, _maxFFT) > 0;
    }

    public bool GetFFTFrequencyIndex(int frequency, out int frequencyIndex)
    {
      frequencyIndex = 0;
      if (_inputStream == null)
        return false;
      frequencyIndex = Un4seen.Bass.Utils.FFTFrequency2Index(frequency, 4096, _inputStream.SampleRate);
      return true;
    }

    public bool GetChannelLevel(out double dbLevelL, out double dbLevelR)
    {
      dbLevelL = 0f;
      dbLevelR = 0f;
      if (!BassWasapi.BASS_WASAPI_IsStarted())
        return false;
      int level = BassWasapi.BASS_WASAPI_GetLevel();
      dbLevelL = Un4seen.Bass.Utils.LevelToDB(Un4seen.Bass.Utils.LowWord32(level), 65535); // the left level
      dbLevelR = Un4seen.Bass.Utils.LevelToDB(Un4seen.Bass.Utils.HighWord32(level), 65535); // the right level
      return true;
    }

    #endregion
  }
}
