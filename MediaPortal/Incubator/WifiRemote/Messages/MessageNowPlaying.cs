#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.Aspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  internal class MessageNowPlaying : MessageNowPlayingBase, IMessage
  {
    public String Type
    {
      get { return "nowplaying"; }
    }


    public IAdditionalMediaInfo MediaInfo
    {
      get
      {
        if (Helper.IsNowPlaying())
        {
          MediaItem mediaItem = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentMediaItem;

          IList<MultipleMediaItemAspect> providerAspects;
          if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerAspects) &&
            providerAspects.Any(pra => MediaItem.OPTICAL_DISC_MIMES.Any(m => m.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase))))
            return new DVDInfo();

          if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerAspects) &&
            providerAspects.Any(pra => LiveTvMediaItem.MIME_TYPE_TV.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase)))
            return new TvInfo(mediaItem);

          if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerAspects) &&
            providerAspects.Any(pra => LiveTvMediaItem.MIME_TYPE_RADIO.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase)))
            return new RadioInfo(mediaItem);

          if (mediaItem.Aspects.ContainsKey(RecordingAspect.ASPECT_ID))
            return new RecordingInfo(mediaItem);

          if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
            return new MusicInfo(mediaItem);

          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
            return new MovingPicturesInfo(mediaItem);

          if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
            return new SeriesEpisodeInfo(mediaItem);

          if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
            return new VideoInfo(mediaItem);
        }

        return null;
      }
    }

    /// <summary>
    /// Constructor. 
    /// </summary>
    public MessageNowPlaying()
    {

    }
  }
}
