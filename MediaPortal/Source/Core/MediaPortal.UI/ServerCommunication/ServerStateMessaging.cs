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
using System;
using System.Collections.Generic;

namespace MediaPortal.UI.ServerCommunication
{
  /// <summary>
  /// Provides an interface for the messages sent by the ServerStateManager.
  /// </summary>
  public class ServerStateMessaging
  {
    // Message channel name
    public const string CHANNEL = "ServerStateChannel";
    public const string STATES = "STATES";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// Indicates that one or more states have changed.
      /// The parameter <see cref="STATES"/> will contain a mapping of state guids to state objects of those states
      /// that have changed.
      /// </summary>
      StatesChanged
    }

    /// <summary>
    /// Sends a message which announces changes to the given <paramref name="states"/>.
    /// </summary>
    /// <param name="states">Mapping of changed state guids and state objects</param>
    public static void SendStatesChangedMessage(IDictionary<Guid, object> states)
    {
      SystemMessage msg = new SystemMessage(MessageType.StatesChanged);
      msg.MessageData[STATES] = states;
      SendMessage(msg);
    }

    private static void SendMessage(SystemMessage msg)
    {
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
