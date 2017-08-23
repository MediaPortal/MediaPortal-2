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
  /// Contains the metadata specification for persons.
  /// It is used to pass information to the RelationshipExtractors and is not persisted to database.
  /// </summary>
  public static class TempPersonAspect
  {
    /// <summary>
    /// Media item aspect id of the person aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("99F2C67A-6A10-4A30-9F6D-183059BA012D");

    /// <summary>
    /// Person name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_NAME =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Name", 100, Cardinality.Inline, false);

    /// <summary>
    /// Person Musicbrainz ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_MBID =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("MbId", 100, Cardinality.Inline, false);

    /// <summary>
    /// Specifies the persons occupation.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_OCCUPATION =
      MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Occupation", 15, Cardinality.Inline, false);

    /// <summary>
    /// Person from album.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_FROMALBUM =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("FromAlbum", typeof(bool), Cardinality.Inline, false);


    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        ASPECT_ID, "TempPersonItem", new[] {
            ATTR_NAME,
            ATTR_MBID,
            ATTR_OCCUPATION,
            ATTR_FROMALBUM
        },
        new[] {
            ATTR_NAME,
        }, 
        true);
  }
}
