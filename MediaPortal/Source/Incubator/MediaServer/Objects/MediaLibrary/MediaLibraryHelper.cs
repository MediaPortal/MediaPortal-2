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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public static class MediaLibraryHelper
  {
    public const string CONTAINER_ROOT_KEY = "0";
    public const string CONTAINER_AUDIO_KEY = "A";
    public const string CONTAINER_VIDEO_KEY = "V";
    public const string CONTAINER_IMAGES_KEY = "I";
    public const string CONTAINER_MEDIA_SHARES_KEY = "M";
    public const string CONTAINER_BROADCAST_KEY = "B";

    public static MediaItem GetMediaItem(Guid id)
    {
      var library = ServiceRegistration.Get<IMediaLibrary>();

      var necessaryMIATypeIDs = new Guid[]
                                  {
                                    ProviderResourceAspect.ASPECT_ID,
                                    MediaAspect.ASPECT_ID,
                                  };
      var optionalMIATypeIDs = new Guid[]
                                 {
                                   DirectoryAspect.ASPECT_ID,
                                   AudioAspect.ASPECT_ID,
                                   ImageAspect.ASPECT_ID,
                                   VideoAspect.ASPECT_ID,
                                   TranscodeItemAudioAspect.ASPECT_ID,
                                   TranscodeItemImageAspect.ASPECT_ID,
                                   TranscodeItemVideoAspect.ASPECT_ID,
                                   TranscodeItemVideoAudioAspect.ASPECT_ID,
                                   TranscodeItemVideoEmbeddedAspect.ASPECT_ID
                                 };

      return library.GetMediaItem(id, necessaryMIATypeIDs, optionalMIATypeIDs);
    }

    public static IDirectoryObject InstansiateMediaLibraryObject(MediaItem item, BasicContainer parent)
    {
      return InstansiateMediaLibraryObject(item, parent, null);
    }

    public static IDirectoryObject InstansiateMediaLibraryObject(MediaItem item, BasicContainer parent, string title)
    {
      BasicObject obj;
      Logger.Debug("Instantiate media library object from {0} {1} {2}", item != null ? item.MediaItemId.ToString() : "null", parent != null ? parent.Key : "null", title ?? "null");
      //Logger.Debug("Media library object title {0}", MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata));
      //Logger.Debug("Media library object title {0}", MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE));
      //Logger.Debug("Media library object title {0}", MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE).ToString());
      // Choose the appropiate MediaLibrary* object for the media item
      try
      {
        if (item.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
        {
          obj = new MediaLibraryBrowser(item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        {
          obj = new MediaLibraryMusicTrack(item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        {
          obj = new MediaLibraryImageItem(item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        {
          obj = new MediaLibraryVideoItem(item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        {
          obj = new MediaLibrarySeriesItem(item, null, parent.Client);
        }
        else if (item.Aspects.ContainsKey(SeasonAspect.ASPECT_ID))
        {
          obj = new MediaLibrarySeasonItem(item, null, parent.Client);
        }
        else
        {
          Logger.Warn("MediaServer item {0} {1} contains no valid aspects", item.MediaItemId, title);
          return null;
        }
      }
      catch (DlnaAspectMissingException e)
      {
        //Unable to create DlnaItem
        Logger.Warn(e.Message);
        return null;
      }
      //Logger.Debug("MediaServer converted {0}:[{1}] into {2}", item.MediaItemId, string.Join(",", item.Aspects.Keys), obj.GetType().Name);

      // Assign the parent
      if (parent != null)
      {
        parent.Add(obj);
      }

      // Initialise the object
      //if (obj is MediaLibraryItem)
      //{
      //  ((MediaLibraryItem)obj).Initialise();
      //}
      obj.Restricted = true;
      Logger.Debug("Created object of type {0} for MediaItem {1}", obj.GetType().Name, item.MediaItemId);
      if (title != null)
      {
        obj.Title = title;
      }
      return obj;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
	  }
  }
}
