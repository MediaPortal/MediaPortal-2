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

using System;

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "VideoAudio" media item aspect which is assigned to all video media items.
  /// </summary>
  public static class SubtitleAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string FORMAT_ASS = "ASS";
    public static readonly string FORMAT_SSA = "SSA";
    public static readonly string FORMAT_MOVTEXT = "MOVTEXT";
    public static readonly string FORMAT_SMI = "SMI";
    public static readonly string FORMAT_SRT = "SRT";
    public static readonly string FORMAT_MICRODVD = "MICRODVD";
    public static readonly string FORMAT_SUBVIEW = "SUBVIEW";
    public static readonly string FORMAT_WEBVTT = "WEBTTV";
    public static readonly string FORMAT_DVBTEXT = "DVBTEXT";
    public static readonly string FORMAT_TELETEXT = "TELETEXT";
    public static readonly string FORMAT_VOBSUB = "VOBSUB";
    public static readonly string FORMAT_PGS = "PGS";
    public static readonly string BINARY_ENCODING = "BIN";

    /// <summary>
    /// Media item aspect id of the video aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("6509EB3B-7F5A-4ACB-BCDD-D76541E2F51F");

    /// <summary>
    /// Resource index for this subtitle.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RESOURCE_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ResourceIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Video resource index the subtitle belongs to.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_VIDEO_RESOURCE_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("VideoResourceIndex", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// The index the stream inside the container. Use -1 for external subtitles.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_STREAM_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("StreamIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Encoding. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SUBTITLE_ENCODING =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("SubtitleEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Subtitle format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SUBTITLE_FORMAT =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("SubtitleFormat", 15, Cardinality.Inline, false);

    /// <summary>
    /// Set to <c>true</c> if this subtitle item is inside the container.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_INTERNAL =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("IsInternal", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// Set to <c>true</c> if this subtitle item is the default.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_DEFAULT =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("IsDefault", typeof(bool), Cardinality.Inline, true);

    /// <summary>
    /// Set to <c>true</c> if this subtitle item is forced.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_FORCED =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("IsForced", typeof(bool), Cardinality.Inline, true);

    /// <summary>
    /// Subtitle language stored as <see cref="CultureInfo.TwoLetterISOLanguageName"/>.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_SUBTITLE_LANGUAGE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("SubtitleLanguage", 2, Cardinality.Inline, true);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "SubtitleItem", new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_VIDEO_RESOURCE_INDEX,
            ATTR_STREAM_INDEX,
            ATTR_SUBTITLE_ENCODING,
            ATTR_SUBTITLE_FORMAT,
            ATTR_INTERNAL,
            ATTR_DEFAULT,
            ATTR_FORCED,
            ATTR_SUBTITLE_LANGUAGE,
        },
        new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_STREAM_INDEX
        },
        ProviderResourceAspect.ASPECT_ID,
        new[] {
            ATTR_RESOURCE_INDEX
        }
        );
  }
}
