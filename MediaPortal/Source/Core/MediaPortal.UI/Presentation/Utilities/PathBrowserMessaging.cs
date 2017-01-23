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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Presentation.Utilities
{
  /// <summary>
  /// This class provides an interface for the messages sent by the path browser.
  /// This class is part of the path browser API.
  /// </summary>
  public static class PathBrowserMessaging
  {
    // Message Queue name
    public const string CHANNEL = "PathBrowser";

    // Message type
    public enum MessageType
    {
      PathChoosen,
      DialogCancelled,
    }

    // Message data
    public const string DIALOG_HANDLE = "DialogHandle"; // Type: Guid
    public const string CHOOSEN_PATH = "ChoosenPath"; // Type: ResourcePath

    public static void SendPathChoosenMessage(Guid dialogHandle, ResourcePath choosenPath)
    {
      SystemMessage msg = new SystemMessage(MessageType.PathChoosen);
      msg.MessageData[DIALOG_HANDLE] = dialogHandle;
      msg.MessageData[CHOOSEN_PATH] = choosenPath;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendDialogCancelledMessage(Guid dialogHandle)
    {
      SystemMessage msg = new SystemMessage(MessageType.DialogCancelled);
      msg.MessageData[DIALOG_HANDLE] = dialogHandle;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
