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
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.RemovableMedia;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.RemovableMediaManager.Settings;

namespace MediaPortal.UiComponents.RemovableMediaManager
{
  public class RemovableMediaManager : IPluginStateTracker
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    public enum VideoMediaType
    {
      Unknown,
      VideoBD,
      VideoDVD,
      VideoCD,
    }

    enum MediaType
    {
      Unknown,
      MediaAudio,
      MediaImages,
      MediaVideo,
      MediaMisc,
      AudioCd,
      Dvd,
      Bd,
    }

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
    /// Tries to play one or more videos, either given in <paramref name="mediaItem"/> or <paramref name="mediaItems"/>.
    /// </summary>
    /// <param name="mediaItem">Video media item to play. Either <paramref name="mediaItem"/> or <paramref name="mediaItems"/> must be given.</param>
    /// <param name="mediaItems">Video media items to play. Either <paramref name="mediaItem"/> or <paramref name="mediaItems"/> must be given.</param>
    /// <param name="playerContextName">Name of the player context to use. For DVD, this could be "DVD", for example.</param>
    public void DoPlayVideo(MediaItem mediaItem, IEnumerable<MediaItem> mediaItems, string playerContextName)
    {
      if (mediaItem != null)
        PlayItemsModel.CheckQueryPlayAction(mediaItem);
      else
        PlayItemsModel.CheckQueryPlayAction(() => mediaItems);
    }

    protected bool ExamineVolume(string drive)
    {
      if (string.IsNullOrEmpty(drive))
        return false;

      VideoMediaType vmt;
      if (DetectVideoMedia(drive, out vmt))
      {
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        ResourcePath rp = LocalFsMediaProviderBase.ToProviderResourcePath(drive);
        using (IResourceAccessor ra = rp.CreateLocalResourceAccessor())
          PlayItemsModel.CheckQueryPlayAction(
              mediaAccessor.CreateMediaItem(ra, mediaAccessor.GetMetadataExtractorsForCategory(DefaultMediaCategory.Video.ToString())));
      }

      // TODO: Other types

        //case MediaType.AudioCd:
        //  ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Audio CD inserted into drive {0}", drive);
        //  bool PlayAudioCd = false;
        //  if (_settings.AutoPlayCD == "Yes")
        //  {
        //    // Automaticaly play the CD
        //    PlayAudioCd = true;
        //    ServiceRegistration.Get<ILogger>().Info("Autplay: CD Autoplay = yes");
        //  }
        //  else if ((_settings.AutoPlayCD == "Ask") && (ShouldWeAutoPlay(MediaType.AUDIO_CD)))
        //  {
        //    PlayAudioCd = true;
        //  }
        //  if (PlayAudioCd)
        //  {
        //    // Play Audio CD
        //    try
        //    {
        //      //window.WaitCursorVisible = true;
        //      // Get the files of the Audio CD via the MediaManager
        //      // This does call the MusicImporter, which does a FreeDB Query
        //      IMediaManager mediaManager = ServiceRegistration.Get<IMediaManager>();
        //      IList<IAbstractMediaItem> tracks = mediaManager.GetView(drive + @"\");

        //      // Add all items of the CD to the Playlist
        //      IPlaylistManager playList = ServiceRegistration.Get<IPlaylistManager>();
        //      foreach (IAbstractMediaItem item in tracks)
        //      {
        //        IMediaItem mediaItem = item as IMediaItem;
        //        if (mediaItem != null)
        //        {
        //          mediaItem.MetaData["MimeType"] = "audio";
        //          playList.PlayList.Add(mediaItem);
        //        }
        //      }
        //      playList.PlayAt(0);
        //    }
        //    finally
        //    {
        //      //window.WaitCursorVisible = false;
        //    }
        //  }
        //  break;

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

    /// <summary>
    /// Detects if a video CD/DVD/BD is contained in the given <paramref name="drive"/>.
    /// </summary>
    /// <param name="drive">The drive to be examined.</param>
    /// <param name="videoMediaType">Returns the type of the media found in the given <paramref name="drive"/>. This parameter
    /// only returns a sensible value when the return value of this method is <c>true</c>.</param>
    /// <returns><c>true</c>, if a video media was identified, else <c>false</c>.</returns>
    protected bool DetectVideoMedia(string drive, out VideoMediaType videoMediaType)
    {
      videoMediaType = VideoMediaType.Unknown;
      if (string.IsNullOrEmpty(drive))
        return false;

      if (Directory.Exists(drive + "\\BDMV"))
      {
        ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: BD inserted into drive {0}", drive);
        videoMediaType = VideoMediaType.VideoBD;
        return true;
      }

      if (Directory.Exists(drive + "\\VIDEO_TS"))
      {
        ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: DVD inserted into drive {0}", drive);
        videoMediaType = VideoMediaType.VideoDVD;
        return true;
      }

      if (Directory.Exists(drive + "\\MPEGAV"))
      {
        ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Video CD inserted into drive {0}", drive);
        videoMediaType = VideoMediaType.VideoCD;
        return true;
      }
      return false;
    }

    protected bool DetectAudioCD(string drive, out IEnumerable<MediaItem> tracks)
    {
      if (!BassUtils.isARedBookCD(drive))
      {
        tracks = null;
        return false;
      }
      ResourcePath resourcePath = LocalFsMediaProviderBase.ToProviderResourcePath(drive);
      LocalDirectoryViewSpecification ldvs = new LocalDirectoryViewSpecification(
          "AudioCD", resourcePath, new Guid[] {MediaAspect.ASPECT_ID, AudioAspect.ASPECT_ID}, new Guid[] {});
      tracks = ldvs.GetAllMediaItems();
      return true;
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
