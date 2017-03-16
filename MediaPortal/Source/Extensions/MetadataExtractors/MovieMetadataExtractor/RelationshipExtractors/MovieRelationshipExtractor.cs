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

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the movie relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "BA7708DA-010E-45E1-A2B3-24B7D43443AC";

    /// <summary>
    /// Series relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;
    private IList<RelationshipHierarchy> _hierarchies;

    public MovieRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Movie relationship extractor");

      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new MovieCollectionRelationshipExtractor());
      _extractors.Add(new MovieActorRelationshipExtractor());
      _extractors.Add(new MovieDirectorRelationshipExtractor());
      _extractors.Add(new MovieWriterRelationshipExtractor());
      _extractors.Add(new MovieCharacterRelationshipExtractor());
      _extractors.Add(new MovieProductionRelationshipExtractor());
      _extractors.Add(new MovieCollectionMovieRelationshipExtractor());

      _hierarchies = new List<RelationshipHierarchy>();
      _hierarchies.Add(new RelationshipHierarchy(MovieAspect.ROLE_MOVIE, MovieAspect.ATTR_MOVIE_NAME, MovieCollectionAspect.ROLE_MOVIE_COLLECTION, MovieCollectionAspect.ATTR_AVAILABLE_MOVIES, true));
    }

    public IDictionary<IFilter, uint> GetLastChangedItemsFilters()
    {
      Dictionary<IFilter, uint> filters = new Dictionary<IFilter, uint>();

      //Add filters for movie collections
      //We need to find movies because importer only works with files
      //The relationship extractor for movie collection should then do the update
      List<MovieCollectionInfo> changedCollections = OnlineMatcherService.Instance.GetLastChangedMovieCollections();
      foreach (MovieCollectionInfo series in changedCollections)
      {
        Dictionary<string, string> ids = new Dictionary<string, string>();
        if (series.MovieDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TMDB, series.MovieDbId.ToString());

        IFilter collectionChangedFilter = null;
        foreach (var id in ids)
        {
          if (collectionChangedFilter == null)
          {
            collectionChangedFilter = new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_COLLECTION),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
              });
          }
          else
          {
            collectionChangedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, collectionChangedFilter,
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_COLLECTION),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
            }));
          }
        }

        if (collectionChangedFilter != null)
          filters.Add(new FilteredRelationshipFilter(MovieAspect.ROLE_MOVIE, collectionChangedFilter), 1);
      }

      //Add filters for changed movies
      List<MovieInfo> changedMovies = OnlineMatcherService.Instance.GetLastChangedMovies();
      foreach (MovieInfo movie in changedMovies)
      {
        Dictionary<string, string> ids = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(movie.ImdbId))
          ids.Add(ExternalIdentifierAspect.SOURCE_IMDB, movie.ImdbId);
        if (movie.MovieDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_TMDB, movie.MovieDbId.ToString());
        if (movie.CinePassionId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_CINEPASSION, movie.CinePassionId.ToString());
        if (movie.CinePassionId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_ALLOCINE, movie.AllocinebId.ToString());

        IFilter moviesChangedFilter = null;
        foreach (var id in ids)
        {
          if (moviesChangedFilter == null)
          {
            moviesChangedFilter = new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_MOVIE),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
              });
          }
          else
          {
            moviesChangedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, moviesChangedFilter,
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_MOVIE),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
            }));
          }
        }

        if (moviesChangedFilter != null)
          filters.Add(moviesChangedFilter, 1);
      }

      return filters;
    }

    public void ResetLastChangedItems()
    {
      OnlineMatcherService.Instance.ResetLastChangedMovieCollections();
      OnlineMatcherService.Instance.ResetLastChangedMovies();
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
