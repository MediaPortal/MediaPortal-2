#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core.Services.Messaging
{
  public class MessageBroker : IMessageBroker
  {
    #region Protected fields

    protected IDictionary<string, Queue> _queues = new Dictionary<string, Queue>();
    protected object _syncObj = new object();
    protected bool _shuttingDown = false;

    #endregion

    public Queue GetOrCreate(string queueName)
    {
      lock (_syncObj)
      {
        if (_shuttingDown)
          throw new IllegalCallException("The MessageBroker is shutting down, no more message queues can be created");
        Queue result;
        if (!_queues.TryGetValue(queueName, out result))
        {
          result = new Queue(queueName);
          _queues[queueName] = result;
        }
        return result;
      }
    }

    public Queue Get(string queueName)
    {
      lock (_syncObj)
      {
        Queue result;
        if (_queues.TryGetValue(queueName, out result))
          return result;
        return null;
      }
    }

    protected Queue GetQueueCheckShutdown(string queueName)
    {
      if (_shuttingDown)
        return Get(queueName);
      else
        return GetOrCreate(queueName);
    }

    #region IMessageBroker implementation

    public ICollection<string> Queues
    {
      get 
      {
        lock (_syncObj)
          return new List<string>(_queues.Keys);
      }
    }

    public void Register_Sync(string queueName, MessageReceivedHandler handler)
    {
      lock (_syncObj)
      {
        Queue queue = GetQueueCheckShutdown(queueName);
        if (queue != null)
          queue.MessageReceived_Sync += handler;
      }
    }

    public void Unregister_Sync(string queueName, MessageReceivedHandler handler)
    {
      lock (_syncObj)
      {
        Queue queue = GetQueueCheckShutdown(queueName);
        if (queue != null)
          queue.MessageReceived_Sync -= handler;
      }
    }

    public void Register_Async(string queueName, MessageReceivedHandler handler)
    {
      lock (_syncObj)
      {
        Queue queue = GetQueueCheckShutdown(queueName);
        if (queue != null)
          queue.MessageReceived_Async += handler;
      }
    }

    public void Unregister_Async(string queueName, MessageReceivedHandler handler, bool waitForAsyncMessages)
    {
      Queue queue;
      lock (_syncObj)
      {
        queue = GetQueueCheckShutdown(queueName);
        if (queue != null)
          queue.MessageReceived_Async -= handler;
      }
      if (waitForAsyncMessages && queue != null)
        queue.WaitForAsyncExecutions();
    }

    public void Send(string queueName, QueueMessage msg)
    {
      Queue queue;
      lock (_syncObj)
      {
        if (_shuttingDown)
        {
          ServiceScope.Get<ILogger>().Warn("Trying to send a message while message broker is still shutting down (Queue = '{0}', MessageData = {1}", queueName, msg.MessageData);
          return;
        }
        queue = GetOrCreate(queueName);
      }
      queue.Send(msg);
    }

    public void Shutdown()
    {
      ICollection<Queue> queuesCopy;
      lock (_syncObj)
      {
        _shuttingDown = true;
        queuesCopy = new List<Queue>(_queues.Values);
      }
      foreach (Queue queue in queuesCopy)
      {
        queue.Shutdown();
        // Block until we don't have any async operations any more in the queue
        queue.WaitForAsyncExecutions();
      }
    }

    #endregion
  }
}
