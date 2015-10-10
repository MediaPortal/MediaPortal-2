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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Plugins.Transcoding.Aspects
{
  public class TranscodeItemVideoAspect
  {
    /// <summary>
    /// Media item aspect id of the recording aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("C267359B-9243-4DAE-8C59-91D20B54BC3F");

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_CONTAINER =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("Container", 20, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_STREAM =
        MediaItemAspectMetadata.CreateAttributeSpecification("Stream", typeof(int), Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_CODEC =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("Codec", 20, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_FOURCC =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("FourCC", 20, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_BRAND =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("Brand", 20, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_PIXEL_FORMAT =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("PixelFmt", 10, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_PIXEL_ASPECTRATIO =
        MediaItemAspectMetadata.CreateAttributeSpecification("PixelAspectRatio", typeof(float), Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_H264_PROFILE =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("H264Profile", 30, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_H264_HEADER_LEVEL =
        MediaItemAspectMetadata.CreateAttributeSpecification("H264HeadLevel", typeof(float), Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_H264_REF_LEVEL =
        MediaItemAspectMetadata.CreateAttributeSpecification("H264RefLevel", typeof(float), Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_TS_TIMESTAMP =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("Timestamp", 10, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOLANGUAGES =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioLanguages", 2, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOCODECS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioCodecs", 20, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOSTREAMS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioStreams", 2, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOBITRATES =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioBitrates", 10, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOCHANNELS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioChannels", 2, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIODEFAULTS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioDefaults", 1, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOFREQUENCIES =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("AudioFrequencies", 5, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EMBEDDED_SUBSTREAMS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EmbeddedSubStreams", 2, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EMBEDDED_SUBCODECS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EmbeddedSubCodecs", 10, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EMBEDDED_SUBLANGUAGES =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EmbeddedSubLanguages", 2, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EMBEDDED_SUBDEFAULTS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EmbeddedSubDefaults", 1, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        ASPECT_ID, "TranscodeItemVideo", new[] {
            ATTR_CONTAINER,
            ATTR_STREAM,
            ATTR_CODEC,
            ATTR_FOURCC,
            ATTR_BRAND,
            ATTR_PIXEL_FORMAT,
            ATTR_PIXEL_ASPECTRATIO,
            ATTR_H264_PROFILE,
            ATTR_H264_HEADER_LEVEL,
            ATTR_H264_REF_LEVEL,
            ATTR_TS_TIMESTAMP,
            ATTR_AUDIOLANGUAGES,
            ATTR_AUDIOCODECS,
            ATTR_AUDIOSTREAMS,
            ATTR_AUDIOBITRATES,
            ATTR_AUDIOCHANNELS,
            ATTR_AUDIODEFAULTS,
            ATTR_AUDIOFREQUENCIES,
            ATTR_EMBEDDED_SUBSTREAMS,
            ATTR_EMBEDDED_SUBCODECS,
            ATTR_EMBEDDED_SUBLANGUAGES,
            ATTR_EMBEDDED_SUBDEFAULTS
        });
  }
}
