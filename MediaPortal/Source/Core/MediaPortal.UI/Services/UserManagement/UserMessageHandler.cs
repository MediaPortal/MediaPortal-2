#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.UI.Services.UserManagement
{
  /// <summary>
  /// <see cref="UserMessageHandler"/> provides a simple handler for user change messages.
  /// </summary>
  public class UserMessageHandler : IDisposable
  {
    #region Fields

    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    public UserMessageHandler(bool asyncMode = false)
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[] { UserMessaging.CHANNEL });
      if (asyncMode)
        _messageQueue.MessageReceived += PreviewMessage; // Asynchronous
      else
        _messageQueue.PreviewMessage += PreviewMessage; // Synchronous
      _messageQueue.Start();
    }

    /// <summary>
    /// Informs listeners that the current user has been changed.
    /// </summary>
    public EventHandler UserChanged;

    /// <summary>
    /// Informs listeners about requests for registration of restrictions.
    /// </summary>
    public EventHandler RequestRestrictions;

    #region Message handling

    protected void PreviewMessage(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName != UserMessaging.CHANNEL)
        return;

      try
      {
        UserMessaging.MessageType messageType = (UserMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case UserMessaging.MessageType.UserChanged:
            UserChanged?.Invoke(this, EventArgs.Empty);
            break;
          case UserMessaging.MessageType.RequestRestrictions:
            RequestRestrictions?.Invoke(this, EventArgs.Empty);
            break;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error handling UserMessage '{0}'", e, message.MessageType);
      }
    }

    #endregion

    #region IDisposable members

    public void Dispose()
    {
      _messageQueue.MessageReceived -= PreviewMessage;
      _messageQueue.Shutdown();
    }

    #endregion
  }
}
