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
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Views
{
  class ServerConnectionChangeNotificator : IViewChangeNotificator
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType type = (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (type)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            FireChanged();
            break;
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

    public void Dispose()
    {
      if (_messageQueue != null)
      {
        _messageQueue.Terminate();
        _messageQueue = null;
      }
    }

    public void Install()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    #endregion
  }
}
