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
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the series relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "FE565C89-AFC6-4036-977D-255A630BD868";

    /// <summary>
    /// Series relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;
    private IList<RelationshipHierarchy> _hierarchies;
    private volatile bool includeFullSeriesFilter = true;

    public SeriesRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Series relationship extractor");

      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new EpisodeSeriesRelationshipExtractor());
      _extractors.Add(new EpisodeSeasonRelationshipExtractor());
      _extractors.Add(new SeasonSeriesRelationshipExtractor());

      _extractors.Add(new EpisodeActorRelationshipExtractor());
      _extractors.Add(new EpisodeDirectorRelationshipExtractor());
      _extractors.Add(new EpisodeWriterRelationshipExtractor());
      _extractors.Add(new EpisodeCharacterRelationshipExtractor());

      _extractors.Add(new SeriesActorRelationshipExtractor());
      _extractors.Add(new SeriesCharacterRelationshipExtractor());
      _extractors.Add(new SeriesNetworkRelationshipExtractor());
      _extractors.Add(new SeriesProductionRelationshipExtractor());

      _extractors.Add(new SeriesEpisodeRelationshipExtractor());

      _hierarchies = new List<RelationshipHierarchy>();
      _hierarchies.Add(new RelationshipHierarchy(EpisodeAspect.ROLE_EPISODE, EpisodeAspect.ATTR_EPISODE, SeriesAspect.ROLE_SERIES, SeriesAspect.ATTR_AVAILABLE_EPISODES, true));
      _hierarchies.Add(new RelationshipHierarchy(EpisodeAspect.ROLE_EPISODE, EpisodeAspect.ATTR_EPISODE, SeasonAspect.ROLE_SEASON, SeasonAspect.ATTR_AVAILABLE_EPISODES, true));
      _hierarchies.Add(new RelationshipHierarchy(SeasonAspect.ROLE_SEASON, SeasonAspect.ATTR_SEASON, SeriesAspect.ROLE_SERIES, SeriesAspect.ATTR_AVAILABLE_SEASONS, false));
    }

    public IDictionary<IFilter, uint> GetLastChangedItemsFilters()
    {
      Dictionary<IFilter, uint> filters = new Dictionary<IFilter, uint>();

      //Add filters for changed series
      //We need to find episodes because importer only works with files
      //The relationship extractor for series should then do the update
      List<SeriesInfo> changedSeries = OnlineMatcherService.Instance.GetLastChangedSeries();
      foreach (SeriesInfo series in changedSeries)
      {
        Dictionary<string, string> ids = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(series.ImdbId))
          ids.Add(ExternalIdentifierAspect.SOURCE_IMDB, series.ImdbId);
        if (series.MovieDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TMDB, series.MovieDbId.ToString());
        if (series.TvdbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TVDB, series.TvdbId.ToString());
        if (series.TvMazeId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TVMAZE, series.TvMazeId.ToString());
        if (series.TvRageId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TVRAGE, series.TvRageId.ToString());

        IFilter seriesChangedFilter = null;
        foreach (var id in ids)
        {
          if (seriesChangedFilter == null)
          {
            seriesChangedFilter = new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_SERIES),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
              });
          }
          else
          {
            seriesChangedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seriesChangedFilter,
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_SERIES),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
            }));
          }
        }

        if (seriesChangedFilter != null)
          filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, seriesChangedFilter), 1);
      }

      if (includeFullSeriesFilter)
      {
        includeFullSeriesFilter = false;

        //Add filter for outdated next episode
        filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE,
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(SeriesAspect.ATTR_ENDED, RelationalOperator.EQ, false),
          new RelationalFilter(SeriesAspect.ATTR_NEXT_AIR_DATE, RelationalOperator.LT, DateTime.Now),
          new NotFilter(new EmptyFilter(SeriesAspect.ATTR_NEXT_AIR_DATE)))), 0);
      }

      //Add filters for changed episodes
      List<EpisodeInfo> changedEpisodes = OnlineMatcherService.Instance.GetLastChangedEpisodes();
      foreach (EpisodeInfo episode in changedEpisodes)
      {
        Dictionary<string, string> ids = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(episode.ImdbId))
          ids.Add(ExternalIdentifierAspect.SOURCE_IMDB, episode.ImdbId);
        if (episode.MovieDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TMDB, episode.MovieDbId.ToString());
        if (episode.TvdbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TVDB, episode.TvdbId.ToString());
        if (episode.TvMazeId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TVMAZE, episode.TvMazeId.ToString());
        if (episode.TvRageId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TVRAGE, episode.TvRageId.ToString());

        IFilter episodesChangedFilter = null;
        foreach (var id in ids)
        {
          if (episodesChangedFilter == null)
          {
            episodesChangedFilter = new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_EPISODE),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
              });
          }
          else
          {
            episodesChangedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, episodesChangedFilter,
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_EPISODE),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
            }));
          }
        }

        if (episodesChangedFilter != null)
          filters.Add(episodesChangedFilter, 1);
      }

      return filters;
    }

    public void ResetLastChangedItems()
    {
      OnlineMatcherService.Instance.ResetLastChangedSeries();
      OnlineMatcherService.Instance.ResetLastChangedEpisodes();
    }

    public RelationshipExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _extractors; }
    }

    public IList<RelationshipHierarchy> Hierarchies
    {
      get { return _hierarchies; }
    }
  }
}
