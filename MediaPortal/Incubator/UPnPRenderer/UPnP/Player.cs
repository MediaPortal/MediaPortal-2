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
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UPnPRenderer.MediaItems;
using MediaPortal.UPnPRenderer.Players;

namespace MediaPortal.UPnPRenderer.UPnP
{
  public class Player
  {
    #region const

    private const int TIMER_INTERVAL = 500; // ms

    #endregion const

    #region local vars

    private ContentType _playerType = ContentType.Unknown;
    private bool _isPaused = false;
    private readonly Timer _timer = new Timer(TIMER_INTERVAL);
    private readonly UPnPRenderingControlServiceImpl _controlServiceImpl = UPnPRendererPlugin.UPnPServer.UpnPDevice.UPnPRenderingControlServiceImpl;
    private readonly UPnPAVTransportServiceImpl _transportServiceImpl = UPnPRendererPlugin.UPnPServer.UpnPDevice.UPnPAVTransportServiceImpl;


    #endregion local vars

    public Player()
    {
      // subscribe to UPnPAVTransportServiceImpl events
      UPnPAVTransportServiceImpl.Play += OnPlay;
      UPnPAVTransportServiceImpl.Pause += OnPause;
      UPnPAVTransportServiceImpl.Stop += OnStop;
      UPnPAVTransportServiceImpl.Seek += OnSeek;
      UPnPAVTransportServiceImpl.SetAVTransportURI += OnSetAvTransportUri;

      // subscribe to UPnPRendeeringControlServiceImpl events
      UPnPRenderingControlServiceImpl.VolumeEvent += OnVolume;

      // timer to update the progress information
      _timer.Elapsed += TimerElapsed;
    }

    #region UPnPAVTransportServiceImpl events

    private void OnPlay()
    {
      TraceLogger.WriteLine("Event Fired! - Play -- ");
      VolumeChanged();

      var avTransportUri = _transportServiceImpl.StateVariables["AVTransportURI"].Value.ToString();
      var avTransportUriMetadata = _transportServiceImpl.StateVariables["AVTransportURIMetaData"].Value.ToString();
      switch (_playerType)
      {
        case ContentType.Audio:
          if (_isPaused)
          {
            ChangeUPnPAVTransportServiceStateToPlaying();
            ResumePlayer<UPnPRendererAudioPlayer>();
            break;
          }

          StopPlayer<UPnPRendererAudioPlayer>();

          var audioItem = UPnPMediaItemFactory.CreateAudioItem(avTransportUri);
          audioItem.AddMetaDataToMediaItem(avTransportUriMetadata);
          PlayItemsModel.CheckQueryPlayAction(audioItem);
          break;
        case ContentType.Image:
          var imageItem = UPnPMediaItemFactory.CreateImageItem(avTransportUri);
          imageItem.AddMetaDataToMediaItem(avTransportUriMetadata);

          var ic = GetPlayerContext<UPnPRendererImagePlayer>();
          if (ic != null)
            ic.DoPlay(imageItem);
          else
            PlayItemsModel.CheckQueryPlayAction(imageItem);
          break;
        case ContentType.Video:
          if (_isPaused)
          {
            Logger.Debug("Resume!!");
            ChangeUPnPAVTransportServiceStateToPlaying();
            ResumePlayer<UPnPRendererVideoPlayer>();
            break;
          }
          Logger.Debug("NO Resume!!");

          StopPlayer<UPnPRendererVideoPlayer>();

          var videoItem = UPnPMediaItemFactory.CreateVideoItem(avTransportUri);
          videoItem.AddMetaDataToMediaItem(avTransportUriMetadata);
          PlayItemsModel.CheckQueryPlayAction(videoItem);
          break;
        case ContentType.Unknown:
          Logger.Warn("Can't play because of unknown player type");
          return; // we don't want to start the timer
      }

      _timer.Enabled = true;
      _timer.AutoReset = true;
    }

    private void OnPause()
    {
      Logger.Debug("Event Fired! - Pause -- ");

      switch (_playerType)
      {
        case ContentType.Audio:
          PausePlayer<UPnPRendererAudioPlayer>();
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          PausePlayer<UPnPRendererVideoPlayer>();
          break;
        case ContentType.Unknown:
          break;
      }

      _isPaused = true;
      _timer.Enabled = false;
    }

