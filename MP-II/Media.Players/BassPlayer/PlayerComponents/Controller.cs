#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Media.MediaManager;
using MediaPortal.Presentation.Players;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    /// <summary>
    /// Player controller.
    /// </summary>
    partial class Controller : IDisposable
    {
      #region Static members

      /// <summary>
      /// Creates and initializes an new instance.
      /// </summary>
      /// <param name="player">Reference to containing IPlayer object.</param>
      /// <returns>The new instance.</returns>
      public static Controller Create(BassPlayer player)
      {
        Controller controller = new Controller(player);
        controller.Initialize();
        return controller;
      }

      #endregion

      #region Fields

      // Reference to the containin IPlayer object.
      private BassPlayer _Player;

      // The current external playback state.
      private PlaybackState _ExternalState;

      // The current internal playback state.
      private InternalPlayBackState _InternalState;

      // The current playback mode.
      private PlaybackMode _PlaybackMode;

      // Mainthread that handles commands.
      private Thread _MainThread;
      private bool _MainThreadAbortFlag;
      private AutoResetEvent _MainThreadNotify;

      // Command queue
      private Queue<ControllerCommand> _CommandQueue;

      #endregion

      #region Delegates

      // Delegates used in conjunction with commands.
      private delegate void PlayDelegate(IMediaItem mediaItem);
      private delegate void SeekDelegate(TimeSpan interval);
      private delegate void CommandDelegate();

      #endregion

      #region Public members

      /// <summary>
      /// Returns the current external playback state. 
      /// </summary>
      /// <remarks>
      /// Because the player operates asynchronous, its internal state 
      /// can differ from its external state as seen by MP.
      /// </remarks>
      public PlaybackState ExternalState
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
      public InternalPlayBackState InternalState
      {
        get { return _InternalState; }
      }

      /// <summary>
      /// Gets the current playbackmode.
      /// </summary>
      public PlaybackMode PlaybackMode
      {
        get { return _PlaybackMode; }
      }

      /// <summary>
      /// Enqueues a play command for the given mediaitem.
      /// </summary>
      /// <param name="mediaItem"></param>
      /// <remarks>
      /// The command will actually be executed on the controller's mainthread.
      /// </remarks>
      public void Play(IMediaItem mediaItem)
      {
        if (_ExternalState == PlaybackState.Stopped || _ExternalState == PlaybackState.Ended)
        {
          EnQueueCommand(new ControllerCommand(new PlayDelegate(InternalPlay), mediaItem));

          _ExternalState = PlaybackState.Playing;
          SendMPMessage(MPMessages.Started);
        }
      }

      /// <summary>
      /// Enqueues a pause command.
      /// </summary>
      /// <remarks>
      /// The command will actually be executed on the controller's mainthread.
      /// </remarks>
      public void Pause()
      {
        if (_ExternalState == PlaybackState.Playing)
        {
          EnQueueCommand(new ControllerCommand(new CommandDelegate(InternalPause)));
          _ExternalState = PlaybackState.Paused;
        }
      }

      /// <summary>
      /// Enqueues a resume command.
      /// </summary>
      /// <remarks>
      /// The command will actually be executed on the controller's mainthread.
      /// </remarks>
      public void Resume()
      {
        if (_ExternalState == PlaybackState.Paused)
        {
          EnQueueCommand(new ControllerCommand(new CommandDelegate(InternalResume)));
          _ExternalState = PlaybackState.Playing;
        }
      }

      /// <summary>
      /// Enqueues a stop command.
      /// </summary>
      /// <remarks>
      /// The command will actually be executed on the controller's mainthread.
      /// </remarks>
      public void Stop()
      {
        // Make sure nothing happens here if _ExternalState == PlaybackState.Ended!

        if (_ExternalState == PlaybackState.Playing || _ExternalState == PlaybackState.Paused)
        {
          EnQueueCommand(new ControllerCommand(new CommandDelegate(InternalStop)));
          _ExternalState = PlaybackState.Stopped;
        }
      }

      public void SetPosition(TimeSpan time)
      {
        if (_ExternalState == PlaybackState.Playing)
        {
          EnQueueCommand(new ControllerCommand(new SeekDelegate(InternalSetPosition), time));
        }
      }

      /// <summary>
      /// Enqueues a command to change the current playbackmode.
      /// </summary>
      /// <remarks>
      /// The command will actually be executed on the controller's mainthread.
      /// </remarks>
      public void TogglePlayBackMode()
      {
        ControllerCommand command = EnQueueCommand(new ControllerCommand(new CommandDelegate(InternalTogglePlayBackMode)));
        command.WaitHandle.WaitOne();
      }

      /// <summary>
      /// Handle the event when a new mediaitem is required to prepare crossfading or gapless playback.
      /// </summary>
      public void HandleNextMediaItemSyncPoint()
      {
        EnQueueCommand(new ControllerCommand(new CommandDelegate(RequestNextMediaItem)));
      }

      /// <summary>
      /// Start crossfading.
      /// </summary>
      public void HandleCrossFadeSyncPoint()
      {
        EnQueueCommand(new ControllerCommand(new CommandDelegate(StartCrossFade)));
      }

      /// <summary>
      /// Handle the event when all data has been played.
      /// </summary>
      public void HandleOutputStreamEnded()
      {
        // When we get here an ended msg is already send to MP (through HandleNextMediaItemSyncPoint).
        // All we have to do is stop playback that only runs in the background.
        EnQueueCommand(new ControllerCommand(new CommandDelegate(InternalStop)));
      }

      #endregion

      #region Private members

      private Controller(BassPlayer player)
      {
        _Player = player;
      }

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      private void Initialize()
      {
        _ExternalState = PlaybackState.Ended;
        _InternalState = InternalPlayBackState.Stopped;
        _MainThreadAbortFlag = false;

        _CommandQueue = new Queue<ControllerCommand>();

        _MainThreadNotify = new AutoResetEvent(false);
        _MainThread = new Thread(new ThreadStart(ThreadMain));
        _MainThread.SetApartmentState(ApartmentState.STA);
        _MainThread.Start();
      }

      /// <summary>
      /// Enqueues a command and notifies the controller mainthread something needs to be done.
      /// </summary>
      /// <param name="command">The command to enqueue.</param>
      /// <returns>The enqueued command.</returns>
      private ControllerCommand EnQueueCommand(ControllerCommand command)
      {
        _CommandQueue.Enqueue(command);
        _MainThreadNotify.Set();

        return command;
      }

      /// <summary>
      /// Enqueues the given mediaitem for playback and starts playback if nessecary.
      /// </summary>
      /// <param name="mediaItem">The mediaitem to play.</param>
      private void InternalPlay(IMediaItem mediaItem)
      {
        Log.Info("Preparing for playback: \"{0}\"", mediaItem.ContentUri);
        IInputSource inputSource = _Player._InputSourceFactory.CreateInputSource(mediaItem);
        _Player._InputSourceQueue.Enqueue(inputSource);

        if (_InternalState != InternalPlayBackState.Playing && _InternalState != InternalPlayBackState.Paused)
        {
          _Player._PlaybackSession = PlaybackSession.Create(_Player, inputSource.OutputStream.Channels, inputSource.OutputStream.SamplingRate, inputSource.OutputStream.IsPassThrough);

          _InternalState = InternalPlayBackState.Playing;
        }
      }

      /// <summary>
      /// Pauses playback.
      /// </summary>
      private void InternalPause()
      {
        if (_InternalState == InternalPlayBackState.Playing)
        {
          _Player._OutputDeviceManager.StopDevice();
          _InternalState = InternalPlayBackState.Paused;
        }
      }

      /// <summary>
      /// Resumes playback from pause.
      /// </summary>
      private void InternalResume()
      {
        if (_InternalState == InternalPlayBackState.Paused)
        {
          _Player._OutputDeviceManager.StartDevice(true);
          _InternalState = InternalPlayBackState.Playing;
        }
      }

      /// <summary>
      /// Stops playback.
      /// </summary>
      private void InternalStop()
      {
        if (_InternalState == InternalPlayBackState.Playing || _InternalState == InternalPlayBackState.Paused)
        {
          _Player._PlaybackSession.End();
          _Player._PlaybackSession = null;

          _InternalState = InternalPlayBackState.Stopped;
        }
      }

      /// <summary>
      /// Sets playback position to the given value.
      /// </summary>
      /// <param name="time"></param>
      private void InternalSetPosition(TimeSpan time)
      {
        if (_InternalState == InternalPlayBackState.Playing)
        {
        }
      }

      /// <summary>
      /// Toggles the current playback mode.
      /// </summary>
      private void InternalTogglePlayBackMode()
      {
      }

      /// <summary>
      /// Request MP to give us the next track on the playlist.
      /// </summary>
      private void RequestNextMediaItem()
      {
        if (_ExternalState == PlaybackState.Playing)
        {
          // Just make MP come up with the next item on its playlist. 
          _ExternalState = PlaybackState.Ended;
          SendMPMessage(MPMessages.NextFile);
        }
      }

      /// <summary>
      /// Take actions required when its time to crossfade.
      /// </summary>
      private void StartCrossFade()
      {
        if (_PlaybackMode == PlaybackMode.CrossFading)
        {
          // Todo: tell _Player._InputSourceSwitcher to start crossfade.
        }
      }

      /// <summary>
      /// Sends a message to MP with the given action.
      /// </summary>
      private void SendMPMessage(string action)
      {
        Log.Debug("Sending message \"{0}\"", action);

        QueueMessage msg = new QueueMessage();
        msg.MessageData["player"] = this;
        msg.MessageData["action"] = action;

        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players-internal");
        queue.Send(msg);
      }

      /// <summary>
      /// Main controllerthread loop.
      /// </summary>
      private void ThreadMain()
      {
        try
        {
          while (!_MainThreadAbortFlag)
          {
            if (_CommandQueue.Count == 0)
              _MainThreadNotify.WaitOne();

            if (_CommandQueue.Count > 0)
            {
              ControllerCommand command = _CommandQueue.Dequeue();
              command.Invoke();
            }
          }
        }
        catch (Exception e)
        {
          throw new BassPlayerException("Exception in controller mainthread.", e);
        }
      }

      /// <summary>
      /// Terminates and waits for the controller thread.
      /// </summary>
      public void TerminateThread()
      {
        if (_MainThread.IsAlive)
        {
          Log.Debug("Stopping controller thread.");

          _MainThreadAbortFlag = true;
          _MainThreadNotify.Set();
          _MainThread.Join();
        }
      }

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
        Log.Debug("Controller.Dispose()");
        
        TerminateThread();
      }

      #endregion
    }
  }
}
