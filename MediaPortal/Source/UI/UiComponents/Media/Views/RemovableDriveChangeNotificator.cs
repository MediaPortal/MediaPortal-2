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
using MediaPortal.UI.RemovableMedia;

namespace MediaPortal.UiComponents.Media.Views
{
  public class RemovableDriveChangeNotificator : IViewChangeNotificator
  {
    protected string _drive;
    protected AsynchronousMessageQueue _messageQueue = null;

    public RemovableDriveChangeNotificator(string drive)
    {
      _drive = drive == null ? null : drive.Substring(0, 2);
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            RemovableMediaMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == RemovableMediaMessaging.CHANNEL)
      {
        RemovableMediaMessaging.MessageType messageType = (RemovableMediaMessaging.MessageType) message.MessageType;
        if (messageType == RemovableMediaMessaging.MessageType.MediaInserted ||
            messageType == RemovableMediaMessaging.MessageType.MediaRemoved)
        {
          string drive = (string) message.MessageData[RemovableMediaMessaging.DRIVE_LETTER];
          if (drive == null || drive == _drive)
            FireChanged();
        }
      }
    }

    protected void FireChanged()
    {
      ViewChangedDlgt dlgt = Changed;
      if (dlgt != null)
        dlgt();
    }

    #region IViewChangeNotificator implementation

    public event ViewChangedDlgt Changed;

    public void Install()
    {
      SubscribeToMessages();
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion
  }
}