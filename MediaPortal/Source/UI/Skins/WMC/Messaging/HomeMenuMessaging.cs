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
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.WMCSkin.Messaging
{
  /// <summary>
  /// This class provides an interface for the messages sent by the home menu model.
  /// </summary>
  public class HomeMenuMessaging
  {
    // Message channel name
    public const string CHANNEL = "WMCHomeMenu";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// The current menu item changed.
      /// </summary>
      CurrentItemChanged,
    }

    // Message data
    public const string NEW_ITEM = "CurrentItem"; // Type: ListItem

    /// <summary>
    /// Sends a <see cref="MessageType.CurrentItemChanged"/> message.
    /// </summary>
    /// <param name="newItem">The new current item.</param>
    public static void SendCurrentItemChangeMessage(ListItem newItem)
    {
      SystemMessage msg = new SystemMessage(MessageType.CurrentItemChanged);
      msg.MessageData[NEW_ITEM] = newItem;
      IMessageBroker messageBroker = ServiceRegistration.Get<IMessageBroker>();
      if (messageBroker != null)
        messageBroker.Send(CHANNEL, msg);
    }
  }
}
