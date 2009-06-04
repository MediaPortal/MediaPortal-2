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

using System;
using System.Collections.Generic;

namespace MediaPortal.Core.Messaging
{
  public abstract class MessageQueueBase : IMessageReceiver, IDisposable
  {
    #region Protected fields

    protected string _queueName;
    protected string[] _registeredChannels;
    protected Queue<QueueMessage> _messages = new Queue<QueueMessage>();
    protected object _syncObj = new object();

    #endregion

    #region Ctor, dtor & Dispose

    protected MessageQueueBase(string queueName, string[] messageChannels)
    {
      _queueName = queueName;
      _registeredChannels = messageChannels;
      foreach (string channel in messageChannels)
        SubscribeToMessageChannel(channel);
    }

    ~MessageQueueBase()
    {
      Dispose();
    }

    public virtual void Dispose()
    {
      if (_registeredChannels == null)
        return;
      foreach (string channel in _registeredChannels)
        UnsubscribeFromMessageChannel(channel);
      _registeredChannels = null;
    }

    #endregion

    protected abstract void HandleMessageAvailable(QueueMessage message);

    public int NumMessages
    {
      get
      {
        lock (_syncObj)
          return _messages.Count;
      }
    }

    public bool IsMessagesAvailable
    {
      get { return NumMessages > 0; }
    }

    public void SubscribeToMessageChannel(string channelName)
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.RegisterMessageQueue(channelName, this);
    }

    public void UnsubscribeFromMessageChannel(string channelName)
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>(false);
      if (broker != null)
        broker.UnregisterMessageQueue(channelName, this);
    }

    /// <summary>
    /// Enqueues the specified <paramref name="message"/> to this queue. This message will be called
    /// by the <see cref="IMessageBroker"/> service.
    /// </summary>
    /// <param name="message">Message to be enqueued in this message queue.</param>
    public void Enqueue(QueueMessage message)
    {
      lock (_syncObj)
      {
        _messages.Enqueue(message);
        HandleMessageAvailable(message);
      }
    }

    public QueueMessage Dequeue()
    {
      lock (_syncObj)
        if (_messages.Count > 0)
          return _messages.Dequeue();
        else
          return null;
    }

    public QueueMessage Peek()
    {
      lock (_syncObj)
        return _messages.Peek();
    }

    public void PurgeAll()
    {
      lock (_syncObj)
        _messages.Clear();
    }
  }
}