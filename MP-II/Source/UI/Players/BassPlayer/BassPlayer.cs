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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.Presentation.Players;
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.PlayerComponents;
using Ui.Players.BassPlayer.Utils;

namespace Ui.Players.BassPlayer
{
  /// <summary>
  /// MediaPortal 2 music player based on the Un4seen Bass library. Supports several player interfaces of the MP2 player API.
  /// </summary>
  public class BassPlayer : IDisposable, IAudioPlayer, IMediaPlaybackControl, IPlayerEvents, IReusablePlayer
  {
    #region Consts

    public const string BASS_PLAYER_ID_STR = "2A6ADBE3-20B3-4fa5-84D4-B0CBCF032722";

    public static readonly Guid BASS_PLAYER_ID = new Guid(BASS_PLAYER_ID_STR);

    #endregion

    #region Fields

    protected readonly object _syncObj = new object();

    protected Controller _controller; // BassPlayer owns the Controller instance; the BassPlayer is the instance which will be disposed by the outside system

    // The playback state which is reflected as part of the IPlayer interface.
    // This field will be updated at once when a method on one of the player interfaces is called.
    // The internal state is held by class PlaybackSession
    protected volatile PlayerState _externalState;
    protected InputSourceFactory _inputSourceFactory;
    protected string _mediaItemTitle = string.Empty;

    // Data and events for the communication with the player manager.
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;

    #endregion

    public BassPlayer(string playerMainDirectory)
    {
      _controller = new Controller(this, playerMainDirectory);
      _inputSourceFactory = new InputSourceFactory();
      _externalState = PlayerState.Stopped;
    }

    #region IDisposable Members

    public void Dispose()
    {
      Log.Debug("Disposing BassPlayer");

      _inputSourceFactory.Dispose();
      _inputSourceFactory = null;
      _controller.Dispose();
      _controller = null;
    }

    #endregion

    #region Public members

    /// <summary>
    /// Returns the current external playback state. 
    /// </summary>
    /// <remarks>
    /// Because the player operates asynchronous, its internal state 
    /// can differ from its external state as seen by MP.
    /// </remarks>
    public PlayerState ExternalState
    {
      get
      {
        lock (_syncObj)
          return _externalState;
      }
    }

    /// <summary>
    /// Tell MP that the current media item ended.
    /// </summary>
    public void RequestNextMediaItemFromSystem()
    {
      lock (_syncObj)
        if (_externalState != PlayerState.Active)
          return;
      // Just make MP come up with the next item on its playlist
      FireNextItemRequest();
    }

    public void PlaybackEnded()
    {
      lock (_syncObj)
      {
        if (_externalState != PlayerState.Active)
          return;
        _externalState = PlayerState.Ended;
      }
      FireEnded();
    }

    #endregion

    #region Protected members

