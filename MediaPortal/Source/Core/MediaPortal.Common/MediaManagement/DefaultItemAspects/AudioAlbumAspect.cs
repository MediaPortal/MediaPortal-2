#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
    public static readonly Guid ASPECT_ID = new Guid("352151B1-50AA-45D4-89E6-517ED8C8F411");

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
    /// Enumeration of artist names.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPOSERS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Composers", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Enumeration of genre names.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Genres", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Number of tracks on the CD.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUMTRACKS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumTracks", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// ID of the disc. TODO: Specification.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DISCID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("DiscId", typeof(int), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "AlbumItem", new[] {
            ATTR_DESCRIPTION,
            ATTR_ARTISTS,
            ATTR_COMPOSERS,
            ATTR_GENRES,
            ATTR_NUMTRACKS,
            ATTR_DISCID,
        });

    public static readonly Guid ROLE_ALBUM = new Guid("CCCA5512-1CBA-4859-BD53-1D7AE96EBBCE");
  }
}
