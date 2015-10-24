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
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Tree;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal static class MediaLibraryHelper
  {
    public const string CONTAINER_ROOT_KEY = "0";
    public const string CONTAINER_AUDIO_KEY = "A";
    public const string CONTAINER_VIDEO_KEY = "V";
    public const string CONTAINER_IMAGES_KEY = "I";
    public const string CONTAINER_MEDIA_SHARES_KEY = "M";

    public static Guid GetObjectId(string key)
    {
      var split = key.IndexOf(':');
      return split > 0 ? new Guid(key.Substring(split + 1)) : Guid.Empty;
    }

    public static string GetBaseKey(string key)
    {
      if(key == null)
        return null;
      var split = key.IndexOf(':');
      return split > 0 ? key.Substring(0, split) : key;
    }

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
                                 };

      return library.GetMediaItem(id, necessaryMIATypeIDs, optionalMIATypeIDs);
    }

    public static IDirectoryObject InstansiateMediaLibraryObject(MediaItem item, string baseKey, BasicContainer parent)
    {
      return InstansiateMediaLibraryObject(item, baseKey, parent, null);
    }

    public static IDirectoryObject InstansiateMediaLibraryObject(MediaItem item, string baseKey, BasicContainer parent, string title)
    {
      IDirectoryObject obj;
      // Choose the appropiate MediaLibrary* object for the media item
      try
      {
        if (item.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
        {
          if (baseKey == null) baseKey = CONTAINER_ROOT_KEY;
          obj = new MediaLibraryContainer(baseKey, item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        {
          if (baseKey == null) baseKey = CONTAINER_AUDIO_KEY;
          obj = new MediaLibraryMusicTrack(baseKey, item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        {
          if (baseKey == null) baseKey = CONTAINER_IMAGES_KEY;
          obj = new MediaLibraryImageItem(baseKey, item, parent.Client);
        }
        else if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        {
          if (baseKey == null) baseKey = CONTAINER_VIDEO_KEY;
          obj = new MediaLibraryVideoItem(baseKey, item, parent.Client);
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
        parent.Add((TreeNode<object>)obj);
      }

      // Initialise the object
      if (obj is MediaLibraryContainer)
      {
        ((MediaLibraryContainer)obj).Initialise();
      }
      else if (obj is MediaLibraryItem)
      {
        ((MediaLibraryItem)obj).Initialise();
      }
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
