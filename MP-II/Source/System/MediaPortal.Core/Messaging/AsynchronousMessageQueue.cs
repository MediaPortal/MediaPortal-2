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
using System.Threading;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Runtime;

namespace MediaPortal.Core.Messaging
{
  public delegate void MessageReceivedHandler(AsynchronousMessageQueue queue, SystemMessage message);

  /// <summary>
  /// Synchronous message queue to be used by message receivers.
  /// </summary>
  public class AsynchronousMessageQueue : MessageQueueBase
  {
    #region Protected fields

    protected Thread _messageDeliveryThread = null;
    protected bool _terminated = false;

    #endregion

    #region Classes

    protected class ShutdownWatcher : IMessageReceiver
    {
      protected AsynchronousMessageQueue _owner;

      protected ShutdownWatcher(AsynchronousMessageQueue owner)
      {
        _owner = owner;
      }

      public static void Create(AsynchronousMessageQueue owner)
      {
        IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
        broker.RegisterMessageQueue(SystemMessaging.CHANNEL, new ShutdownWatcher(owner));
      }

      public void Receive(SystemMessage message)
      {
        if (message.ChannelName == SystemMessaging.CHANNEL)
        {
          SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
          ISystemStateService sss = ServiceScope.Get<ISystemStateService>();
          if (messageType == SystemMessaging.MessageType.SystemStateChanged)
            if (sss.CurrentState == SystemState.ShuttingDown || sss.CurrentState == SystemState.Ending)
              _owner.Terminate();
        }
      }
    }

    #endregion

    #region Ctor & Dispose

    /// <summary>
    /// Creates a new asynchronous message queue.
    /// </summary>
    /// <param name="owner">Owner of this queue. Used for setting the queue's default name.</param>
    /// <param name="messageChannels">Message channels this message queue will be registered at the message broker.</param>
    public AsynchronousMessageQueue(object owner, IEnumerable<string> messageChannels) :
        this(owner == null ? "Unknown" : owner.GetType().Name, messageChannels) { }

    /// <summary>
    /// Creates a new asynchronous message queue.
    /// </summary>
    /// <param name="ownerType">Type of the owner of this queue. Used for setting the queue's default name.</param>
    /// <param name="messageChannels">Message channels this message queue will be registered at the message broker.</param>
    public AsynchronousMessageQueue(string ownerType, IEnumerable<string> messageChannels) : base(messageChannels)
    {
      _queueName = string.Format("Async message queue '{0}'", ownerType);
      ShutdownWatcher.Create(this);
    }

    public override void Dispose()
    {
      Terminate();
      base.Dispose();
    }

    #endregion

    #region Protected methods

    protected void DoWork()
    {
      while (true)
      {
        SystemMessage message = Dequeue();
        if (message != null)
        {
          MessageReceivedHandler handler = MessageReceived;
          if (handler == null)
            ServiceScope.Get<ILogger>().Warn(
              "AsynchronousMessageQueue: Asynchronous message queue '{0}' has no message handler. Incoming message (channel '{1}', type '{2}') will be discarded.",
              _queueName, message.ChannelName, message.MessageType);
          else
            try
            {
              handler(this, message);
            }
            catch (Exception e)
            {
              ServiceScope.Get<ILogger>().Error("Unhandled exception in message handler of async message queue '{0}' when handling a message of type '{1}'",
                  e, Name, message.MessageType);
            }
        }
        lock (_syncObj)
        {
          if (_terminated)
            // We have to check this in the synchronized block, else we could miss the PulseAll event
            break;
          // We need to check this in a synchronized block. If we wouldn't prevent other threads from
          // enqueuing data in this moment, we could miss the PulseAll event
          else if (!IsMessagesAvailable)
            Monitor.Wait(_syncObj);
        }
      }
    }

    protected override void HandleMessageAvailable(SystemMessage message)
    {
      lock (_syncObj)
        Monitor.PulseAll(_syncObj); // Awake the possibly sleeping message delivery thread
      MessageReceivedHandler handler = PreviewMessage;
      if (handler != null)
        handler(this, message);
    }

    #endregion

    #region Public members

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

    public string Name
    {
      get { return _queueName; }
    }

    /// <summary>
    /// Starts this message queue.
    /// </summary>
    public void Start()
    {
      RegisterAtAllMessageChannels();
      lock (_syncObj)
      {
        if (_messageDeliveryThread != null)
          return;
        _terminated = false;
        _messageDeliveryThread = new Thread(DoWork)
          {
              Name = string.Format("'{0}' - message delivery thread", _queueName)
          };
        _messageDeliveryThread.Start();
      }
    }

    /// <summary>
    /// Shuts this async message listener thread down. No more messages will be delivered asynchronously when
    /// this method returns. If this method is called from the message delivery thread, it cannot wait until all
    /// messages were delivered. In this case, this method returns <c>true</c>. Else it returns <c>false</c>.
    /// </summary>
    /// <remarks>
    /// A message queue being shut down can be restarted later by calling <see cref="Start"/> again.
    /// </remarks>
    /// <returns><c>true</c>, if there are still messages being delivered. <c>false</c>, if no more messages will be
    /// delivered by this queue.</returns>
    public bool Shutdown()
    {
      UnregisterFromAllMessageChannels();
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

    public override string ToString()
    {
      return Name;
    }

    #endregion
  }
}
