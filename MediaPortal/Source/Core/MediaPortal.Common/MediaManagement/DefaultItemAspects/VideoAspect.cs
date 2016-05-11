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

using System;

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Video" media item aspect which is assigned to all video media items.
  /// </summary>
  public static class VideoAspect
  {
    /// <summary>
    /// Media item aspect id of the video aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("FEA2DA04-1FDC-4836-B669-F3CA73ADF120");

    /// <summary>
    /// Resource index for this video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RESOURCE_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ResourceIndex", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// The index of the stream inside the container.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_STREAM_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("StreamIndex", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Duration", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Number of audio streams for this video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOSTREAMCOUNT =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AudioStreamCount", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Encoding of the video. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEOENCODING =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("VideoEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate of the video in kbits/second.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEOBITRATE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("VideoBitRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Width of the video in pixels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Width", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Height of the video in pixels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Height", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Aspect ratio of the resulting video in width/height. Might differ from the quotient width/height of the properties
    /// <see cref="ATTR_WIDTH"/> and <see cref="ATTR_HEIGHT"/> if the resulting video must be stretched.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ASPECTRATIO =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AspectRatio", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Frames/second of the video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_FPS =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("FPS", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Set to <c>true</c> if this video item represents a DVD.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ISDVD =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("IsDVD", typeof(bool), Cardinality.Inline, false);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "VideoItem", new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_STREAM_INDEX,
            ATTR_DURATION,
            ATTR_AUDIOSTREAMCOUNT,
            ATTR_VIDEOENCODING,
            ATTR_VIDEOBITRATE,
            ATTR_WIDTH,
            ATTR_HEIGHT,
            ATTR_ASPECTRATIO,
            ATTR_FPS,
            ATTR_ISDVD,
        },
        new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_STREAM_INDEX
        }
        );
  }
}
