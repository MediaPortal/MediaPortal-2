#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.BackgroundManager.Helper;
using MediaPortal.UiComponents.BackgroundManager.Settings;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class VideoBackgroundModel : IDisposable
  {
    public const string MODEL_ID_STR = "441288AC-F88D-4186-8993-6E259F7C75D8";

    protected string _videoFilename;
    protected AbstractProperty _isEnabledProperty;
    protected AsynchronousMessageQueue _messageQueue;

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool) _isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public VideoBackgroundModel()
    {
      _isEnabledProperty = new SProperty(typeof(bool), false);
      _messageQueue = new AsynchronousMessageQueue(this, new[] { BackgroundManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
      RefreshSettings();
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == BackgroundManagerMessaging.CHANNEL)
      {
        BackgroundManagerMessaging.MessageType messageType = (BackgroundManagerMessaging.MessageType) message.MessageType;
        if (messageType == BackgroundManagerMessaging.MessageType.SettingsChanged)
          RefreshSettings(true);
      }
    }

    /// <summary>
    /// Loads settings on startup or when changed inside configuration.
    /// </summary>
    protected void RefreshSettings(bool refresh = false)
    {
      EndBackgroundPlayback();
      BackgroundManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<BackgroundManagerSettings>();
      _videoFilename = settings.VideoBackgroundFileName;
      IsEnabled = settings.EnableVideoBackground && !string.IsNullOrEmpty(_videoFilename) && File.Exists(_videoFilename);
      if (IsEnabled && refresh)
        StartBackgroundPlayback();
    }

    public void Dispose()
    {
      EndBackgroundPlayback();
      _messageQueue.Shutdown();
    }

    public void EndBackgroundPlayback()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.CloseSlot(PlayerManagerConsts.BACKGROUND_SLOT);
    }

    public void StartBackgroundPlayback()
    {
      if (!IsEnabled)
        return;

      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IPlayerSlotController playerSlotController;
      playerManager.ResetSlot(PlayerManagerConsts.BACKGROUND_SLOT, out playerSlotController);
      VideoPlayer videoPlayer = null;
      try
      {
        ResourceLocator resourceLocator = new ResourceLocator(LocalFsResourceProviderBase.ToResourcePath(_videoFilename));
        videoPlayer = new VideoPlayer { AutoRepeat = true };
        videoPlayer.SetMediaItem(resourceLocator, "VideoBackground");
        playerSlotController.Play(videoPlayer);
      }
      catch (Exception)
      {
        if (videoPlayer != null)
          videoPlayer.Dispose();
      }
    }
  }
}
