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

using System.Globalization;
using MediaPortal.Common.Messaging;

namespace MediaPortal.Common.Localization
{
  /// <summary>
  /// This class provides an interface for the messages sent by the localization strings manager.
  /// This class is part of the localization API.
  /// </summary>
  public class LocalizationMessaging
  {
    // Message channel name
    public const string CHANNEL = "Localization";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// This message will be sent after the <see cref="ILocalization.CurrentCulture"/> is changed.
      /// </summary>
      LanguageChanged,
    }

    // Message data
    public const string NEW_CULTURE = "NewCulture"; // The new culture which was set

    public static void SendLanguageChangedMessage(CultureInfo newCulture)
    {
      SystemMessage msg = new SystemMessage(MessageType.LanguageChanged);
      msg.MessageData[NEW_CULTURE] = newCulture;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
