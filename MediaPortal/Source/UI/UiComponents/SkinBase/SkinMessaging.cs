#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.UiComponents.SkinBase
{
  /// <summary>
  /// This class provides an interface for the messages sent in this plugin.
  /// </summary>
  public static class SkinMessaging
  {
    // Message channel name
    public const string CHANNEL = "SkinMessages";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// This message will be sent when the skin's date format or time format was changed.
      /// </summary>
      DateTimeFormatChanged,
    }

    public static void SendSkinMessage(MessageType messageType)
    {
      // Send Startup Finished Message.
      SystemMessage msg = new SystemMessage(messageType);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}