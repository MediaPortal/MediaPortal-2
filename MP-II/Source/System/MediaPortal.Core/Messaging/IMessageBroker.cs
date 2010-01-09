#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
    void Receive(SystemMessage message);
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
    /// <paramref name="channel"/>. The given <paramref name="receiver"/> will be referenced weakly by the message broker
    /// service, i.e. registering a message handler won't prevent the handler from being garbage collected.
    /// </summary>
    void RegisterMessageQueue(string channel, IMessageReceiver receiver);

    /// <summary>
    /// Unregisters the specified message <paramref name="receiver"/> from receiving messages of the specified messages
    /// <paramref name="channel"/>.
    /// </summary>
    void UnregisterMessageQueue(string channel, IMessageReceiver receiver);

    /// <summary>
    /// Sends the specified message in the message channel of the specified <paramref name="channelName"/>.
    /// </summary>
    /// <remarks>
    /// This method may be called while holding locks.
    /// </remarks>
    /// <param name="channelName">Name of the message channel to be used for sending the message.</param>
    /// <param name="msg">Message to send.</param>
    void Send(string channelName, SystemMessage msg);
  }
}
