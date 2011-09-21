#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.Messaging;

namespace MediaPortal.UI.Presentation.Screens
{
  public delegate bool CloseHandlerDlgt(DialogResult dialogResult);

  /// <summary>
  /// Watcher class to react to close messages from the <see cref="IDialogManager"/>.
  /// </summary>
  /// <remarks>
  /// The caller must hold a strong reference to instances of this class, else it might be collected by the garbage collector
  /// before it receives the dialog's close message.
  /// </remarks>
  public class DialogCloseWatcher : IDisposable
  {
    protected MessageWatcher _watcher;

    public DialogCloseWatcher(object owner, Guid dialogHandleId, CloseHandlerDlgt handler)
    {
      _watcher = new MessageWatcher(owner, DialogManagerMessaging.CHANNEL, message =>
        {
          if (message.ChannelName == DialogManagerMessaging.CHANNEL)
          {
            DialogManagerMessaging.MessageType messageType = (DialogManagerMessaging.MessageType) message.MessageType;
            if (messageType == DialogManagerMessaging.MessageType.DialogClosed)
            {
              Guid closedDialogHandle = (Guid) message.MessageData[DialogManagerMessaging.DIALOG_HANDLE];
              DialogResult dialogResult = (DialogResult) message.MessageData[DialogManagerMessaging.DIALOG_RESULT];
              if (closedDialogHandle == dialogHandleId)
                return handler(dialogResult);
            }
          }
          return false;
        }, true);
      _watcher.Start();
    }
    
    public void Dispose()
    {
      _watcher.Dispose();
    }
  }
}