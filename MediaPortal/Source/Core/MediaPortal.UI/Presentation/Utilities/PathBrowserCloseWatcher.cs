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
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Presentation.Utilities
{
  public delegate void PathChoosenHandlerDlgt(ResourcePath choosenPath);
  public delegate void DialogCancelledHandlerDlgt();

  /// <summary>
  /// Watcher class to react to close messages from <see cref="IPathBrowser"/> dialogs.
  /// </summary>
  /// <remarks>
  /// The caller must hold a strong reference to instances of this class, else it might be collected by the garbage collector
  /// before it receives the dialog's close message.
  /// </remarks>
  public class PathBrowserCloseWatcher : IDisposable
  {
    protected MessageWatcher _watcher;

    public PathBrowserCloseWatcher(object owner, Guid dialogHandleId, PathChoosenHandlerDlgt pathChoosenHandler, DialogCancelledHandlerDlgt cancelledHandler)
    {
      _watcher = new MessageWatcher(owner, PathBrowserMessaging.CHANNEL, message =>
        {
          if (message.ChannelName == PathBrowserMessaging.CHANNEL)
          {
            PathBrowserMessaging.MessageType messageType = (PathBrowserMessaging.MessageType) message.MessageType;
            if (messageType == PathBrowserMessaging.MessageType.PathChoosen)
            {
              Guid closedDialogHandle = (Guid) message.MessageData[PathBrowserMessaging.DIALOG_HANDLE];
              ResourcePath choosenPath = (ResourcePath) message.MessageData[PathBrowserMessaging.CHOOSEN_PATH];
              if (closedDialogHandle == dialogHandleId)
              {
                if (pathChoosenHandler != null)
                  pathChoosenHandler(choosenPath);
                return true;
              }
            }
            else if (messageType == PathBrowserMessaging.MessageType.DialogCancelled)
            {
              if (cancelledHandler != null)
                cancelledHandler();
              return true;
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