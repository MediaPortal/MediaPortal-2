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
  /// Contains the metadata specification of the "Album" media item aspect which is assigned to all album media items.
  /// </summary>
  public static class AudioAlbumAspect
  {
    /// <summary>
    /// Media item aspect id of the album aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("E33242E8-3AD6-487B-9BA4-EB61037CED3E");

    /// <summary>
    /// Album name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ALBUM =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Album", 100, Cardinality.Inline, true);

    /// <summary>
    /// Album description
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DESCRIPTION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Description", 5000, Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of artist names.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ARTISTS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Artists", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// List of music labels involved in making the album.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LABELS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Labels", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains list of awards.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AWARDS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Awards", 20, Cardinality.ManyToMany, true);

    /// <summary>
    /// If set to <c>true</c>, the album is a compilation of music from various artists.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPILATION =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("IsCompilation", typeof(bool), Cardinality.Inline, true);

    /// <summary>
    /// Number of tracks on the CD.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUMTRACKS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumTracks", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// ID of the disc in a collection.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DISCID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("DiscId", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Number of discs in the collection.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUMDISCS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumDiscs", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the number of sold albums.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SALES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Sales", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the album. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TOTAL_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TotalRating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Contains the overall number ratings of the album.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING_COUNT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("RatingCount", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains the number of tracks available for listening.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AVAILABLE_TRACKS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AvailTracks", typeof(int), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "AlbumItem", new[] {
            ATTR_ALBUM,
            ATTR_DESCRIPTION,
            ATTR_ARTISTS,
            ATTR_LABELS,
            ATTR_AWARDS,
            ATTR_COMPILATION,
            ATTR_NUMTRACKS,
            ATTR_DISCID,
            ATTR_NUMDISCS,
            ATTR_SALES,
            ATTR_TOTAL_RATING,
            ATTR_RATING_COUNT,
            ATTR_AVAILABLE_TRACKS
        });

    public static readonly Guid ROLE_ALBUM = new Guid("CCCA5512-1CBA-4859-BD53-1D7AE96EBBCE");
  }
}