    private void OnStop()
    {
      Logger.Debug("Event Fired! - Stop -- ");

      switch (_playerType)
      {
        case ContentType.Audio:
          StopPlayer<UPnPRendererAudioPlayer>();
          break;
        case ContentType.Image:
          StopPlayer<UPnPRendererImagePlayer>();
          break;
        case ContentType.Video:
          StopPlayer<UPnPRendererVideoPlayer>();
          break;
        case ContentType.Unknown:
          break;
      }

      _isPaused = false;
      _timer.Enabled = false;
      string elapsedTime = TimeSpan.FromSeconds(0).ToString();

      _transportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState",
        "AbsoluteTimePosition",
        "RelativeTimePosition"
      }, new List<object>
      {
        "STOPPED",
        elapsedTime,
        elapsedTime
      });
    }

    /// <summary>
    /// In the UPnP Context the term "relative" means something else compared to the MP context:
    /// "This state variable ["RelativeTimePosition"] contains the current position, in terms of time, from the beginning of the current track"
    /// That means if a control point wants to seek relative, we have to seek absolute in MP.
    /// </summary>
    private void OnSeek()
    {
      Logger.Debug("Event Fired! - Seek -- ");
      //TraceLogger.WriteLine("--" + _transportServiceImpl.StateVariables["A_ARG_TYPE_SeekTarget"].Value.ToString());
      string[] relTime = _transportServiceImpl.StateVariables["A_ARG_TYPE_SeekTarget"].Value.ToString().Split(':');
      //TraceLogger.WriteLine(string.Join(", ", relTime));
      var timespan = new TimeSpan(Int32.Parse(relTime[0]), Int32.Parse(relTime[1]), Int32.Parse(relTime[2]));

      switch (_playerType)
      {
        case ContentType.Audio:
          GetPlayer<UPnPRendererAudioPlayer>().CurrentTime = timespan;
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          GetPlayer<UPnPRendererVideoPlayer>().CurrentTime = timespan;
          break;
        case ContentType.Unknown:
          break;
      }
    }

    private void OnSetAvTransportUri(OnEventSetAVTransportURIEventArgs e)
    {
      Logger.Debug("Set Uri Event fired");
      Logger.Debug("CurrentURI " + e.CurrentURI);
      Logger.Debug("CurrentURIMetaData " + e.CurrentURIMetaData);

      //Logger.Debug("MimeType: {0}", Utils.GetMimeFromUrl(e.CurrentURI, e.CurrentURIMetaData));
      _playerType = Utils.GetContentTypeFromUrl(e.CurrentURI, e.CurrentURIMetaData);

      switch (_playerType)
      {
        case ContentType.Audio:
          break;
        case ContentType.Image:
          var imageItem = UPnPMediaItemFactory.CreateImageItem(e.CurrentURI);
          imageItem.AddMetaDataToMediaItem(e.CurrentURIMetaData);

          var ic = GetPlayerContext<UPnPRendererImagePlayer>();
          if (ic != null)
            ic.DoPlay(imageItem);
          else
            PlayItemsModel.CheckQueryPlayAction(imageItem);
          break;
        case ContentType.Video:
          break;
        case ContentType.Unknown:
          break;
      }
    }

    #endregion UPnPAVTransportServiceImpl events

    #region UPnPRenderingControlServiceImpl events

    private void OnVolume(OnEvenSetVolumeEventArgs e)
    {
      ServiceRegistration.Get<IPlayerManager>().Volume = Convert.ToInt32(e.Volume);
      //TraceLogger.WriteLine("Volume set: " + e.Volume.ToString());
      //TraceLogger.WriteLine("Wolume is: " + ServiceRegistration.Get<IPlayerManager>().Volume);
    }

    #endregion UPnPRenderingControlServiceImpl events


    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
      if (_timer == null)
      {
        Logger.Debug("timer is null");
        return;
      }
      string elapsedTime = "00:00:00";
      string duration = "00:00:00";

      switch (_playerType)
      {
        case ContentType.Audio:
          UpdateProgress<UPnPRendererAudioPlayer>(ref elapsedTime, ref duration);
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          UpdateProgress<UPnPRendererVideoPlayer>(ref elapsedTime, ref duration);
          break;
        case ContentType.Unknown:
          break;
      }

      _transportServiceImpl.ChangeStateVariables(new List<string>
      {
        "AbsoluteTimePosition",
        "RelativeTimePosition",
        "CurrentTrackDuration"

      }, new List<object>
      {
        elapsedTime,
        elapsedTime,
        duration
      });
    }

    public void UpdateProgress<TE>(ref string elapsedTime, ref string duration)
      where TE : IPlayer, IMediaPlaybackControl
    {
      var playerCtx = GetPlayerContext<TE>();
      if (playerCtx != null)
      {
        var player = GetPlayer<TE>();
        if (player.State == PlayerState.Ended)
        {
          Logger.Debug("Playback ended");
          Stop();
          return;
        }
        elapsedTime = player.CurrentTime.ToString(@"hh\:mm\:ss");
        duration = player.Duration.ToString(@"hh\:mm\:ss");
      }
      else
      {
        TraceLogger.WriteLine("PlayerContext null");
      }

    }
    #region PlayerMessages

    public void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        // React to player changes
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType)message.MessageType;
        IPlayerSlotController psc;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerResumeState:
            Logger.Debug("Player Resume");
            //Resume();
            break;
          case PlayerManagerMessaging.MessageType.PlaybackStateChanged:
            Logger.Debug("Player PlaybackStateChanged");
            psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            IMediaPlaybackControl mpc = psc.CurrentPlayer as IMediaPlaybackControl;

            if (mpc != null && mpc.IsPaused)
              Pause();
            else
              Resume();

            break;
          case PlayerManagerMessaging.MessageType.PlayerError:
            Logger.Error("Player Error");
            break;
          case PlayerManagerMessaging.MessageType.PlayerEnded:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            Logger.Debug("Player Stopped or Ended");
            Stop();
            break;
          case PlayerManagerMessaging.MessageType.PlayerStarted:
            Logger.Debug("Player Started");
            break;
          case PlayerManagerMessaging.MessageType.VolumeChanged:
            Logger.Debug("Volume changed");
            VolumeChanged();
            break;
        }
      }
    }

    #endregion PlayerMessages

    #region handle player messages

    private void VolumeChanged()
    {
      TraceLogger.WriteLine("Player Message volume changed");
      _controlServiceImpl.ChangeStateVariables(new List<string>
      {
        "Volume"
      },
      new List<object>
      {
        (UInt16)ServiceRegistration.Get<IPlayerManager>().Volume
      });
    }

    private void Stop()
    {
      OnStop();
    }

    private void Pause()
    {
      _transportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState"
      },
      new List<object>
      {
        "PAUSED_PLAYBACK"
      });

      _isPaused = true;
      _timer.Enabled = false;
    }

    private void Resume()
    {
      _transportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState"
      },
      new List<object>
      {
        "PLAYING"
      });

      _isPaused = false;
      _timer.Enabled = true;
    }

    #endregion handle player messages

    #region Utils

    private void ChangeUPnPAVTransportServiceStateToPlaying()
    {
      _transportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState"
      },
      new List<object>
      {
        "PLAYING"
      });

      _isPaused = false;
      _timer.Enabled = true;
    }

    T GetPlayer<T>()
    {
      var context = GetPlayerContext<T>();
      if (context != null)
        return (T)context.CurrentPlayer;
      return default(T);
    }

    IPlayerContext GetPlayerContext<T>()
    {
      var contexts = ServiceRegistration.Get<IPlayerContextManager>().PlayerContexts;
      return contexts.FirstOrDefault(vc => vc.CurrentPlayer is T);
    }

    void PausePlayer<T>()
    {
      IPlayerContext context = GetPlayerContext<T>();
      if (context != null)
        context.Pause();
    }

    void StopPlayer<T>()
    {
      IPlayerContext context = GetPlayerContext<T>();
      if (context != null)
        context.Stop();
    }

    void ResumePlayer<T>()
    {
      IPlayerContext context = GetPlayerContext<T>();
      if (context != null)
        context.Play();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    /*void cleanupAudioPlayback()
    {
      if (isAudioBuffering)
      {
        ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
        isAudioBuffering = false;
      }
      restoreVolume();
      isAudioPlaying = false;
    }

    void cleanupVideoPlayback()
    {
      if (hlsParser != null)
      {
        ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
        hlsParser = null;
      }
      if (proxy != null)
      {
        proxy.Stop();
        proxy = null;
      }

      restoreVolume();
      currentVideoSessionId = null;
      currentVideoUrl = null;
    }

    void restoreVolume()
    {
      lock (volumeSync)
      {
        if (savedVolume != null)
        {
          ServiceRegistration.Get<IPlayerManager>().Volume = (int)savedVolume;
          savedVolume = null;
        }
      }
    }*/

    #endregion
  }
}
