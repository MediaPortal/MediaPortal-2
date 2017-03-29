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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  /// <summary>
  /// Contains the metadata specification for an album.
  /// It is used to pass information to the RelationshipExtractors and is not persisted to database.
  /// </summary>
  public static class TempAlbumAspect
  {
    /// <summary>
    /// Media item aspect id of the person aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("8287D25C-CDEF-48AB-B575-7A9916019BE2");

    /// <summary>
    /// Album name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Name", 100, Cardinality.Inline, false);

    /// <summary>
    /// Album sort name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SORT_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SortName", 100, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "TempAlbumItem", new[] {
            ATTR_NAME,
            ATTR_SORT_NAME,
        },
        true);
  }
}
