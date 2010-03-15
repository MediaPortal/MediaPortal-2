#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  /// <summary>
  /// This class provides an interface for the messages sent by the screen manager.
  /// </summary>
  public class ScreenManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "ScreenManager";

    /// <summary>
    /// Messages of this type are sent by the <see cref="ScreenManager"/>.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// Internal message to show a screen asynchronously. The screen to be shown will be given in the
      /// parameter <see cref="SCREEN"/>. A bool indicating if open dialogs should be closed will be given in the
      /// parameter <see cref="CLOSE_DIALOGS"/>.
      /// </summary>
      ShowScreen,

      /// <summary>
      /// Internal message to show a dialog asynchronously. The dialog to be shown will be given in the
      /// parameter <see cref="SCREEN"/>.
      /// </summary>
      ShowDialog,

      /// <summary>
      /// Internal message to close a dialog asynchronously. The dialog data structure of the dialog to close is
      /// given in the parameter <see cref="DIALOG_DATA"/>.
      /// </summary>
      CloseDialog,

      /// <summary>
      /// Internal message to reload the screen and all open dialogs.
      /// </summary>
      ReloadScreens,
    }

    // Message data
    public const string SCREEN = "Screen"; // Type Screen
    public const string CLOSE_DIALOGS = "CloseDialogs"; // Type bool
    public const string DIALOG_DATA = "DialogData"; // Type DialogData
    public const string DIALOG_CLOSE_CALLBACK = "DialogCloseCallback"; // Type DialogCloseCallbackDlgt

    internal static void SendMessageShowScreen(Screen screen, bool closeDialogs)
    {
      SystemMessage msg = new SystemMessage(MessageType.ShowScreen);
      msg.MessageData[SCREEN] = screen;
      msg.MessageData[CLOSE_DIALOGS] = closeDialogs;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageShowDialog(Screen dialog, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      SystemMessage msg = new SystemMessage(MessageType.ShowDialog);
      msg.MessageData[SCREEN] = dialog;
      msg.MessageData[DIALOG_CLOSE_CALLBACK] = dialogCloseCallback;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageCloseDialog(DialogData dd)
    {
      SystemMessage msg = new SystemMessage(MessageType.CloseDialog);
      msg.MessageData[DIALOG_DATA] = dd;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageReloadScreens()
    {
      SystemMessage msg = new SystemMessage(MessageType.ReloadScreens);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
