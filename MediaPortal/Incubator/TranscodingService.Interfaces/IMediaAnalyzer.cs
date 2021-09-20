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
    /// <param name="mediaResources">The IResourceAccessors to the media file. If the media is split into multiple stream
    /// (like VOB files), multiple accessors can be added</param>
    /// <param name="analysisName">Name for the analysis if it needs to be other than the resource name</param>
    /// <param name="useCache">If true the cache will be used to avoid running analysis if possible</param>
    /// <returns>Metadata Container with all information about the media</returns>
    Task<MetadataContainer> ParseMediaStreamAsync(IEnumerable<IResourceAccessor> mediaResources);

    /// <summary>
    /// Parses a MediaItem and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="media">The MediaItem to parse</param>
    /// <param name="mediaPartSetId">The media set part to analyze. Null for first set.</param>
    /// <param name="cache">Store the analysis to file so it can be reused later</param>
    /// <returns>Metadata Container with all information about the MediaItem</returns>
    Task<MetadataContainer> ParseMediaItemAsync(MediaItem media, int? mediaPartSetId = null, bool cache = true);

    /// <summary>
    /// Parses a channel (SlimTv) and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="channelId">Channel ID of the channel stream</param>
    /// <param name="channelMediaItem">Channel media item</param>
    /// <returns>Metadata Container with all information about the channel</returns>
    Task<MetadataContainer> ParseChannelStreamAsync(int channelId, LiveTvMediaItem channelMediaItem);

    /// <summary>
    /// Deletes any stored analysis files for a specified media item
    /// </summary>
    /// <param name="mediaItemId">Media item id to delete the analysis file for</param>
    Task DeleteAnalysisAsync(Guid mediaItemId);

    /// <summary>
    /// Gets a list of all the currently analyzed media items
    /// </summary>
    /// <returns>List of all the media items that currently have been analyzed</returns>
    ICollection<Guid> GetAllAnalysisIds();
  }
}
