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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.UI.Presentation.SkinResources
{
  /// <summary>
  /// This class provides an interface for the messages sent by the skin resource manager (located in the SkinEngine).
  /// </summary>
  public class SkinResourcesMessaging
  {
    // Message channel name
    public const string CHANNEL = "SkinResources";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// Sent when the resource collection of skin resources changed. This is the case if plugins
      /// are added or removed, for example.
      /// </summary>
      SkinResourcesChanged,
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a parameterless skin resource message.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    public static void SendSkinResourcesMessage(MessageType type)
    {
      QueueMessage msg = new QueueMessage(type);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
