using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Extensions.UPnPRenderer.MediaItems;
using MediaPortal.Common;
using MediaPortal.Extensions.UPnPRenderer.Players;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Models;
using UPnPRenderer.UPnP;

namespace MediaPortal.Extensions.UPnPRenderer
{
  class Player
  {

    #region const

    private const int TIMER_INTERVAL = 500; // ms

    #endregion const

    #region local vars

    private ContentType _playerType = ContentType.Unknown;
    private bool _isPaused = false;
    private byte[] _imageData;
    private static Timer _timer = new Timer(TIMER_INTERVAL);
    private readonly UPnPRenderingControlServiceImpl _UPnPRenderingControlServiceImpl = UPnP_RendererMain._upnpServer.UpnpDevice.UPnPRenderingControlServiceImpl;
    private readonly UPnPAVTransportServiceImpl _UPnPAVTransportServiceImpl = UPnP_RendererMain._upnpServer.UpnpDevice.UPnPAVTransportServiceImpl;
    

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
    }

    #region UPnPAVTransportServiceImpl events

    private void OnPlay()
    {
      Console.WriteLine("Event Fired! - Play -- ");
      VolumeChanged();

      switch (_playerType)
      {
        case ContentType.Audio:
          if (_isPaused)
          {
            changeUPnPAVTransportServiceStateToPlaying();
            resumePlayer<UPnPRendererAudioPlayer>();
            break;
          }

          stopPlayer<UPnPRendererAudioPlayer>();

          AudioItem audioItem = new AudioItem(_UPnPAVTransportServiceImpl.StateVariables["AVTransportURI"].Value.ToString());
          utils.addMetaDataToMediaItem(ref audioItem, _UPnPAVTransportServiceImpl.StateVariables["AVTransportURIMetaData"].Value.ToString());

          PlayItemsModel.CheckQueryPlayAction(audioItem);
          break;
        case ContentType.Image:
          ImageItem item = new ImageItem(Guid.NewGuid().ToString(), _imageData);
          utils.addMetaDataToMediaItem(ref item, _UPnPAVTransportServiceImpl.StateVariables["AVTransportURIMetaData"].Value.ToString());

          var ic = getPlayerContext<UPnPRendererImagePlayer>();
            if (ic != null)
                ic.DoPlay(item);
            else
                PlayItemsModel.CheckQueryPlayAction(item);
          break;
        case ContentType.Video:
          if (_isPaused)
          {
            Logger.Debug("Resume!!");
            changeUPnPAVTransportServiceStateToPlaying();
            resumePlayer<UPnPRendererVideoPlayer>();
            break;
          }
          Logger.Debug("NO Resume!!");

          stopPlayer<UPnPRendererVideoPlayer>();

          VideoItem videoItem = new VideoItem(_UPnPAVTransportServiceImpl.StateVariables["AVTransportURI"].Value.ToString());
          utils.addMetaDataToMediaItem(ref videoItem, _UPnPAVTransportServiceImpl.StateVariables["AVTransportURIMetaData"].Value.ToString());

          PlayItemsModel.CheckQueryPlayAction(videoItem);
          break;
        case ContentType.Unknown:
          Logger.Warn("Can't play because of unknown player type");
          return; // we don't want to start the timer
      }
      
      // timer to update the progress information
      //_timer = new Timer(TIMER_INTERVAL);
      _timer.Elapsed += (sender, e) => _timer_Elapsed();
      _timer.Enabled = true;
      _timer.AutoReset = true;
    }

