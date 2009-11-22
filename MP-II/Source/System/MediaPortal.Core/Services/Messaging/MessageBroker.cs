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

using System.Collections.Generic;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Core.Services.Messaging
{
  public class MessageBroker : IMessageBroker
  {
    protected IDictionary<string, ICollection<IMessageReceiver>> _registeredQueues =
        new Dictionary<string, ICollection<IMessageReceiver>>();
    protected object _syncObj = new object();

    public void RegisterMessageQueue(string channel, IMessageReceiver queue)
    {
      lock (_syncObj)
      {
        ICollection<IMessageReceiver> queues;
        if (!_registeredQueues.TryGetValue(channel, out queues))
          _registeredQueues[channel] = queues = new List<IMessageReceiver>();
        queues.Add(queue);
      }
    }

    public void UnregisterMessageQueue(string channel, IMessageReceiver queue)
    {
      lock (_syncObj)
      {
        ICollection<IMessageReceiver> queues;
        if (_registeredQueues.TryGetValue(channel, out queues))
        {
          queues.Remove(queue);
          if (queues.Count == 0)
            _registeredQueues.Remove(channel);
        }
      }
    }

    public void Send(string channelName, QueueMessage msg)
    {
      msg.ChannelName = channelName;
      ICollection<IMessageReceiver> queues;
      if (_registeredQueues.TryGetValue(channelName, out queues))
        foreach (IMessageReceiver queue in queues)
          queue.Receive(msg);
    }
  }
}
