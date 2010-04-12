#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;

namespace MediaPortal.Core.Messaging
{
  public abstract class MessageQueueBase : IMessageReceiver, IDisposable
  {
    #region Protected fields

    protected ICollection<string> _registeredChannels;
    protected string _queueName = null;
    protected Queue<SystemMessage> _messages = new Queue<SystemMessage>();
    protected object _syncObj = new object();

    #endregion

    #region Ctor, dtor & Dispose

    protected MessageQueueBase(IEnumerable<string> messageChannels)
    {
      _registeredChannels = new List<string>(messageChannels);
    }

    ~MessageQueueBase()
    {
      Dispose();
    }

    public virtual void Dispose()
    {
      if (_registeredChannels == null)
        return;
      UnregisterFromAllMessageChannels();
      _registeredChannels = null;
    }

    #endregion

    #region Protected members

    protected abstract void HandleMessageAvailable(SystemMessage message);

    public void RegisterAtAllMessageChannels()
    {
      ICollection<string> channels;
      lock (_syncObj)
        channels = new List<string>(_registeredChannels);
      foreach (string channel in channels)
        RegisterAtMessageChannel(channel);
    }

    protected void UnregisterFromAllMessageChannels()
    {
      ICollection<string> channels;
      lock (_syncObj)
        channels = new List<string>(_registeredChannels);
      foreach (string channel in channels)
        UnregisterFromMessageChannel(channel);
    }

    protected void RegisterAtMessageChannel(string channelName)
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.RegisterMessageQueue(channelName, this);
    }

    protected void UnregisterFromMessageChannel(string channelName)
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>(false); // Unregistering might be done after the message broker service has gone
      if (broker != null)
        broker.UnregisterMessageQueue(channelName, this);
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets or sets the name of this queue.
    /// </summary>
    public string QueueName
    {
      get { return _queueName; }
      set { _queueName = value; }
    }

    /// <summary>
    /// Returns the number of messages in the queue.
    /// </summary>
    public int NumMessages
    {
      get
      {
        lock (_syncObj)
          return _messages.Count;
      }
    }

    /// <summary>
    /// Returns the information if messages are awaiting to be fetched.
    /// </summary>
    public bool IsMessagesAvailable
    {
      get { return NumMessages > 0; }
    }

    public void SubscribeToMessageChannel(string channelName)
    {
      lock (_syncObj)
        _registeredChannels.Add(channelName);
      RegisterAtMessageChannel(channelName);
    }

    public void UnsubscribeFromMessageChannel(string channelName)
    {
      lock (_syncObj)
        _registeredChannels.Remove(channelName);
      UnregisterFromMessageChannel(channelName);
    }

    public void UnsubscribeFromAllMessageChannels()
    {
      ICollection<string> channels;
      lock (_syncObj)
      {
        channels = _registeredChannels;
        _registeredChannels = new List<string>();
      }
      foreach (string channel in channels)
        UnregisterFromMessageChannel(channel);
    }

    public SystemMessage Dequeue()
    {
      lock (_syncObj)
        if (_messages.Count > 0)
          return _messages.Dequeue();
        else
          return null;
    }

    public SystemMessage Peek()
    {
      lock (_syncObj)
        return _messages.Peek();
    }

    public void PurgeAll()
    {
      lock (_syncObj)
        _messages.Clear();
    }

    #endregion

    #region IMessageReceiver implementation

    /// <summary>
    /// Enqueues the specified <paramref name="message"/> to this queue. This message will be called
    /// by the <see cref="IMessageBroker"/> service.
    /// </summary>
    /// <param name="message">Message to be enqueued in this message queue.</param>
    public void Receive(SystemMessage message)
    {
      lock (_syncObj)
        _messages.Enqueue(message);
      HandleMessageAvailable(message);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0} '{1}'", GetType().Name, _queueName);
    }

    #endregion
  }
}
