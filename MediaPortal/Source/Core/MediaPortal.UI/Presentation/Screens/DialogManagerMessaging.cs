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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;

namespace MediaPortal.UI.Presentation.Screens
{
  public enum DialogResult
  {
    Ok,
    Yes,
    No,
    Cancel
  }

  /// <summary>
  /// This class provides an interface for the messages sent by the dialog manager.
  /// This class is part of the dialog manager API.
  /// </summary>
  public static class DialogManagerMessaging
  {
    // Message Queue name
    public const string CHANNEL = "DialogManager";

    // Message type
    public enum MessageType
    {
      DialogClosed,
    }

    // Message data
    public const string DIALOG_HANDLE = "DialogHandle"; // Type: Guid
    public const string DIALOG_RESULT = "DialogResult"; // Type: DialogResult

    public static void SendDialogManagerMessage(Guid dialogHandle, DialogResult result)
    {
      SystemMessage msg = new SystemMessage(MessageType.DialogClosed);
      msg.MessageData[DIALOG_HANDLE] = dialogHandle;
      msg.MessageData[DIALOG_RESULT] = result;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
