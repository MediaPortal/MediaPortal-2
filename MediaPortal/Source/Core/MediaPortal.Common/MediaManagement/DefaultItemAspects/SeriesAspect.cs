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
  /// Contains the metadata specification of the "Series" media item aspect which is assigned to series media items.
  /// </summary>
  public static class SeriesAspect
  {
    /// <summary>
    /// Media item aspect id of the series aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("14169484-C425-4294-9693-6902211039CF");

    /// <summary>
    /// Series name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SERIES_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SeriesName", 200, Cardinality.Inline, true);

    /// <summary>
    /// Contains the original name of the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ORIG_SERIES_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("OrigName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Album description
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DESCRIPTION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Description", 5000, Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of actor name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ACTORS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Actors", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Enumeration of fictional character name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CHARACTERS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Characters", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains list of awards.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AWARDS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Awards", 20, Cardinality.ManyToMany, true);

    /// <summary>
    /// List of TV networks involved in making the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NETWORKS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Networks", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// List of production company's involved in making the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPANIES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Companies", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Contains the certification.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CERTIFICATION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Certification", 20, Cardinality.Inline, true);

    /// <summary>
    /// If set to <c>true</c>, the series is cancelled/ended.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ENDED =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("IsEnded", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// Contains the number of the season for the next episode.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NEXT_SEASON =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NextSeason", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the number(s) for the next episode(s). The numbers start at 1.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NEXT_EPISODE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NextEpisode", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Name of the next episode. We only store the first episode name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NEXT_EPISODE_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("NextEpisodeName", 300, Cardinality.Inline, false);

    /// <summary>
    /// Contains the air date for the upcoming episode.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NEXT_AIR_DATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NextAirDate", typeof(DateTime), Cardinality.Inline, true);

    /// <summary>
    /// Contains a popularity of series, based on user votings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_POPULARITY =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Popularity", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Contains the score of the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SCORE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Score", typeof(float), Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the series. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TOTAL_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TotalRating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Contains the overall number ratings of the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING_COUNT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("RatingCount", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains the number of episodes available for watching.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AVAILABLE_EPISODES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AvailEpisodes", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the number of seasons available for watching.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_AVAILABLE_SEASONS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("AvailSeasons", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the total number of episodes currently available for the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUM_EPISODES =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumEpisodes", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the total number of seasons currently available for the series.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUM_SEASONS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumSeasons", typeof(int), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "SeriesItem", new[] {
            ATTR_SERIES_NAME,
            ATTR_ORIG_SERIES_NAME,
            ATTR_DESCRIPTION,
            ATTR_AWARDS,
            ATTR_ACTORS,
            ATTR_CHARACTERS,
            ATTR_NETWORKS,
            ATTR_COMPANIES,
            ATTR_CERTIFICATION,
            ATTR_ENDED,
            ATTR_NEXT_SEASON,
            ATTR_NEXT_EPISODE,
            ATTR_NEXT_EPISODE_NAME,
            ATTR_NEXT_AIR_DATE,
            ATTR_POPULARITY,
            ATTR_SCORE,
            ATTR_TOTAL_RATING,
            ATTR_RATING_COUNT,
            ATTR_AVAILABLE_EPISODES,
            ATTR_AVAILABLE_SEASONS,
            ATTR_NUM_EPISODES,
            ATTR_NUM_SEASONS
        });

    public static readonly Guid ROLE_SERIES = new Guid("13FDBDAF-F5D0-46C8-952F-F22647812C50");
  }
}
