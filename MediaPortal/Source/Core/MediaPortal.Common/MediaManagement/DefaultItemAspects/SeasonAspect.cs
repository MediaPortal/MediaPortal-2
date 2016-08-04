#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
  /// Contains the metadata specification of the "Season" media item aspect which is assigned to season media items.
  /// </summary>
  public static class SeasonAspect
  {
    /// <summary>
    /// Media item aspect id of the season aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("6CA6556B-7A0F-4930-BE90-358AB6EA319B");

    /// <summary>
    /// Series name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SERIES_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SeriesName", 200, Cardinality.Inline, false);

    /// <summary>
    /// Contains the number of the season, usually starting at 1. A value of 0 is also valid for specials.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SEASON =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Season", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains a combination of <see cref="ATTR_SERIES_NAME"/> and the <see cref="ATTR_SEASON"/> to allow filtering and retrieval of season banners.
    /// This name must be built in form "{0} S{1}", using SeriesName and Season.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SERIES_SEASON =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SeriesSeasonName", 200, Cardinality.Inline, false);

    /// <summary>
    /// Album description
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DESCRIPTION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Description", 5000, Cardinality.Inline, false);

    /// <summary>
    /// Contains the number of episodes available for watching.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AVAILABLE_EPISODES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AvailEpisodes", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains the total number of episodes currently available for the season.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUM_EPISODES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumEpisodes", typeof(int), Cardinality.Inline, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "SeasonItem", new[] {
            ATTR_SERIES_NAME,
            ATTR_SEASON,
            ATTR_SERIES_SEASON,
            ATTR_DESCRIPTION,
            ATTR_AVAILABLE_EPISODES,
            ATTR_NUM_EPISODES
        });

    public static readonly Guid ROLE_SEASON = new Guid("830D5DCD-708C-4E30-B043-CCDCBF593E12");
  }
}
