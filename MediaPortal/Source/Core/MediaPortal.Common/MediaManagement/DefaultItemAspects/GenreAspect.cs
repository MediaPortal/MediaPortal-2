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
  /// Contains the metadata specification for a genre.
  /// </summary>
  public static class GenreAspect
  {
    /// <summary>
    /// Media item aspect id of the genre aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("BAF64D2D-6646-46E6-963F-E16D0318B0CF");

    /// <summary>
    /// Source of the identifier
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ID =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("GenreId", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// The type of identifier
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_GENRE =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Genre", 100, Cardinality.Inline, true);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
      // TODO: Localize name
      ASPECT_ID, "Genre",
        new[] {
          ATTR_ID,
          ATTR_GENRE,
        },
        new[] {
          ATTR_GENRE,
        }
      );
  }
}
