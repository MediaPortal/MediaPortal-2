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
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.MP_Messages
{
  class MPMessageHandler
  {
    private AsynchronousMessageQueue _messageQueue;

    private static readonly string[] SUBSCRIBED_CHANNELS = new string[]
    {
      PlayerManagerMessaging.CHANNEL
    };

    public void SubscribeToMessages()
    {
      Console.WriteLine("subscribe to player messages");
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, SUBSCRIBED_CHANNELS);
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
      Console.WriteLine("subscribe to player messages end of function");
    }

    public void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    public void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        HandlePlayerMessages.OnMessageReceived(message);
      }
    }
  }
}
