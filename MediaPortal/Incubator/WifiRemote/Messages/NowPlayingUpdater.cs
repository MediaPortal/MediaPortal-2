#region Copyright (C) 2007-2020 Team MediaPortal

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
  internal static class NowPlayingUpdater
  {
    private const int UPDATE_INTERVAL = 1000;
    private static Thread _nowPlayingUpdateThread;
    private static bool _nowPlayingUpdateThreadRunning;
    private static bool _nowPlayingWasSend;
    private static ManualResetEventSlim _nowPlayingUpdateThreadSleep;

    internal static void Start()
    {
      if (_nowPlayingUpdateThread == null)
      {
        _nowPlayingUpdateThreadSleep = new ManualResetEventSlim(false);
        _nowPlayingUpdateThread = new Thread(new ThreadStart(DoNowPlayingUpdate));
        _nowPlayingUpdateThread.IsBackground = true;
        _nowPlayingUpdateThread.Start();
      }
    }

    internal static void Stop()
    {
      _nowPlayingUpdateThreadRunning = false;
      _nowPlayingWasSend = false;
      _nowPlayingUpdateThreadSleep?.Set();
      if (_nowPlayingUpdateThread?.Join(UPDATE_INTERVAL) ?? false)
        _nowPlayingUpdateThread?.Abort();
      _nowPlayingUpdateThread = null;
    }

    private static void DoNowPlayingUpdate()
    {
      ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Start now-playing update thread");
      _nowPlayingUpdateThreadRunning = true;
      try
      {
        while (_nowPlayingUpdateThreadRunning)
        {
          if (!ServiceRegistration.IsShuttingDown && Helper.IsNowPlaying())
          {
            ServiceRegistration.Get<ILogger>(false)?.Debug("WifiRemote: Send now-playing");
            if (_nowPlayingWasSend)
            {
              SendMessageToAllClients.Send(new MessageNowPlayingUpdate(), ref SocketServer.Instance.connectedSockets);
            }
            else
            {
              SendMessageToAllClients.Send(new MessageNowPlaying(), ref SocketServer.Instance.connectedSockets);
              _nowPlayingWasSend = true;
            }
          }
          _nowPlayingUpdateThreadSleep.Wait(UPDATE_INTERVAL);
        }
        ServiceRegistration.Get<ILogger>(false)?.Debug("WifiRemote: Stop now-playing update thread");
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>(false)?.Error("WifiRemote: Now-playing update thread crashed", ex);
      }
    }
  }
}
