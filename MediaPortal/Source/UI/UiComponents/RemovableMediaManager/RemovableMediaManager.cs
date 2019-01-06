#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.RemovableMediaManager
{
  public class RemovableMediaManager : IPluginStateTracker
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();
    protected ConcurrentDictionary<string, IEnumerable<MediaItem>> _removableMediaItems = new ConcurrentDictionary<string, IEnumerable<MediaItem>>();
    protected bool _runStartupCheck = true;

    #endregion

    void SubscribeToMessages()
    {
      lock (_syncObj)
      {
        _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
             RemovableMediaMessaging.CHANNEL,
             ServerConnectionMessaging.CHANNEL
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
          var type = ExamineVolume(drive);
          UpdateRemovableMediaItems();
          RemovableMediaManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaManagerSettings>();
          if (settings.AutoPlay == AutoPlayType.AutoPlay)
          {
            IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
            if (!pcm.IsFullscreenContentWorkflowStateActive)
              CheckAutoPlay(drive, type);
          }
        }
        else if (messageType == RemovableMediaMessaging.MessageType.MediaRemoved)
        {
          string drive = (string)message.MessageData[RemovableMediaMessaging.DRIVE_LETTER];
          IEnumerable<MediaItem> items;
          _removableMediaItems.TryRemove(drive, out items);
          RemoveRemovableMediaItems(items);
          _removableMediaItems.TryRemove(drive + @"\", out items);
          RemoveRemovableMediaItems(items);
        }
      }
      else if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType = (ServerConnectionMessaging.MessageType)message.MessageType;
        if (messageType == ServerConnectionMessaging.MessageType.HomeServerConnected && _runStartupCheck)
        {
          _runStartupCheck = false;
          StartupCheck();
        }
      }
    }

    protected async void StartupCheck()
    {
      //Check if any removable drives are currently mounted
      var tasks = DriveInfo.GetDrives().Where(driveInfo => driveInfo.DriveType == DriveType.CDRom || driveInfo.DriveType == DriveType.Removable).
        Select(driveInfo => Task.Run(() => ExamineVolume(driveInfo.Name)));
      await Task.WhenAll(tasks);
      UpdateRemovableMediaItems();
    }

    /// <summary>
    /// Examines the media in the given <paramref name="drive"/> and auto-plays the inserted media. Depending on the type of the
    /// inserted media, different play modes are choosen.
    /// </summary>
    /// <param name="drive">Drive to be examined. Format: <c>D:</c>.</param>
    protected AVType ExamineVolume(string drive)
    {
      if (string.IsNullOrEmpty(drive))
        return AVType.None;

      DriveInfo driveInfo = new DriveInfo(drive);
      VideoDriveHandler vdh;
      AudioCDDriveHandler acddh;
      MultimediaDriveHandler mcddh;
      if ((vdh = VideoDriveHandler.TryCreateVideoDriveHandler(driveInfo, Consts.NECESSARY_VIDEO_MIAS)) != null)
      {
        var items = new[] { vdh.VideoItem };
        _removableMediaItems.AddOrUpdate(drive, items, (d, e) => items);
        return AVType.Video;
      }
      else if ((acddh = AudioCDDriveHandler.TryCreateAudioCDDriveHandler(driveInfo)) != null)
      {
        var items = acddh.GetAllMediaItems();
        _removableMediaItems.AddOrUpdate(drive, items, (d, e) => items);
        return AVType.Audio;
      }
      else if ((mcddh = MultimediaDriveHandler.TryCreateMultimediaCDDriveHandler(driveInfo, Consts.NECESSARY_VIDEO_MIAS, Consts.NECESSARY_AUDIO_MIAS, Consts.NECESSARY_IMAGE_MIAS)) != null)
      {
        var items = mcddh.GetAllMediaItems();
        _removableMediaItems.AddOrUpdate(drive, items, (d, e) => items);
        switch (mcddh.MediaType)
        {
          case MultiMediaType.Video:
          case MultiMediaType.Image:
            return AVType.Video;
          case MultiMediaType.Audio:
            return AVType.Audio;
        }
      }
      return AVType.None;
    }

    protected void CheckAutoPlay(string drive, AVType type)
    {
      if (string.IsNullOrEmpty(drive))
        return;

      if(_removableMediaItems.TryGetValue(drive, out var items))
      {
        if (type == AVType.None)
          PlayItemsModel.CheckQueryPlayAction(() => items);
        else
          PlayItemsModel.CheckQueryPlayAction(() => items, type);
      }
    }

    protected void UpdateRemovableMediaItems()
    {
      foreach (var item in _removableMediaItems.Values.SelectMany(i => i))
        PlayItemsModel.RemovableMediaItems.AddOrUpdate(item.MediaItemId, item, (g, i) => item);
    }

    protected void RemoveRemovableMediaItems(IEnumerable<MediaItem> mediaItems)
    {
      foreach (var item in mediaItems)
        PlayItemsModel.RemovableMediaItems.TryRemove(item.MediaItemId, out _);
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
