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

using MediaPortal.Common.Messaging;

namespace MediaPortal.Common.Runtime
{
  /// <summary>
  /// This class provides an interface for the messages sent by the system.
  /// </summary>
  public class SystemMessaging
  {
    // Message channel name
    public const string CHANNEL = "System";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// The system state changed to the state given in the NEW_STATE parameter.
      /// </summary>
      SystemStateChanged,

      // TODO: Further events like hibernate, suspend, ...
    }

    // Message data
    public const string NEW_STATE = "NewState"; // Type: SystemState

    /// <summary>
    /// Sends a <see cref="MessageType.SystemStateChanged"/> message.
    /// </summary>
    /// <param name="newState">The state the system will switch to.</param>
    public static void SendSystemStateChangeMessage(SystemState newState)
    {
      SystemMessage msg = new SystemMessage(MessageType.SystemStateChanged);
      msg.MessageData[NEW_STATE] = newState;
      IMessageBroker messageBroker = ServiceRegistration.Get<IMessageBroker>();
      if (messageBroker != null)
        messageBroker.Send(CHANNEL, msg);
    }
  }
}