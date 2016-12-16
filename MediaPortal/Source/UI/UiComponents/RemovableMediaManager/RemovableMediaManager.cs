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

using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.RemovableMedia;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Views.RemovableMediaDrives;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.RemovableMediaManager.Settings;

namespace MediaPortal.UiComponents.RemovableMediaManager
{
  public class RemovableMediaManager : IPluginStateTracker
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    #endregion

    void SubscribeToMessages()
    {
      lock (_syncObj)
      {
        _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
             RemovableMediaMessaging.CHANNEL
          });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();
      }
    }

    void UnsubscribeFromMessages()
    {
      lock (_syncObj)
      {
        if (_messageQueue == null)
          return;
        _messageQueue.Shutdown();
        _messageQueue = null;
      }
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == RemovableMediaMessaging.CHANNEL)
      {
        RemovableMediaMessaging.MessageType messageType = (RemovableMediaMessaging.MessageType) message.MessageType;
        if (messageType == RemovableMediaMessaging.MessageType.MediaInserted)
        {
          string drive = (string) message.MessageData[RemovableMediaMessaging.DRIVE_LETTER];
          RemovableMediaManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaManagerSettings>();
          if (settings.AutoPlay == AutoPlayType.AutoPlay)
          {
            IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
            if (!pcm.IsFullscreenContentWorkflowStateActive)
              ExamineVolume(drive);
          }
        }
      }
    }

    /// <summary>
    /// Examines the media in the given <paramref name="drive"/> and auto-plays the inserted media. Depending on the type of the
    /// inserted media, different play modes are choosen.
    /// </summary>
    /// <param name="drive">Drive to be examined. Format: <c>D:</c>.</param>
    protected void ExamineVolume(string drive)
    {
      if (string.IsNullOrEmpty(drive))
        return;

      DriveInfo driveInfo = new DriveInfo(drive);
      VideoDriveHandler vdh;
      AudioCDDriveHandler acddh;
      MultimediaDriveHandler mcddh;
      if ((vdh = VideoDriveHandler.TryCreateVideoDriveHandler(driveInfo, Consts.NECESSARY_AUDIO_MIAS)) != null)
        PlayItemsModel.CheckQueryPlayAction(vdh.VideoItem);
      else if ((acddh = AudioCDDriveHandler.TryCreateAudioCDDriveHandler(driveInfo)) != null)
        PlayItemsModel.CheckQueryPlayAction(() => acddh.GetAllMediaItems(), AVType.Audio);
      else if ((mcddh = MultimediaDriveHandler.TryCreateMultimediaCDDriveHandler(driveInfo,
          Consts.NECESSARY_VIDEO_MIAS, Consts.NECESSARY_AUDIO_MIAS, Consts.NECESSARY_IMAGE_MIAS)) != null)
        switch (mcddh.MediaType)
        {
          case MultiMediaType.Video:
          case MultiMediaType.Image:
            PlayItemsModel.CheckQueryPlayAction(() => mcddh.GetAllMediaItems(), AVType.Video);
            break;
          case MultiMediaType.Audio:
            PlayItemsModel.CheckQueryPlayAction(() => mcddh.GetAllMediaItems(), AVType.Audio);
            break;
          case MultiMediaType.Diverse:
            PlayItemsModel.CheckQueryPlayAction(() => mcddh.GetAllMediaItems());
            break;
        }
      return;
    }

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      SubscribeToMessages();
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      UnsubscribeFromMessages();
    }

    public void Continue() { }

    public void Shutdown()
    {
      UnsubscribeFromMessages();
    }

    #endregion
  }
}
