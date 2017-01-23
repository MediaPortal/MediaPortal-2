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
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Services.Players.VideoPlayerSynchronizationStrategies
{
  /// <summary>
  /// Abstract video player synchronization strategy implementation to inherit from.
  /// </summary>
  public abstract class BaseVideoPlayerSynchronizationStrategy : IVideoPlayerSynchronizationStrategy
  {
    protected readonly AsynchronousMessageQueue _messageQueue;

    protected BaseVideoPlayerSynchronizationStrategy()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            PlayerManagerMessaging.CHANNEL,
            PlayerContextManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerStarted:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
            HandlePlayerChange();
            break;
          case PlayerManagerMessaging.MessageType.PlaybackStateChanged:
            HandlePlaybackStateChanged();
            break;
        }
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType = (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.PlayerSlotsChanged:
            HandlePlayerChange();
            break;
        }
      }
    }

    protected abstract IVideoPlayer GetPlayerToSynchronize();

    private void HandlePlayerChange()
    {
      IVideoPlayer player = GetPlayerToSynchronize();
      FireUpdateVideoPlayerState(player);
      FireSynchronizeToVideoPlayerFramerate(player);
    }

    private void HandlePlaybackStateChanged()
    {
      IVideoPlayer player = GetPlayerToSynchronize();
      FireUpdateVideoPlayerState(player);
    }

    protected void FireUpdateVideoPlayerState(IVideoPlayer player)
    {
      UpdateVideoPlayerStateDlgt dlgt = UpdateVideoPlayerState;
      if (dlgt != null)
        dlgt(player);
    }

    protected void FireSynchronizeToVideoPlayerFramerate(IVideoPlayer player)
    {
      SynchronizeToVideoPlayerFramerateDlgt dlgt = SynchronizeToVideoPlayerFramerate;
      if (dlgt != null)
        dlgt(player);
    }

    public event UpdateVideoPlayerStateDlgt UpdateVideoPlayerState;
    public event SynchronizeToVideoPlayerFramerateDlgt SynchronizeToVideoPlayerFramerate;

    public void Start()
    {
      _messageQueue.Start();
    }

    public void Stop()
    {
      _messageQueue.Terminate();
    }
  }
}