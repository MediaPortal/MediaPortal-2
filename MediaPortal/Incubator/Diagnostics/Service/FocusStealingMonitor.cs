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
using System.Windows.Forms;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.General;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
  internal class FocusStealingMonitor : IDisposable
  {

    #region Private Fields

    private AsynchronousMessageQueue _messageQueue;

    #endregion Private Fields

    #region Internal Properties

    /// <summary>
    /// Gets a value indicating the execution status
    /// </summary>
    internal bool IsMonitoring { get; private set; }

    #endregion Internal Properties

    #region Public Methods

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Subscribe to message & start focus stealing monitoring
    /// </summary>
    internal void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL });
      _messageQueue.PreviewMessage += OnPreviewMessage;
      _messageQueue.Start();
      IsMonitoring = true;
    }

    /// <summary>
    /// Unsubscribe from message & stop focus stealing monitoring
    /// </summary>
    internal void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
      IsMonitoring = false;
    }

    #endregion Internal Methods

    #region Protected Methods

    protected virtual void HandleWindowsMessage(ref Message m)
    {
      ActivationMonitor.HandleMessage(ref m);
    }

    #endregion Protected Methods

    #region Private Methods

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

    #endregion Private Methods

  }
}
