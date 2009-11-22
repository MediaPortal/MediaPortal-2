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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.UI.ServerCommunication
{
  /// <summary>
  /// This class provides an interface for the messages sent by the server connection manager.
  /// This class is part of the server connection manager API.
  /// </summary>
  public class ServerConnectionMessaging
  {
    // Message channel name
    public const string CHANNEL = "ServerConnectionManager";

    /// <summary>
    /// Messages of this type are sent by the <see cref="IServerConnectionManager"/>.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// A new MediaPortal server is available in the network or was removed from the network.
      /// The <see cref="QueueMessage.MessageData"/> contains an entry for
      /// <see cref="ServerConnectionMessaging.AVAILABLE_SERVERS"/> containing a collection of server descriptors currently
      /// available in the local network and an entry for <see cref="ServerConnectionMessaging.SERVERS_WERE_ADDED"/> containing
      /// a bool which indicates that new servers were added to that collection until the last message of this type.
      /// </summary>
      AvailableServersChanged,

      /// <summary>
      /// The client conneted to the homeserver.
      /// </summary>
      HomeServerConnected,

      /// <summary>
      /// The homeserver was disconnected.
      /// </summary>
      HomeServerDisconnected,

      /// <summary>
      /// A MediaPortal server was attached.
      /// </summary>
      HomeServerAttached,

      /// <summary>
      /// The home server was detached.
      /// </summary>
      HomeServerDetached,
    }

    // Message data
    public const string AVAILABLE_SERVERS = "AvailableServers"; // Contains a collection of ServerDescriptor instances
    public const string SERVERS_WERE_ADDED = "ServersWereAdded"; // Contains a bool

    /// <summary>
    /// Sends a <see cref="MessageType.HomeServerConnected"/> or <see cref="MessageType.HomeServerDisconnected"/> message.
    /// </summary>
    /// <param name="messageType">One of the <see cref="MessageType.HomeServerConnected"/> or
    /// <see cref="MessageType.HomeServerDisconnected"/> messages.</param>
    public static void SendConnectionStateChangedMessage(MessageType messageType)
    {
      QueueMessage msg = new QueueMessage(messageType);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a <see cref="MessageType.AvailableServersChanged"/> message.
    /// </summary>
    /// <param name="availableServers">Collection of descriptors of available servers.</param>
    /// <param name="serversWereAdded"><c>true</c> if new servers are present from the last notification.</param>
    public static void SendAvailableServersChangedMessage(ICollection<ServerDescriptor> availableServers, bool serversWereAdded)
    {
      QueueMessage msg = new QueueMessage(MessageType.AvailableServersChanged);
      msg.MessageData[SERVERS_WERE_ADDED] = serversWereAdded;
      msg.MessageData[AVAILABLE_SERVERS] = availableServers;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
