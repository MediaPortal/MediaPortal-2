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
using System;

namespace MediaPortal.Common.Services.Settings
{
  /// <summary>
  /// This class provides an interface for the messages sent by the system.
  /// </summary>
  public class SettingsManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "SettingsManager";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// A setting was changed by SettingsManager, it's type is sent as SETTINGSTYPE parameter.
      /// </summary>
      SettingsChanged,
    }

    // Message data
    public const string SETTINGSTYPE = "SettingsType"; // Type: C# Type

    /// <summary>
    /// Sends a <see cref="MessageType.SettingsChanged"/> message.
    /// </summary>
    /// <param name="settingsType">The Type of setting that was changed.</param>
    public static void SendSettingsChangeMessage(Type settingsType)
    {
      SystemMessage msg = new SystemMessage(MessageType.SettingsChanged);
      msg.MessageData[SETTINGSTYPE] = settingsType;
      IMessageBroker messageBroker = ServiceRegistration.Get<IMessageBroker>(false);
      if (messageBroker != null)
        messageBroker.Send(CHANNEL, msg);
    }
  }
}
