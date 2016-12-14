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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
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
    public static readonly Guid ASPECT_ID = new Guid("91DB3E1C-270D-4B9F-9FDB-09803811554A");

    /// <summary>
    /// Person name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_NAME =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Name", 100, Cardinality.Inline, false);

    /// <summary>
    /// Person name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_CHARACTER =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Character", 100, Cardinality.Inline, false);

    /// <summary>
    /// Person IMDB ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_IMDBID =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("IMDB", 100, Cardinality.Inline, false);

    /// <summary>
    /// Specifies the persons occupation.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_OCCUPATION =
      MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Occupation", 15, Cardinality.Inline, false);

    /// <summary>
    /// Person biography.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_BIOGRAPHY =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Biography", 10000, Cardinality.Inline, false);

    /// <summary>
    /// The origin of the person.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ORIGIN =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Origin", 300, Cardinality.Inline, false);

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
    /// Person from series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_FROMSERIES =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("FromSeries", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// Person order.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_ORDER =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Order", typeof(int), Cardinality.Inline, false);


    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        ASPECT_ID, "TempPersonItem", new[] {
            ATTR_NAME,
            ATTR_CHARACTER,
            ATTR_IMDBID,
            ATTR_OCCUPATION,
            ATTR_BIOGRAPHY,
            ATTR_ORIGIN,
            ATTR_DATEOFBIRTH,
            ATTR_DATEOFDEATH,
            ATTR_FROMSERIES,
            ATTR_ORDER
        }, 
        new[] {
            ATTR_NAME,
        },
        true);
  }
}
