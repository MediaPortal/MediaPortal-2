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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieCollectionMovieRelationshipExtractor : IMovieRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { MovieCollectionAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { MovieAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      //We don't want to build collection -> movie relation because there already is a movie -> collection relation
      get { return false; }
    }

    public Guid Role
    {
      
      get { return MovieCollectionAspect.ROLE_MOVIE_COLLECTION; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return MovieAspect.ROLE_MOVIE; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return MovieInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetMovieSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out IDictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid> extractedLinkedAspects, bool importOnly)
    {
      extractedLinkedAspects = null;

      if (importOnly)
        return false;

      if (MovieMetadataExtractor.OnlyLocalMedia)
        return false;

      MovieCollectionInfo collectionInfo = new MovieCollectionInfo();
      if (!collectionInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(collectionInfo))
        return false;

      if (!MovieMetadataExtractor.SkipOnlineSearches && collectionInfo.HasExternalId)
        OnlineMatcherService.Instance.UpdateCollection(collectionInfo, true, importOnly);

      if (collectionInfo.Movies.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < collectionInfo.Movies.Count)
        collectionInfo.HasChanged = true; //Force save for new movies
      else
        return false;

      if (!collectionInfo.HasChanged)
        return false;

      AddToCheckCache(collectionInfo);

      extractedLinkedAspects = new Dictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid>();
      for (int i = 0; i < collectionInfo.Movies.Count; i++)
      {
        MovieInfo movieInfo = collectionInfo.Movies[i];
        movieInfo.CollectionNameId = collectionInfo.NameId;

        IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        movieInfo.SetMetadata(movieAspects);
        MediaItemAspect.SetAttribute(movieAspects, MediaAspect.ATTR_ISVIRTUAL, true);

        if (movieAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(movieAspects, Guid.Empty);
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(MovieAspect.ASPECT_ID))
        return false;

      MovieInfo linkedMovie = new MovieInfo();
      if (!linkedMovie.FromMetadata(extractedAspects))
        return false;

      MovieInfo existingMovie = new MovieInfo();
      if (!existingMovie.FromMetadata(existingAspects))
        return false;

      return linkedMovie.Equals(existingMovie);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(linkedAspects, MovieAspect.Metadata, out linkedAspect))
        return false;

      MovieInfo movieInfo = new MovieInfo();
      if (!movieInfo.FromMetadata(linkedAspects))
        return false;

      if(movieInfo.ReleaseDate.HasValue)
      {
        index = movieInfo.ReleaseDate.Value.Year;
      }
      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
