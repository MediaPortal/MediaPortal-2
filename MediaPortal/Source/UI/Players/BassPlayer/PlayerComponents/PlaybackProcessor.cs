#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Management class for playback sessions and available input sources.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This playback processor class maintains a queue of input sources to be played and a current playback session.
  /// Input sources with the same number of channels and the same samplerate are compatible, which means that the same
  /// playback session instance can play them and crossfade between them.
  /// </para>
  /// <para>
  /// The playback session will automatically move to the next input source, if it is compatible.
  /// </para>
  /// <para>
  /// If the next input source is not compatible with the current playback session, the current session will notify
  /// its containing <see cref="PlaybackProcessor"/> which then will switch to a new playback session.
  /// </para>
  /// </remarks>
  public class PlaybackProcessor : IDisposable
  {
    #region Protected fields

    protected readonly object _syncObj = new object();

    protected readonly Controller _controller;
    protected  IInputSource _nextInputSource = null;

    protected PlaybackMode _playbackMode = PlaybackMode.Normal;
    protected PlaybackSession _playbackSession = null;

    // The internal playback state reflecting the internal player state information. This might differ from the player's
    // external state
    protected volatile InternalPlaybackState _internalState;

    #endregion

    /// <summary>
    /// Creates and initializes a new instance.
    /// </summary>
    /// <param name="controller">Containing controller instance.</param>
    public PlaybackProcessor(Controller controller)
    {
      _controller = controller;
    }

    #region IDisposable implementation

    public void Dispose()
    {
      lock (_syncObj)
        if (_nextInputSource != null)
        {
          _nextInputSource.Dispose();
          _nextInputSource = null;
        }
      PlaybackSession playbackSession = _playbackSession;
      if (playbackSession != null)
        playbackSession.Dispose();
    }

    #endregion

    protected void Initialize()
    {
      _internalState = InternalPlaybackState.Stopped;
    }

    protected void Ended()
    {
      _internalState = InternalPlaybackState.Stopped;
      _controller.PlaybackEnded_Async();
    }

    /// <summary>
    /// Moves to the next available input source.
    /// </summary>
    /// <remarks>
    /// This method blocks the calling thread as long as the switching to the new input source lasts. This includes
    /// the crossfading duration (if crossfading is done) or the fading out (if no crossfading is done).
    /// </remarks>
    protected void MoveToNextInputSource_Sync()
    {
      // TODO: Insert gap between tracks if we are in playback mode Normal
      IInputSource inputSource = PeekNextInputSource();

      if (_playbackSession != null)
      {
        BassCDTrackInputSource bcdtisNew = inputSource as BassCDTrackInputSource;
        IInputSource currentInputSource = _playbackSession.CurrentInputSource;
        BassCDTrackInputSource bcdtisOld = currentInputSource as BassCDTrackInputSource;
        if (bcdtisOld != null && bcdtisNew != null)
        {
          // Special treatment for CD drives: If the new input source is from the same audio CD drive, we must take the stream over
          if (bcdtisOld.SwitchTo(bcdtisNew))
          {
            _playbackSession.IsAwaitingNextInputSource = false;
            ClearNextInputSource();
            return;
          }
        }
        // TODO: Trigger crossfading if CF is configured
        _playbackSession.End(_internalState == InternalPlaybackState.Playing); // Only wait for fade out when we are playing
      }

      _internalState = InternalPlaybackState.Playing;
      if (inputSource == null)
        Log.Debug("No more input sources available.");
      else
        Log.Debug("Playing next input source '{0}'", inputSource);
      if (_playbackSession != null)
      {
        if (_playbackSession.InitializeWithNewInputSource(inputSource))
        {
          _playbackSession.Play();
          ClearNextInputSource();
          return;
        }
        _playbackSession.Dispose();
        _playbackSession = null;
      }

      if (inputSource == null)
      {
        Ended();
        return;
      }

      _playbackSession = PlaybackSession.Create(_controller);
      if (_playbackSession == null)
      {
        _internalState = InternalPlaybackState.Stopped;
        return;
      }
      _playbackSession.Play();

      _internalState = InternalPlaybackState.Playing;
      _controller.StateReady();
    }

    /// <summary>
    /// Notifies this instance that a new input source has become available.
    /// </summary>
    /// <remarks>
    /// This method might block the calling thread as long as the switching to the new input source lasts in some cases.
    /// See the notes for <see cref="MoveToNextInputSource_Sync()"/>.
    /// </remarks>
    protected void NextInputSourceAvailable_Sync()
    {
      PlaybackSession session = _playbackSession;
      if (session != null && session.IsAwaitingNextInputSource)
        // In this case, the session will automatically switch to the new item
        return;
      if (session == null || session.State == SessionState.Ended)
        // We might come here if the session ran out of samples before a new input source arrived.
        // In that case, we must continue "by hand".
        MoveToNextInputSource_Sync();
    }

    internal void CheckInputSourceAvailable()
    {
      lock (_syncObj)
        if (_nextInputSource == null)
          _controller.RequestNextMediaItem_Async();
    }

    internal void ScheduleMoveToNextInputSource()
    {
      _controller.EnqueueWorkItem(new WorkItem(new Controller.WorkItemDelegate(MoveToNextInputSource_Sync)));
    }

    internal void ScheduleNextInputSourceAvailable()
    {
      _controller.EnqueueWorkItem(new WorkItem(new Controller.WorkItemDelegate(NextInputSourceAvailable_Sync)));
    }

    /// <summary>
    /// Handle the event when all data has been played and no new data is available.
    /// </summary>
    internal void HandleOutputStreamEnded()
    {
      // We are coming here if
      // 1) The playback session ended and the next media item isn't compatible with the current session
      //    => In this case we need to start a new playback session for the next item
      // 2) There is no more media item
      //    => In this case we need to tell the player that we are finished
      MoveToNextInputSource_Sync();
    }

    /// <summary>
    /// Gets the current internal playback state. 
    /// </summary>
    /// <remarks>
    /// Because the player operates asynchronous, its internal state 
    /// can differ from its external state as seen by MP.
    /// </remarks>
    public InternalPlaybackState InternalState
    {
      get { return _internalState; }
    }

    public PlaybackMode PlaybackMode
    {
      get { return _playbackMode; }
    }

    public PlaybackSession PlaybackSession
    {
      get { return _playbackSession; }
    }

    public void SetNextInputSource(IInputSource item)
    {
      lock (_syncObj)
      {
        if (_nextInputSource != null)
          _nextInputSource.Dispose();
        _nextInputSource = item;
      }
    }

    public void ClearNextInputSource()
    {
      lock (_syncObj)
        _nextInputSource = null;
    }

    public IInputSource GetAndClearNextInputSource()
    {
      lock (_syncObj)
      {
        IInputSource result = _nextInputSource;
        _nextInputSource = null;
        return result;
      }
    }

    public IInputSource PeekNextInputSource()
    {
      lock (_syncObj)
        return _nextInputSource;
    }

    public void Stop()
    {
      if (_internalState == InternalPlaybackState.Playing || _internalState == InternalPlaybackState.Paused)
      {
        PlaybackSession session = _playbackSession;
        _playbackSession = null;
        if (session != null)
        {
          session.End(false);
          session.Dispose();
        }

        _internalState = InternalPlaybackState.Stopped;
      }
    }

    public void Pause()
    {
      if (_internalState == InternalPlaybackState.Playing)
      {
        _controller.OutputDeviceManager.StopDevice(false);
        _internalState = InternalPlaybackState.Paused;
      }
    }

    public void Resume()
    {
      if (_internalState == InternalPlaybackState.Paused)
      {
        _controller.OutputDeviceManager.StartDevice();
        _internalState = InternalPlaybackState.Playing;
      }
    }

    public TimeSpan CurrentPosition
    {
      get
      {
        BassStream outputStream = OutputStream;
        if (outputStream == null)
          return TimeSpan.Zero;
        return outputStream.GetPosition();
      }
      set
      {
        if (_internalState == InternalPlaybackState.Playing || _internalState == InternalPlaybackState.Paused)
        {
          BassStream stream = OutputStream;

          if (stream == null)
            return;
          stream.SetPosition(value);
        }
      }
    }

    public BassStream OutputStream
    {
      get
      {
        PlaybackSession playbackSession = _playbackSession;
        if (playbackSession == null || playbackSession.CurrentInputSource == null)
          return null;
        return playbackSession.CurrentInputSource.OutputStream;
      }
    }

    public IAudioPlayerAnalyze AudioPlayerAnalyze
    {
      get
      {
        PlaybackSession playbackSession = _playbackSession;
        if (playbackSession == null)
          return null;
        return playbackSession.PlaybackBuffer;
      }
    }

    // Convenience method for BassPlayer.CurrentTime setter, which needs a method to be executed asynchronously
    public void SetPosition(TimeSpan value)
    {
      CurrentPosition = value;
    }
  }
}
