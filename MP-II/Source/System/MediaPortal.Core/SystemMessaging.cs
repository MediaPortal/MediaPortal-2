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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Core
{
  /// <summary>
  /// This class provides an interface for the messages sent by the system.
  /// </summary>
  public class SystemMessaging
  {
    // Message Queue name
    public const string QUEUE = "System";

    public enum MessageType
    {
      /// <summary>
      /// All initialization has been done, all plugins are loaded.
      /// </summary>
      SystemStarted,

      /// <summary>
      /// The system will go down now.
      /// </summary>
      SystemShutdown,
    }

    // Message data
    public const string MESSAGE_TYPE = "MessagType"; // Message type stored as MessageType

    /// <summary>
    /// Sends a parameterless system message.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    public static void SendSystemMessage(MessageType type)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(QUEUE);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[MESSAGE_TYPE] = type;
      queue.Send(msg);
    }
  }
}
