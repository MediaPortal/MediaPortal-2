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
  /// Contains the metadata specification of the "Episode" media item aspect which is assigned to episode media items (i.e. videos, recordings).
  /// </summary>
  public static class EpisodeAspect
  {
    /// <summary>
    /// Media item aspect id of the episode aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("22E58B5D-77A3-4F74-95DB-4B9DD3A289F6");

    /// <summary>
    /// Series name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SERIES_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SeriesName", 200, Cardinality.Inline, true);

    /// <summary>
    /// Contains the number of the season, usually starting at 1. A value of 0 is also valid for specials.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SEASON =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Season", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains a combination of <see cref="ATTR_SERIESNAME"/> and the <see cref="ATTR_SEASON"/> to allow filtering and retrieval of season banners.
    /// This name must be built in form "{0} S{1}", using SeriesName and Season.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SERIES_SEASON =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SeasonName", 200, Cardinality.Inline, false);

    /// <summary>
    /// Contains the number(s) of the episode(s). If a file contains multiple episodes, all episode numbers are added separately.
    /// The numbers start at 1.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_EPISODE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Episode", typeof(int), Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains the number(s) of the episode(s) as they are published on DVD. The number can be different to <see cref="ATTR_EPISODE"/>.
    /// If a file contains multiple episodes, all episode numbers are added separately. The numbers start at 1.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DVDEPISODE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("DvdEpisode", typeof(double), Cardinality.ManyToMany, true);

    /// <summary>
    /// Name of the episode. We only store the first episode name (or combined name) if the file contains multiple episodes.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_EPISODE_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("EpisodeName", 300, Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the episode. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TOTAL_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TotalRating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Contains the overall number ratings of the episode.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING_COUNT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("RatingCount", typeof(int), Cardinality.Inline, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "EpisodeItem", new[] {
            ATTR_SERIES_NAME,
            ATTR_SEASON,
            ATTR_SERIES_SEASON,
            ATTR_EPISODE,
            ATTR_DVDEPISODE,
            ATTR_EPISODE_NAME,
            ATTR_TOTAL_RATING,
            ATTR_RATING_COUNT,
        });

      public static readonly Guid ROLE_EPISODE = new Guid("83C2CDF9-717E-4881-8411-3A31C2027B77");
  }
}
