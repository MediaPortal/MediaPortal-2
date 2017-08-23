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
  /// Contains the metadata specification of the "Video" media item aspect which is assigned to all video media items.
  /// </summary>
  public static class VideoAspect
  {
    /// <summary>
    /// Media item aspect id of the video aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("55D6A91B-8867-4A8D-BED3-9CB7F3AECD24");

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
    /// Set to <c>true</c> if this video item represents a disc image, like DVD or BluRay.
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
            ATTR_ACTORS,
            ATTR_DIRECTORS,
            ATTR_WRITERS,
            ATTR_CHARACTERS,
            ATTR_ISDVD,
            ATTR_STORYPLOT,
        });

    public static readonly Guid ROLE_VIDEO = new Guid("96DB4C0E-13CC-4B5B-B66D-F2CC7D205414");
  }
}
