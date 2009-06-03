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
  /// <summary>
  /// Named message queue to send messages through the system.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This service is thread-safe.
  /// </para>
  /// <para>
  /// TODO: There is a potential problem with asynchronous messages sent to clients: You never know, when a message to a client C
  /// will arrive. It depends on other message handlers which were registered before C registered its async message handler.
  /// This leads to multiple problems. 1) Other handlers might paralyse the whole async message sender thread for a single
  /// queue, because the same message sender thread will be reused. 2) After C unregistered its message handler, it is still
  /// possible to receive an async message because the async message sender thread, well, runs asynchronous. This might lead
  /// to problems in the disposal phase of a client. There is no real solution for this problem while retaining async messages.
  /// One "naive" solution could be to call <see cref="WaitForAsyncExecutions"/> before returning from the
  /// <see cref="IMessageBroker.Unregister_Async"/> method. But then the system can deadlock when the client, which calls
  /// <see cref="IMessageBroker.Unregister_Async"/> (or any of its callers), are holding multithreading locks somewhere in the
  /// system.
  /// Another possible solution would be to implement a "messages_were_unregistered" or "terminated" flag in the client which
  /// will be checked first when receiving a message. But this solution doesn't work very well when the system wants to unload
  /// the client's code after its disposal.
  /// </para>
  /// </remarks>
  public class Queue
  {
    #region Classes

    protected class AsyncMessageSender
    {
      protected Queue<QueueMessage> _asyncMessages = new Queue<QueueMessage>();
      protected volatile bool _terminated = false; // Once terminated, no more messages are sent
      protected Queue _queue;

      public AsyncMessageSender(Queue parent)
      {
        _queue = parent;
      }

      public bool MessagesAvailable
      {
        get
        {
          lock (_queue.SyncObj)
            return _asyncMessages.Count > 0;
        }
      }

      public void EnqueueAsyncMessage(QueueMessage message)
      {
        lock (_queue.SyncObj)
        {
          if (_terminated)
            return;
          _asyncMessages.Enqueue(message);
          Monitor.PulseAll(_queue.SyncObj);
        }
      }

      public QueueMessage Dequeue()
      {
        lock (_queue.SyncObj)
          if (_asyncMessages.Count > 0)
            return _asyncMessages.Dequeue();
          else
            return null;
      }

      /// <summary>
      /// Terminates this sender. Once the sender is terminated, no more messages are delivered.
      /// </summary>
      public void Terminate()
      {
        lock (_queue.SyncObj)
        {
          _terminated = true;
          Monitor.PulseAll(_queue.SyncObj);
        }
      }

      public bool IsTerminated
      {
        get
        {
          lock (_queue.SyncObj)
            return _terminated;
        }
      }

      public void WaitForAsyncExecutions()
      {
        lock (_queue.SyncObj)
        {
          while (true)
          {
            if (_terminated || !MessagesAvailable)
              return;
            Monitor.Wait(_queue.SyncObj);
          }
        }
      }

      public void DoWork()
      {
        while (true)
        {
          QueueMessage message;
          if ((message = Dequeue()) != null)
            _queue.DoSendAsync(message);
          lock (_queue.SyncObj)
          {
            if (_terminated)
              // We have to check this in the synchronized block, else we could miss the PulseAll event
              break;
            else if (!MessagesAvailable)
            // We need to check this here again in a synchronized block. If we wouldn't prevent other threads from
            // enqueuing data in this moment, we could miss the PulseAll event
            {
              Monitor.PulseAll(_queue.SyncObj); // Necessary to awake the waiting threads in method WaitForAsyncExecutions()
              Monitor.Wait(_queue.SyncObj);
            }
          }
        }
        lock (_queue.SyncObj)
          Monitor.PulseAll(_queue.SyncObj); // Necessary to awake the waiting threads in method WaitForAsyncExecutions()
      }
    }

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected Thread _asyncThread = null; // Lazy initialized
    protected string _queueName;
    protected AsyncMessageSender _asyncMessageSender;

    #endregion

    public Queue(string name)
    {
      _queueName = name;
      _asyncMessageSender = new AsyncMessageSender(this);
      InitializeAsyncMessaging();
    }

    protected void InitializeAsyncMessaging()
    {
      lock (_syncObj)
      {
        if (_asyncThread != null)
          return;
        _asyncThread = new Thread(_asyncMessageSender.DoWork);
        _asyncThread.Name = string.Format("Message queue '{0}': Async sender thread", _queueName);
        _asyncThread.Start();
      }
    }

    protected void DoSendAsync(QueueMessage message)
    {
      MessageReceivedHandler asyncHandler = MessageReceived_Async;
      if (asyncHandler != null)
        asyncHandler(message);
    }

    /// <summary>
    /// Delivers all queue messages synchronously.
    /// </summary>
    /// <remarks>
    /// Because the sender might hold locks on its internal mutexes while sending a synchronous message,
    /// it absolutely necessary to not acquire any multithreading locks in the handler of this event.
    /// If the callee needs to acquire any locks as a result of this event, it MUST do this asynchronous from this event.
    /// </remarks>
    public event MessageReceivedHandler MessageReceived_Sync;

    /// <summary>
    /// Delivers all queue messages asynchronously.
    /// </summary>
    /// <remarks>
    /// In contrast to <see cref="MessageReceived_Sync"/>, the callee is allowed to acquire mutexes while executing this event.
    /// </remarks>
    public event MessageReceivedHandler MessageReceived_Async;

    /// <summary>
    /// Returns the name of this message queue.
    /// </summary>
    public string Name
    {
      get { return _queueName; }
    }

    /// <summary>
    /// Returns the information if this queue is already shut down.
    /// </summary>
    public bool IsShutdown
    {
      get
      {
        lock (_syncObj)
          return _asyncThread == null;
      }
    }

    /// <summary>
    /// The synchronization object of this queue to lock out other threads.
    /// </summary>
    public object SyncObj
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Locks the current thread down until the async message sender thread sent all its messages to subscribers
    /// (Exception: If the current thread is the async sender thread itself. In this case, this method will return
    /// immediately).
    /// </summary>
    /// <remarks>
    /// This method will request and release the queue's <see cref="SyncObj"/>. When calling this method,
    /// the caller tree should not hold any multithreading locks in the system, else the method might deadlock.
    /// </remarks>
    public void WaitForAsyncExecutions()
    {
      // Check, if the current thread is the async sender thread itself. This case could lead to a deadlock.
      if (Thread.CurrentThread == _asyncThread)
        return;
      _asyncMessageSender.WaitForAsyncExecutions();
    }

    /// <summary>
    /// Shuts this queue down. No more messages will be delivered.
    /// </summary>
    public void Shutdown()
    {
      _asyncMessageSender.Terminate();
      Thread threadToJoin;
      lock (_syncObj)
        threadToJoin = _asyncThread;
      if (threadToJoin != null)
      {
        threadToJoin.Join(); // Holding the lock while waiting for the thread would cause a deadlock
        lock (_syncObj)
          _asyncThread = null;
      }
    }

    public void Send(QueueMessage message)
    {
      lock (_syncObj)
        if (IsShutdown)
          return;
      message.MessageQueue = _queueName;
      // Send message synchronously...
      MessageReceivedHandler syncHandler = MessageReceived_Sync;
      if (syncHandler != null)
        syncHandler(message);
      // ... and asynchronously
      if (_asyncMessageSender.IsTerminated)
        // If already shut down, discard the message
        return;
      _asyncMessageSender.EnqueueAsyncMessage(message);
    }
  }
}