    internal void FireStarted()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_started != null)
        _started(this);
    }

    internal void FireStateReady()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_stateReady != null)
        _stateReady(this);
    }

    internal void FireStopped()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_stopped != null)
        _stopped(this);
    }

    internal void FireEnded()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_ended != null)
        _ended(this);
    }

    internal void FirePlaybackStateChanged()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_playbackStateChanged != null)
        _playbackStateChanged(this);
    }

    protected void FireNextItemRequest()
    {
      RequestNextItemDlgt dlgt = NextItemRequest;
      if (dlgt != null)
        dlgt(this);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Enqueues a play workitem for the given mediaitem.
    /// </summary>
    /// <param name="locator"></param>
    /// <remarks>
    /// The workitem will actually be executed on the controller's mainthread.
    /// </remarks>
    public void SetMediaItemLocator(IResourceLocator locator)
    {
      if (_externalState != PlayerState.Stopped)
        Stop();
      IInputSource inputSource = _inputSourceFactory.CreateInputSource(locator);
      if (inputSource == null)
      {
        ServiceScope.Get<ILogger>().Warn("Unable to play '{0}'", locator);
        return;
      }
      _externalState = PlayerState.Active;
      _controller.MoveToNextItem_Async(inputSource, StartTime.AtOnce);
    }

    #endregion

    #region IPlayer implementation

    public Guid PlayerId
    {
      get { return BASS_PLAYER_ID; }
    }

    public string Name
    {
      get { return "AudioPlayer"; }
    }

    public PlayerState State
    {
      get
      {
        lock (_syncObj)
          return _externalState;
      }
    }

    public string MediaItemTitle
    {
      get
      {
        lock (_syncObj)
          return _mediaItemTitle;
      }
    }

    public void SetMediaItemTitleHint(string title)
    {
      lock (_syncObj)
        _mediaItemTitle = title;
    }

    public void Stop()
    {
      lock (_syncObj)
      {
        if (_externalState != PlayerState.Active)
          return;
        _externalState = PlayerState.Stopped;
      }
      _controller.Stop_Async();
      FireStopped();
    }

    #endregion

    #region IAudioPlayer implementation

    public bool Mute
    {
      get { return _controller.GetMuteState(); }
      set { _controller.SetMuteState_Async(value); }
    }

    public int Volume
    {
      get { return _controller.GetVolume(); }
      set { _controller.SetVolume_Async(value); }
    }

    #endregion

    #region IMediaPlaybackControl implementation

    public TimeSpan CurrentTime
    {
      get
      {
        lock (_syncObj)
          if (_externalState != PlayerState.Active)
            return TimeSpan.Zero;
        return _controller.GetCurrentPlayTime();
      }
      set { _controller.SetCurrentPlayTime_Async(value); }
    }

    public TimeSpan Duration
    {
      get
      {
        lock (_syncObj)
          if (_externalState != PlayerState.Active)
            return TimeSpan.Zero;
        return _controller.GetDuration();
      }
    }

    // TODO: Playback rate control

    public bool CanSeekForwards
    {
      get { return false; }
    }

    public bool CanSeekBackwards
    {
      get { return false; }
    }

    public double PlaybackRate
    {
      get { return 1.0; }
    }

    public bool SetPlaybackRate(double value)
    {
      return false;
    }

    public bool IsPlayingAtNormalRate
    {
      get { return true; }
    }

    public bool IsSeeking
    {
      get { return false; }
    }

    public bool IsPaused
    {
      get { return _controller.IsPaused; }
    }

    public void Pause()
    {
      lock (_syncObj)
        if (_externalState != PlayerState.Active)
          return;
      _controller.Pause_Async();
    }

    public void Resume()
    {
      lock (_syncObj)
        if (_externalState != PlayerState.Active)
          return;
      _controller.Resume_Async();
    }

    public void Restart()
    {
      lock (_syncObj)
        _externalState = PlayerState.Active;
      _controller.Restart_Async();
      FireStarted();
    }

    #endregion

    #region IPlayerEvents implementation

    // Not implemented thread-safe by design
    public void InitializePlayerEvents(PlayerEventDlgt started, PlayerEventDlgt stateReady, PlayerEventDlgt stopped,
        PlayerEventDlgt ended, PlayerEventDlgt playbackStateChanged, PlayerEventDlgt playbackError)
    {
      _started = started;
      _stateReady = stateReady;
      _stopped = stopped;
      _ended = ended;
      _playbackStateChanged = playbackStateChanged;
      _playbackError = playbackError;
    }

    // Not implemented thread-safe by design
    public void ResetPlayerEvents()
    {
      _started = null;
      _stateReady = null;
      _stopped = null;
      _ended = null;
      _playbackStateChanged = null;
      _playbackError = null;
    }

    #endregion

    #region IReusablePlayer implementation

    public event RequestNextItemDlgt NextItemRequest;

    public bool NextItem(IResourceLocator locator, string mimeType, StartTime startTime)
    {
      IInputSource inputSource = _inputSourceFactory.CreateInputSource(locator);
      if (inputSource == null)
        return false;
      lock (_syncObj)
        _externalState = PlayerState.Active;
      _controller.MoveToNextItem_Async(inputSource, startTime);
      return true;
    }

    #endregion
  }
}
