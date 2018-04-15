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
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.InputSources;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  public enum SessionState
  {
    Reset,
    Initialized,
    Playing,
    Ended
  }

  /// <summary>
  /// A single playback session can play a sequence of sources that have the same number of channels and the same samplerate.
  /// Within a playback session, we can perform crossfading and gapless switching.
  /// </summary>
  /// TODO: Crossfading
  public class PlaybackSession : IDisposable
  {
    #region Consts

    public static TimeSpan REQUEST_NEXT_ITEM_THRESHOLD = new TimeSpan(0, 0, 0, 0, 500);

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();
    protected readonly Controller _controller;
    protected readonly PlaybackProcessor _playbackProcessor;
    protected readonly int _channels;
    protected readonly int _sampleRate;
    protected readonly bool _isPassThrough;

    protected SessionState _state;
    protected bool _isAwaitingNextInputSource = false;
    protected volatile IInputSource _currentInputSource = null;
    protected volatile BassStream _outputStream;
    protected PlaybackBuffer _playbackBuffer;
    protected UpDownMixer _upDownMixer;
    protected VSTProcessor _vstProcessor;
    protected WinAmpDSPProcessor _winAmpDSPProcessor;
    protected readonly STREAMPROC _streamWriteProcDelegate;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates and initializes a new instance.
    /// </summary>
    /// <param name="controller">Reference to controller object.</param>
    public static PlaybackSession Create(Controller controller)
    {
      IInputSource inputSource = controller.PlaybackProcessor.GetAndClearNextInputSource();
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
      _upDownMixer = new UpDownMixer();
      _vstProcessor = new VSTProcessor();
      _winAmpDSPProcessor = new WinAmpDSPProcessor(controller);
      _channels = channels;
      _sampleRate = sampleRate;
      _isPassThrough = isPassThrough;
      _streamWriteProcDelegate = OutputStreamWriteProc;
      _state = SessionState.Reset;
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      Log.Debug("PlaybackSession.Dispose()");
      End(true);
      _playbackBuffer.Dispose();
      _winAmpDSPProcessor.Dispose();
      _vstProcessor.Dispose();
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
      get { return _vstProcessor; }
    }

    public WinAmpDSPProcessor WinAmpDSPProcessor
    {
      get { return _winAmpDSPProcessor; }
    }

    public SessionState State
    {
      get
      {
        lock (_syncObj)
          return _state;
      }
    }

    public bool IsAwaitingNextInputSource
    {
      get
      {
        lock (_syncObj)
          return _isAwaitingNextInputSource;
      }
      set
      {
        lock (_syncObj)
          _isAwaitingNextInputSource = value;
      }
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
      lock (_syncObj)
      {
        if (_state != SessionState.Initialized)
          return;
        _state = SessionState.Playing;
      }
      _controller.OutputDeviceManager.StartDevice();
    }

    /// <summary>
    /// Ends and discards this playback session.
    /// </summary>
    public void End(bool waitForFadeOut)
    {
      lock (_syncObj)
      {
        if (_state == SessionState.Reset)
          return;
        _state = SessionState.Reset;
      }
      _controller.OutputDeviceManager.StopDevice(waitForFadeOut);

      _controller.OutputDeviceManager.ResetInputStream();
      _playbackBuffer.ResetInputStream();
      _winAmpDSPProcessor.ResetInputStream();
      _vstProcessor.ResetInputStream();
      _upDownMixer.ResetInputStream();

      lock (_syncObj)
      {
        _controller.ScheduleDisposeObject_Async(_outputStream);
        _outputStream = null;
        _controller.ScheduleDisposeObject_Async(_currentInputSource);
        _currentInputSource = null;
      }
    }

    /// <summary>
    /// Initializes this playback session with the given <paramref name="inputSource"/>.
    /// <see cref="End"/> must have been called before.
    /// </summary>
    /// <param name="inputSource">The new input source to play.</param>
    /// <returns><c>true</c>, if the new input source can be played. <c>false</c> if either <see cref="End"/> was not
    /// called or if the new input source is not compatible with this playback session.</returns>
    public bool InitializeWithNewInputSource(IInputSource inputSource)
    {
      return Initialize(inputSource);
    }

    #endregion

    #region Protected members

    protected bool Initialize(IInputSource inputSource)
    {
      if (_state != SessionState.Reset)
        return false;

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

      _state = SessionState.Initialized;

      _upDownMixer.SetInputStream(_outputStream);
      _vstProcessor.SetInputStream(_upDownMixer.OutputStream);
      _winAmpDSPProcessor.SetInputStream(_vstProcessor.OutputStream);
      _playbackBuffer.SetInputStream(_winAmpDSPProcessor.OutputStream);
      _controller.OutputDeviceManager.SetInputStream(_playbackBuffer.OutputStream, _outputStream.IsPassThrough);
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
      IInputSource inputSource;
      lock (_syncObj)
      {
        if (_state == SessionState.Reset)
          return 0;
        inputSource = _currentInputSource;
        if (inputSource == null)
        {
          _state = SessionState.Ended;
          return (int) BASSStreamProc.BASS_STREAMPROC_END;
        }
      }

      try
      {
        BassStream stream = inputSource.OutputStream;
        int read = stream.Read(buffer, requestedBytes);

        bool doCheckNextInputSource = false;
        lock (_syncObj)
          if (!_isAwaitingNextInputSource && stream.GetPosition() > stream.Length.Subtract(REQUEST_NEXT_ITEM_THRESHOLD))
          { // Near end of the stream - make sure that next input source is available
            _isAwaitingNextInputSource = true;
            doCheckNextInputSource = true;
          }
        if (doCheckNextInputSource)
          _playbackProcessor.CheckInputSourceAvailable();

        if (read > 0)
          // Normal case, we have finished
          return read;

        // Old buffer ran out of samples - either we can get another valid input source below or we are finished. End wait state.
        _isAwaitingNextInputSource = false;

        // Nothing could be read from old input source. Second try: Next input source.
        IInputSource newInputSource = _playbackProcessor.PeekNextInputSource();

        // Special treatment for CD drives: If the new input source is from the same audio CD drive, we must take the stream over
        BassCDTrackInputSource bcdtisOld = inputSource as BassCDTrackInputSource;
        BassCDTrackInputSource bcdtisNew = newInputSource as BassCDTrackInputSource;
        if (bcdtisOld != null && bcdtisNew != null)
        {
          if (bcdtisOld.SwitchTo(bcdtisNew))
          {
            _playbackProcessor.ClearNextInputSource();
            return OutputStreamWriteProc(streamHandle, buffer, requestedBytes, userData);
          }
        }

        lock (_syncObj)
        {
          _currentInputSource = null;
          _controller.ScheduleDisposeObject_Async(inputSource);
          if (newInputSource == null)
          {
            _state = SessionState.Ended;
            return (int) BASSStreamProc.BASS_STREAMPROC_END;
          }
        }

        if (!MatchesInputSource(newInputSource))
        { // The next available input source is not compatible, so end our stream. The playback processor will start a new playback session later.
          lock (_syncObj)
            _state = SessionState.Ended;
          return (int) BASSStreamProc.BASS_STREAMPROC_END;
        }
        _playbackProcessor.ClearNextInputSource(); // Should be the contents of newInputSource
        lock (_syncObj)
        {
          _currentInputSource = newInputSource;
          _state = SessionState.Playing;
        }

        // Next try
        return OutputStreamWriteProc(streamHandle, buffer, requestedBytes, userData);
      }
      catch (Exception)
      {
        // We might come here due to a race condition. To avoid that, we would have to employ a new manual locking mechanism
        // to avoid that during the execution of this method, no methods are called from outside which change our
        // streams/partner instances.
        return 0;
      }
    }

    #endregion
  }
}
