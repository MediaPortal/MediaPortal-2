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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
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
    /// <returns></returns>
    protected bool ExamineVolume(string drive)
    {
      if (string.IsNullOrEmpty(drive))
        return false;

      DriveInfo driveInfo = new DriveInfo(drive);
      VideoDriveHandler vdh;
      AudioCDDriveHandler acddh;
      if ((vdh = VideoDriveHandler.TryCreateVideoDriveHandler(driveInfo, Consts.NECESSARY_MUSIC_MIAS)) != null)
        PlayItemsModel.CheckQueryPlayAction(vdh.VideoItem);
      else if ((acddh = AudioCDDriveHandler.TryCreateAudioDriveHandler(driveInfo, Consts.NECESSARY_MUSIC_MIAS)) != null)
        PlayItemsModel.CheckQueryPlayAction(() => acddh.GetAllMediaItems(), AVType.Audio);


      // TODO: Other types

        //case MediaType.PHOTOS:
        //  ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Photo volume inserted {0}", drive);
        //  if (ShouldWeAutoPlay(MediaType.PHOTOS))
        //  {
        //  }
        //  break;

        //case MediaType.VIDEOS:
        //  ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Video volume inserted {0}", drive);
        //  if (ShouldWeAutoPlay(MediaType.VIDEOS))
        //  {
        //  }
        //  break;

        //case MediaType.AUDIO:
        //  ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Audio volume inserted {0}", drive);
        //  if (ShouldWeAutoPlay(MediaType.AUDIO))
        //  {
        //  }
        //  break;

        //default:
        //  ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Unknown media type inserted into drive {0}", drive);
        //  break;

      // Have a look at MediaInfo dll how to detect mime types.
      /*
      foreach (string FileName in allfiles)
      {
        string ext = System.IO.Path.GetExtension(FileName).ToLower();
        if (MediaPortal.Util.CdUtils.IsVideo(FileName)) return MediaType.VIDEOS;
      }

      foreach (string FileName in allfiles)
      {
        string ext = System.IO.Path.GetExtension(FileName).ToLower();
        if (MediaPortal.Util.Utils.IsAudio(FileName)) return MediaType.AUDIO;
      }

      foreach (string FileName in allfiles)
      {
        if (MediaPortal.Util.Utils.IsPicture(FileName)) return MediaType.PHOTOS;
      }
      */
      return false;
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
