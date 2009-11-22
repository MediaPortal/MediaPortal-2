#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Media.Players.BassPlayer.ASIOInterop;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    partial class OutputDeviceManager
    {
      partial class OutputDeviceFactory
      {
        /// <summary>
        /// Represents the user-selected ASIO outputdevice.
        /// </summary>
        class ASIOOutputDevice : IOutputDevice
        {
          #region Static members

          /// <summary>
          /// Creates and initializes an new instance.
          /// </summary>
          /// <param name="player">Reference to containing IPlayer object.</param>
          /// <returns>The new instance.</returns>
          public static ASIOOutputDevice Create(BassPlayer player)
          {
            ASIOOutputDevice outputDevice = new ASIOOutputDevice(player);
            outputDevice.Initialize();
            return outputDevice;
          }

          Dictionary<int, DeviceInfo> _DeviceInfos = new Dictionary<int, DeviceInfo>();

          #endregion

          #region Fields

          private AsioDriver _Driver;
          private AutoResetEvent _STAThreadNotify;
          private BassPlayer _Player;
          private BassStream _InputStream;
          private BassStream _MixerStream;
          private BassStreamFader _Fader;
          private DeviceState _DeviceState;
          private Thread _STAThread;
          private WorkItemQueue _STAWorkItemQueue;

          private bool _OutputStreamEnded;
          private bool _STAThreadAbortFlag = false;
          private float[] _Buffer = new float[0];
          private int _DeviceNo;
          private int _FirstOutputChannel;
          private int _LastOutputChannel;

          private int[] SampleRates = new int[] {
      			8000,
			      9600,
			      11025,
			      12000,
			      16000,
			      22050,
			      24000,
			      32000,
			      44100,
			      48000,
			      88200,
			      96000,
			      176400,
			      192000
		      };

          #endregion
          
          #region Delegates
          
          private delegate AsioDriver SelectDriverDelegate(InstalledDriver driver, IntPtr sysHandle);
          private delegate bool CreateBuffersDelegate(bool useMaxBufferSize);
          private delegate bool CanSampleRateDelegate(int sampleRate);
          private delegate bool SetSampleRateDelegate(int sampleRate);
          private delegate int GetSampleRateDelegate();
          private delegate bool StartStopDelegate();
          private delegate bool DisposeBuffersDelegate();
          private delegate void ReleaseDelegate();

          #endregion
          
          #region IOutputDevice Members

          public BassStream InputStream
          {
            get { return _InputStream; }
          }

          public DeviceState DeviceState
          {
            get { return _DeviceState; }
          }

          public string Name
          {
            get { return _DeviceInfos[_DeviceNo]._Name; }
          }

          public string Driver
          {
            get { return _DeviceInfos[_DeviceNo]._Driver; }
          }

          public int Channels
          {
            get { return _DeviceInfos[_DeviceNo]._Channels; }
          }

          public int MinRate
          {
            get { return _DeviceInfos[_DeviceNo]._MinRate; }
          }

          public int MaxRate
          {
            get { return _DeviceInfos[_DeviceNo]._MaxRate; }
          }

          public TimeSpan Latency
          {
            get { return _DeviceInfos[_DeviceNo]._Latency; }
          }

          public void SetInputStream(BassStream stream)
          {
            if (_DeviceState != DeviceState.Stopped)
              throw new BassPlayerException("Device state is not 'DeviceState.Stopped'.");

            _InputStream = stream;

            if (_Player.Settings.ASIOFirstChan == BassPlayerSettings.Constants.Auto)
              _FirstOutputChannel = 0;
            else
              _FirstOutputChannel = _Player.Settings.ASIOFirstChan;

            _FirstOutputChannel = Math.Max(_FirstOutputChannel, 0);
            _FirstOutputChannel = Math.Min(_FirstOutputChannel, _Driver.OutputChannels.Length - 1);

            if (_Player.Settings.ASIOLastChan == BassPlayerSettings.Constants.Auto)
              _LastOutputChannel = _Driver.OutputChannels.Length;
            else
              _LastOutputChannel = _Player.Settings.ASIOLastChan;

            _LastOutputChannel = Math.Max(_LastOutputChannel, _FirstOutputChannel);
            _LastOutputChannel = Math.Min(_LastOutputChannel, _FirstOutputChannel + _InputStream.Channels - 1);
            _LastOutputChannel = Math.Min(_LastOutputChannel, _Driver.OutputChannels.Length - 1);

            Log.Info("Using ASIO channels {0} - {1}", _FirstOutputChannel, _LastOutputChannel);

            int inputRate = _InputStream.SampleRate;
            int outputRate = inputRate;

            outputRate = Math.Max(outputRate, MinRate);
            outputRate = Math.Min(outputRate, MaxRate);

            if (outputRate != inputRate)
              Log.Info("Resampling {0} -> {1}...", inputRate, outputRate);

            Log.Debug("Creating mixer");

            BASSFlag streamFlags =
                BASSFlag.BASS_SAMPLE_FLOAT |
                BASSFlag.BASS_STREAM_DECODE;

            int handle = BassMix.BASS_Mixer_StreamCreate(outputRate, _InputStream.Channels, streamFlags);
            if (handle == BassConstants.BassInvalidHandle)
              throw new BassLibraryException("BASS_StreamCreate");

            _MixerStream = BassStream.Create(handle);

            if (!BassMix.BASS_Mixer_StreamAddChannel(_MixerStream.Handle, _InputStream.Handle, BASSFlag.BASS_MIXER_NORAMPIN))
              throw new BassLibraryException("BASS_Mixer_StreamAddChannel");

            WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new SetSampleRateDelegate(_Driver.SetSampleRate), outputRate));
            workItem.WaitHandle.WaitOne();

            if (!workItem.ResultAsBool)
              throw new BassLibraryException("Error setting samplerate");

            DeviceInfo deviceInfo = _DeviceInfos[_DeviceNo];
            deviceInfo._Latency = GetASIOLatency();

            if (!_Player._InputSourceSwitcher.CurrentInputSource.OutputStream.IsPassThrough)
              _Fader = new BassStreamFader(_InputStream, _Player.Settings.FadeDuration);
          }

          public void PrepareFadeIn()
          {
            if (_Fader != null)
              _Fader.PrepareFadeIn();
          }

          public void FadeIn()
          {
            if (_Fader != null)
              _Fader.FadeIn();
          }

          public void FadeOut()
          {
            if (_Fader != null && !_OutputStreamEnded)
            {
              Log.Debug("Fading out");
              _Fader.FadeOut();
            }
          }

          public void Start()
          {
            if (_DeviceState != DeviceState.Started)
            {
              Log.Debug("Starting output");

              int bufferSize = _Driver.BufferSize * (_LastOutputChannel - _FirstOutputChannel + 1);
              if (_Buffer.Length != bufferSize)
                Array.Resize<float>(ref _Buffer, bufferSize);

              WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new StartStopDelegate(_Driver.Start)));
              workItem.WaitHandle.WaitOne();

              if (!workItem.ResultAsBool)
                throw new BassPlayerException("Error starting device");

              _DeviceState = DeviceState.Started;
            }
          }

          public void Stop()
          {
            if (_DeviceState != DeviceState.Stopped)
            {
              Log.Debug("Stopping output");

              WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new StartStopDelegate(_Driver.Stop)));
              workItem.WaitHandle.WaitOne();

              if (!workItem.ResultAsBool)
                throw new BassPlayerException("Error stopping device");

              _DeviceState = DeviceState.Stopped;
            }
          }

          public void ClearBuffers()
          {
            int bufferSize = _Driver.BufferSize;
            foreach (Channel channel in _Driver.OutputChannels)
            {
              for (int i = 0; i < bufferSize; i++)
                channel[i] = 0.0f;
            }
          }

          #endregion

          #region IDisposable Members

          public void Dispose()
          {
            if (_Driver != null)
            {
              Log.Debug("Disposing driver");

              Stop();

              WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new DisposeBuffersDelegate(_Driver.DisposeBuffers)));
              workItem.WaitHandle.WaitOne();

              workItem = EnQueueSTAWorkItem(new WorkItem(new ReleaseDelegate(_Driver.Release)));
              workItem.WaitHandle.WaitOne();
              _Driver = null;
            }

            if (_STAThread.IsAlive)
            {
              Log.Debug("Stopping ASIO STA thread.");

              _STAThreadAbortFlag = true;
              _STAThreadNotify.Set();
              _STAThread.Join();
            }

            if (_MixerStream != null)
            {
              Log.Debug("Disposing mixer");
              _MixerStream.Dispose();
              _MixerStream = null;
            }
          }

          #endregion

          #region Public members

          #endregion

          #region Private members

          private ASIOOutputDevice(BassPlayer player)
          {
            _Player = player;
          }

          /// <summary>
          /// Initializes a new instance.
          /// </summary>
          private void Initialize()
          {
            // ASIO requires to be initialized on a STA thread.
            // To assure this we create a dedicated STA thread that performs all driver calls.
            _STAWorkItemQueue = new WorkItemQueue();
            _STAThreadNotify = new AutoResetEvent(false);
            _STAThread = new Thread(new ThreadStart(STAThread));
            _STAThread.SetApartmentState(ApartmentState.STA);
            _STAThread.IsBackground = true;
            _STAThread.Start();

            _DeviceState = DeviceState.Stopped;

            _DeviceNo = GetDeviceNo();

            InstalledDriver driver = AsioDriver.InstalledDrivers[_DeviceNo];

            WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new SelectDriverDelegate(AsioDriver.SelectDriver), driver, IntPtr.Zero));
            workItem.WaitHandle.WaitOne();

            if (workItem.Result == null)
              throw new BassPlayerException("Error selecting driver");

            _Driver = (AsioDriver)workItem.Result;

            workItem = EnQueueSTAWorkItem(new WorkItem(new CreateBuffersDelegate(_Driver.CreateBuffers), _Player.Settings.ASIOUseMaxBufferSize));
            workItem.WaitHandle.WaitOne();

            if (!workItem.ResultAsBool)
              throw new BassPlayerException("Error creating buffers");

            _Driver.BufferUpdate += new EventHandler(_Driver_BufferUpdate);

            CollectDeviceInfo(_DeviceNo);
          }

          /// <summary>
          /// ASIO STA thread loop.
          /// </summary>
          private void STAThread()
          {
            try
            {
              while (!_STAThreadAbortFlag)
              {
                if (_STAWorkItemQueue.Count == 0)
                  _STAThreadNotify.WaitOne();

                if (_STAWorkItemQueue.Count > 0)
                {
                  WorkItem workItem = _STAWorkItemQueue.Dequeue();
                  workItem.Invoke();
                }
              }
            }
            catch (Exception e)
            {
              throw new BassPlayerException("Exception in ASIO STA thread.", e);
            }
          }

          /// <summary>
          /// Retrieves information on a device and adds it to the 
          /// static deviceinfo dictionary do it can be reused later. 
          /// </summary>
          /// <param name="deviceNo">Device number to retrieve information on.</param>
          private void CollectDeviceInfo(int deviceNo)
          {
            // Device info is saved in a dictionary so it can be reused lateron.
            if (!_DeviceInfos.ContainsKey(deviceNo))
            {
              Log.Debug("Collecting device info");

              DeviceInfo deviceInfo = new DeviceInfo();
              deviceInfo._Name = _Driver.DriverName;
              deviceInfo._Driver = _Driver.Version.ToString();
              deviceInfo._Channels = _Driver.OutputChannels.Length;
              deviceInfo._MaxRate = GetMaxASIORate();
              deviceInfo._MinRate = GetMinASIORate();
              deviceInfo._Latency = GetASIOLatency();

              lock (_DeviceInfos)
              {
                _DeviceInfos.Add(deviceNo, deviceInfo);
              }
            }
            Log.Debug(String.Format("ASIO Device info: {0}", _DeviceInfos[_DeviceNo].ToString()));
          }

          /// <summary>
          /// Determines the minimum supported samplerate for the device.
          /// </summary>
          /// <returns></returns>
          private int GetMinASIORate()
          {
            int minimumRate;
            if (_Player.Settings.ASIOMinRate == BassPlayerSettings.Constants.Auto)
            {
              Log.Debug("Auto-detecting minimum supported ASIO samplerate");

              minimumRate = SampleRates[0];
              for (int index = 0; index < SampleRates.Length; index++)
              {
                int rate = SampleRates[index];

                WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new CanSampleRateDelegate(_Driver.CanSampleRate), rate));
                workItem.WaitHandle.WaitOne();
                if (workItem.ResultAsBool)
                {
                  minimumRate = rate;
                  break;
                }
              }
            }
            else
            {
              minimumRate = _Player.Settings.ASIOMinRate;
            }
            return minimumRate;
          }

          /// <summary>
          /// Determines the maximum supported samplerate for the device.
          /// </summary>
          /// <returns></returns>
          private int GetMaxASIORate()
          {
            int maximumRate;
            if (_Player.Settings.ASIOMaxRate == BassPlayerSettings.Constants.Auto)
            {
              Log.Debug("Auto-detecting maximum supported ASIO samplerate");

              maximumRate = SampleRates[SampleRates.Length - 1];
              for (int index = SampleRates.Length - 1; index >= 0; index--)
              {
                int rate = SampleRates[index];

                WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new CanSampleRateDelegate(_Driver.CanSampleRate), rate));
                workItem.WaitHandle.WaitOne();
                if (workItem.ResultAsBool)
                {
                  maximumRate = rate;
                  break;
                }
              }
            }
            else
            {
              maximumRate = _Player.Settings.ASIOMaxRate;
            }
            return maximumRate;
          }

          /// <summary>
          /// Determines the latency for the device.
          /// </summary>
          /// <returns></returns>
          private TimeSpan GetASIOLatency()
          {
            int sampleLatency = _Driver.Latency.OutputLatency;

            WorkItem workItem = EnQueueSTAWorkItem(new WorkItem(new GetSampleRateDelegate(_Driver.GetSampleRate)));
            workItem.WaitHandle.WaitOne();
            int freq = workItem.ResultAsInt;

            int latency = ((sampleLatency * 1000) / freq);

            if (latency == 0)
              latency = 50;

            return TimeSpan.FromMilliseconds(latency);
          }

          /// <summary>
          /// Gets the device number for the selected ASIO device.
          /// </summary>
          /// <returns></returns>
          private int GetDeviceNo()
          {
            if (AsioDriver.InstalledDrivers.Length == 0)
              throw new BassPlayerException("No ASIO devices installed");

            string deviceName = _Player.Settings.ASIODevice;
            int deviceNo;

            if (String.IsNullOrEmpty(deviceName))
            {
              Log.Info("Using first available ASIO Device");
              deviceNo = 0;
            }
            else
            {
              deviceNo = BassConstants.BassDefaultDevice;

              // Check if the ASIO device read is amongst the one retrieved
              for (int i = 0; i < AsioDriver.InstalledDrivers.Length; i++)
              {
                if (AsioDriver.InstalledDrivers[i].Name == deviceName)
                {
                  deviceNo = i;
                  break;
                }
              }
              if (deviceNo == BassConstants.BassDefaultDevice)
              {
                Log.Warn("Specified ASIO device does not exist. Initializing first available ASIO Device");
                deviceNo = 0;
              }
              else
              {
                Log.Info("Using ASIO device \"{0}\"", deviceName);
              }
            }
            return deviceNo;
          }

          /// <summary>
          /// Enqueues a workitem and notifies the STA thread something needs to be done.
          /// </summary>
          /// <param name="workItem">The workitem to enqueue.</param>
          /// <returns>The enqueued workitem.</returns>
          private WorkItem EnQueueSTAWorkItem(WorkItem workItem)
          {
            _STAWorkItemQueue.Enqueue(workItem);
            _STAThreadNotify.Set();

            return workItem;
          }

          /// <summary>
          /// ASIO driver buffer update callback
          /// </summary>
          /// <param name="sender"></param>
          /// <param name="e"></param>
          private void _Driver_BufferUpdate(object sender, EventArgs e)
          {
            int samplesRead;
            if (_MixerStream != null)
              samplesRead = _MixerStream.Read(_Buffer);
            else
              samplesRead = 0;

            if (samplesRead == 0)
            {
              // Set a flag so we call HandleOutputStreamEnded() only once.
              if (!_OutputStreamEnded)
              {
                _OutputStreamEnded = true;

                // Let the world know that we can stop now.
                _Player._Controller.HandleOutputStreamEnded();
              }
            }

            int channelIndex = _FirstOutputChannel;
            int channelSample = 0;
            int inputChannels = _InputStream.Channels;

            for (int index = 0; index < _Buffer.Length; index++)
            {
              if (channelIndex <= _LastOutputChannel)
                if (index < samplesRead)
                  _Driver.OutputChannels[channelIndex][channelSample] = _Buffer[index];
                else
                  _Driver.OutputChannels[channelIndex][channelSample] = 0.0f;

              channelIndex++;
              if (channelIndex - _FirstOutputChannel == inputChannels)
              {
                channelIndex = _FirstOutputChannel;
                channelSample++;
              }
            }
          }

          #endregion

        }
      }
    }
  }
}