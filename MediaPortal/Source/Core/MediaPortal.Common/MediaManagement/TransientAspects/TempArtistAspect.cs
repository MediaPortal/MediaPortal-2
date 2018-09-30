#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Common.MediaManagement.TransientAspects
{
  /// <summary>
  /// Contains the metadata specification for artists.
  /// It is used to pass aartist information to the RelationshipExtractors and is not persisted to database.
  /// </summary>
  public static class TempArtistAspect
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
    /// Person AudioDB ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ADBID =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ADbId", typeof(long), Cardinality.Inline, false);

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

    /// <summary>
    /// Person biography.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_BIOGRAPHY =
    MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Biography", 10000, Cardinality.Inline, false);

    /// <summary>
    /// Date and time the person was born.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_DATEOFBIRTH =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("BornDate", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Date and time the person died.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_DATEOFDEATH =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("DeathDate", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// If set to <c>true</c>, the person is actually a group of people i.e. a music band.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_GROUP =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("IsGroup", typeof(bool), Cardinality.Inline, false);


    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        ASPECT_ID, "TempArtistItem", new[] {
            ATTR_NAME,
            ATTR_MBID,
            ATTR_ADBID,
            ATTR_OCCUPATION,
            ATTR_FROMALBUM,
            ATTR_BIOGRAPHY,
            ATTR_DATEOFBIRTH,
            ATTR_DATEOFDEATH,
            ATTR_GROUP
        },
        new[] {
            ATTR_NAME,
        }, 
        true);
  }
}
