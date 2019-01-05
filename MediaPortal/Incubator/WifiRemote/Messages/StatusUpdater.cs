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
          if (WifiRemotePlugin.MessageStatus.IsChanged())
          {
            ServiceRegistration.Get<ILogger>().Debug("Send Statusupdate");
            SendMessageToAllClients.Send(WifiRemotePlugin.MessageStatus, ref SocketServer.Instance.connectedSockets);
          }
        }
        Thread.Sleep(UPDATE_INTERVAL);
      }
    }
  }
}
