using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Players;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserCommand
  {
    private const int KEY_DOWN_TIMEOUT = 2000;
    private static Thread _commandDownThread;
    private static String _commandDownChar;
    private static int _commandDownPauses;
    private static bool _isCommandDown;
    private static Stopwatch _mKeyDownTimer;
    private static IDictionary<string, IGeometry> availableGeometries = ServiceRegistration.Get<IGeometryManager>().AvailableGeometries;

    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      ServiceRegistration.Get<ILogger>().Debug("ParserCommand: command: {0}", (string)message["Command"]);
      SendCommand((string)message["Command"]);
      return true;
    }

    public static bool ParseCommandStartRepeat(JObject message, SocketServer server, AsyncSocket sender)
    {
      _commandDownChar = (string)message["Command"];
      _commandDownPauses = (int)message["Pause"];
      _mKeyDownTimer.Restart();

      if (!_isCommandDown)
      {
        _commandDownThread = new Thread(DoKeyDown);
        _commandDownThread.Start();
      }

      return true;
    }

    /// <summary>
    /// Sends key-up so a running key-down is cancelled
    /// </summary>
    public static bool ParseCommandStopRepeat(JObject message, SocketServer server, AsyncSocket sender)
    {
      _isCommandDown = false;
      return true;
    }

    /// <summary>
    /// Thread for sending key-down
    /// </summary>
    private static void DoKeyDown()
    {
      _isCommandDown = true;
      while (_isCommandDown && _mKeyDownTimer.ElapsedMilliseconds < KEY_DOWN_TIMEOUT)
      {
        SendCommand(_commandDownChar);
        Thread.Sleep(_commandDownPauses);
      }
      _mKeyDownTimer.Stop();
      _isCommandDown = false;
    }

    private static void SendCommand(string command)
    {
      Key button = null;

      switch (command.ToLower())
      {
        case "stop":
          button = Key.Stop;
          break;

        case "record":
          button = Key.Record;
          break;

        case "pause":
          button = Key.PlayPause;
          break;

        case "play":
          button = Key.Play;
          break;

        case "rewind":
          button = Key.Rew;
          break;

        case "forward":
          button = Key.Fwd;
          break;

        case "replay":
          //button = RemoteButton.Replay;
          break;

        case "skip":
          //button = Key.Skip;
          break;

        case "back":
          button = Key.Escape;
          break;

        case "info":
          button = Key.Info;
          break;

        case "menu":
          button = Key.ContextMenu;
          break;

        case "up":
          button = Key.Up;
          break;

        case "down":
          button = Key.Down;
          break;

        case "left":
          button = Key.Left;
          break;

        case "right":
          button = Key.Right;
          break;

        case "ok":
          button = Key.Ok;
          break;

        case "volup":
          button = Key.VolumeUp;
          break;

        case "voldown":
          button = Key.VolumeDown;
          break;

        case "volmute":
          button = Key.Mute;
          break;

        case "chup":
          button = Key.ChannelUp;
          break;

        case "chdown":
          button = Key.ChannelDown;
          break;

        case "dvdmenu":
          button = Key.DVDMenu;
          break;

        /*case "0":
          button = RemoteButton.NumPad0;
          break;

        case "1":
          button = RemoteButton.NumPad1;
          break;

        case "2":
          button = RemoteButton.NumPad2;
          break;

        case "3":
          button = RemoteButton.NumPad3;
          break;

        case "4":
          button = RemoteButton.NumPad4;
          break;

        case "5":
          button = RemoteButton.NumPad5;
          break;

        case "6":
          button = RemoteButton.NumPad6;
          break;

        case "7":
          button = RemoteButton.NumPad7;
          break;

        case "8":
          button = RemoteButton.NumPad8;
          break;

        case "9":
          button = RemoteButton.NumPad9;
          break;*/

        case "clear":
          button = Key.Clear;
          break;

        case "enter":
          button = Key.Enter;
          break;

        case "teletext":
          button = Key.TeleText;
          break;

        case "red":
          button = Key.Red;
          break;

        case "blue":
          button = Key.Blue;
          break;

        case "yellow":
          button = Key.Yellow;
          break;

        case "green":
          button = Key.Green;
          break;

        case "home":
          button = Key.Home;
          break;

        case "basichome":
          button = Key.Home;
          break;

        case "nowplaying":
          //button = Key.NowPlaying;
          break;

        case "tvguide":
          button = Key.Guide;
          break;

        case "tvrecs":
          button = Key.RecordedTV;
          break;

        case "dvd":
          //button = Key.PlayDVD;
          break;

        case "playlists":
          //button = RemoteButton.MyPlaylists;
          break;

        case "first":
          //button = RemoteButton.First;
          break;

        case "last":
          //button = RemoteButton.Last;
          break;

        case "fullscreen":
          button = Key.Fullscreen;
          break;

        case "subtitles":
          if (Helper.IsNowPlaying())
          {
            ISubtitlePlayer subtitlePlayer = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.CurrentPlayer as ISubtitlePlayer;
            if (subtitlePlayer != null)
            {
              var availableSubtitlesList = subtitlePlayer.Subtitles.ToList();
              int index = 0;
              if (subtitlePlayer.CurrentSubtitle != null)
              {
                index = availableSubtitlesList.FindIndex(x => x == subtitlePlayer.CurrentSubtitle) + 1;
                if (index == (availableSubtitlesList.Count - 1))
                  index = 0;
              }
              subtitlePlayer.SetSubtitle(availableSubtitlesList[index]);
            }

          }
          break;

        case "audiotrack":
          if (Helper.IsNowPlaying())
          {
            ISharpDXVideoPlayer videoPlayer = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.CurrentPlayer as ISharpDXVideoPlayer;
            if (videoPlayer != null)
            {
              var availableAudiotracksList = videoPlayer.AudioStreams.ToList();
              int index = 0;
              if (videoPlayer.CurrentAudioStream != null)
              {
                index = availableAudiotracksList.FindIndex(x => x == videoPlayer.CurrentAudioStream) + 1;
                if (index == (availableAudiotracksList.Count - 1))
                  index = 0;
              }
              videoPlayer.SetAudioStream(availableAudiotracksList[index]);
            }

          }
          break;

        /*case "screenshot":
          button = Key.Screenshot;
          break;*/

        case "aspectratio":
          if (Helper.IsNowPlaying())
          {
            ISharpDXVideoPlayer videoPlayer = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.CurrentPlayer as ISharpDXVideoPlayer;
            if (videoPlayer != null)
            {
              var availableGeometriesList = availableGeometries.Values.ToList();
              int index = 0;
              if (videoPlayer.GeometryOverride != null)
              {
                index = availableGeometriesList.FindIndex(x => x.Name == videoPlayer.GeometryOverride.Name) + 1;
                if (index == (availableGeometriesList.Count - 1))
                  index = 0;
              }
              videoPlayer.GeometryOverride = availableGeometriesList[index];
            }
              
          }
          break;

        /*case "ejectcd":
          button = RemoteButton.EjectCD;
          break;*/

        default:
          break;
      }

      if (button != null) ServiceRegistration.Get<IInputManager>().KeyPress(button);
    }
  }
}