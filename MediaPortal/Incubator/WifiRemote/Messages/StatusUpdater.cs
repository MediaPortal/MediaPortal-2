﻿#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
    private static ManualResetEventSlim _statusUpdateThreadSleep;

    internal static void Start()
    {
      if (_statusUpdateThread == null)
      {
        _statusUpdateThreadSleep = new ManualResetEventSlim(false);
        _statusUpdateThread = new Thread(DoStatusUpdate);
        _statusUpdateThread.IsBackground = true;
        _statusUpdateThread.Start();
      }
    }

    internal static void Stop()
    {
      _statusUpdateThreadRunning = false;
      _statusUpdateThreadSleep?.Set();
      if (_statusUpdateThread?.Join(UPDATE_INTERVAL) ?? false)
        _statusUpdateThread?.Abort();
      _statusUpdateThread = null;
    }

    private static void DoStatusUpdate()
    {
      ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Start status update thread");
      _statusUpdateThreadRunning = true;
      try
      {
        while (_statusUpdateThreadRunning)
        {
          if (!ServiceRegistration.IsShuttingDown)
          {
            if (WifiRemotePlugin.MessageStatus.IsChanged())
            {
              ServiceRegistration.Get<ILogger>(false)?.Debug("WifiRemote: Send Status update");
              SendMessageToAllClients.Send(WifiRemotePlugin.MessageStatus, ref SocketServer.Instance.connectedSockets);
            }
          }
          _statusUpdateThreadSleep.Wait(UPDATE_INTERVAL);
        }
        ServiceRegistration.Get<ILogger>(false)?.Debug("WifiRemote: Stop status update thread");
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>(false)?.Error("WifiRemote: Status update thread crashed", ex);
      }
    }
  }
}
