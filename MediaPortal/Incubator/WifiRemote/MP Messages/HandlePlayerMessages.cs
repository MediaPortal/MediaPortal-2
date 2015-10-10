using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
