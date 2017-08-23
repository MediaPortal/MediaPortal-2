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
  class MovieCollectionRelationshipExtractor : IMovieRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { MovieAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { MovieCollectionAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return MovieAspect.ROLE_MOVIE; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return MovieCollectionAspect.ROLE_MOVIE_COLLECTION; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return MovieCollectionInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetMovieCollectionSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      MovieInfo movieInfo = new MovieInfo();
      if (!movieInfo.FromMetadata(aspects))
        return false;

      MovieCollectionInfo cachedCollection;
      Guid collectionId;
      MovieCollectionInfo collectionInfo = movieInfo.CloneBasicInstance<MovieCollectionInfo>();
      if (TryGetInfoFromCache(collectionInfo, out cachedCollection, out collectionId))
        collectionInfo = cachedCollection;
      else if (!MovieMetadataExtractor.SkipOnlineSearches && collectionInfo.HasExternalId)
        OnlineMatcherService.Instance.UpdateCollection(collectionInfo, false, false);

      bool hasId = false;
      if (!MovieMetadataExtractor.SkipOnlineSearches)
        hasId = collectionInfo.HasExternalId || !string.IsNullOrEmpty(movieInfo.CollectionNameId);
      else
        hasId = !string.IsNullOrEmpty(movieInfo.CollectionNameId);

      if (!BaseInfo.HasRelationship(aspects, LinkedRole) && hasId)
        collectionInfo.HasChanged = true; //Force save if no relationship exists

      if (!collectionInfo.HasChanged && !importOnly)
        return false;

      extractedLinkedAspects = new List<RelationshipItem>();

      IDictionary<Guid, IList<MediaItemAspect>> collectionAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      
      if (collectionId != Guid.Empty)
      {
        collectionInfo.SetMetadata(collectionAspects);

        bool movieVirtual = true;
        if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out movieVirtual))
        {
          MediaItemAspect.SetAttribute(collectionAspects, MediaAspect.ATTR_ISVIRTUAL, movieVirtual);
        }

        if (collectionAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(new RelationshipItem(collectionAspects, collectionId));
      }
      else
      {
        //Create custom collection
        if (!string.IsNullOrEmpty(movieInfo.CollectionNameId) && !collectionInfo.HasExternalId)
        {
          collectionInfo = movieInfo.CloneBasicInstance<MovieCollectionInfo>();
          collectionInfo.HasChanged = true;
        }
        collectionInfo.SetMetadata(collectionAspects);

        bool movieVirtual = true;
        if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out movieVirtual))
        {
          MediaItemAspect.SetAttribute(collectionAspects, MediaAspect.ATTR_ISVIRTUAL, movieVirtual);
        }

        if (collectionAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(new RelationshipItem(collectionAspects, Guid.Empty));
      }

      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(MovieCollectionAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      MovieInfo movieInfo = new MovieInfo();
      if (!movieInfo.FromMetadata(aspects))
        return false;

      if (movieInfo.ReleaseDate.HasValue)
      {
        index = movieInfo.ReleaseDate.Value.Year;
      }

      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      MovieCollectionInfo collection = new MovieCollectionInfo();
      collection.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, collection, false);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