    private void OnPause()
    {
      Logger.Debug("Event Fired! - Pause -- ");

      switch (_playerType)
      {
        case ContentType.Audio:
          pausePlayer<UPnPRendererAudioPlayer>();
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          pausePlayer<UPnPRendererVideoPlayer>();
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
          stopPlayer<UPnPRendererAudioPlayer>();
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          stopPlayer<UPnPRendererVideoPlayer>();
          break;
        case ContentType.Unknown:
          break;
      }

      _isPaused = false;
      _timer.Enabled = false;
      string elapsedTime = TimeSpan.FromSeconds(0).ToString();

      _UPnPAVTransportServiceImpl.ChangeStateVariables(new List<string>
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
      Console.WriteLine("--" + _UPnPAVTransportServiceImpl.StateVariables["A_ARG_TYPE_SeekTarget"].Value.ToString());
      string[] relTime = _UPnPAVTransportServiceImpl.StateVariables["A_ARG_TYPE_SeekTarget"].Value.ToString().Split(':');
      Console.WriteLine(string.Join(", ", relTime));
      var timespan = new TimeSpan(Int32.Parse(relTime[0]), Int32.Parse(relTime[1]), Int32.Parse(relTime[2]));
      
      switch (_playerType)
      {
        case ContentType.Audio:
          getPlayer<UPnPRendererAudioPlayer>().CurrentTime = timespan;
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          getPlayer<UPnPRendererVideoPlayer>().CurrentTime = timespan;
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

      Logger.Debug("MimeType: {0}", utils.GetMimeFromUrl(e.CurrentURI.ToString(), e.CurrentURIMetaData.ToString()));
      _playerType = utils.GetContentTypeFromUrl(e.CurrentURI.ToString(), e.CurrentURIMetaData.ToString());

      switch (_playerType)
      {
        case ContentType.Audio:
          break;
        case ContentType.Image:
          _imageData = utils.downloadImage(e.CurrentURI.ToString());
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
      Console.WriteLine("Volume set: " + e.Volume.ToString());
      Console.WriteLine("Wolume is: " + ServiceRegistration.Get<IPlayerManager>().Volume);
    }

    #endregion UPnPRenderingControlServiceImpl events

    void _timer_Elapsed()
    {
      if (_timer == null)
      {
        Logger.Debug("timer is null");
        return;
      }
      string elapsedTime = "00:00:00";
      string duration = "00:00:00";

      IPlayerContext UPnPPlayerCtx;

      switch (_playerType)
      {
        case ContentType.Audio:
          var audioContexts = ServiceRegistration.Get<IPlayerContextManager>().GetPlayerContextsByAVType(AVType.Audio);
          UPnPPlayerCtx = audioContexts.FirstOrDefault(vc => vc.CurrentPlayer is UPnPRendererAudioPlayer);
          if (UPnPPlayerCtx != null)
          {
            if (getPlayer<UPnPRendererAudioPlayer>().State == PlayerState.Ended)
            {
              Logger.Debug("Playback ended");
              Stop();
              return;
            }
            elapsedTime = getPlayer<UPnPRendererAudioPlayer>().CurrentTime.ToString(@"hh\:mm\:ss");
            duration = getPlayer<UPnPRendererAudioPlayer>().Duration.ToString(@"hh\:mm\:ss");
          }
          else
          {
            Console.WriteLine("PlayerContext null");
          }
          break;
        case ContentType.Image:
          break;
        case ContentType.Video:
          var videoContexts = ServiceRegistration.Get<IPlayerContextManager>().GetPlayerContextsByAVType(AVType.Video);
          UPnPPlayerCtx = videoContexts.FirstOrDefault(vc => vc.CurrentPlayer is UPnPRendererVideoPlayer);
          if (UPnPPlayerCtx != null)
          {
            if (getPlayer<UPnPRendererVideoPlayer>().State == PlayerState.Ended)
            {
              Console.WriteLine("Playback ended");
              Stop();
              return;
            }
            elapsedTime = getPlayer<UPnPRendererVideoPlayer>().CurrentTime.ToString(@"hh\:mm\:ss");
            duration = getPlayer<UPnPRendererVideoPlayer>().Duration.ToString(@"hh\:mm\:ss");
          }
          else
          {
            Console.WriteLine("PlayerContext null");
          }
          break;
        case ContentType.Unknown:
          break;
      }

      _UPnPAVTransportServiceImpl.ChangeStateVariables(new List<string>
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
     Console.WriteLine("Player Message volume changed");
      _UPnPRenderingControlServiceImpl.ChangeStateVariables(new List<string>
      {
        "Volume"

      }, new List<object>
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
      _UPnPAVTransportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState"
      }, new List<object>
      {
        "PAUSED_PLAYBACK"
      });

      _isPaused = true;
      _timer.Enabled = false;
    }

    private void Resume()
    {
      _UPnPAVTransportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState"
      }, new List<object>
      {
        "PLAYING"
      });

      _isPaused = false;
      _timer.Enabled = true;
    }

    #endregion handle player messages

    #region Utils

    private void changeUPnPAVTransportServiceStateToPlaying()
    {
      _UPnPAVTransportServiceImpl.ChangeStateVariables(new List<string>
      {
        "TransportState"
      }, new List<object>
      {
        "PLAYING"
      });

      _isPaused = false;
      _timer.Enabled = true;
    }
    
    T getPlayer<T>()
    {
      var context = getPlayerContext<T>();
      if (context != null)
        return (T)context.CurrentPlayer;
      return default(T);
    }

    IPlayerContext getPlayerContext<T>()
    {
      var contexts = ServiceRegistration.Get<IPlayerContextManager>().PlayerContexts;
      return contexts.FirstOrDefault(vc => vc.CurrentPlayer is T);
    }

    void pausePlayer<T>()
    {
      IPlayerContext context = getPlayerContext<T>();
      if (context != null)
        context.Pause();
    }

    void stopPlayer<T>()
    {
      IPlayerContext context = getPlayerContext<T>();
      if (context != null)
        context.Stop();
    }

    void resumePlayer<T>()
    {
      IPlayerContext context = getPlayerContext<T>();
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
