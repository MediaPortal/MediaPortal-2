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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.UI.Presentation.Players;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.WifiRemote.MP_Messages
{
  class HandlePlayerMessages
  {
    public static void OnMessageReceived(SystemMessage message)
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
          SendMessageToAllClients.Send(WifiRemote.MessageStatus, ref SocketServer.Instance.connectedSockets);
          break;
        case PlayerManagerMessaging.MessageType.PlayerError:
          Logger.Error("Player Error");
          break;
        case PlayerManagerMessaging.MessageType.PlayerEnded:
        case PlayerManagerMessaging.MessageType.PlayerStopped:
          Logger.Debug("Player Stopped or Ended");
          SendMessageToAllClients.Send(WifiRemote.MessageStatus, ref SocketServer.Instance.connectedSockets);
          NowPlayingUpdater.Stop();
          break;
        case PlayerManagerMessaging.MessageType.PlayerStarted:
          Logger.Debug("Player Started");
          SendMessageToAllClients.Send(WifiRemote.MessageStatus, ref SocketServer.Instance.connectedSockets);
          NowPlayingUpdater.Start();
          break;
        case PlayerManagerMessaging.MessageType.VolumeChanged:
          Logger.Debug("Volume changed");
          SendMessageToAllClients.Send(new MessageVolume(), ref SocketServer.Instance.connectedSockets);
          break;
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
