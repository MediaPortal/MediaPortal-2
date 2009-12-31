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

namespace MediaPortal.Core.Messaging
{
  public interface IMessageReceiver
  {
    void Receive(QueueMessage message);
  }

  /// <summary>
  /// Message delivery service.
  /// </summary>
  /// <remarks>
  /// This service is thread-safe.
  /// </remarks>
  public interface IMessageBroker
  {
    /// <summary>
    /// Registers the specified message <paramref name="receiver"/> to receive messages of the specified messages
    /// <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Name of the channel whose messages should be sent to the given
    /// <paramref name="receiver"/>.</param>
    /// <param name="receiver">Receiver instance which will receive all messages of the given message
    /// <paramref name="channel"/>.</param>
    void RegisterMessageQueue(string channel, IMessageReceiver receiver);

    /// <summary>
    /// Unregisters the specified message <paramref name="receiver"/> from receiving messages of the specified messages
    /// <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Name of the channel where the given <paramref name="receiver"/> should be removed.</param>
    /// <param name="receiver">Receiver instance which will be removed from the given message
    /// <paramref name="channel"/>.</param>
    void UnregisterMessageQueue(string channel, IMessageReceiver receiver);

    /// <summary>
    /// Sends the specified message in the message channel of the specified <paramref name="channelName"/>.
    /// </summary>
    /// <remarks>
    /// This method may be called while holding locks.
    /// </remarks>
    /// <param name="channelName">Name of the message channel to be used for sending the message.</param>
    /// <param name="msg">Message to send.</param>
    void Send(string channelName, QueueMessage msg);
  }
}
