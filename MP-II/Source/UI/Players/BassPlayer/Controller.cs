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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Players;
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.PlayerComponents;
using Ui.Players.BassPlayer.Settings;
using Ui.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace Ui.Players.BassPlayer
{
  /// <summary>
  /// Central controller for the BASS player system.
  /// </summary>
  /// <remarks>
  /// <para>
  /// We separate the BASS player MediaPortal interface (class <see cref="BassPlayer"/>) from this controller class because
  /// the <see cref="BassPlayer"/> class provides several functions to the outside world which we don't want to mix
  /// with central controller functions. Furthermore, the interface class needs to track an external
  /// <see cref="PlayerState">playback state</see> which should be separated from the internal process and state of this
  /// controller class.
  /// </para>
  /// <para>
  /// This class manages the <see cref="PlaybackProcessor"/> which does the actual playback, the
  /// <see cref="OutputDeviceManager"/> which is responsible for managing output devices and provides the single player thread
  /// which is used to communicate with the underlaying BASS library.
  /// </para>
  /// <para>
  /// Multithreading:
  /// This class is multithreading safe. But all components of this class which make use of the BASS library will be used
  /// single-threaded. To achieve this, we use a work item queue where we put all to-be-executed work items. A player thread
  /// is responsible for executing work items from that queue.
  /// </para>
  /// </remarks>
  public class Controller : IDisposable
  {
    #region Delegates

    // Delegates used in conjunction with workitems.
    internal delegate void WorkItemDelegate();
    internal delegate void SetVolumeWorkItemDelegate(int value);
    internal delegate void SetMuteWorkItemDelegate(bool value);
    internal delegate void SetPositionWorkItemDelegate(TimeSpan value);
    internal delegate void PlayWorkItemDelegate(IInputSource inputSource, StartTime startTime);
    internal delegate void DisposeObjectDlgt(IInputSource inputSource);

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();
    protected volatile BassPlayer _player; // Not owned by this instance
    protected BassLibraryManager _bassLibraryManager;
    protected PlaybackProcessor _playbackProcessor;
    protected OutputDeviceManager _outputDeviceManager;

    protected volatile bool _isMuted = false;
    protected volatile int _volume = 100;

    protected Thread _playerThread;
    protected volatile bool _mainThreadTerminated;
    protected WorkItemQueue _workItemQueue;

    #endregion

    /// <summary>
    /// Initializes a new instance of the BASS player controller.
    /// </summary>
    /// <param name="player">BASS player instance which contains this controller instance.</param>
    /// <param name="playerMainDirectory">Directory where the BASS player is located. Plugins will be searched relative
    /// to this directory.</param>
    public Controller(BassPlayer player, string playerMainDirectory)
    {
      Log.Debug("Initializing BASS controller");
      _player = player;

      _bassLibraryManager = BassLibraryManager.Get(Path.Combine(playerMainDirectory, InternalSettings.PluginsPath));

      _playbackProcessor = new PlaybackProcessor(this);
      _outputDeviceManager = new OutputDeviceManager(this);

      _mainThreadTerminated = false;

      _workItemQueue = new WorkItemQueue();

      _playerThread = new Thread(PlayerThreadMain) {Name = "Bass Player work item executor thread"};
      _playerThread.Start();

      SetVolume_Async(100);
    }

    public void Dispose()
    {
      TerminateThread();

      lock (_syncObj)
      {
        _playbackProcessor.Dispose();
        _outputDeviceManager.Dispose();

        _bassLibraryManager.Dispose();
      }
    }

    #region Public members

    public object GlobalSyncObj
    {
      get { return _syncObj; }
    }

    public PlaybackProcessor PlaybackProcessor
    {
      get { return _playbackProcessor; }
    }

    public OutputDeviceManager OutputDeviceManager
    {
      get { return _outputDeviceManager; }
    }

    public bool IsPaused
    {
      get
      {
        lock (_syncObj)
          return _playbackProcessor.InternalState == InternalPlaybackState.Paused;
      }
    }

    public BassPlayer Player
    {
      get { return _player; }
    }

    public static BassPlayerSettings GetSettings()
    {
      return ServiceScope.Get<ISettingsManager>().Load<BassPlayerSettings>();
    }

    public static void SaveSettings(BassPlayerSettings settings)
    {
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    public WorkItem DequeueWorkItem()
    {
      lock (_syncObj)
        return _workItemQueue.Count == 0 ? null : _workItemQueue.Dequeue();
    }

    /// <summary>
    /// Enqueues a workitem and notifies the controller mainthread that something needs to be done.
    /// </summary>
    /// <param name="workItem">The workitem to enqueue.</param>
    /// <returns>The enqueued workitem.</returns>
    public void EnqueueWorkItem(WorkItem workItem)
    {
      lock (_syncObj)
      {
        _workItemQueue.Enqueue(workItem);
        Monitor.PulseAll(_syncObj);
      }
    }

    public void StateReady()
    {
      _player.FireStateReady();
    }

    public void Stop_Async()
    {
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(_playbackProcessor.Stop)));
    }

    public bool GetMuteState()
    {
      return _isMuted;
    }

    public void SetMuteState_Async(bool value)
    {
      EnqueueWorkItem(new WorkItem(new SetMuteWorkItemDelegate(BASS_SetMute), value));
    }

    public int GetVolume()
    {
      return _volume;
    }

    public void SetVolume_Async(int value)
    {
      EnqueueWorkItem(new WorkItem(new SetVolumeWorkItemDelegate(BASS_SetVolume), value));
    }

    public void Pause_Async()
    {
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(_playbackProcessor.Pause)));
    }

    public void Resume_Async()
    {
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(_playbackProcessor.Resume)));
    }

    public void Restart_Async()
    {
      EnqueueWorkItem(new WorkItem(new SetPositionWorkItemDelegate(_playbackProcessor.SetPosition), TimeSpan.Zero));
    }

    public TimeSpan GetCurrentPlayTime()
    {
      return _playbackProcessor.CurrentPosition;
    }

    public void SetCurrentPlayTime_Async(TimeSpan value)
    {
      EnqueueWorkItem(new WorkItem(new SetPositionWorkItemDelegate(_playbackProcessor.SetPosition), value));
    }

    public TimeSpan GetDuration()
    {
      BassStream outputStream = _playbackProcessor.OutputStream;
      if (outputStream == null)
        return TimeSpan.Zero;
      return outputStream.Length;
    }

    /// <summary>
    /// Enqueues the given input source for playback and starts playback if nessecary.
    /// </summary>
    /// <param name="inputSource">The input source for the media item to play.</param>
    /// <param name="startTime">Time to start the new media item.</param>
    public void MoveToNextItem_Async(IInputSource inputSource, StartTime startTime)
    {
      Log.Info("Preparing for playback: '{0}'", inputSource);
      _playbackProcessor.EnqueueInputSource(inputSource);
      if (startTime == StartTime.AtOnce)
        _playbackProcessor.ScheduleMoveToNextInputSource();
      else
        _playbackProcessor.ScheduleNextInputSourceAvailable();
    }

    /// <summary>
    /// Make MP provide the next music item.
    /// </summary>
    public void RequestNextMediaItem_Async()
    {
      BassPlayer player = _player;
      if (player == null)
        return;
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(player.RequestNextMediaItemFromSystem)));
    }

    /// <summary>
    /// Update external state and tell MP that the current media item ended.
    /// </summary>
    public void PlaybackEnded_Async()
    {
      BassPlayer player = _player;
      if (player == null)
        return;
      EnqueueWorkItem(new WorkItem(new WorkItemDelegate(player.PlaybackEnded)));
    }

    /// <summary>
    /// Schedules to dispose the given object asynchronously.
    /// </summary>
    /// <param name="obj">The object to dispose. May be null.</param>
    public void ScheduleDisposeObject_Async(IDisposable obj)
    {
      EnqueueWorkItem(new WorkItem(new DisposeObjectDlgt(DisposeObject), obj));
    }

    #endregion

    #region Protected methods

    protected static void DisposeObject(IDisposable obj)
    {
      if (obj != null)
        obj.Dispose();
    }

    protected void BASS_SetVolume(int value)
    {
      Log.Debug("Setting global BASS player volume to {0}", value);
      // value is from 0 to 100 in a linear scale, internal _volume is from 0 to 10000 in a linear scale
      _volume = value*100;
      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_MUSIC, _volume);
      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_SAMPLE, _volume);
      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, _volume);
    }

    protected void BASS_SetMute(bool value)
    {
      Log.Debug("Setting global BASS player mute state to {0}", value);
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
    /// Main player thread loop.
    /// </summary>
    /// <remarks>
    /// Each call to the BASS API must be done by the main player thread. The reason is that the BASS library stores
    /// context data in a thread-local store: The current device is stored per thread. So to avoid the necessity of
    /// the data initialization and maintainance for each thread, we only use that single thread to communicate with the
    /// BASS API.
    /// </remarks>
    protected void PlayerThreadMain()
    {
      try
      {
        while (!_mainThreadTerminated)
        {
          WorkItem item = DequeueWorkItem();

          lock (_syncObj)
          {
            if (_mainThreadTerminated)
              // Has to be checked again inside the lock statement
              break;
            if (item == null)
              Monitor.Wait(_syncObj);
          }
          if (item != null) // Must be done outside lock
            item.Invoke();
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Exception in player main thread", e);
      }
    }

    /// <summary>
    /// Terminates and waits for the controller thread.
    /// </summary>
    protected void TerminateThread()
    {
      if (!_playerThread.IsAlive)
        return;
      Log.Debug("Stopping player main thread");

      lock (_syncObj)
      {
        _mainThreadTerminated = true;
        Monitor.PulseAll(_syncObj);
      }
      _playerThread.Join();
    }

    #endregion
  }
}
