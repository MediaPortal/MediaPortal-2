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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;

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
      /// </summary>
      ClientAttached,

      /// <summary>
      /// A MediaPortal client was detached from this server.
      /// </summary>
      ClientDetached,

      /// <summary>
      /// An attached client has become online.
      /// </summary>
      ClientOnline,

      /// <summary>
      /// An attached client has become offline.
      /// </summary>
      ClientOffline,

      /// <summary>
      /// Internal message. Will be sent if the server should validate whether an attached client is still attached.
      /// </summary>
      ValidateAttachmentState,
    }

    // Message data
    public const string CLIENT_DESCRIPTOR = "ClientDescriptor"; // Contains an instance of ClientDescriptor

    /// <summary>
    /// Sends a message concerning a client.
    /// </summary>
    /// <param name="messageType">One of the <see cref="MessageType"/> messages.</param>
    /// <param name="client">Descriptor describing the client which is affected.</param>
    public static void SendConnectionStateChangedMessage(MessageType messageType, ClientDescriptor client)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[CLIENT_DESCRIPTOR] = client;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
