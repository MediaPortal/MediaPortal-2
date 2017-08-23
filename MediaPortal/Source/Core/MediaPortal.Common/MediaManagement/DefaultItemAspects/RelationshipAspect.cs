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
  /// Contains the metadata specification of the "Relationship" media item aspect which
  /// encapsulates the links between one media item and another.
  /// </summary>
  public static class RelationshipAspect
  {
    /// <summary>
    /// Media item aspect id of the relationship aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("68F60451-9E01-4EA2-AA87-A104E940F2DA");

    /// <summary>
    /// The role played by this media item
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ROLE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Role", typeof(Guid), Cardinality.Inline, true);

    /// <summary>
    /// The role played by the media item being linked to
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_LINKED_ROLE =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("LinkedRole", typeof(Guid), Cardinality.Inline, true);

    /// <summary>
    /// The media item being linked to
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_LINKED_ID =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("LinkedID", typeof(Guid), Cardinality.Inline, true);

    /// <summary>
    /// The index of the media item being linked to
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_RELATIONSHIP_INDEX =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("RelationshipIndex", typeof(int), Cardinality.Inline, false);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "Relationship",
          new[] {
            ATTR_ROLE,
            ATTR_LINKED_ROLE,
            ATTR_LINKED_ID,
            ATTR_RELATIONSHIP_INDEX,
          },
          new[]
          {
            ATTR_ROLE,
            ATTR_LINKED_ROLE,
            ATTR_LINKED_ID,
            // TODO: Should linked index be part of the unique key?
          }
        );
  }
}
