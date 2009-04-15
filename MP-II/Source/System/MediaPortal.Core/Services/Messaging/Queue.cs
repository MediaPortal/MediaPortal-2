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
using System.Threading;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Core.Services.Messaging
{
  public class Queue : IMessageQueue
  {
    #region Classes

    protected class AsyncMessageSender
    {
      protected Queue<QueueMessage> _asyncMessages = new Queue<QueueMessage>();
      protected object _syncObj = new object();
      protected volatile bool _terminated = false;
      protected Queue _queue;

      public AsyncMessageSender(Queue parent)
      {
        _queue = parent;
      }

      public object SyncObj
      {
        get { return _syncObj; }
      }

      public bool MessagesAvailable
      {
        get
        {
          lock (_syncObj)
            return _asyncMessages.Count > 0;
        }
      }

      public void EnqueueAsyncMessage(QueueMessage message)
      {
        lock (_syncObj)
        {
          _asyncMessages.Enqueue(message);
          Monitor.PulseAll(_syncObj);
        }
      }

      public QueueMessage Dequeue()
      {
        lock (_syncObj)
          if (_asyncMessages.Count > 0)
            return _asyncMessages.Dequeue();
          else
            return null;
      }

      public void Terminate()
      {
        lock (_syncObj)
        {
          _terminated = true;
          Monitor.PulseAll(_syncObj);
        }
      }

      public void DoWork()
      {
        _terminated = false;
        while (true)
        {
          QueueMessage message;
          if ((message = Dequeue()) != null)
            _queue.Send(message);
          lock (_syncObj)
            if (_terminated)
              break;
            else if (!MessagesAvailable)
              // We need to check this here again in a synchronized block. If we wouldn't prevent other threads from
              // enqueuing data in this moment, we could miss the PulseAll event
              Monitor.Wait(_syncObj);
        }
      }
    }

    #endregion

    #region Protected fields

    protected string _queueName;
    protected IList<IMessageFilter> _filters;
    protected AsyncMessageSender _asyncMessageSender;
    protected Thread _asyncThread = null; // Lazy initialized

    #endregion

    public Queue(string name)
    {
      _queueName = name;
      _filters = new List<IMessageFilter>();
      _asyncMessageSender = new AsyncMessageSender(this);
    }

    protected void CheckAsyncMessagingInitialized()
    {
      lock (_asyncMessageSender.SyncObj)
      {
        if (_asyncThread != null)
          return;
        _asyncThread = new Thread(_asyncMessageSender.DoWork);
        _asyncThread.Name = string.Format("Message queue '{0}': Async sender thread", _queueName);
        _asyncThread.Start();
      }
    }

    #region IMessageQueue implementation

    public event MessageReceivedHandler MessageReceived;

    public IList<IMessageFilter> Filters
    {
      get { return _filters; }
    }

    public string Name
    {
      get { return _queueName; }
    }

    public bool HasSubscribers
    {
      get { return (MessageReceived != null); }
    }

    public void Shutdown()
    {
      if (_asyncMessageSender != null)
        _asyncMessageSender.Terminate();
      Thread threadToJoin = null;
      lock (_asyncMessageSender.SyncObj)
        threadToJoin = _asyncThread;
      if (threadToJoin != null)
      {
        threadToJoin.Join(); // Holding the lock while waiting for the thread would cause a deadlock
        lock (_asyncMessageSender.SyncObj)
          _asyncThread = null;
      }
    }

    public void Send(QueueMessage message)
    {
      message.MessageQueue = this;
      foreach (IMessageFilter filter in _filters)
      {
        message = filter.Process(message);
        if (message == null) return;
      }
      if (MessageReceived != null)
        MessageReceived(message);
    }

    public void SendAsync(QueueMessage message)
    {
      CheckAsyncMessagingInitialized();
      _asyncMessageSender.EnqueueAsyncMessage(message);
    }

    #endregion
  }
}
