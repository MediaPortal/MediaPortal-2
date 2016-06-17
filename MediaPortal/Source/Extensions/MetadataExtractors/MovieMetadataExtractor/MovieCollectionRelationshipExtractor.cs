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
using MediaPortal.Extensions.OnlineLibraries.Matchers;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieCollectionRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { MovieAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { MovieCollectionAspect.ASPECT_ID };

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

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      // Build the person MI

      MovieCollectionInfo collectionInfo = new MovieCollectionInfo();
      if (!collectionInfo.FromMetadata(aspects))
        return false;

      MovieTheMovieDbMatcher.Instance.UpdateCollection(collectionInfo, forceQuickMode);

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      IDictionary<Guid, IList<MediaItemAspect>> collectionAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      extractedLinkedAspects.Add(collectionAspects);

      return collectionInfo.SetMetadata(collectionAspects);
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(MovieCollectionAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      MovieCollectionInfo collectionInfo = new MovieCollectionInfo();
      if (!collectionInfo.FromMetadata(linkedAspects))
        return false;

      if (!MovieTheMovieDbMatcher.Instance.UpdateCollection(collectionInfo, true))
        return false;

      MovieInfo movieInfo = new MovieInfo();
      if (!movieInfo.FromMetadata(aspects))
        return false;

      foreach(MovieInfo movie in collectionInfo.Movies)
      {
        if (movie.MovieDbId == movieInfo.MovieDbId)
        {
          index = movie.Order;
          break;
        }
      }
      return index >= 0;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
