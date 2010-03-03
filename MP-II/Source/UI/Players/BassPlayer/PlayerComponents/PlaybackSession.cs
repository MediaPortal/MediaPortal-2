#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace Ui.Players.BassPlayer.PlayerComponents
{
  public enum SessionState
  {
    Reset,
    Playing,
    Ended,
    AwaitingNextInputSource
  }

  /// <summary>
  /// A single playback session can play a sequence of sources that have the same number of channels and the same samplerate.
  /// Within a playback session, we can perform crossfading and gapless switching.
  /// </summary>
  /// TODO: Crossfading
  public class PlaybackSession : IDisposable
  {
    #region Fields

    private readonly Controller _controller;
    private readonly PlaybackProcessor _playbackProcessor;
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly bool _isPassThrough;

    private volatile SessionState _state;
    private volatile IInputSource _currentInputSource = null;
    private volatile BassStream _outputStream;
    private readonly STREAMPROC _streamWriteProcDelegate;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates and initializes a new instance.
    /// </summary>
    /// <param name="controller">Reference to controller object.</param>
    public static PlaybackSession Create(Controller controller)
    {
      IInputSource inputSource = controller.PlaybackProcessor.DequeueNextInputSource();
      if (inputSource == null)
        return null;
      BassStream stream = inputSource.OutputStream;
      PlaybackSession playbackSession = new PlaybackSession(controller, stream.Channels, stream.SampleRate, stream.IsPassThrough);
      playbackSession.Initialize(inputSource);
      return playbackSession;
    }

    private PlaybackSession(Controller controller, int channels, int sampleRate, bool isPassThrough)
    {
      _controller = controller;
      _playbackProcessor = _controller.PlaybackProcessor;
      _channels = channels;
      _sampleRate = sampleRate;
      _isPassThrough = isPassThrough;
      _streamWriteProcDelegate = OutputStreamWriteProc;
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      Log.Debug("PlaybackSession.Dispose()");
      Reset();
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets the number of channels for the session.
    /// </summary>
    public int Channels
    {
      get { return _channels; }
    }

    /// <summary>
    /// Gets the samplerate for the session.
    /// </summary>
    public int SampleRate
    {
      get { return _sampleRate; }
    }

    /// <summary>
    /// Gets whether the session is in AC3/DTS passthrough mode.
    /// </summary>
    public bool IsPassThrough
    {
      get { return _isPassThrough; }
    }

    public IInputSource CurrentInputSource
    {
      get { return _currentInputSource; }
    }

    /// <summary>
    /// Gets the output Bass stream.
    /// </summary>
    public BassStream OutputStream
    {
      get { return _outputStream; }
    }

    public SessionState State
    {
      get { return _state; }
    }

    /// <summary>
    /// Determines whether a given inputsource fits in this session or not.
    /// </summary>
    /// <param name="inputSource">The inputsource to check.</param>
    /// <returns><c>true</c>, if the given <paramref name="inputSource"/> matches to this playback session,
    /// else <c>false</c>.</returns>
    public bool MatchesInputSource(IInputSource inputSource)
    {
      return inputSource != null &&
          inputSource.OutputStream.Channels == Channels &&
          inputSource.OutputStream.SampleRate == SampleRate &&
          inputSource.OutputStream.IsPassThrough == IsPassThrough;
    }

    /// <summary>
    /// Ends and discards the playback session.
    /// </summary>
    public void End(bool waitForFadeOut)
    {
      if (_state == SessionState.Reset)
        return;
      _controller.OutputDeviceManager.StopDevice(waitForFadeOut);
        
      _controller.OutputDeviceManager.ResetInputStream();
      _controller.PlaybackBuffer.ResetInputStream();
      _controller.WinAmpDSPProcessor.ResetInputStream();
      _controller.VSTProcessor.ResetInputStream();
      _controller.UpDownMixer.ResetInputStream();
      Reset();
    }

    public bool MoveToNewInputSource(IInputSource inputSource)
    {
      Reset();
      return Initialize(inputSource);
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Resets the instance to its uninitialized state.
    /// </summary>
    protected void Reset()
    {
      Log.Debug("PlaybackSession.Reset()");

      if (_outputStream != null)
        _outputStream.Dispose();
      _outputStream = null;
        
      IInputSource inputSource = _currentInputSource;
      _currentInputSource = null;
      if (inputSource != null)
        inputSource.Dispose();

      _state = SessionState.Reset;
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    protected bool Initialize(IInputSource inputSource)
    {
      if (!MatchesInputSource(inputSource))
        return false;

      _currentInputSource = inputSource;

      Log.Debug("PlaybackSession: Creating output stream");

      const BASSFlag flags = BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE;

      int handle = Bass.BASS_StreamCreate(
          _currentInputSource.OutputStream.SampleRate,
          _currentInputSource.OutputStream.Channels,
          flags,
          _streamWriteProcDelegate,
          IntPtr.Zero);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_StreamCreate");

      // Is it necessary to do this before streams get connected? Or should we do it just before StartDevice() is called?
      _state = SessionState.Playing;

      _outputStream = BassStream.Create(handle);
        
      _controller.UpDownMixer.SetInputStream(_outputStream);
      _controller.VSTProcessor.SetInputStream(_controller.UpDownMixer.OutputStream);
      _controller.WinAmpDSPProcessor.SetInputStream(_controller.VSTProcessor.OutputStream);
      _controller.PlaybackBuffer.SetInputStream(_controller.WinAmpDSPProcessor.OutputStream);
      _controller.OutputDeviceManager.SetInputStream(_controller.PlaybackBuffer.OutputStream);
        
      _controller.OutputDeviceManager.StartDevice();
      return true;
    }

    /// <summary>
    /// Callback function for the outputstream.
    /// </summary>
    /// <param name="streamHandle">Bass stream handle that requests sample data.</param>
    /// <param name="buffer">Buffer to write the sampledata to.</param>
    /// <param name="requestedBytes">Requested number of bytes.</param>
    /// <param name="userData"></param>
    /// <returns>Number of bytes read.</returns>
    private int OutputStreamWriteProc(int streamHandle, IntPtr buffer, int requestedBytes, IntPtr userData)
    {
      IInputSource inputSource = _currentInputSource;
      if (inputSource == null)
      {
        _state = SessionState.Ended;
        return (int) BASSStreamProc.BASS_STREAMPROC_END;
      }
      BassStream stream = inputSource.OutputStream;
      int read = stream.Read(buffer, requestedBytes);

      if (_state == SessionState.Playing && stream.GetPosition() > stream.Length - new TimeSpan(0, 0, 0, 0, 500))
      { // Near end of the stream - make sure that next input source is available
        _state = SessionState.AwaitingNextInputSource;
        _playbackProcessor.CheckInputSourceAvailable();
      }

      if (read > 0)
        return read;

      // Second try: Next input source
      _currentInputSource = null;
      _controller.ScheduleDisposeObject_Async(inputSource);
      inputSource = _playbackProcessor.PeekNextInputSource();
      if (inputSource == null)
      {
        _state = SessionState.Ended;
        return (int) BASSStreamProc.BASS_STREAMPROC_END;
      }
      if (!MatchesInputSource(inputSource))
      { // The next available input source is not compatible, so end our stream. The playback processor will start a new playback session later.
        _state = SessionState.Ended;
        return (int) BASSStreamProc.BASS_STREAMPROC_END;
      }
      _currentInputSource = _playbackProcessor.DequeueNextInputSource();
      _state = SessionState.Playing;
      stream = inputSource.OutputStream;
      read = stream.Read(buffer, requestedBytes);

      if (read > 0)
        return read;

      // No chance: Stream ended and we don't have another stream to switch to
      _state = SessionState.Ended;
      return (int) BASSStreamProc.BASS_STREAMPROC_END;
    }

    #endregion
  }
}
