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

using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.UI.General
{
  /// <summary>
  /// This class provides an interface for broadcasting and receiving messages sent by
  /// windows to the main form.
  /// </summary>
  public static class WindowsMessaging
  {
    // Message channel name
    public const string CHANNEL = "Windows";

    // Message type
    public enum MessageType
    {
      WindowsBroadcast,
    }

    // Message data
    public const string MESSAGE = "Message"; // Windows message stored as System.Windows.Forms.Message - Take care to copy the message back to the message data after modifying it, else the auto unboxing will prevent applying the new values

    public static void BroadcastWindowsMessage(ref Message message)
    {
      QueueMessage msg = new QueueMessage(MessageType.WindowsBroadcast);
      msg.MessageData[MESSAGE] = message;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
      // Copy message back to the ref message
      message = (Message) msg.MessageData[MESSAGE];
    }
  }
}
