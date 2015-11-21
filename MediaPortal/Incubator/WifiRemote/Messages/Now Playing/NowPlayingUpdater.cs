using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Threading;
using MediaPortal.Plugins.WifiRemote.SendMessages;

namespace MediaPortal.Plugins.WifiRemote.Messages.Now_Playing
{
  internal static class NowPlayingUpdater
  {
    private const int UPDATE_INTERVAL = 1000;
    private static Thread _nowPlayingUpdateThread;
    private static bool _nowPlayingUpdateThreadRunning;
    private static bool _nowPlayingWasSend;

    internal static void Start()
    {
      if (_nowPlayingUpdateThread == null)
      {
        _nowPlayingUpdateThread = new Thread(new ThreadStart(DoNowPlayingUpdate));
        _nowPlayingUpdateThread.Start();
      }
    }

    internal static void Stop()
    {
      _nowPlayingUpdateThreadRunning = false;
      _nowPlayingWasSend = false;
      _nowPlayingUpdateThread = null;
    }

    private static void DoNowPlayingUpdate()
    {
      ServiceRegistration.Get<ILogger>().Debug("Start now-playing update thread");
      _nowPlayingUpdateThreadRunning = true;
      while (_nowPlayingUpdateThreadRunning)
      {
        if (Helper.IsNowPlaying() && _nowPlayingUpdateThreadRunning)
        {
          ServiceRegistration.Get<ILogger>().Debug("Send Nowplaying");
          if (_nowPlayingWasSend)
            SendMessageToAllClients.Send(new MessageNowPlayingUpdate(), ref SocketServer.Instance.connectedSockets);
          else
          {
            SendMessageToAllClients.Send(new MessageNowPlaying(), ref SocketServer.Instance.connectedSockets);
            _nowPlayingWasSend = true;
          }
        }
        Thread.Sleep(UPDATE_INTERVAL);
      }
      ServiceRegistration.Get<ILogger>().Debug("Stop now-playing update thread");
    }
  }
}
