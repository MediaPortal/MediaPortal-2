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

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_CONTAINER =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Container", 20, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_STREAM =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Stream", typeof(int), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_CODEC =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Codec", 20, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_FOURCC =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("FourCC", 20, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_BRAND =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Brand", 20, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_PIXEL_FORMAT =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("PixelFmt", 10, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_PIXEL_ASPECTRATIO =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("PixelAspectRatio", typeof(float), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_H264_PROFILE =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("H264Profile", 30, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_H264_HEADER_LEVEL =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("H264HeadLevel", typeof(float), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_H264_REF_LEVEL =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("H264RefLevel", typeof(float), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata.SingleAttributeSpecification ATTR_TS_TIMESTAMP =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Timestamp", 10, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
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
        });
  }
}
