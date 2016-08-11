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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.General;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieCollectionMovieRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { MovieCollectionAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { MovieAspect.ASPECT_ID };
    private CheckedItemCache<MovieCollectionInfo> _checkCache = new CheckedItemCache<MovieCollectionInfo>(MovieMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE);

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

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      if (forceQuickMode)
        return false;

      MovieCollectionInfo collectionInfo = new MovieCollectionInfo();
      if (!collectionInfo.FromMetadata(aspects))
        return false;

      if (_checkCache.IsItemChecked(collectionInfo))
        return false;

      OnlineMatcherService.UpdateCollection(collectionInfo, true, forceQuickMode);

      if (collectionInfo.Movies.Count == 0)
        return false;

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();

      for (int i = 0; i < collectionInfo.Movies.Count; i++)
      {
        MovieInfo movieInfo = collectionInfo.Movies[i];

        IDictionary<Guid, IList<MediaItemAspect>> movieAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        movieInfo.SetMetadata(movieAspects);

        if (movieAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(movieAspects);
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

      MovieCollectionInfo collectionInfo = new MovieCollectionInfo();
      if (!collectionInfo.FromMetadata(linkedAspects))
        return false;

      if (!OnlineMatcherService.UpdateCollection(collectionInfo, true, true))
        return false;

      index = collectionInfo.Movies.IndexOf(movieInfo);
      return index >= 0;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
