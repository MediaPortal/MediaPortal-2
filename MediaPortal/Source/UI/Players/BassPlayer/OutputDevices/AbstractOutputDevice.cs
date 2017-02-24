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
using System.Collections.Generic;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.PlayerComponents;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer.OutputDevices
{
  /// <summary>
  /// Represents the user-selected DirectX outputdevice.
  /// </summary>
  internal abstract class AbstractOutputDevice : IOutputDevice
  {
    #region Static members

    protected static readonly Dictionary<int, DeviceInfo> _deviceInfos = new Dictionary<int, DeviceInfo>();

    #endregion

    #region Fields

    protected readonly Controller _controller;
    protected int _deviceNo;
    protected readonly Silence _silence;
    protected volatile DeviceState _deviceState;

    protected BassStream _inputStream = null;
    protected BassStreamFader _fader = null;
    protected volatile bool _outputStreamEnded = false;

    #endregion

    protected AbstractOutputDevice(Controller controller)
    {
      _controller = controller;
      _deviceState = DeviceState.Stopped;
      _silence = new Silence();
    }

    #region IDisposable Members

    public virtual void Dispose()
    {
      Stop();
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
      get { return _deviceInfos[_deviceNo].Latency + TimeSpan.FromMilliseconds(Controller.GetSettings().DirectSoundBufferSizeMilliSecs); }
    }

    /// <summary>
    /// Gets the stream to read from, which is usually the <see cref="InputStream"/>. For outputs that involve a mixer this can be changed.
    /// </summary>
    protected virtual BassStream ReadStream
    {
      get { return _inputStream; }
    }

    public abstract void SetInputStream(BassStream stream, bool passThrough);

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

    public abstract void Start();

    public abstract void Stop();

    public virtual void ClearBuffers()
    {
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

    protected virtual int WriteOutputStream(IntPtr buffer, int requestedBytes)
    {
      if (_deviceState == DeviceState.Stopped || _outputStreamEnded)
        return (int)BASSStreamProc.BASS_STREAMPROC_END;

      int read = ReadStream.Read(buffer, requestedBytes);
      if (read != -1)
        return read;

      // We're done!
      // Play silence until playback has stopped to avoid any buffer underruns.
      read = _silence.Write(buffer, requestedBytes);

      // Set a flag so we call HandleOutputStreamEnded() only once.
      _outputStreamEnded = true;

      // Our input stream is finished, wait for device to end playback
      HandleOutputStreamAboutToEnd();
      return read;
    }

    /// <summary>
    /// Schedules a call to <see cref="PlaybackProcessor.HandleOutputStreamEnded"/> when our output device has finished
    /// playing.
    /// </summary>
    protected internal void HandleOutputStreamAboutToEnd()
    {
      _controller.EnqueueWorkItem(new WorkItem(new Controller.WorkItemDelegate(WaitAndHandleOutputEnd_Sync)));
    }

    /// <summary>
    /// Blocks the current thread until our output device has finished and then calls
    /// <see cref="PlaybackProcessor.HandleOutputStreamEnded"/>.
    /// </summary>
    protected internal virtual void WaitAndHandleOutputEnd_Sync()
    {
      if (_deviceState == DeviceState.Started)
        _controller.PlaybackProcessor.HandleOutputStreamEnded();
    }

    /// <summary>
    /// Resets all stored state.
    /// </summary>
    protected virtual void ResetState()
    {
      _outputStreamEnded = false;
    }

    #endregion
  }
}
