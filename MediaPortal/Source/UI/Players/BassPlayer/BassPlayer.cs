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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.PlayerComponents;
using MediaPortal.UI.Presentation.Players;
using Un4seen.Bass.AddOn.Tags;

namespace MediaPortal.UI.Players.BassPlayer
{
  /// <summary>
  /// MediaPortal 2 audio player based on the Un4seen Bass library. Supports several player interfaces of the MP2 player API.
  /// </summary>
  /// <remarks>
  /// There are several aspects which need to be understood in the architecture of this player:
  /// <para>
  /// This <see cref="BassPlayer"/> class is a container for a complete player system, i.e. it contains, creates and
  /// disposes the complete system. It provides an external player state and maintains some fields for the communication
  /// with the external world like the player event delegates. But this <see cref="BassPlayer"/> class is not necessary
  /// for the actual player functionality; the actual player functionality is provided by the <see cref="Controller"/>
  /// class. The <see cref="Controller"/> class maintains the core player components and only does very little communication
  /// with this class for calling the player events.<br/>
  /// For internal architecture docs, see the class docs of the <see cref="Controller"/> class.
  /// </para>
  /// <para>
  /// Multithreading: This "external" player interface class is safe for multithreading.
  /// </para>
  /// </remarks>
  public class BassPlayer : IDisposable, IAudioPlayer, IAudioPlayerAnalyze, IMediaPlaybackControl, IPlayerEvents, IReusablePlayer, ITagSource
  {
    #region Protected fields

    protected readonly object _syncObj = new object();

    protected Controller _controller; // BassPlayer owns the Controller instance; the BassPlayer is the instance which will be disposed by the outside system

    // The playback state which is reflected as part of the IPlayer interface.
    // This field will be updated at once when a method on one of the player interfaces is called.
    // The internal state is held by class PlaybackSession
    protected volatile PlayerState _externalState;
    protected InputSourceFactory _inputSourceFactory;
    protected string _mediaItemTitle = string.Empty;

    // Spectrum related fields
    protected int _sampleFrequency = 0;

    // Data and events for the communication with the player manager.
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;

    #endregion

    [Obsolete("Player plugins are now located in BassLibraries plugin. Setting other folder is no longer supported.")]
    public BassPlayer(string playerMainDirectory)
      : this()
    { }

    public BassPlayer()
    {
      _controller = new Controller(this);
      _inputSourceFactory = new InputSourceFactory();
      _externalState = PlayerState.Stopped;
    }

    #region IDisposable Members

    public void Dispose()
    {
      Log.Debug("Disposing BassPlayer");

      _controller.Dispose();
      _controller = null;
      _inputSourceFactory.Dispose();
      _inputSourceFactory = null;
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
    /// Gets tags from current source, which contain information about artist and title. This is mainly important for streams where this information will
    /// change regulary.
    /// </summary>
    public TAG_INFO Tags
    {
      get
      {
        return _controller != null ? _controller.GetTags() : null;
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
    /// <param name="locator">Resource locator of the to-be-played item.</param>
    /// <param name="mimeType">Mime type of the media item to be played, if given. May be <c>null</c>.</param>
    /// <param name="mediaItemTitle">Title of the media item to be played.</param>
    /// <remarks>
    /// The workitem will actually be executed on the controller's mainthread.
    /// </remarks>
    public void SetMediaItemLocator(IResourceLocator locator, string mimeType, string mediaItemTitle)
    {
      if (_externalState != PlayerState.Stopped)
        Stop();
      IInputSource inputSource = _inputSourceFactory.CreateInputSource(locator, mimeType);
      if (inputSource == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("Unable to play '{0}'", locator);
        return;
      }
      _mediaItemTitle = mediaItemTitle;
      _externalState = PlayerState.Active;
      _controller.MoveToNextItem_Async(inputSource, StartTime.AtOnce);
    }

    #endregion

    #region IPlayer implementation

    public string Name
    {
      get { return "Audio"; }
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

    public bool NextItem(MediaItem mediaItem, StartTime startTime)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return false;
      IResourceLocator locator = mediaItem.GetResourceLocator();
      if (locator == null)
        return false;
      IInputSource inputSource = _inputSourceFactory.CreateInputSource(locator, mimeType);
      if (inputSource == null)
        return false;
      lock (_syncObj)
        _externalState = PlayerState.Active;
      _controller.MoveToNextItem_Async(inputSource, startTime);
      _mediaItemTitle = title; // This is a bit too early because we're not switching to the next item at once, but doesn't matter
      return true;
    }

    #endregion

    #region IAudioPlayerAnalyze Member

    /// <summary>
    /// Provides access to valid source for analyze: if the OutputDevice implements own techniques to retrieve data, it will be preferred (WASAPI).
    /// Otherwise the PlaybackBuffer's VizStream will be used (DirectSound).
    /// </summary>
    private IAudioPlayerAnalyze AudioPlayerAnalyze
    {
      get { return _controller.OutputDeviceManager.OutputDevice as IAudioPlayerAnalyze ?? _controller.PlaybackProcessor.AudioPlayerAnalyze; }
    }

    public bool GetWaveData32(int length, out float[] waveData32)
    {
      waveData32 = null;
      var analyze = AudioPlayerAnalyze;
      return analyze != null && analyze.GetWaveData32(length, out waveData32);
    }

    public bool GetFFTData(float[] fftDataBuffer)
    {
      var analyze = AudioPlayerAnalyze;
      return analyze != null && analyze.GetFFTData(fftDataBuffer);
    }

    public bool GetFFTFrequencyIndex(int frequency, out int frequencyIndex)
    {
      frequencyIndex = 0;
      var analyze = AudioPlayerAnalyze;
      return analyze != null && analyze.GetFFTFrequencyIndex(frequency, out frequencyIndex);
    }

    public bool GetChannelLevel(out double dbLevelL, out double dbLevelR)
    {
      dbLevelL = 0;
      dbLevelR = 0;
      var analyze = AudioPlayerAnalyze;
      return analyze != null && analyze.GetChannelLevel(out dbLevelL, out dbLevelR);
    }

    #endregion
  }

}
