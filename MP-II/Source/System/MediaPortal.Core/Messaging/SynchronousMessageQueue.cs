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

using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Messaging
{
  public delegate void MessagesAvailableHandler(SynchronousMessageQueue queue);

  /// <summary>
  /// Synchronous message queue to be used by message receivers.
  /// </summary>
  public class SynchronousMessageQueue : MessageQueueBase
  {
    /// <summary>
    /// Creates a new synchronous message queue.
    /// </summary>
    /// <param name="queueName">Name of this message queue.<param>
    /// <param name="messageChannels">Message channels this message queue will be registered at the message broker.</param>
    public SynchronousMessageQueue(string queueName, string[] messageChannels) : base(queueName, messageChannels) { }

    /// <summary>
    /// Handler which will be called synchronously in the message sender when a new message was received for one of the
    /// registered message types of this queue.
    /// </summary>
    /// <remarks>
    /// Handlers of this event mustn't aquire any multithreading locks in the system synchronously, as this could lead
    /// to deadlocks.
    /// </remarks>
    public event MessagesAvailableHandler MessagesAvailable;

    protected override void HandleMessageAvailable(QueueMessage message)
    {
      MessagesAvailableHandler handler = MessagesAvailable;
      if (handler == null)
      {
        ServiceScope.Get<ILogger>().Warn(
          "SynchronousMessageQueue: Synchronous message queue '{0}' has no message handler and there are already {1} messages to be delivered",
              _queueName, NumMessages);
        return;
      }
      handler(this);
    }
  }
}
