#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using UPnP.Infrastructure.Dv;

namespace MediaPortal.Common.UPnP
{
  /// <summary>
  /// Helper class to handle system resume from standby. In case of a resume the UPnPServer will be forced to send a new SSDP advertise.
  /// </summary>
  public class UPnPSystemResumeHelper : IDisposable
  {
    protected AsynchronousMessageQueue _messageQueue;
    protected UPnPServer _upnpServer;
    protected bool _suspended;

    public UPnPSystemResumeHelper(UPnPServer upnpServer)
    {
      _upnpServer = upnpServer;
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL });
      _messageQueue.PreviewMessage += OnMessageReceived;
    }

    public void Startup()
    {
      _messageQueue.Start();
    }

    public void Shutdown()
    {
      _messageQueue.Shutdown();
    }

    public void Dispose()
    {
      Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Suspending)
            {
              _suspended = true;
            }
            if (newState == SystemState.Resuming && _suspended)
            {
              _suspended = false;
              ServiceRegistration.Get<ILogger>().Info("UPnPSystemResumeHelper: System resuming, trigger UpdateConfiguration for UPnPServer.");
              _upnpServer.UpdateConfiguration();
            }
            break;
        }
      }
    }
  }
}
