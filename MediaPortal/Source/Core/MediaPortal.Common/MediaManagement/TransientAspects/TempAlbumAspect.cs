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

    /// <summary>
    /// Album AudioDB ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ADBID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("ADbId", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Album Musicbrainz ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_MBID =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("MbId", 100, Cardinality.Inline, false);

    /// <summary>
    /// Album Musicbrainz Group ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_MBGID =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("MbGroupId", 100, Cardinality.Inline, false);

    /// <summary>
    /// Date and time the person was born.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RELEASEDATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("ReleaseDate", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Album review
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_REVIEW =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Review", 5000, Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of artist names.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ARTISTS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Artists", 100, Cardinality.ManyToMany, false);

    /// <summary>
    /// List of music labels involved in making the album.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LABELS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Labels", 100, Cardinality.ManyToMany, false);

    /// <summary>
    /// List of genres for the album.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Genres", 100, Cardinality.ManyToMany, false);

    /// <summary>
    /// Contains the overall rating of the album. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Rating", typeof(double), Cardinality.Inline, false);


    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "TempAlbumItem", new[] {
            ATTR_NAME,
            ATTR_SORT_NAME,
            ATTR_ADBID,
            ATTR_MBID,
            ATTR_MBGID,
            ATTR_RELEASEDATE,
            ATTR_REVIEW,
            ATTR_ARTISTS,
            ATTR_LABELS,
            ATTR_GENRES,
            ATTR_RATING
        },
        true);
  }
}
