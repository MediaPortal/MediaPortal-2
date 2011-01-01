#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Runtime;

namespace MediaPortal.Core.Messaging
{
  public delegate void MessageReceivedHandler(AsynchronousMessageQueue queue, SystemMessage message);

  /// <summary>
  /// Asynchronous message queue to be used by message receivers.
  /// </summary>
  /// <remarks>
  /// This message queue is only able to deliver messages until the <see cref="SystemState.ShuttingDown"/> system message.
  /// The <see cref="SystemState.ShuttingDown"/> message is the last message being delivered; messages which are sent
  /// later are not delivered by this message queue any more.
  /// </remarks>
  public class AsynchronousMessageQueue : MessageQueueBase
  {
    #region Protected fields

    protected ShutdownWatcher _shutdownWatcher;
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

      public static ShutdownWatcher Create(AsynchronousMessageQueue owner)
      {
        IMessageBroker broker = ServiceRegistration.Get<IMessageBroker>();
        ShutdownWatcher result = new ShutdownWatcher(owner);
        broker.RegisterMessageReceiver(SystemMessaging.CHANNEL, result);
        return result;
      }

      public void Remove()
      {
        IMessageBroker broker = ServiceRegistration.Get<IMessageBroker>();
        broker.UnregisterMessageReceiver(SystemMessaging.CHANNEL, this);
      }

      public void Receive(SystemMessage message)
      {
        if (message.ChannelName == SystemMessaging.CHANNEL)
        {
          SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
          ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
          if (messageType == SystemMessaging.MessageType.SystemStateChanged)
            if (sss.CurrentState == SystemState.ShuttingDown || sss.CurrentState == SystemState.Ending)
              // It is necessary to block the main thread until our message delivery thread has delivered all pending
              // messages to avoid asynchronous threads during the shutdown phase.
              // It's a bit illegal to call the Shutdown() method which acquires a lock in this synchronous message
              // handler method. But as we know that the SystemStateChanged message is only called by our application
              // main thread which doesn't hold locks, this won't cause deadlocks.
              // How ugly, but that's life.
              _owner.Shutdown();
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
            ServiceRegistration.Get<ILogger>().Warn(
              "AsynchronousMessageQueue: Asynchronous message queue '{0}' has no message handler. Incoming message (channel '{1}', type '{2}') will be discarded.",
              _queueName, message.ChannelName, message.MessageType);
          else
            try
            {
              handler(this, message);
            }
            catch (Exception e)
            {
              ServiceRegistration.Get<ILogger>().Error("Unhandled exception in message handler of async message queue '{0}' when handling a message of type '{1}'",
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
          if (!IsMessagesAvailable)
            Monitor.Wait(_syncObj);
        }
      }
    }

    protected override void HandleMessageAvailable(SystemMessage message)
    {
      lock (_syncObj)
        Monitor.PulseAll(_syncObj); // Awake the possibly sleeping asynchronous message delivery thread
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

    /// <summary>
    /// Gets or sets the priority in which the async message delivery thread of this message queue should run.
    /// </summary>
    public ThreadPriority ThreadPriority
    {
      get { return _messageDeliveryThread.Priority; }
      set { _messageDeliveryThread.Priority = value; }
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
      }
      _shutdownWatcher = ShutdownWatcher.Create(this);
      lock (_syncObj)
      {
        _messageDeliveryThread = new Thread(DoWork)
          {
              Name = string.Format("'{0}' - message delivery thread", _queueName)
          };
        _messageDeliveryThread.Start();
      }
    }

    /// <summary>
    /// Shuts this async message listener thread down and waits for the last message to be delivered if possible.
    /// </summary>
    /// <remarks>
    /// A message queue being shut down can be restarted later by calling <see cref="Start"/> again.
    /// Be very careful with this method. It blocks until all messages are delivered, which means it must not
    /// be called while holding locks, according to the MP2 multithreading guidelines.
    /// 
    /// If this method is called from the message delivery thread, it cannot wait until all
    /// messages were delivered. In this case, this method possibly returns before all messages could be delivered,
    /// returning <c>true</c>. Else it returns <c>false</c>.
    /// </remarks>
    /// <returns><c>true</c>, if there are still messages pending to be delivered. <c>false</c>, if no more messages will be
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
    /// Terminates this message queue. Once the queue is terminated, no further messages are delivered.
    /// But as this method runs asynchronously to the message delivery thread, it is possible that a message is still being
    /// delivered when this method returns.
    /// </summary>
    /// <remarks>
    /// This method requests its internal lock, so it must not be called while holding other locks, according to the
    /// MP2 multithreading guidelines.
    /// </remarks>
    public void Terminate()
    {
      if (_shutdownWatcher != null)
        _shutdownWatcher.Remove();
      _shutdownWatcher = null;
      UnregisterFromAllMessageChannels();
      lock (_syncObj)
      {
        _terminated = true;
        Monitor.PulseAll(_syncObj);
      }
    }

    public override string ToString()
    {
      return Name;
    }

    #endregion
  }
}
