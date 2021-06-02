#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Plugins.InputDeviceManager.Messaging
{
  public static class InputDeviceMessaging
  {
    // Message channel name
    public const string CHANNEL = "InputDevices";

    // Message type
    public enum MessageType
    {
      HidBroadcast
    }

    public const string HID_EVENT = "HidEvent"; // USB HID event data

    public static void BroadcastHidMessage(object hidEvent)
    {
      SystemMessage msg = new SystemMessage(MessageType.HidBroadcast);
      msg.MessageData[HID_EVENT] = hidEvent;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
