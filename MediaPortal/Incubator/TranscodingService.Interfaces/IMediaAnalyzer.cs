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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.TranscodingService.Interfaces
{
    public interface IMediaAnalyzer
    {
    /// <summary>
    /// Parses a local media file and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="MediaResource">The IResourceAccessor to the media file</param>
    /// <returns>Metadata Container with all information about the media</returns>
    Task<MetadataContainer> ParseMediaStreamAsync(IResourceAccessor MediaResource);

    /// <summary>
    /// Parses a MediaItem and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="Media">The MediaItem to parse</param>
    /// <param name="MediaPartSetId">The media set part to analyze. Null for first set.</param>
    /// <returns>Metadata Container with all information about the MediaItem</returns>
    Task<IList<MetadataContainer>> ParseMediaItemAsync(MediaItem Media, int? MediaPartSetId);

    /// <summary>
    /// Parses a channel (SlimTv) and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="ChannelId">Channel ID of the channel stream</param>
    /// <param name="ChannelMediaItem">Channel media item</param>
    /// <returns>Metadata Container with all information about the channel</returns>
    Task<MetadataContainer> ParseChannelStreamAsync(int ChannelId, LiveTvMediaItem ChannelMediaItem);
  }
}
