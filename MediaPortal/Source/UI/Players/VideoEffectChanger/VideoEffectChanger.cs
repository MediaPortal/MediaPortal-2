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
using System.Timers;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UiComponents.VideoEffectChanger.Settings;

namespace MediaPortal.UiComponents.VideoEffectChanger
{
  public class VideoEffectChanger
  {
    private double DEFAULT_CHECK_INTERVAL = 1000;

    private AsynchronousMessageQueue _messageQueue;
    private readonly SettingsChangeWatcher<VideoEffectChangerSettings> _settings = new SettingsChangeWatcher<VideoEffectChangerSettings>();
    private Timer _timer;

    public VideoEffectChanger()
    {
      _settings.SettingsChanged += ConfigureHandler;
      ConfigureHandler();
    }

    private void ConfigureHandler(object sender, EventArgs e)
    {
      ConfigureHandler();
    }

    private void ConfigureHandler()
    {
      if (_settings.Settings.IsEnabled)
      {
        SubscribeToMessages();
      }
      else
      {
        UnsubscribeFromMessages();
      }
    }

    void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
           SystemMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();

      _timer = new Timer(DEFAULT_CHECK_INTERVAL);
      _timer.Elapsed += CheckVideoResolution;
      _timer.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;

      _timer.Dispose();
      _timer = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
          if (sss.CurrentState == SystemState.ShuttingDown || sss.CurrentState == SystemState.Ending)
          {
            UnsubscribeFromMessages();
          }
      }
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        // React to player changes
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerStarted:
            IPlayerSlotController psc = (IPlayerSlotController)message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandleVideoEffectSelection(psc);
            break;
        }
      }
    }

    private void CheckVideoResolution(object sender, ElapsedEventArgs e)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>(false);
      if (playerContextManager == null)
        return;
      for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
      {
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
        if (playerContext == null || playerContext.PlayerSlotController == null)
          continue;
        HandleVideoEffectSelection(playerContext.PlayerSlotController);
      }
    }

    private void HandleVideoEffectSelection(IPlayerSlotController psc)
    {
      ISharpDXVideoPlayer player = psc.CurrentPlayer as ISharpDXVideoPlayer;
      if (player == null)
        return;

      lock (player.SurfaceLock)
      {
        if (player.Texture == null)
          return;

        var videoFrameHeight = player.VideoSize.Height;
        player.EffectOverride = videoFrameHeight <= _settings.Settings.ResolutionLimit ?
          _settings.Settings.LowerResolutionEffect :
          _settings.Settings.HigherResolutionEffect;
      }
    }
  }
}
