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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider
{
  public class SlimTvMediaItemBuilder
  {
    /// <summary>
    /// Creates a MediaItem that represents a TV stream. The MediaItem also holds information about stream indices to provide PiP
    /// functions (<paramref name="slotIndex"/>).
    /// </summary>
    /// <param name="slotIndex">Index of the slot (0/1)</param>
    /// <param name="path">Path or URL of the stream</param>
    /// <param name="channel"></param>
    /// <returns></returns>
    public static LiveTvMediaItem.LiveTvMediaItem CreateMediaItem(int slotIndex, string path, IChannel channel)
    {
      return CreateMediaItem(slotIndex, path, channel, true);
    }

    /// <summary>
    /// Creates a MediaItem that represents a Radio stream. 
    /// </summary>
    /// <param name="slotIndex">Index of the slot (0/1)</param>
    /// <param name="path">Path or URL of the stream</param>
    /// <param name="channel"></param>
    /// <returns></returns>
    public static LiveTvMediaItem.LiveTvMediaItem CreateRadioMediaItem(int slotIndex, string path, IChannel channel)
    {
      return CreateMediaItem(slotIndex, path, channel, false);
    }

    public static LiveTvMediaItem.LiveTvMediaItem CreateMediaItem(int slotIndex, string path, IChannel channel, bool isTv)
    {
      if (!String.IsNullOrEmpty(path))
      {
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        IDictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();
        MediaItemAspect providerResourceAspect;
        MediaItemAspect mediaAspect;

        SlimTvResourceAccessor resourceAccessor = new SlimTvResourceAccessor(slotIndex, path);
        aspects[ProviderResourceAspect.ASPECT_ID] = providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
        aspects[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemResolver.LocalSystemId);

        String raPath = resourceAccessor.CanonicalLocalResourcePath.Serialize();
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, raPath);

        string title;
        string mimeType;
        if (isTv)
        {
          // VideoAspect needs to be included to associate VideoPlayer later!
          aspects[VideoAspect.ASPECT_ID] = new MediaItemAspect(VideoAspect.Metadata);
          title = "Live TV";
          mimeType = LiveTvMediaItem.LiveTvMediaItem.MIME_TYPE_TV;
        }
        else
        {
          // AudioAspect needs to be included to associate an AudioPlayer later!
          aspects[AudioAspect.ASPECT_ID] = new MediaItemAspect(AudioAspect.Metadata);
          title = "Live Radio";
          mimeType = LiveTvMediaItem.LiveTvMediaItem.MIME_TYPE_RADIO;
        }
        mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, title);
        mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, mimeType); // Custom mimetype for LiveTv or Radio
        LiveTvMediaItem.LiveTvMediaItem tvStream = new LiveTvMediaItem.LiveTvMediaItem(new Guid(), aspects);

        tvStream.AdditionalProperties[LiveTvMediaItem.LiveTvMediaItem.SLOT_INDEX] = slotIndex;
        tvStream.AdditionalProperties[LiveTvMediaItem.LiveTvMediaItem.CHANNEL] = channel;
        tvStream.AdditionalProperties[LiveTvMediaItem.LiveTvMediaItem.TUNING_TIME] = DateTime.Now;
        return tvStream;
      }
      return null;
    }
  }
}
