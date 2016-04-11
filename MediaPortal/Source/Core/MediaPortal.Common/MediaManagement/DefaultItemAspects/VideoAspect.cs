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
using System.Globalization;

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
    /// Genre string.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Genres", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Duration", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Number of audio streams for this video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AUDIOSTREAMCOUNT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AudioStreamCount", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Encoding. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AUDIOENCODINGS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("AudioEncodings", 50, Cardinality.ManyToMany, false);

    /// <summary>
    /// Bitrate of the first audio stream in kbits/second.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AUDIOBITRATES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AudioBitRates", typeof(long), Cardinality.ManyToMany, false);

    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AUDIOCHANNELS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AudioChannels", typeof(int), Cardinality.ManyToMany, false);

    /// <summary>
    /// List of available audio languages. Values are stored as <see cref="CultureInfo.TwoLetterISOLanguageName"/>.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AUDIOLANGUAGES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("AudioLanguages", 2, Cardinality.ManyToMany, true);

    /// <summary>
    /// Encoding of the video. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_VIDEOENCODING =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("VideoEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate of the video in kbits/second.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_VIDEOBITRATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("VideoBitRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Width of the video in pixels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Width", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Height of the video in pixels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Height", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Aspect ratio of the resulting video in width/height. Might differ from the quotient width/height of the properties
    /// <see cref="ATTR_WIDTH"/> and <see cref="ATTR_HEIGHT"/> if the resulting video must be stretched.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ASPECTRATIO =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AspectRatio", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Frames/second of the video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_FPS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("FPS", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of actor name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ACTORS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Actors", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Enumeration of director name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DIRECTORS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Directors", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Enumeration of writer name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_WRITERS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Writers", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Enumeration of fictional character name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CHARACTERS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Characters", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Set to <c>true</c> if this video item represents a DVD.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ISDVD =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("IsDVD", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// String describing the story plot of the video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_STORYPLOT =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("StoryPlot", 10000, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "VideoItem", new[] {
            ATTR_GENRES,
            ATTR_DURATION,
            ATTR_AUDIOSTREAMCOUNT,
            ATTR_AUDIOENCODINGS,
            ATTR_AUDIOBITRATES,
            ATTR_AUDIOCHANNELS,
            ATTR_AUDIOLANGUAGES,
            ATTR_VIDEOENCODING,
            ATTR_VIDEOBITRATE,
            ATTR_WIDTH,
            ATTR_HEIGHT,
            ATTR_ASPECTRATIO,
            ATTR_FPS,
            ATTR_ACTORS,
            ATTR_DIRECTORS,
            ATTR_WRITERS,
            ATTR_CHARACTERS,
            ATTR_ISDVD,
            ATTR_STORYPLOT,
        });
  }
}
