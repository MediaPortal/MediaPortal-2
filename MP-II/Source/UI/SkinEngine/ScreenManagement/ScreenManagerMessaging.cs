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

namespace MediaPortal.SkinEngine.ScreenManagement
{
  /// <summary>
  /// This class provides an interface for the messages sent by the screen manager.
  /// </summary>
  public class ScreenManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "WorkflowManager";

    /// <summary>
    /// Messages of this type are sent by the <see cref="ScreenManager"/>.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// Internal message to hide screens asynchronously.
      /// </summary>
      InternalHideScreens,
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a <see cref="MessageType.InternalHideScreens"/> message.
    /// </summary>
    public static void InternalSendHideScreensMessage()
    {
      QueueMessage msg = new QueueMessage(MessageType.InternalHideScreens);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
