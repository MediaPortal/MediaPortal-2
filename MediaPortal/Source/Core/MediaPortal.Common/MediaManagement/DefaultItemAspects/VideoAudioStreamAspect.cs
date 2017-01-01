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
  public static class VideoAudioStreamAspect
  {
    /// <summary>
    /// Media item aspect id of the video aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("B23FB013-8A3E-464F-B490-1BF0CB5AB2DD");

    /// <summary>
    /// Resource index for this video.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RESOURCE_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ResourceIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// The index of the stream inside the container.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_STREAM_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("StreamIndex", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Encoding. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOENCODING =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("AudioEncoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate of the audio stream in kbits/second.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOBITRATE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AudioBitRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Sample rate of the audio in Hz.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOSAMPLERATE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AudioSampleRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOCHANNELS =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("AudioChannels", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Audio language stored as <see cref="CultureInfo.TwoLetterISOLanguageName"/>.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_AUDIOLANGUAGE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("AudioLanguage", 2, Cardinality.Inline, true);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "VideoAudioStreamItem", new[] {
            ATTR_RESOURCE_INDEX,
            ATTR_STREAM_INDEX,
            ATTR_AUDIOENCODING,
            ATTR_AUDIOBITRATE,
            ATTR_AUDIOSAMPLERATE,
            ATTR_AUDIOCHANNELS,
            ATTR_AUDIOLANGUAGE,
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
