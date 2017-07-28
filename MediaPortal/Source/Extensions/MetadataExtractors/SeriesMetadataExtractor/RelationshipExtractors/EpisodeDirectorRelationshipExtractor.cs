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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities.Collections;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class EpisodeDirectorRelationshipExtractor : ISeriesRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID, VideoAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { PersonAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return EpisodeAspect.ROLE_EPISODE; }
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
      return GetPersonSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      if (!SeriesMetadataExtractor.IncludeDirectorDetails)
        return false;

      if (importOnly)
        return false;

      if (BaseInfo.IsVirtualResource(aspects))
        return false;

      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (!episodeInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(episodeInfo))
        return false;

      int count = 0;
      if (!SeriesMetadataExtractor.SkipOnlineSearches)
      {
        OnlineMatcherService.Instance.UpdateEpisodePersons(episodeInfo, PersonAspect.OCCUPATION_DIRECTOR, importOnly);
        count = episodeInfo.Directors.Where(p => p.HasExternalId).Count();
        if (!episodeInfo.IsRefreshed)
          episodeInfo.HasChanged = true; //Force save to update external Ids for metadata found by other MDEs
      }
      else
      {
        count = episodeInfo.Directors.Where(p => !string.IsNullOrEmpty(p.Name)).Count();
      }

      if (episodeInfo.Directors.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < count || (BaseInfo.CountRelationships(aspects, LinkedRole) == 0 && episodeInfo.Directors.Count > 0))
        episodeInfo.HasChanged = true; //Force save if no relationship exists

      if (!episodeInfo.HasChanged)
        return false;

      AddToCheckCache(episodeInfo);

      extractedLinkedAspects = new List<RelationshipItem>();
      foreach (PersonInfo person in episodeInfo.Directors)
      {
        person.AssignNameId();
        person.HasChanged = episodeInfo.HasChanged;
        IDictionary<Guid, IList<MediaItemAspect>> personAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        person.SetMetadata(personAspects);

        if (personAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          Guid existingId;
          if (TryGetIdFromCache(person, out existingId))
            extractedLinkedAspects.Add(new RelationshipItem(personAspects, existingId));
          else
            extractedLinkedAspects.Add(new RelationshipItem(personAspects, Guid.Empty));
        }
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

      IEnumerable<string> actors = aspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS);
      List<string> nameList = new SafeList<string>(actors);

      index = nameList.IndexOf(name);
      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      PersonInfo person = new PersonInfo();
      person.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, person);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
