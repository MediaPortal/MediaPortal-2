using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.SendMessages;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  internal static class StatusUpdater
  {
    private const int UPDATE_INTERVAL = 1000;
    private static Thread _statusUpdateThread;
    private static bool _statusUpdateThreadRunning;

    internal static void Start()
    {
      if (_statusUpdateThread == null)
      {
        _statusUpdateThread = new Thread(DoStatusUpdate);
        _statusUpdateThread.Start();
      }
    }

    internal static void Stop()
    {
      _statusUpdateThreadRunning = false;
      _statusUpdateThread = null;
    }

    private static void DoStatusUpdate()
    {
      ServiceRegistration.Get<ILogger>().Debug("Start status update thread");
      _statusUpdateThreadRunning = true;
      while (_statusUpdateThreadRunning)
      {
        if (_statusUpdateThreadRunning)
        {
          if (WifiRemote.MessageStatus.IsChanged())
          {
            ServiceRegistration.Get<ILogger>().Debug("Send Statusupdate");
            SendMessageToAllClients.Send(WifiRemote.MessageStatus, ref SocketServer.Instance.connectedSockets);
          }
        }
        Thread.Sleep(UPDATE_INTERVAL);
      }
    }
  }
}
