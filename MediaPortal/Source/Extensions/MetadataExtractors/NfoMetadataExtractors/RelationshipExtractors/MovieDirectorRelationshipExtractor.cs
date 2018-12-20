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
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class MovieDirectorRelationshipExtractor : NfoMovieExtractorBase, IRelationshipRoleExtractor
  {
    #region Static fields

    private static readonly Guid[] ROLE_ASPECTS = { MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { PersonAspect.ASPECT_ID };

    #endregion

    #region Protected methods

    /// <summary>
    /// Asynchronously tries to extract a movie director for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspects">List of MediaItemAspect dictionaries to update with metadata</param>
    /// <param name="reimport">During reimport only allow if nfo is for same media as this</param>
    /// <returns><c>true</c> if metadata was found and stored into the <paramref name="extractedAspects"/>, else <c>false</c></returns>
    protected async Task<bool> TryExtractMovieDirectorMetadataAsync(IResourceAccessor mediaItemAccessor, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspects, MovieInfo reimport)
    {
      NfoMovieReader movieNfoReader = await TryGetNfoMovieReaderAsync(mediaItemAccessor).ConfigureAwait(false);
      if (movieNfoReader != null)
      {
        if (reimport != null && !VerifyMovieReimport(movieNfoReader, reimport))
          return false;

        return movieNfoReader.TryWriteDirectorMetadata(extractedAspects);
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
      get { return PersonAspect.ROLE_DIRECTOR; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return PersonInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_PERSON);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_PERSON);
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
      if (!await TryExtractMovieDirectorMetadataAsync(mediaItemAccessor, nfoLinkedAspects, reimport).ConfigureAwait(false))
        return false;

      List<PersonInfo> directors;
      if (!RelationshipExtractorUtils.TryCreateInfoFromLinkedAspects(nfoLinkedAspects, out directors))
        return false;

      directors = directors.Where(p => p != null && !string.IsNullOrEmpty(p.Name)).ToList();
      if (directors.Count == 0)
        return false;

      extractedLinkedAspects.Clear();
      foreach (PersonInfo person in directors)
      {
        if (person.SetLinkedMetadata() && person.LinkedAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(person.LinkedAspects);
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return false;

      PersonInfo linkedPerson = new PersonInfo();
      if (!linkedPerson.FromMetadata(extractedAspects))
        return false;

      PersonInfo existingPerson = new PersonInfo();
      if (!existingPerson.FromMetadata(existingAspects))
        return false;

      return linkedPerson.Equals(existingPerson);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(linkedAspects, PersonAspect.Metadata, out linkedAspect))
        return false;

      string name = linkedAspect.GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME);

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out aspect))
        return false;

      IEnumerable<string> persons = aspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS);
      List<string> nameList = new SafeList<string>(persons);

      index = nameList.IndexOf(name);
      return index >= 0;
    }

    #endregion
  }
}
