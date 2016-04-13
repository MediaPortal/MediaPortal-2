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
using System.Windows.Forms;
using log4net;
using log4net.Core;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.General;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
  public class DiagnosticsHandler : IDisposable
  {
    private AsynchronousMessageQueue _messageQueue;

    public DiagnosticsHandler()
    {
      ChangeLogLevel();
      SubscribeToMessages();
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    private void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL, });
      _messageQueue.PreviewMessage += OnPreviewMessage;
      _messageQueue.Start();
    }

    private void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    private void OnPreviewMessage(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case WindowsMessaging.MessageType.WindowsBroadcast:
            Message msg = (Message)message.MessageData[WindowsMessaging.MESSAGE];
            HandleWindowsMessage(ref msg);
            message.MessageData[WindowsMessaging.MESSAGE] = msg;
            break;
        }
      }
    }

    protected virtual void HandleWindowsMessage(ref Message m)
    {
      ActivationMonitor.HandleMessage(ref m);
    }

    private void ChangeLogLevel()
    {
      var loggerRepository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
      loggerRepository.Root.Level = Level.Debug;
      loggerRepository.RaiseConfigurationChanged(EventArgs.Empty);
      ServiceRegistration.Get<Common.Logging.ILogger>().Debug("DiagnosticService: Switched LogLevel to DEBUG.");
    }
  }
}
