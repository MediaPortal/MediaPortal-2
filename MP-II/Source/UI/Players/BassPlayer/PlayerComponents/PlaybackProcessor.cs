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
using System.Collections.Generic;
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.Utils;

namespace Ui.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Management class for playback sessions and available input sources.
  /// </summary>
  public class PlaybackProcessor : IDisposable
  {
    #region Protected fields

    protected readonly object _syncObj = new object();

    protected readonly Controller _controller;
    protected readonly Queue<IInputSource> _inputSourceQueue;

    protected PlaybackMode _playbackMode = PlaybackMode.Normal;
    protected PlaybackSession _playbackSession = null;

    // The internal playback state reflecting the internal player state information. This might differ from the player's
    // external state
    protected volatile InternalPlaybackState _internalState;

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      lock (_syncObj)
        while (_inputSourceQueue.Count > 0)
        {
          IInputSource item = _inputSourceQueue.Dequeue();
          item.Dispose();
        }
      PlaybackSession playbackSession = _playbackSession;
      if (playbackSession != null)
        playbackSession.Dispose();
    }

    #endregion

    /// <summary>
    /// Creates and initializes a new instance.
    /// </summary>
    /// <param name="controller">Containing controller instance.</param>
    public PlaybackProcessor(Controller controller)
    {
      _controller = controller;
      _inputSourceQueue = new Queue<IInputSource>();
    }

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
      Resume();

      // TODO: Insert gap between tracks if we are in playback mode Normal
      IInputSource inputSource = PeekNextInputSource();
      if (_playbackSession != null)
        // TODO: Trigger crossfading if CF is configured
        _playbackSession.End(true);

      Log.Debug("Playing next input source '{0}'", inputSource);      
      if (_playbackSession != null)
      {
        if (_playbackSession.MoveToNewInputSource(inputSource))
        {
          _playbackSession.Play();
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
    /// See the notes for <see cref="MoveToNextInputSource_Sync"/>.
    /// </remarks>
    protected void NextInputSourceAvailable_Sync()
    {
      PlaybackSession session = _playbackSession;
      if (session != null && session.State == SessionState.AwaitingNextInputSource)
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
        if (_inputSourceQueue.Count == 0)
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

    public void EnqueueInputSource(IInputSource item)
    {
      lock (_syncObj)
        _inputSourceQueue.Enqueue(item);
    }

    public IInputSource DequeueNextInputSource()
    {
      lock (_syncObj)
        return _inputSourceQueue.Count == 0 ? null : _inputSourceQueue.Dequeue();
    }

    public IInputSource PeekNextInputSource()
    {
      lock (_syncObj)
        return _inputSourceQueue.Count == 0 ? null : _inputSourceQueue.Peek();
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
        if (playbackSession == null)
          return null;
        return playbackSession.OutputStream;
      }
    }

    // Convenience method for BassPlayer.CurrentTime setter, which needs a method to be executed asynchronously
    public void SetPosition(TimeSpan value)
    {
      CurrentPosition = value;
    }
  }
}
