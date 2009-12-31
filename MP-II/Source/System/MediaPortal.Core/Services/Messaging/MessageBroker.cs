#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Core.Services.Messaging
{
  public class MessageBroker : IMessageBroker
  {
    protected IDictionary<string, ICollection<IMessageReceiver>> _registeredQueues =
        new Dictionary<string, ICollection<IMessageReceiver>>();
    protected object _syncObj = new object();

    public void RegisterMessageQueue(string channel, IMessageReceiver receiver)
    {
      lock (_syncObj)
      {
        ICollection<IMessageReceiver> receivers;
        if (!_registeredQueues.TryGetValue(channel, out receivers))
          _registeredQueues[channel] = receivers = new List<IMessageReceiver>();
        receivers.Add(receiver);
      }
    }

    public void UnregisterMessageQueue(string channel, IMessageReceiver receiver)
    {
      lock (_syncObj)
      {
        ICollection<IMessageReceiver> receivers;
        if (_registeredQueues.TryGetValue(channel, out receivers))
        {
          receivers.Remove(receiver);
          if (receivers.Count == 0)
            _registeredQueues.Remove(channel);
        }
      }
    }

    public void Send(string channelName, QueueMessage msg)
    {
      msg.ChannelName = channelName;
      ICollection<IMessageReceiver> receivers;
      lock (_syncObj)
        receivers = _registeredQueues.TryGetValue(channelName, out receivers) ? new List<IMessageReceiver>(receivers) : null;
      if (receivers != null)
        foreach (IMessageReceiver messageReceiver in receivers)
          try
          {
            messageReceiver.Receive(msg);
          }
          catch (Exception e)
          {
            ServiceScope.Get<ILogger>().Error("MessageBroker: Unable to send message to message receiver of channel '{0}'", e, channelName);
          }
    }
  }
}
