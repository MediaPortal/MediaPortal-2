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

namespace MediaPortal.Plugins.Transcoding.Interfaces.Aspects
{
  public class TranscodeItemVideoEmbeddedAspect
  {
    /// <summary>
    /// Media item aspect id of the recording aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("A97FCAEF-694A-4D46-8162-4A3D5EA9C594");

    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_EMBEDDED_SUBSTREAM =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("EmbeddedSubStream", 2, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_EMBEDDED_SUBCODEC =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("EmbeddedSubCodecs", 10, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_EMBEDDED_SUBLANGUAGE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("EmbeddedSubLanguage", 2, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_EMBEDDED_SUBDEFAULT =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("EmbeddedSubDefault", 1, Cardinality.Inline, false);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        ASPECT_ID, "TranscodeItemVideoEmbedded", new[] {
            ATTR_EMBEDDED_SUBSTREAM,
            ATTR_EMBEDDED_SUBCODEC,
            ATTR_EMBEDDED_SUBLANGUAGE,
            ATTR_EMBEDDED_SUBDEFAULT
        });
  }
}
