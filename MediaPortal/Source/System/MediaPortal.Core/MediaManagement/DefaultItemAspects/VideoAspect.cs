#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Video" media item aspect which is assigned to all video media items.
  /// </summary>
  public static class VideoAspect
  {
    /// <summary>
    /// Media item aspect id of the movie aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("8F8B7A4F-767C-4180-B58E-7C8999C52067");

    /// <summary>
    /// Genre string.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_GENRE =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Genre", 100, Cardinality.ManyToOne, true);

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Duration", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Director name string.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DIRECTOR =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Director", 100, Cardinality.ManyToOne, false);

    /// <summary>
    /// Number of audio streams for this video.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOSTREAMCOUNT =
        MediaItemAspectMetadata.CreateAttributeSpecification("AudioStreamCount", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Encoding. TODO: Describe format.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOENCODING =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate of the first audio stream in bits/second.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOBITRATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("AudioBitRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Encoding of the video. TODO: Describe format.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_VIDEOENCODING =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("VideoEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate of the video in bits/second.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_VIDEOBITRATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("VideoBitRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Width of the video in pixels.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateAttributeSpecification("Width", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Height of the video in pixels.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Height", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Aspect ratio of the resulting video in width/height. Might differ from the quotient width/height of the properties
    /// <see cref="ATTR_WIDTH"/> and <see cref="ATTR_HEIGHT"/> if the resulting video must be stretched.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ASPECTRATIO =
        MediaItemAspectMetadata.CreateAttributeSpecification("AspectRatio", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Frames/second of the video.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_FPS =
        MediaItemAspectMetadata.CreateAttributeSpecification("FPS", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of actor name strings.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ACTORS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Actors", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Set to <c>true</c> if this video item represents a DVD.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ISDVD =
        MediaItemAspectMetadata.CreateAttributeSpecification("IsDVD", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// String describing the story plot of the video.
    /// </summary>
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_STORYPLOT =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("StoryPlot", 10000, Cardinality.Inline, false);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "VideoItem", new[] {
            ATTR_GENRE,
            ATTR_DURATION,
            ATTR_DIRECTOR,
            ATTR_AUDIOSTREAMCOUNT,
            ATTR_AUDIOENCODING,
            ATTR_AUDIOBITRATE,
            ATTR_VIDEOENCODING,
            ATTR_VIDEOBITRATE,
            ATTR_WIDTH,
            ATTR_HEIGHT,
            ATTR_ASPECTRATIO,
            ATTR_FPS,
            ATTR_ACTORS,
            ATTR_ISDVD,
            ATTR_STORYPLOT,
        });
  }
}
