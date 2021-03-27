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

using MediaPortal.Common.MediaManagement;
using System;
using MediaPortal.Common.Messaging;

namespace MediaPortal.UiComponents.Media.Helpers
{
  public sealed class AspectWrapperHelper : IDisposable
  {
    private static readonly Lazy<AspectWrapperHelper> _staticInstance = new Lazy<AspectWrapperHelper>(() => new AspectWrapperHelper());
    public static AspectWrapperHelper Instance => _staticInstance.Value;

    private AsynchronousMessageQueue _messageQueue;

    public AspectWrapperHelper()
    {
      SubscribeToMessages();
    }

    private void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(typeof(AspectWrapperHelper), new string[]
      {
        ContentDirectoryMessaging.CHANNEL,
      });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.MediaItemChanged:
            MediaItem mediaItem = (MediaItem)message.MessageData[ContentDirectoryMessaging.MEDIA_ITEM];
            ContentDirectoryMessaging.MediaItemChangeType changeType = (ContentDirectoryMessaging.MediaItemChangeType)message.MessageData[ContentDirectoryMessaging.MEDIA_ITEM_CHANGE_TYPE];
            if (changeType == ContentDirectoryMessaging.MediaItemChangeType.Updated)
              MediaItemChanged?.Invoke(mediaItem);
            break;
        }
      }
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    public MediaItemChangedHandler MediaItemChanged { get; set; }
  }

  public delegate void MediaItemChangedHandler(MediaItem mediaItem);
}
