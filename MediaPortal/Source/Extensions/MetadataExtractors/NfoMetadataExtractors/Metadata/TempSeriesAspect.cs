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
  /// Contains the metadata specification for series.
  /// It is used to pass information to the RelationshipExtractors and is not persisted to database.
  /// </summary>
  public static class TempSeriesAspect
  {
    /// <summary>
    /// Media item aspect id of the series aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("26544F42-4C33-4FED-AD3E-03141EE54254");

    /// <summary>
    /// Series name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Name", 100, Cardinality.Inline, false);

    /// <summary>
    /// Series sort name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SORT_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SortName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Series TV network.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_STATION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Station", 100, Cardinality.Inline, false);

    /// <summary>
    /// Series TVDB ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TVDBID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TVDB", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Series genres.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GENRES =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Genres", 50, Cardinality.ManyToMany, false);

    /// <summary>
    /// Series plot.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_PLOT =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Plot", 10000, Cardinality.Inline, false);

    /// <summary>
    /// Series certification.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CERTIFICATION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Certification", 10, Cardinality.Inline, false);

    /// <summary>
    /// Date and time the series was premiered.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_PREMIERED =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Premiered", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Date and time the person died.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Rating", typeof(double), Cardinality.Inline, false);

    /// <summary>
    /// Series status.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ENDED =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Ended", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// Person order.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_VOTES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Votes", typeof(int), Cardinality.Inline, false);


    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "TempSeriesItem", new[] {
            ATTR_NAME,
            ATTR_SORT_NAME,
            ATTR_STATION,
            ATTR_TVDBID,
            ATTR_GENRES,
            ATTR_PLOT,
            ATTR_CERTIFICATION,
            ATTR_PREMIERED,
            ATTR_RATING,
            ATTR_ENDED,
            ATTR_VOTES
        }, true);
  }
}
