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
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.RemovableMedia;
using MediaPortal.UiComponents.RemovableMediaManager.Settings;

namespace MediaPortal.UiComponents.RemovableMediaManager
{
  public class RemovableMediaManagerService : IPluginStateTracker
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    enum MediaType
    {
      Unknown,
      Media,
      AudioCd,
      Dvd,
      Bd,
    }

    #endregion

    #region Ctor & maintainance

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

    #region Event Handlers

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Requests the main window handle from the main screen.
    /// </summary>
    /// <param name="queue">Queue which sent the message.</param>
    /// <param name="message">Message containing the notification data.</param>
    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == RemovableMediaMessaging.CHANNEL)
      {
        RemovableMediaMessaging.MessageType messageType = (RemovableMediaMessaging.MessageType) message.MessageType;
        if (messageType == RemovableMediaMessaging.MessageType.MediaInserted ||
            messageType == RemovableMediaMessaging.MessageType.MediaRemoved)
        {
          RemovableMediaManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaManagerSettings>();
          if (settings.AutoPlay == AutoPlayType.AutoPlay)
          {
            weiter:
              - Checken, ob wir in einem FSC-State sind. Wenn nicht: AutoPlay!
          }
        }
      }
    }

    #endregion

    #endregion

    weiter
    public void ExamineVolume(string strDrive)
    {
      if (strDrive == null) return;
      if (strDrive.Length == 0) return;

      switch (DetectMediaType(strDrive))
      {
        case MediaType.DVD:
          ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: DVD inserted into {0}", strDrive);
          bool PlayDVD = false;
          if (_settings.AutoPlayDVD == "Yes")
          {
            ServiceRegistration.Get<ILogger>().Info("Autoplay: DVD AutoPlay = ýes");
            PlayDVD = true;
          }
          else if ((_settings.AutoPlayDVD == "Ask") && (ShouldWeAutoPlay(MediaType.DVD)))
          {
            PlayDVD = true;
          }
          if (PlayDVD)
          {
            // Play DVD
            try
            {
              //window.WaitCursorVisible = true;
              IPlayerCollection collection = ServiceRegistration.Get<IPlayerCollection>();
              IPlayerFactory factory = ServiceRegistration.Get<IPlayerFactory>();
              IMediaItem mediaItem = new AutoPlayMediaItem(strDrive + @"\VIDEO_TS\VIDEO_TS.IFO");
              mediaItem.Title = "DVD";
              mediaItem.MetaData["MimeType"] = "audio";
              IPlayer player = factory.GetPlayer(mediaItem);


              //play it
              player.Play(mediaItem);
              collection.Add(player);
              if (player.CanResumeSession(null))
              {
                player.Paused = true;
                ServiceRegistration.Get<IScreenManager>().ShowDialog("movieResume");
              }

            }
            finally
            {
              //window.WaitCursorVisible = false;
            }
            IScreenManager manager = (IScreenManager)ServiceRegistration.Get<IScreenManager>();
            // We need to show the movies window first, otherwise we'll have problems returning back from full screen on stop.
            manager.ShowScreen("movies");
            manager.ShowScreen("fullscreenvideo");
          }
          break;

        case MediaType.AUDIO_CD:
          ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Audio CD inserted into drive {0}", strDrive);
          bool PlayAudioCd = false;
          if (_settings.AutoPlayCD == "Yes")
          {
            // Automaticaly play the CD
            PlayAudioCd = true;
            ServiceRegistration.Get<ILogger>().Info("Autplay: CD Autoplay = yes");
          }
          else if ((_settings.AutoPlayCD == "Ask") && (ShouldWeAutoPlay(MediaType.AUDIO_CD)))
          {
            PlayAudioCd = true;
          }
          if (PlayAudioCd)
          {
            // Play Audio CD
            try
            {
              //window.WaitCursorVisible = true;
              // Get the files of the Audio CD via the MediaManager
              // This does call the MusicImporter, which does a FreeDB Query
              IMediaManager mediaManager = ServiceRegistration.Get<IMediaManager>();
              IList<IAbstractMediaItem> tracks = mediaManager.GetView(strDrive + @"\");

              // Add all items of the CD to the Playlist
              IPlaylistManager playList = ServiceRegistration.Get<IPlaylistManager>();
              foreach (IAbstractMediaItem item in tracks)
              {
                IMediaItem mediaItem = item as IMediaItem;
                if (mediaItem != null)
                {
                  mediaItem.MetaData["MimeType"] = "audio";
                  playList.PlayList.Add(mediaItem);
                }
              }
              playList.PlayAt(0);
            }
            finally
            {
              //window.WaitCursorVisible = false;
            }
          }
          break;

        case MediaType.PHOTOS:
          ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Photo volume inserted {0}", strDrive);
          if (ShouldWeAutoPlay(MediaType.PHOTOS))
          {
          }
          break;

        case MediaType.VIDEOS:
          ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Video volume inserted {0}", strDrive);
          if (ShouldWeAutoPlay(MediaType.VIDEOS))
          {
          }
          break;

        case MediaType.AUDIO:
          ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Audio volume inserted {0}", strDrive);
          if (ShouldWeAutoPlay(MediaType.AUDIO))
          {
          }
          break;

        default:
          ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Unknown media type inserted into drive {0}", strDrive);
          break;
      }
    }

    weiter
    /// <summary>
    /// Detects the media type of the CD/DVD/BD inserted into a drive.
    /// </summary>
    /// <param name="strDrive">The drive that contains the data.</param>
    /// <returns>The media type of the drive.</returns>
    private MediaType DetectMediaType(string strDrive)
    {
      if (strDrive == null)
        return MediaType.UNKNOWN;

      if (strDrive == string.Empty)
        return MediaType.UNKNOWN;

      try
      {
        if (Directory.Exists(strDrive + "\\VIDEO_TS"))
          return MediaType.DVD;

        if (BassUtils.isARedBookCD(strDrive))
          return MediaType.AUDIO_CD;

        List<string> allfiles = new List<string>();
        GetAllFiles(strDrive + "\\", ref allfiles);

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
      }
      catch (Exception)
      {
      }
      return MediaType.UNKNOWN;
    }

    weiter
    private void GetAllFiles(string strFolder, ref List<string> allfiles)
    {
      if (strFolder == null) return;
      if (strFolder.Length == 0) return;
      if (allfiles == null) return;

      try
      {
        string[] files = Directory.GetFiles(strFolder);
        if (files != null && files.Length > 0)
        {
          for (int i = 0; i < files.Length; ++i) allfiles.Add(files[i]);
        }
        string[] folders = Directory.GetDirectories(strFolder);
        if (folders != null && folders.Length > 0)
        {
          for (int i = 0; i < folders.Length; ++i) GetAllFiles(folders[i], ref allfiles);
        }
      }
      catch (Exception)
      {
      }
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
