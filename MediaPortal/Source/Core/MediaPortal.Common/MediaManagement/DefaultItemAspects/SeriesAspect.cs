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
  /// Contains the metadata specification of the "Series" media item aspect which is assigned to series media items (i.e. videos, recordings).
  /// </summary>
  public static class SeriesAspect
  {
    /// <summary>
    /// Media item aspect id of the series aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("287A2809-D38D-4F98-B613-E9C09904392D");

    /// <summary>
    /// Contains the TMDB ID of the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_TVDB_ID =
        MediaItemAspectMetadata.CreateAttributeSpecification("TVDBID", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the IMDB ID of the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_IMDB_ID =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("IMDBID", 20, Cardinality.Inline, false);

    /// <summary>
    /// Series name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_SERIESNAME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("SeriesName", 200, Cardinality.Inline, false);

    /// <summary>
    /// Contains the number of the season, usually starting at 1. A value of 0 is also valid for specials.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_SEASON =
        MediaItemAspectMetadata.CreateAttributeSpecification("Season", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains a combination of <see cref="ATTR_SERIESNAME"/> and the <see cref="ATTR_SEASON"/> to allow filtering and retrieval of season banners.
    /// This name must be built in form "{0} S{1}", using SeriesName and Season.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_SERIES_SEASON =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("SeriesSeasonName", 200, Cardinality.Inline, false);

    /// <summary>
    /// Contains the number(s) of the episode(s). If a file contains multiple episodes, all episode numbers are added separately.
    /// The numbers start at 1.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EPISODE =
        MediaItemAspectMetadata.CreateAttributeSpecification("Episode", typeof(int), Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains the number(s) of the episode(s) as they are published on DVD. The number can be different to <see cref="ATTR_EPISODE"/>.
    /// If a file contains multiple episodes, all episode numbers are added separately. The numbers start at 1.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_DVDEPISODE =
        MediaItemAspectMetadata.CreateAttributeSpecification("DvdEpisode", typeof(double), Cardinality.ManyToMany, true);

    /// <summary>
    /// Name of the episode. We only store the first episode name (or combined name) if the file contains multiple episodes.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EPISODENAME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EpisodeName", 300, Cardinality.Inline, false);

    /// <summary>
    /// First aired date of episode.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_FIRSTAIRED =
        MediaItemAspectMetadata.CreateAttributeSpecification("FirstAired", typeof (DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the episode. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_TOTAL_RATING =
        MediaItemAspectMetadata.CreateAttributeSpecification("TotalRating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Contains the overall number ratings of the episode.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_RATING_COUNT =
        MediaItemAspectMetadata.CreateAttributeSpecification("RatingCount", typeof(int), Cardinality.Inline, true);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        ASPECT_ID, "SeriesItem", new[] {
            ATTR_IMDB_ID,
            ATTR_TVDB_ID,
            ATTR_SERIESNAME,
            ATTR_SEASON,
            ATTR_SERIES_SEASON,
            ATTR_EPISODE,
            ATTR_DVDEPISODE,
            ATTR_EPISODENAME,
            ATTR_FIRSTAIRED,
            ATTR_TOTAL_RATING,
            ATTR_RATING_COUNT
        });
  }
}
