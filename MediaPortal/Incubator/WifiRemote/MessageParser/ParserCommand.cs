#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
  internal class ParserCommand : BaseParser
  {
    private const int KEY_DOWN_TIMEOUT = 2000;
    private static Thread _commandDownThread;
    private static String _commandDownChar;
    private static int _commandDownPauses;
    private static bool _isCommandDown;
    private static Stopwatch _mKeyDownTimer;
    private static IDictionary<string, IGeometry> availableGeometries = ServiceRegistration.Get<IGeometryManager>().AvailableGeometries;

    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string command = GetMessageValue<string>(message, "Command");
      ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Parser Command: Command: {0}", command);
      SendCommand(command);
      return Task.FromResult(true);
    }

    public static Task<bool> ParseCommandStartRepeatAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      _commandDownChar = GetMessageValue<string>(message, "Command");
      _commandDownPauses = GetMessageValue<int>(message, "Pause");
      _mKeyDownTimer.Restart();

      if (!_isCommandDown)
      {
        _commandDownThread = new Thread(DoKeyDown);
        _commandDownThread.Start();
      }

      return Task.FromResult(true);
    }

    /// <summary>
    /// Sends key-up so a running key-down is cancelled
    /// </summary>
    public static Task<bool> ParseCommandStopRepeatAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      _isCommandDown = false;
      return Task.FromResult(true);
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
          button = Key.Previous;
          break;

        case "skip":
          button = Key.Next;
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

        case "0":
        case "1":
        case "2":
        case "3":
        case "4":
        case "5":
        case "6":
        case "7":
        case "8":
        case "9":
          button = new Key(command[0]);
          break;

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
          button = Key.DVDMenu;
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

      if (button != null)
        ServiceRegistration.Get<IInputManager>().KeyPress(button);
    }
  }
}
