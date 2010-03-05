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
    Initialized,
    Playing,
    AwaitingNextInputSource,
    Ended
  }

  /// <summary>
  /// A single playback session can play a sequence of sources that have the same number of channels and the same samplerate.
  /// Within a playback session, we can perform crossfading and gapless switching.
  /// </summary>
  /// TODO: Crossfading
  public class PlaybackSession : IDisposable
  {
    #region Protected fields

    protected readonly Controller _controller;
    protected readonly PlaybackProcessor _playbackProcessor;
    protected readonly int _channels;
    protected readonly int _sampleRate;
    protected readonly bool _isPassThrough;

    protected volatile SessionState _state;
    protected volatile IInputSource _currentInputSource = null;
    protected volatile BassStream _outputStream;
    protected PlaybackBuffer _playbackBuffer;
    protected UpDownMixer _upDownMixer;
    protected VSTProcessor _VSTProcessor;
    protected WinAmpDSPProcessor _WinAmpDSPProcessor;
    protected readonly STREAMPROC _streamWriteProcDelegate;

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
      _playbackBuffer = new PlaybackBuffer(controller);
      _upDownMixer = new UpDownMixer(controller);
      _VSTProcessor = new VSTProcessor(controller);
      _WinAmpDSPProcessor = new WinAmpDSPProcessor(controller);
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
      End(true);
      _playbackBuffer.Dispose();
      _WinAmpDSPProcessor.Dispose();
      _VSTProcessor.Dispose();
      _upDownMixer.Dispose();
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets the number of channels for this session.
    /// </summary>
    public int Channels
    {
      get { return _channels; }
    }

    /// <summary>
    /// Gets the samplerate for this session.
    /// </summary>
    public int SampleRate
    {
      get { return _sampleRate; }
    }

    /// <summary>
    /// Gets the information whether this session is in AC3/DTS passthrough mode.
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

    public PlaybackBuffer PlaybackBuffer
    {
      get { return _playbackBuffer; }
    }

    public UpDownMixer UpDownMixer
    {
      get { return _upDownMixer; }
    }

    public VSTProcessor VSTProcessor
    {
      get { return _VSTProcessor; }
    }

    public WinAmpDSPProcessor WinAmpDSPProcessor
    {
      get { return _WinAmpDSPProcessor; }
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

    public void Play()
    {
      if (_state != SessionState.Initialized)
        return;
      _state = SessionState.Playing;
      _controller.OutputDeviceManager.StartDevice();
    }

    /// <summary>
    /// Ends and discards this playback session.
    /// </summary>
    public void End(bool waitForFadeOut)
    {
      if (_state == SessionState.Reset)
        return;
      _controller.OutputDeviceManager.StopDevice(waitForFadeOut);
        
      _controller.OutputDeviceManager.ResetInputStream();
      _playbackBuffer.ResetInputStream();
      _WinAmpDSPProcessor.ResetInputStream();
      _VSTProcessor.ResetInputStream();
      _upDownMixer.ResetInputStream();
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

      _outputStream = BassStream.Create(handle);
        
      _upDownMixer.SetInputStream(_outputStream);
      _VSTProcessor.SetInputStream(_upDownMixer.OutputStream);
      _WinAmpDSPProcessor.SetInputStream(_VSTProcessor.OutputStream);
      _playbackBuffer.SetInputStream(_WinAmpDSPProcessor.OutputStream);
      _controller.OutputDeviceManager.SetInputStream(_playbackBuffer.OutputStream, _outputStream.IsPassThrough);

      _state = SessionState.Initialized;
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
