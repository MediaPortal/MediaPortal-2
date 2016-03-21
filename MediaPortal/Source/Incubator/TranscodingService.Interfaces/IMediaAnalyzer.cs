#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;

namespace MediaPortal.Plugins.Transcoding.Interfaces
{
    public interface IMediaAnalyzer
    {
    /// <summary>
    /// Parses a local media file and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="MediaResource">The IResourceAccessor to the media file</param>
    /// <returns>Metadata Container with all information about the media</returns>
    MetadataContainer ParseMediaStream(IResourceAccessor MediaResource);

    /// <summary>
    /// Parses a MediaItem and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="Media">The MediaItem to parse</param>
    /// <returns>Metadata Container with all information about the MediaItem</returns>
    MetadataContainer ParseMediaItem(MediaItem Media);

    /// <summary>
    /// Parses a channel (SlimTv) and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="ChannelId">Channel ID of the channel stream</param>
    /// <returns>Metadata Container with all information about the channel</returns>
    MetadataContainer ParseChannelStream(int ChannelId, out MediaItem ChannelMediaItem);
  }
}
