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
using System.IO;
using System.Threading;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Utilities.Exceptions;
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.PlayerComponents;
using Ui.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace Ui.Players.BassPlayer
{
  /// <summary>
  /// Music player based on the Un4seen Bass library.
  /// </summary>
  public class BassPlayer : IDisposable, IAudioPlayer, IMediaPlaybackControl, IPlayerEvents // TODO: ICrossfadingEnabledPlayer
  {
    #region Consts

    public const string BASS_PLAYER_ID_STR = "2A6ADBE3-20B3-4fa5-84D4-B0CBCF032722";

    public static readonly Guid BASS_PLAYER_ID = new Guid(BASS_PLAYER_ID_STR);

    #endregion

    #region Delegates

    // Delegates used in conjunction with workitems.
    private delegate void WorkItemDelegate();
    private delegate void SetVolumeWorkItemDelegate(int value);
    private delegate void SetMuteWorkItemDelegate(bool value);
    private delegate void SetPositionWorkItemDelegate(TimeSpan value);
    private delegate void PlayWorkItemDelegate(IResourceLocator locator);

    #endregion

    #region Static members

    protected static BassPlayer _instance = null;
    private static readonly object _syncObj = new object();

    /// <summary>
    /// Creates and initializes a new instance.
    /// </summary>
    /// <param name="playerMainDirectory">Directory where the BASS player is located.</param>
    /// <returns>The new instance.</returns>
    public static BassPlayer Create(string playerMainDirectory)
    {
      lock (_syncObj)
      {
        if (_instance != null)
          throw new IllegalCallException("The BassPlayer cannot be instantiated multiple times");
        _instance = new BassPlayer();
        _instance.Initialize(playerMainDirectory);
        return _instance;
      }
    }

    #endregion

    #region Fields

    private BassLibraryManager _BassLibraryManager;
    private BassPlayerSettings _Settings;
    private InputSourceFactory _InputSourceFactory;
    private InputSourceQueue _InputSourceQueue;
    private PlaybackSession _PlaybackSession;

    private InputSourceSwitcher _InputSourceSwitcher;
    private UpDownMixer _UpDownMixer;
    private VSTProcessor _VSTProcessor;
    private WinAmpDSPProcessor _WinAmpDSPProcessor;
    private PlaybackBuffer _PlaybackBuffer;
    private OutputDeviceManager _OutputDeviceManager;

    // The playback state which is reflected as part of the IPlayer interface
    private volatile PlayerState _ExternalState;

    // The internal playback state reflecting additional player state information
    private volatile InternalPlaybackState _InternalState;

    private volatile bool _isMuted = false;

    private volatile int _volume = 100;

    // The current playback mode
    private PlaybackMode _PlaybackMode = PlaybackMode.Normal;

    private Thread _MainThread;
    private bool _MainThreadAbortFlag;
    private WorkItemQueue _WorkItemQueue;

    protected string _mediaItemTitle = string.Empty;

    // Data and events for the communication with the player manager.
    protected PlayerEventDlgt _started;
    protected PlayerEventDlgt _stateReady;
    protected PlayerEventDlgt _stopped;
    protected PlayerEventDlgt _ended;
    protected PlayerEventDlgt _playbackStateChanged;
    protected PlayerEventDlgt _playbackError;

    #endregion

    #region Public members

    internal BassPlayerSettings Settings
    {
      get { return _Settings; }
    }

    internal InputSourceSwitcher InputSourceSwitcher
    {
      get { return _InputSourceSwitcher; }
    }

    internal InputSourceQueue InputSourceQueue
    {
      get { return _InputSourceQueue; }
    }

    internal UpDownMixer UpDownMixer
    {
      get { return _UpDownMixer; }
    }

    internal VSTProcessor VSTProcessor
    {
      get { return _VSTProcessor; }
    }

    internal WinAmpDSPProcessor WinAmpDSPProcessor
    {
      get { return _WinAmpDSPProcessor; }
    }

    internal PlaybackBuffer PlaybackBuffer
    {
      get { return _PlaybackBuffer; }
    }

    internal OutputDeviceManager OutputDeviceManager
    {
      get { return _OutputDeviceManager; }
    }

    /// <summary>
    /// Returns the current external playback state. 
    /// </summary>
    /// <remarks>
    /// Because the player operates asynchronous, its internal state 
    /// can differ from its external state as seen by MP.
    /// </remarks>
    public PlayerState ExternalState
    {
      get { return _ExternalState; }
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
      get { return _InternalState; }
    }

    /// <summary>
    /// Gets the current playback mode.
    /// </summary>
    public PlaybackMode PlaybackMode
    {
      get { return _PlaybackMode; }
    }

    /// <summary>
    /// Handle the event when a new mediaitem is required to prepare crossfading or gapless playback.
    /// </summary>
    public void HandleNextMediaItemSyncPoint()
    {
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(RequestNextMediaItem)));
    }

    /// <summary>
    /// Start crossfading.
    /// </summary>
    public void HandleCrossFadeSyncPoint()
    {
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(StartCrossFade)));
    }

    /// <summary>
    /// Handle the event when all data has been played.
    /// </summary>
    public void HandleOutputStreamEnded()
    {
      // When we get here, a request for the next media item was already sent to MP (through HandleNextMediaItemSyncPoint).
      // All we have to do is stop playback that only runs in the background.
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(InternalStop)));
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Enqueues a workitem and notifies the controller mainthread something needs to be done.
    /// </summary>
    /// <param name="workItem">The workitem to enqueue.</param>
    /// <returns>The enqueued workitem.</returns>
    protected void EnqueueWorkItem(WorkItem workItem)
    {
      lock (_syncObj)
      {
        _WorkItemQueue.Enqueue(workItem);
        Monitor.PulseAll(_syncObj);
      }
    }

    protected WorkItem DequeueWorkItem()
    {
      lock (_syncObj)
        return _WorkItemQueue.Count == 0 ? null : _WorkItemQueue.Dequeue();
    }

    /// <summary>
    /// Enqueues the given mediaitem for playback and starts playback if nessecary.
    /// </summary>
    /// <param name="locator">The resource locator for the media item to play.</param>
    protected void InternalPlay(IResourceLocator locator)
    {
      Log.Info("Preparing for playback: '{0}'", locator);
      IInputSource inputSource = _InputSourceFactory.CreateInputSource(locator);
      if (inputSource == null)
      {
        ServiceScope.Get<ILogger>().Warn("Unable to play '{0}'", locator);
        return;
      }
      _InputSourceQueue.Enqueue(inputSource);

      if (_InternalState != InternalPlaybackState.Playing && _InternalState != InternalPlaybackState.Paused)
      {
        _PlaybackSession = PlaybackSession.Create(this, inputSource.OutputStream.Channels,
            inputSource.OutputStream.SampleRate, inputSource.OutputStream.IsPassThrough);

        _InternalState = InternalPlaybackState.Playing;
        FireStateReady();
      }
    }

    private void InternalStop()
    {
      if (_InternalState == InternalPlaybackState.Playing || _InternalState == InternalPlaybackState.Paused)
      {
        _PlaybackSession.End();
        _PlaybackSession = null;

        _InternalState = InternalPlaybackState.Stopped;
      }
    }

    protected void InternalPause()
    {
      if (_InternalState == InternalPlaybackState.Playing)
      {
        _OutputDeviceManager.StopDevice();
        _InternalState = InternalPlaybackState.Paused;
      }
    }

    private void InternalResume()
    {
      if (_InternalState == InternalPlaybackState.Paused)
      {
        _OutputDeviceManager.StartDevice(true);
        _InternalState = InternalPlaybackState.Playing;
      }
    }

    protected void InternalSetPosition(TimeSpan value)
    {
      if (_InternalState == InternalPlaybackState.Playing || _InternalState == InternalPlaybackState.Paused)
        _InputSourceSwitcher.CurrentInputSource.OutputStream.SetPosition(value);
    }

    protected void InternalSetVolume(int value)
    {
      _volume = value*100;
      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_MUSIC, _volume);
      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_SAMPLE, _volume);
      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, _volume);
    }

    protected void InternalSetMute(bool value)
    {
      if (value)
      {
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_MUSIC, 0);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_SAMPLE, 0);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, 0);
      }
      else
      {
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_MUSIC, _volume);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_SAMPLE, _volume);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, _volume);
      }
      _isMuted = value;
    }

    /// <summary>
    /// Request MP to give us the next track on the playlist.
    /// </summary>
    protected void RequestNextMediaItem()
    {
      if (_ExternalState == PlayerState.Active)
      {
        // Just make MP come up with the next item on its playlist. 
        _ExternalState = PlayerState.Ended;
        // FIXME: Go on here
      }
    }

    /// <summary>
    /// Take actions required when its time to crossfade.
    /// </summary>
    protected void StartCrossFade()
    {
      if (_PlaybackMode == PlaybackMode.CrossFading)
      {
        // Todo: tell _Player._InputSourceSwitcher to start crossfade.
      }
    }

    /// <summary>
    /// Main controller thread loop.
    /// </summary>
    /// <remarks>
    /// Each call to the BASS API must be done by the main controller thread. The reason is that the BASS library stores
    /// context data in a thread-local store: The current device is stored per thread. So to avoid the necessity of
    /// the initialization for each thread, we only use that single thread to communicate with the BASS API.
    /// </remarks>
    protected void WorkItemExecutorMain()
    {
      try
      {
        while (!_MainThreadAbortFlag)
        {
          WorkItem item = DequeueWorkItem();

          lock (_syncObj)
          {
            if (_MainThreadAbortFlag)
              // Has to be checked again inside the lock statement
              break;
            if (item == null)
              Monitor.Wait(_syncObj);
            else
              item.Invoke();
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Exception in controller mainthread", e);
      }
    }

    protected void FireStarted()
    {
      if (_started != null)
        _started(this);
    }

    protected void FireStateReady()
    {
      if (_stateReady != null)
        _stateReady(this);
    }

    protected void FireStopped()
    {
      if (_stopped != null)
        _stopped(this);
    }

    // TODO: To be called
    protected void FireEnded()
    {
      if (_ended != null)
        _ended(this);
    }

    protected void FirePlaybackStateChanged()
    {
      if (_playbackStateChanged != null)
        _playbackStateChanged(this);
    }

    /// <summary>
    /// Terminates and waits for the controller thread.
    /// </summary>
    public void TerminateThread()
    {
      if (_MainThread.IsAlive)
      {
        Log.Debug("Stopping controller thread");

        lock (_syncObj)
        {
          _MainThreadAbortFlag = true;
          Monitor.PulseAll(_syncObj);
        }
        _MainThread.Join();
      }
    }

    /// <summary>
    /// Enqueues a play workitem for the given mediaitem.
    /// </summary>
    /// <param name="locator"></param>
    /// <remarks>
    /// The workitem will actually be executed on the controller's mainthread.
    /// </remarks>
    public void SetMediaItemLocator(IResourceLocator locator)
    {
      if (_ExternalState != PlayerState.Stopped)
        Stop();
      EnqueueWorkItem(new WorkItem(new PlayWorkItemDelegate(InternalPlay), locator));
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
      get { return _ExternalState; }
    }

    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    public void SetMediaItemTitleHint(string title)
    {
      _mediaItemTitle = title;
    }

    /// <summary>
    /// Enqueues a stop workitem.
    /// </summary>
    /// <remarks>
    /// The workitem will actually be executed on the controller's mainthread.
    /// </remarks>
    public void Stop()
    {
      // Make sure nothing happens here if _ExternalState == PlaybackState.Ended!

      if (_ExternalState == PlayerState.Active &&
          (_InternalState == InternalPlaybackState.Playing || _InternalState == InternalPlaybackState.Paused))
      {
        EnqueueWorkItem(new WorkItem(new WorkItemDelegate(InternalStop)));
        _ExternalState = PlayerState.Stopped;
        FireStopped();
      }
    }

    #endregion

    #region IAudioPlayer implementation

    public bool Mute
    {
      get { return _isMuted; }
      set { EnqueueWorkItem(new WorkItem(new SetMuteWorkItemDelegate(InternalSetMute), value)); }
    }

    public int Volume
    {
      get { return _volume; }
      set { EnqueueWorkItem(new WorkItem(new SetVolumeWorkItemDelegate(InternalSetVolume), value)); }
    }

    #endregion

    #region IMediaPlaybackControl implementation

    public bool CanSeekForwards
    {
      get { return false; }
    }

    public bool CanSeekBackwards
    {
      get { return false; }
    }

    public void Pause()
    {
      if (_ExternalState == PlayerState.Active && _InternalState == InternalPlaybackState.Playing)
        EnqueueWorkItem(new WorkItem(new WorkItemDelegate(InternalPause)));
    }

    public void Resume()
    {
      if (_ExternalState != PlayerState.Active || _InternalState != InternalPlaybackState.Paused)
        return;
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(InternalResume)));
    }

    public void Restart()
    {
      EnqueueWorkItem(new WorkItem(new SetPositionWorkItemDelegate(InternalSetPosition), TimeSpan.Zero));
      FireStarted();
    }

    public TimeSpan CurrentTime
    {
      get
      {
        lock (_syncObj)
        {
          if (_ExternalState != PlayerState.Active || _InternalState != InternalPlaybackState.Playing && _InternalState != InternalPlaybackState.Paused)
            return TimeSpan.Zero;
          IInputSource currentInputSource = _InputSourceSwitcher.CurrentInputSource;
          if (currentInputSource == null)
            return TimeSpan.Zero;
          return currentInputSource.OutputStream.GetPosition();
        }
      }
      set { EnqueueWorkItem(new WorkItem(new SetPositionWorkItemDelegate(InternalSetPosition), value)); }
    }

    public TimeSpan Duration
    {
      get
      {
        lock (_syncObj)
        {
          if (_ExternalState != PlayerState.Active || _InternalState != InternalPlaybackState.Playing && _InternalState != InternalPlaybackState.Paused)
            return TimeSpan.Zero;
          IInputSource currentInputSource = _InputSourceSwitcher.CurrentInputSource;
          if (currentInputSource == null)
            return TimeSpan.Zero;
          return currentInputSource.OutputStream.Length;
        }
      }
    }

    // TODO: Playback rate control

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
      get { return _InternalState == InternalPlaybackState.Paused; }
    }

    #endregion

    #region IPlayerEvents implementation

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

    #region IDisposable Members

    public void Dispose()
    {
      Log.Debug("Disposing BassPlayer");

      TerminateThread();

      lock (_syncObj)
      {
        _OutputDeviceManager.Dispose();
        _PlaybackBuffer.Dispose();
        _WinAmpDSPProcessor.Dispose();
        _VSTProcessor.Dispose();
        _UpDownMixer.Dispose();
        _InputSourceSwitcher.Dispose();
        _InputSourceQueue.Dispose();
        _InputSourceFactory.Dispose();

        _BassLibraryManager.Dispose();
        _instance = null;
      }
    }

    #endregion

    #region Private members

    private BassPlayer()
    {
    }

    private void Initialize(string playerMainDirectory)
    {
      Log.Debug("Initializing BassPlayer");
      _BassLibraryManager = BassLibraryManager.Create(Path.Combine(playerMainDirectory, InternalSettings.PluginsPath));

      _Settings = ServiceScope.Get<ISettingsManager>().Load<BassPlayerSettings>();

      _InputSourceFactory = new InputSourceFactory(this);

      _InputSourceQueue = new InputSourceQueue();
      _InputSourceSwitcher = InputSourceSwitcher.Create(this);
      _UpDownMixer = UpDownMixer.Create(this);
      _VSTProcessor = VSTProcessor.Create(this);
      _WinAmpDSPProcessor = WinAmpDSPProcessor.Create(this);
      _PlaybackBuffer = PlaybackBuffer.Create(this);
      _OutputDeviceManager = OutputDeviceManager.Create(this);

      _ExternalState = PlayerState.Ended;
      _InternalState = InternalPlaybackState.Stopped;
      _MainThreadAbortFlag = false;

      _WorkItemQueue = new WorkItemQueue();

      _MainThread = new Thread(WorkItemExecutorMain) {Name = "Bass Player work item executor thread"};
      _MainThread.Start();

      Volume = 100;
    }

    #endregion
  }
}
