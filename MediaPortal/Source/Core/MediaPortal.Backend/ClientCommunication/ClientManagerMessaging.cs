#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// This class provides an interface for the messages sent by the client manager.
  /// This class is part of the client manager API.
  /// </summary>
  public class ClientManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "ClientManager";

    /// <summary>
    /// Messages of this type are sent by the <see cref="IClientManager"/>.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// A new MediaPortal client was attached to this server.
      /// The <see cref="CLIENT_SYSTEM"/> is set.
      /// </summary>
      ClientAttached,

      /// <summary>
      /// A MediaPortal client was detached from this server.
      /// The <see cref="CLIENT_SYSTEM"/> is set.
      /// </summary>
      ClientDetached,

      /// <summary>
      /// An attached client has become online.
      /// The <see cref="CLIENT_DESCRIPTOR"/> is set.
      /// </summary>
      ClientOnline,

      /// <summary>
      /// An attached client has become offline.
      /// The <see cref="CLIENT_DESCRIPTOR"/> is set.
      /// </summary>
      ClientOffline,
    }

    // Message data
    public const string CLIENT_DESCRIPTOR = "ClientDescriptor"; // Contains an instance of ClientDescriptor
    public const string CLIENT_SYSTEM = "ClientSystemId"; // Contains a string with the system ID of a client

    /// <summary>
    /// Sends a message concerning a client.
    /// </summary>
    /// <param name="messageType">One of the <see cref="MessageType"/> messages.</param>
    /// <param name="client">Descriptor describing the client which is affected.</param>
    public static void SendConnectionStateChangedMessage(MessageType messageType, ClientDescriptor client)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[CLIENT_DESCRIPTOR] = client;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a client attach or detach message.
    /// </summary>
    /// <param name="messageType">One of the <see cref="MessageType"/> messages.</param>
    /// <param name="clientSystemId">System ID of the client that was detached.</param>
    public static void SendClientAttachmentChangeMessage(MessageType messageType, string clientSystemId)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[CLIENT_SYSTEM] = clientSystemId;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
