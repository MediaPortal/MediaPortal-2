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

using System.Threading;
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Messaging
{
  public delegate void MessageReceivedHandler(AsynchronousMessageQueue queue, QueueMessage message);

  /// <summary>
  /// Synchronous message queue to be used by message receivers.
  /// </summary>
  public class AsynchronousMessageQueue : MessageQueueBase
  {
    protected Thread _messageDeliveryThread = null;
    protected bool _terminated = false;

    /// <summary>
    /// Creates a new asynchronous message queue.
    /// </summary>
    /// <param name="queueName">Name of this message queue.<param>
    /// <param name="messageChannels">Message channels this message queue will be registered at the message broker.</param>
    public AsynchronousMessageQueue(string queueName, string[] messageChannels) : base(queueName, messageChannels) { }

    protected void DoWork()
    {
      while (true)
      {
        QueueMessage message = Dequeue();
        if (message != null)
        {
          MessageReceivedHandler handler = MessageReceived;
          if (handler == null)
            ServiceScope.Get<ILogger>().Warn(
              "AsynchronousMessageQueue: Asynchronous message queue '{0}' has no message handler. Incoming message (channel '{1}', type '{2}') will be discarded.",
              _queueName, message.ChannelName, message.MessageType);
          else
            handler(this, message);
        }
        lock (_syncObj)
        {
          if (_terminated)
            // We have to check this in the synchronized block, else we could miss the PulseAll event
            break;
          else if (!IsMessagesAvailable)
            // We need to check this here again in a synchronized block. If we wouldn't prevent other threads from
            // enqueuing data in this moment, we could miss the PulseAll event
            Monitor.Wait(_syncObj);
        }
      }
    }

    protected override void HandleMessageAvailable(QueueMessage message)
    {
      lock (_syncObj)
        Monitor.PulseAll(_syncObj); // Awake the possibly sleeping message delivery thread
      MessageReceivedHandler handler = PreviewMessage;
      if (handler != null)
        handler(this, message);
    }

    /// <summary>
    /// Handler event which will be raised when a new message can be received. This event handler will be called
    /// synchronously. It is not allowed to request any multithreading locks in handlers of this event because the system
    /// could deadlock.
    /// </summary>
    public event MessageReceivedHandler PreviewMessage;

    /// <summary>
    /// Handler event which will be raised when a new message can be received. This event handler will be called
    /// in the asynchronous message delivery thread.
    /// </summary>
    public event MessageReceivedHandler MessageReceived;

    public override void Dispose()
    {
      Terminate();
      base.Dispose();
    }

    public void Start()
    {
      lock (_syncObj)
      {
        if (_messageDeliveryThread != null)
          return;
        _messageDeliveryThread = new Thread(DoWork)
          {
              Name = string.Format("Message queue '{0}': Async message delivery thread", _queueName)
          };
        _messageDeliveryThread.Start();
      }
    }

    /// <summary>
    /// Shuts this async message listener thread down. No more messages will be delivered asynchronously when
    /// this method returns. If this method is called from the message delivery thread, it cannot wait until all
    /// messages were delivered. In this case, this method returns <c>true</c>. Else it returns <c>false</c>.
    /// </summary>
    /// <returns><c>true</c>, if there are still messages being delivered. <c>false</c>, if no more messages will be
    /// delivered by this queue.</returns>
    public bool Shutdown()
    {
      Terminate();
      Thread threadToJoin;
      lock (_syncObj)
        threadToJoin = _messageDeliveryThread;
      if (threadToJoin != null)
      {
        bool completed = false;
        if (Thread.CurrentThread != threadToJoin)
        {
          threadToJoin.Join(); // Holding the lock while waiting for the thread would cause a deadlock
          completed = true;
        }
        lock (_syncObj)
          _messageDeliveryThread = null;
        return !completed;
      }
      return false;
    }

    /// <summary>
    /// Terminates this message queue. Once the queue is terminated, no more messages are delivered.
    /// This method terminates this queue asynchronously, which means it is possible that messages are still
    /// delivered when this method returns.
    /// </summary>
    public void Terminate()
    {
      lock (_syncObj)
      {
        _terminated = true;
        Monitor.PulseAll(_syncObj);
      }
    }

    /// <summary>
    /// Returns the information if this async message queue is terminated. A terminated message queue won't
    /// enqueue any more messages, and its asynchronous message delivery thread will terminate when possible.
    /// </summary>
    public bool IsTerminated
    {
      get
      {
        lock (_syncObj)
          return _terminated;
      }
    }
  }
}
