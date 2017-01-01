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
      /// Internal message to set the super layer asynchronously. The screen to be shown will be given in the
      /// parameter <see cref="SCREEN"/>.
      /// </summary>
      SetSuperLayer,

      /// <summary>
      /// Internal message to show a dialog asynchronously. The dialog to be shown will be given in the
      /// parameter <see cref="DIALOG_DATA"/>.
      /// </summary>
      ShowDialog,

      /// <summary>
      /// Internal message to close multiple dialogs asynchronously. The instance id of the dialog is given in the
      /// parameter <see cref="DIALOG_INSTANCE_ID"/>. The parameter <see cref="ScreenManagerMessaging.CLOSE_DIALOGS_MODE"/>
      /// is set to the desired close mode.
      /// </summary>
      CloseDialogs,

      /// <summary>
      /// Internal message to reload the screen and all open dialogs.
      /// </summary>
      ReloadScreens,

      /// <summary>
      /// Internal message to switch the skin/theme. The parameters <see cref="SKIN_NAME"/> and <see cref="THEME_NAME"/> are set.
      /// </summary>
      SwitchSkinAndTheme,

      /// <summary>
      /// Internal message to set the <see cref="ScreenManager.BackgroundDisabled"/> property.
      /// </summary>
      SetBackgroundDisabled,
    }

    // Message data
    public const string SCREEN = "Screen"; // Type Screen
    public const string CLOSE_DIALOGS = "CloseDialogs"; // Type bool
    public const string DIALOG_DATA = "DialogData"; // Type DialogData
    public const string DIALOG_INSTANCE_ID = "DialogInstanceId"; // Type Guid
    public const string CLOSE_DIALOGS_MODE = "Mode"; // Type CloseDialogsMode
    public const string SKIN_NAME = "SkinName"; // Type string
    public const string THEME_NAME = "ThemeName"; // Type string
    public const string IS_DISABLED = "IsDisabled"; // Type bool

    internal static void SendMessageShowScreen(Screen screen, bool closeDialogs)
    {
      SystemMessage msg = new SystemMessage(MessageType.ShowScreen);
      msg.MessageData[SCREEN] = screen;
      msg.MessageData[CLOSE_DIALOGS] = closeDialogs;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageSetSuperLayer(Screen screen)
    {
      SystemMessage msg = new SystemMessage(MessageType.SetSuperLayer);
      msg.MessageData[SCREEN] = screen;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageShowDialog(DialogData dialogData)
    {
      SystemMessage msg = new SystemMessage(MessageType.ShowDialog);
      msg.MessageData[DIALOG_DATA] = dialogData;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageCloseDialogs(Guid dialogInstanceId, CloseDialogsMode mode)
    {
      SystemMessage msg = new SystemMessage(MessageType.CloseDialogs);
      msg.MessageData[DIALOG_INSTANCE_ID] = dialogInstanceId;
      msg.MessageData[CLOSE_DIALOGS_MODE] = mode;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageReloadScreens()
    {
      SystemMessage msg = new SystemMessage(MessageType.ReloadScreens);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendMessageSwitchSkinAndTheme(string newSkinName, string newThemeName)
    {
      SystemMessage msg = new SystemMessage(MessageType.SwitchSkinAndTheme);
      msg.MessageData[SKIN_NAME] = newSkinName;
      msg.MessageData[THEME_NAME] = newThemeName;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendSetBackgroundDisableMessage(bool disabled)
    {
      SystemMessage msg = new SystemMessage(MessageType.SetBackgroundDisabled);
      msg.MessageData[IS_DISABLED] = disabled;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
