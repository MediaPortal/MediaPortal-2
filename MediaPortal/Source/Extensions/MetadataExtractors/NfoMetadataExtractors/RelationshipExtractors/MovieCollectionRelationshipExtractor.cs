#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class MovieCollectionRelationshipExtractor : NfoMovieExtractorBase, IRelationshipRoleExtractor
  {
    #region Static fields

    private static readonly Guid[] ROLE_ASPECTS = { MovieAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { MovieCollectionAspect.ASPECT_ID };

    #endregion

    #region Protected methods

    /// <summary>
    /// Asynchronously tries to extract a movie collection for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspects">List of MediaItemAspect dictionaries to update with metadata</param>
    /// <param name="reimport">During reimport only allow if nfo is for same media as this</param>
    /// <returns><c>true</c> if metadata was found and stored into the <paramref name="extractedAspects"/>, else <c>false</c></returns>
    protected async Task<bool> TryExtractMovieCollectionMetadataAsync(IResourceAccessor mediaItemAccessor, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspects, MovieInfo reimport)
    {
      NfoMovieReader movieNfoReader = await TryGetNfoMovieReaderAsync(mediaItemAccessor).ConfigureAwait(false);
      if (movieNfoReader != null)
      {
        if (reimport != null && !VerifyMovieReimport(movieNfoReader, reimport))
          return false;

        return movieNfoReader.TryWriteCollectionMetadata(extractedAspects);
      }
      return false;
    }

    #endregion

    #region IRelationshipRoleExtractor implementation

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
      if (!extractedAspects.ContainsKey(MovieCollectionAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_COLLECTION);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(MovieCollectionAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_COLLECTION);
    }

    public async Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      MovieInfo reimport = null;
      if (aspects.ContainsKey(ReimportAspect.ASPECT_ID))
      {
        reimport = new MovieInfo();
        reimport.FromMetadata(aspects);
      }

      IList<IDictionary<Guid, IList<MediaItemAspect>>> nfoLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      if (!await TryExtractMovieCollectionMetadataAsync(mediaItemAccessor, nfoLinkedAspects, reimport).ConfigureAwait(false))
        return false;

      List<MovieCollectionInfo> collections;
      if (!RelationshipExtractorUtils.TryCreateInfoFromLinkedAspects(nfoLinkedAspects, out collections))
        return false;

      collections = collections.Where(c => c != null && !c.CollectionName.IsEmpty).ToList();

      bool movieVirtual;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out movieVirtual))
        movieVirtual = false;

      extractedLinkedAspects.Clear();
      foreach (MovieCollectionInfo collection in collections)
      {
        if (collection.SetLinkedMetadata() && collection.LinkedAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          MediaItemAspect.SetAttribute(collection.LinkedAspects, MediaAspect.ATTR_ISVIRTUAL, movieVirtual);
          extractedLinkedAspects.Add(collection.LinkedAspects);
        }
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

    #endregion
  }
}
